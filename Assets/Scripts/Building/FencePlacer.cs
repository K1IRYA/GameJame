using System.Collections.Generic;
using UnityEngine;

public class FencePlacer : MonoBehaviour
{
    public static FencePlacer Instance { get; private set; }

    [Header("Префабы вариаций стройплощадок забора")]
    [Tooltip("Список префабов (zabor1, zabor2, zabor3) со скриптом ConstructionSite")]
    [SerializeField] private List<GameObject> fenceVariants = new List<GameObject>();

    [Header("Префаб призрака для предпросмотра (Preview)")]
    [SerializeField] private GameObject previewPrefab;

    [Header("Параметры сегмента")]
    [Tooltip("Длина одной секции забора (расстояние между столбами)")]
    [SerializeField] private float segmentLength = 2.5f;

    [Tooltip("Дистанция примагничивания к соседним заборам")]
    [SerializeField] private float snapDistance = 1.8f;

    [Header("Слои")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask fenceSnapLayer;

    private bool isPlacing = false;
    private bool isDragging = false;
    private float currentRotationY = 0f;
    private Vector3 dragStartPos;

    private readonly List<GameObject> activePreviews = new List<GameObject>();
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCam = Camera.main;
    }

    private void Update()
    {
        if (!isPlacing) return;

        // Поворот на 90 градусов по нажатию R
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentRotationY = (currentRotationY + 90f) % 360f;
        }

        // Отмена по правой кнопке мыши или Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        Vector3 groundPos = GetMouseGroundPosition();

        // 1. Начало протяжки линии заборов (нажатие ЛКМ)
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = GetSnappedPosition(groundPos);
        }

        // 2. Протягивание (зажата ЛКМ)
        if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDragLinePreviews(dragStartPos, groundPos);
        }
        // 3. Просто перемещение курсора без зажатия (одиночный призрак)
        else if (!isDragging)
        {
            UpdateSinglePreview(groundPos);
        }

        // 4. Завершение строительства (отпускание ЛКМ)
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            BuildFenceChain(dragStartPos, groundPos);
            isDragging = false;
            ClearPreviews();
        }
    }

    // Вызывается из UI-кнопки строительства забора
    public void StartPlacingFence()
    {
        if (fenceVariants.Count == 0)
        {
            Debug.LogError("[FencePlacer] Список fenceVariants пуст!");
            return;
        }

        isPlacing = true;
        isDragging = false;
        currentRotationY = 0f;
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        isDragging = false;
        ClearPreviews();
    }

    private Vector3 GetMouseGroundPosition()
    {
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundLayer))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    // Ищет ближайший край уже построенного забора для примагничивания
    private Vector3 GetSnappedPosition(Vector3 originalPos)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(originalPos, snapDistance, fenceSnapLayer);
        float closestDist = float.MaxValue;
        Vector3 bestSnapPos = originalPos;

        foreach (var col in nearbyColliders)
        {
            // Перебираем все дочерние точки забора (SnapPoint_L, SnapPoint_R)
            Transform[] allChildren = col.GetComponentsInChildren<Transform>();
            foreach (var child in allChildren)
            {
                if (child.name.StartsWith("SnapPoint"))
                {
                    float dist = Vector3.Distance(originalPos, child.position);
                    if (dist < snapDistance && dist < closestDist)
                    {
                        closestDist = dist;
                        bestSnapPos = child.position;
                    }
                }
            }
        }

        return bestSnapPos;
    }

    private void UpdateSinglePreview(Vector3 currentMousePos)
    {
        EnsurePreviewCount(1);
        Vector3 snappedPos = GetSnappedPosition(currentMousePos);

        activePreviews[0].transform.position = snappedPos;
        activePreviews[0].transform.rotation = Quaternion.Euler(0f, currentRotationY, 0f);
        activePreviews[0].SetActive(true);
    }

    private void UpdateDragLinePreviews(Vector3 start, Vector3 current)
    {
        Vector3 direction = current - start;
        direction.y = 0f;
        float totalDist = direction.magnitude;

        if (totalDist < 0.1f)
        {
            UpdateSinglePreview(start);
            return;
        }

        // Вычисляем количество секций в цепи
        int count = Mathf.Max(1, Mathf.RoundToInt(totalDist / segmentLength));
        EnsurePreviewCount(count);

        Vector3 stepVector = direction.normalized * segmentLength;
        Quaternion rot = Quaternion.LookRotation(direction.normalized, Vector3.up);

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = start + stepVector * i;
            activePreviews[i].transform.position = pos;
            activePreviews[i].transform.rotation = rot;
            activePreviews[i].SetActive(true);
        }
    }

    private void BuildFenceChain(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        direction.y = 0f;
        float totalDist = direction.magnitude;

        int count = totalDist < segmentLength ? 1 : Mathf.Max(1, Mathf.RoundToInt(totalDist / segmentLength));
        Vector3 stepVector = totalDist < 0.1f ? Vector3.zero : direction.normalized * segmentLength;
        Quaternion rot = totalDist < 0.1f ? Quaternion.Euler(0f, currentRotationY, 0f) : Quaternion.LookRotation(direction.normalized, Vector3.up);

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = start + stepVector * i;

            // Выбираем случайную вариацию (zabor1, zabor2, zabor3)
            GameObject chosenPrefab = fenceVariants[Random.Range(0, fenceVariants.Count)];

            if (chosenPrefab != null)
            {
                Instantiate(chosenPrefab, spawnPos, rot);
            }
        }
    }

    private void EnsurePreviewCount(int requiredCount)
    {
        while (activePreviews.Count < requiredCount)
        {
            GameObject p = Instantiate(previewPrefab != null ? previewPrefab : fenceVariants[0]);

            // Отключаем коллайдеры у призраков, чтобы луч мыши не упирался в них
            foreach (var col in p.GetComponentsInChildren<Collider>()) col.enabled = false;

            activePreviews.Add(p);
        }

        for (int i = 0; i < activePreviews.Count; i++)
        {
            activePreviews[i].SetActive(i < requiredCount);
        }
    }

    private void ClearPreviews()
    {
        foreach (var p in activePreviews)
        {
            if (p != null) Destroy(p);
        }
        activePreviews.Clear();
    }
}
