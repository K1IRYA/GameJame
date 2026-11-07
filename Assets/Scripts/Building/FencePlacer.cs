using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FencePlacer : MonoBehaviour
{
    public static FencePlacer Instance { get; private set; }

    [Header("Префаб стройплощадки забора")]
    [SerializeField] private GameObject fenceSitePrefab;

    [Header("Префаб призрака для предпросмотра")]
    [SerializeField] private GameObject previewPrefab;

    [Header("Параметры секции")]
    [Tooltip("Точная длина секции (расстояние между столбами вдоль линии забора)")]
    [SerializeField] private float segmentLength = 6.0f;

    [Tooltip("Дистанция примагничивания к соседним точкам SnapPoint")]
    [SerializeField] private float snapDistance = 2.0f;

    [Tooltip("Дополнительный угол модели, если забор развернут боком")]
    [SerializeField] private float modelAngleOffset = 0f;

    [Header("Слои")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask fenceSnapLayer;


    [Header("Связь со стоимостью на кнопке UI")]
    [Tooltip("Ссылка на компонент BuildingCostUI на кнопке забора (если оставить пустым, найдет автоматически)")]
    [SerializeField] private BuildingCostUI fenceCostUI;

    private bool isPlacing = false;
    private bool isDragging = false;
    private float manualRotationAngle = 0f; // Угол при повороте на R (0, 90, 180, 270)
    private Vector3 dragStartPos;
    public bool IsPlacing => isPlacing;
    private readonly List<GameObject> activePreviews = new List<GameObject>();
    private Camera mainCam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        mainCam = Camera.main;
    }

    private void Update()
    {
        if (!isPlacing) return;

        // Поворот на 90 градусов кнопкой R
        bool isRotateTriggered = InputManager.Instance != null
            ? InputManager.Instance.GetActionKeyDown(GameAction.RotatePlacement)
            : Input.GetKeyDown(KeyCode.R);

        if (isRotateTriggered)
        {
            manualRotationAngle = (manualRotationAngle + 90f) % 360f;
        }

        // Отмена по ПКМ или Escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        Vector3 groundPos;
        bool hasHit = GetMouseGroundPosition(out groundPos);

        // Игнорируем нажатия, если курсор находится над интерфейсом (UI)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (!hasHit) return;

        // 1. Старт протяжки (нажали ЛКМ)
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartPos = GetSnappedPosition(groundPos);
        }

        // 2. Протягивание линии
        if (isDragging && Input.GetMouseButton(0))
        {
            UpdateDragLine(dragStartPos, groundPos);
        }
        // 3. Свободное движение мыши (одиночный призрак)
        else if (!isDragging)
        {
            UpdateSinglePreview(groundPos);
        }

        // 4. Завершение стройки (отпустили ЛКМ)
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            BuildFenceChain(dragStartPos, groundPos);
            isDragging = false;
            ClearPreviews();
        }
    }

    public void StartPlacingFence()
    {
        isPlacing = true;
        isDragging = false;
        manualRotationAngle = 0f;
        ClearPreviews();
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        isDragging = false;
        ClearPreviews();
    }

    private bool GetMouseGroundPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return false;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            position = hit.point;
            return true;
        }
        return false;
    }

    private Vector3 GetSnappedPosition(Vector3 originalPos)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(originalPos, snapDistance, fenceSnapLayer);
        float closestDist = float.MaxValue;
        Vector3 bestSnapPos = originalPos;

        foreach (var col in nearbyColliders)
        {
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

    private void UpdateSinglePreview(Vector3 mousePos)
    {
        EnsurePreviewPool(1);
        Vector3 snappedPos = GetSnappedPosition(mousePos);

        GameObject p = activePreviews[0];
        p.transform.position = snappedPos;
        // Сохраняем X = 89.98f, чтобы модель стояла вертикально
        p.transform.rotation = Quaternion.Euler(89.98f, manualRotationAngle + modelAngleOffset, 0f);
        p.SetActive(true);
    }

    private void UpdateDragLine(Vector3 start, Vector3 current)
    {
        Vector3 lineVec = current - start;
        lineVec.y = 0f;
        float totalDist = lineVec.magnitude;

        if (totalDist < 0.2f)
        {
            UpdateSinglePreview(start);
            return;
        }

        Vector3 dir = lineVec.normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(89.98f, modelAngleOffset, 0f);

        int desiredCount = Mathf.Max(1, Mathf.RoundToInt(totalDist / segmentLength));

        // Ограничиваем число секций доступными ресурсами
        int affordableCount = GetMaxAffordableSegments(desiredCount);
        int finalCount = Mathf.Min(desiredCount, affordableCount);

        EnsurePreviewPool(finalCount);

        Vector3 step = dir * segmentLength;

        for (int i = 0; i < finalCount; i++)
        {
            Vector3 pos = start + step * i;
            GameObject p = activePreviews[i];
            p.transform.position = pos;
            p.transform.rotation = targetRot;
            p.SetActive(true);
        }

        // Отключаем лишние превью, если на них не хватило ресурсов
        for (int i = finalCount; i < activePreviews.Count; i++)
        {
            activePreviews[i].SetActive(false);
        }
    }

    private void BuildFenceChain(Vector3 start, Vector3 end)
    {
        Vector3 lineVec = end - start;
        lineVec.y = 0f;
        float totalDist = lineVec.magnitude;

        int desiredCount = totalDist < segmentLength ? 1 : Mathf.Max(1, Mathf.RoundToInt(totalDist / segmentLength));
        int affordableCount = GetMaxAffordableSegments(desiredCount);
        int finalCount = Mathf.Min(desiredCount, affordableCount);

        if (finalCount <= 0)
        {
            return;
        }

        // Списываем суммарную стоимость через ResourceManager
        if (ResourceManager.Instance != null && fenceCostUI != null)
        {
            int totalScrap = 0;
            int totalIron = 0;
            int totalWood = 0;

            foreach (var req in fenceCostUI.CostList)
            {
                switch (req.resourceType)
                {
                    case ResourceType.ScrapMetal: totalScrap += req.amount * finalCount; break;
                    case ResourceType.IronOre: totalIron += req.amount * finalCount; break;
                    case ResourceType.Wood: totalWood += req.amount * finalCount; break;
                }
            }

            bool success = ResourceManager.Instance.TrySpendResources(totalScrap, totalIron, totalWood);
            if (!success)
            {
                return;
            }
        }

        Vector3 dir = totalDist < 0.2f ? Vector3.forward : lineVec.normalized;
        Quaternion targetRot = totalDist < 0.2f
            ? Quaternion.Euler(89.98f, manualRotationAngle + modelAngleOffset, 0f)
            : Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(89.98f, modelAngleOffset, 0f);

        Vector3 step = dir * segmentLength;

        if (fenceSitePrefab == null)
        {
            return;
        }

        for (int i = 0; i < finalCount; i++)
        {
            Vector3 spawnPos = start + step * i;
            Instantiate(fenceSitePrefab, spawnPos, targetRot);
        }
    }

    private int GetMaxAffordableSegments(int targetCount)
    {
        if (ResourceManager.Instance == null || fenceCostUI == null) return targetCount;

        int maxAffordable = int.MaxValue;

        foreach (var req in fenceCostUI.CostList)
        {
            if (req.amount <= 0) continue;
            int playerStock = ResourceManager.Instance.GetResourceAmount(req.resourceType);
            int canAffordThis = playerStock / req.amount;
            if (canAffordThis < maxAffordable)
            {
                maxAffordable = canAffordThis;
            }
        }

        return Mathf.Max(0, maxAffordable);
    }

    private void EnsurePreviewPool(int requiredCount)
    {
        while (activePreviews.Count < requiredCount)
        {
            GameObject prefabToUse = previewPrefab != null ? previewPrefab : fenceSitePrefab;
            GameObject p = Instantiate(prefabToUse);

            // Отключаем скрипты BuildingGhost, чтобы они не перезаписывали transform призрака
            MonoBehaviour[] scripts = p.GetComponentsInChildren<MonoBehaviour>();
            foreach (var s in scripts)
            {
                if (s.GetType().Name == "BuildingGhost") s.enabled = false;
            }

            // Отключаем коллайдеры у превью
            Collider[] colliders = p.GetComponentsInChildren<Collider>();
            foreach (var c in colliders) c.enabled = false;

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
