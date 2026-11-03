using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CameraSettings settings;

    private Transform cameraTransform;
    private Vector3 targetPosition;
    private float currentYaw;
    private float currentPitch = 45f;
    private float currentZoomDistance;

    private void Start()
    {
        if (settings == null)
        {
            enabled = false;
            return;
        }

        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            cameraTransform = GetComponentInChildren<Camera>().transform;
        }

        targetPosition = transform.position;
        currentYaw = transform.eulerAngles.y;
        currentZoomDistance = Mathf.Clamp(cameraTransform.localPosition.magnitude, settings.minZoomDistance, settings.maxZoomDistance);

        UpdateCameraOffset();

        ApplyCursorLock();
    }

    private void Update()
    {
        if (pause.isPaused)
            return;

        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    private void ApplyCursorLock()
    {
        if (settings.lockCursorInWindow)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void LateUpdate()
    {
        if (pause.isPaused)
            return;

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * settings.movementSmoothing);
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);

        UpdateCameraOffset();
    }

    private void HandleMovement() // Перемещаяемся на WASD, и мышкой по краям экрына
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (settings.enableEdgeScrolling)
        {
            Vector3 mousePos = Input.mousePosition;

            bool isWindow = mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height;
            
            if (isWindow)
            {
                if (mousePos.x <= settings.edgeBoundaryThickness)
                    h = -1f;
                else if (mousePos.x >= Screen.width - settings.edgeBoundaryThickness)
                    h = 1f;

                if (mousePos.y <= settings.edgeBoundaryThickness)
                    v = -1f;
                else if (mousePos.y >= Screen.height - settings.edgeBoundaryThickness)
                    v = 1f;
            }
        }

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            Vector3 moveDirection = (forward * inputDir.z + right * inputDir.x).normalized;
            targetPosition += moveDirection * (settings.moveSpeed * Time.deltaTime);
        }
    }

    private void HandleRotation() // При зажатом Mouse3 крутим камеру по оси
    {
        if (Input.GetMouseButton(2) || Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw += mouseX * settings.rotationSpeed * Time.deltaTime;
            currentPitch -= mouseY * settings.rotationSpeed * Time.deltaTime;
            currentPitch = Mathf.Clamp(currentPitch, settings.minPitch, settings.maxPitch);
        }
    }

    private void HandleZoom() // Крутим колесо что позволяет приблежаться и отдаляться
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoomDistance -= scroll * settings.zoomSpeed * 100f * Time.deltaTime;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, settings.minZoomDistance, settings.maxZoomDistance);
        }
    }

    private void UpdateCameraOffset() // Расчет позиции камеры, а также заставляем её вращяться вокруг локальной оси
    {
        Quaternion pitchRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        Vector3 offset = pitchRotation * new Vector3(0f, 0f, -currentZoomDistance);

        cameraTransform.localPosition = offset;
        cameraTransform.LookAt(transform.position);
    }
}
