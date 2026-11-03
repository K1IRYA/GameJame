using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraStats", menuName = "RTS/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Movement")]
    [Tooltip("Базовая скорость перемещения камеры")]
    public float moveSpeed = 30f;
    [Tooltip("Коэффициент сглаживания движения (Lerp)")]
    public float movementSmoothing = 10f;

    [Header("Edge Scrolling")]
    [Tooltip("Включить перемещение курсором у края экрана")]
    public bool enableEdgeScrolling = true;
    [Tooltip("Ширина зоны у края экрана в пикселях")]
    public float edgeBoundaryThickness = 20f;
    [Tooltip("Запереть курсор внутри окна игры (для удобного тестирования Edge Scrolling)")]
    public bool lockCursorInWindow = true;

    [Header("Rotation")]
    [Tooltip("Скорость вращения по горизонтали и вертикали")]
    public float rotationSpeed = 100f;
    [Tooltip("Минимальный угол наклона камеры (в градусах)")]
    public float minPitch = 15f;
    [Tooltip("Максимальный угол наклона камеры (в градусах)")]
    public float maxPitch = 80f;

    [Header("Zoom")]
    [Tooltip("Скорость приближения / отдаления")]
    public float zoomSpeed = 20f;
    [Tooltip("Минимальная дистанция зума")]
    public float minZoomDistance = 5f;
    [Tooltip("Максимальная дистанция зума")]
    public float maxZoomDistance = 60f;
}
