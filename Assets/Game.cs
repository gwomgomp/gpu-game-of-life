using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class Game : MonoBehaviour
{
    [SerializeField]
	ComputeShader computeShader;

    RenderTexture texture;
    
    [SerializeField]
    MeshRenderer meshRenderer;
    
    [SerializeField]
    Camera mainCamera;

    static readonly int sourceFieldsId = Shader.PropertyToID("sourceFields");
    static readonly int targetFieldsId = Shader.PropertyToID("targetFields");
    static readonly int resolutionId = Shader.PropertyToID("resolution");
    static readonly int textureId = Shader.PropertyToID("targetTexture");
    static readonly int runningId = Shader.PropertyToID("running");

    ComputeBuffer sourceFieldsBuffer;
    ComputeBuffer targetFieldsBuffer;

    [SerializeField]
    int resolution = 64;
    [SerializeField]
    int colorCubeCount = 1;

    [SerializeField]
    Vector2 simulationSpeedLimits;
    
    Field[] fields;

    float timeSinceUpdate = 0f;
    float timePerUpdate;
    bool doSwitch = false;

    bool running = false;
    bool stopOnNextStep = false;

	void Start()
	{
        timePerUpdate = (1 / simulationSpeedLimits.x + 1 / simulationSpeedLimits.y) / 2;

        texture = new RenderTexture(resolution, resolution, 1);
        texture.enableRandomWrite = true;
        texture.filterMode = FilterMode.Point;
        texture.Create();

        meshRenderer.material.mainTexture = texture;

		fields = new Field[resolution * resolution];
        float colorCubeSize = resolution / colorCubeCount;
		for (float x = 0f; x < resolution; x++)
		{
            for (float y = 0f; y < resolution; y++) {
                float r = Mathf.Abs(Mathf.Lerp(-1f, 1f, (x % colorCubeSize) / colorCubeSize));
                float g = Mathf.Abs(Mathf.Lerp(-1f, 1f, (y % colorCubeSize) / colorCubeSize));
                float b = 1 - (r + g) / 2;
                Vector4 state = new Vector4(
                    r,
                    g,
                    b,
                    0.0f
                );
                fields[(int) (x * resolution + y)] = new Field() {position = new Vector2(x, y), state = state};
            }
		}

		sourceFieldsBuffer = new ComputeBuffer(resolution * resolution, 6 * 4);
        sourceFieldsBuffer.SetData(fields);
		targetFieldsBuffer = new ComputeBuffer(resolution * resolution, 6 * 4);
        targetFieldsBuffer.SetData(fields);

        timeSinceUpdate = timePerUpdate;
	}

    void OnDestroy() {
        sourceFieldsBuffer.Release();
		sourceFieldsBuffer = null;
        targetFieldsBuffer.Release();
		targetFieldsBuffer = null;
    }

    void Update () {
        timeSinceUpdate += Time.deltaTime;
        if (!running || timeSinceUpdate >= timePerUpdate) {
			ExecuteTurnOnGPU();
            timeSinceUpdate = 0f;
            if (running) {
                doSwitch = !doSwitch;
            }
            if (stopOnNextStep) {
                running = false;
                stopOnNextStep = false;
            }
        }

        HandleMouseInput();
	}

    void ExecuteTurnOnGPU () {
		computeShader.SetInt(resolutionId, resolution);
		computeShader.SetBool(runningId, running);
        if (doSwitch) {
            computeShader.SetBuffer(0, targetFieldsId, sourceFieldsBuffer);
            computeShader.SetBuffer(0, sourceFieldsId, targetFieldsBuffer);
        } else {
            computeShader.SetBuffer(0, sourceFieldsId, sourceFieldsBuffer);
            computeShader.SetBuffer(0, targetFieldsId, targetFieldsBuffer);
        }
        computeShader.SetTexture(0, textureId, texture);
        uint threadGroupX, threadGroupY, threadGroupZ;
        computeShader.GetKernelThreadGroupSizes(0, out threadGroupX, out threadGroupY, out threadGroupZ);
        int numGroupsX = Mathf.CeilToInt(resolution * resolution / (float) threadGroupX);
		computeShader.Dispatch(0, numGroupsX, 1, 1);
    }

    void HandleMouseInput() {
        if (!running && Input.GetMouseButtonDown(1)) {
            RaycastHit hit;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit)) {
                Vector2 pixelUV = hit.textureCoord;
                ComputeBuffer buffer;
                if (doSwitch) {
                    buffer = targetFieldsBuffer;
                } else {
                    buffer = sourceFieldsBuffer;
                }
                int xPosition = (int) (pixelUV.x * (float) resolution);
                int yPosition = (int) (pixelUV.y * (float) resolution);
                buffer.GetData(fields);
                Field selectedField = fields[xPosition * resolution + yPosition];
                if (selectedField.state.w == 0) {
                    selectedField.state.w = 1;
                } else {
                    selectedField.state.w = 0;
                }
                fields[xPosition * resolution + yPosition] = selectedField;
                buffer.SetData(fields);
            }
        }
    }

    public void runStop() {
        running = !running;
    }

    public void singleStep() {
        running = true;
        stopOnNextStep = true;
    }

    public void setSpeed(float speed) {
        timePerUpdate = Mathf.Lerp(1 / simulationSpeedLimits.x, 1 / simulationSpeedLimits.y, speed);
    }

	public struct Field
	{
		public Vector2 position;
		public Vector4 state;
	}
}
