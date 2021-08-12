using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    float moveSpeed;
    [SerializeField]
    Vector2 zoomLimits;
    [SerializeField]
    float zoomSpeed;

    Camera mainCamera;

    bool dragging = false;
    Vector3 lastMousePosition;
    
    void Start()
    {
        mainCamera = transform.GetComponentInChildren<Camera>();
        mainCamera.orthographicSize = zoomLimits.x;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            Application.Quit();
        }

        float newCameraSize = mainCamera.orthographicSize + Input.mouseScrollDelta.y * zoomSpeed * -1;
        if (newCameraSize >= zoomLimits.x && newCameraSize <= zoomLimits.y) {
            mainCamera.orthographicSize = newCameraSize;
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) {
            dragging = true;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(0)) {
            dragging = false;
            Cursor.visible = true;
        }
        if (dragging) {
            if (lastMousePosition != null) {
                float zoomLevel = (mainCamera.orthographicSize - zoomLimits.x) / (zoomLimits.y - zoomLimits.x);
                Vector3 mouseDiff = (Input.mousePosition - lastMousePosition) * -1;
                mouseDiff.z = mouseDiff.y;
                mouseDiff.y = 0;
                Vector3 newPosition = transform.position + mouseDiff * moveSpeed * (zoomLevel + 0.1f);
                transform.position = newPosition;
            } 
        }
        lastMousePosition = Input.mousePosition;
    }
}
