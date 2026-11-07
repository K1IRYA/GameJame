using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class RTSController : MonoBehaviour
{
    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unitLayer;

    [Header("Selection Box Visuals")]
    [SerializeField] private Color boxColor = new Color(0.8f, 1f, 0.8f, 0.15f); // Полупрозрачный зеленый внутри
    [SerializeField] private Color borderColor = new Color(0.8f, 1f, 0.8f, 1f); // Яркая граница
    [SerializeField] private float borderThickness = 2f;

    [Header("Movement Marker")]
    [Tooltip("Префаб эффекта или точки, который будет появляться при клике ПКМ")]
    [SerializeField] private GameObject moveMarkerPrefab;

    private bool isDragging = false;
    private Vector3 mouseStartScreenPos;
    private Camera mainCamera;
    private readonly List<UnitsController> selectedUnits = new List<UnitsController>();
    private BuildingTypeInfo selectedTownHall = null;

    private void Awake()
    {
        mainCamera = Camera.main;
    }
    private int rightClickCount = 0;
    private Coroutine singleClickCoroutine = null;
    private const float doubleClickThreshold = 0.25f;
    private void Update()
    {
        HandleSelection();
        HandleOrders();

        // ДОБАВЛЕНО: обновляем UI при изменении состава выделенных
        if (UnitSelectionUIManager.Instance != null)
            UnitSelectionUIManager.Instance.UpdateSelectionDisplay(selectedUnits);

    }

    private void HandleSelection()
    {
        // Нажали ЛКМ — фиксируем начало выделения
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            isDragging = true;
            mouseStartScreenPos = Input.mousePosition;
        }

        // Отпустили ЛКМ – завершаем выделение и собираем юнитов
        if (Input.GetMouseButtonUp(0))
        {
            // Игнорируем завершение выделения на земле, если отпустили мышь над UI-элементом
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isDragging = false;
                return;
            }

            if (isDragging)
            {
                // Если мы почти не двигали мышь, считаем это обычным одиночным кликом
                if (Vector3.Distance(mouseStartScreenPos, Input.mousePosition) < 4f)
                {
                    PerformSingleClickSelection();
                }
                else
                {
                    PerformBoxSelection();
                }
                isDragging = false;
            }
        }
    }

    private void PerformSingleClickSelection()
    {
        Ray ray = mainCamera.ScreenPointToRay(mouseStartScreenPos);

        // 1. Проверяем клик по юниту
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, unitLayer))
        {
            UnitsController unit = hit.collider.GetComponentInParent<UnitsController>();
            if (unit != null)
            {
                if (!Input.GetKey(KeyCode.LeftShift))
                {
                    DeselectAll();
                }
                SelectUnit(unit);
                return;
            }
        }

        if (Physics.Raycast(ray, out RaycastHit buildingHit, Mathf.Infinity))
        {
            // Игнорируем превью (если в названии объекта есть preview/ghost или выключен коллайдер)
            if (buildingHit.collider.name.ToLower().Contains("preview") || buildingHit.collider.name.ToLower().Contains("ghost"))
                return;

            // Игнорируем стройплощадку (недостроенный фундамент)
            ConstructionSite site = buildingHit.collider.GetComponentInParent<ConstructionSite>();
            if (site != null && !site.IsCompleted)
                return;

            BuildingTypeInfo building = buildingHit.collider.GetComponentInParent<BuildingTypeInfo>();
            if (building != null && building.Type == BuildingTypeInfo.BuildingType.TownHall)
            {
                DeselectAll();
                SelectTownHall(building);
                return;
            }
        }
    }


    private void PerformBoxSelection()
    {
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeselectAll();
        }

        // Создаем границы рамки в координатах Viewport (от 0 до 1)
        Vector3 v1 = mainCamera.ScreenToViewportPoint(mouseStartScreenPos);
        Vector3 v2 = mainCamera.ScreenToViewportPoint(Input.mousePosition);

        Bounds viewportBounds = new Bounds();
        viewportBounds.center = (v1 + v2) / 2f;
        viewportBounds.size = new Vector3(Mathf.Abs(v1.x - v2.x), Mathf.Abs(v1.y - v2.y), 0f);

        // Ищем всех юнитов на сцене
        UnitsController[] allUnits = FindObjectsByType<UnitsController>(FindObjectsSortMode.None);

        foreach (var unit in allUnits)
        {
            if (unit == null) continue;

            // Переводим позицию каждого юнита на экране в координаты Viewport
            Vector3 unitScreenPos = mainCamera.WorldToViewportPoint(unit.transform.position);
            unitScreenPos.z = 0f; // Обнуляем глубину, так как рамка плоская

            // Если юнит находится в плоскости экрана внутри рамки — выделяем его
            if (viewportBounds.Contains(unitScreenPos))
            {
                SelectUnit(unit);
            }
        }
    }

    private void HandleOrders()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 1. Проверка интерактивных объектов (Стройплощадка / Руда / Атака Врага)
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                Health enemyHealth = hit.collider.GetComponentInParent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    Faction targetFaction = Faction.Neutral;

                    UnitsController allianceUnit = enemyHealth.GetComponent<UnitsController>();
                    EnemyAI enemyUnit = enemyHealth.GetComponent<EnemyAI>();

                    if (allianceUnit != null && allianceUnit.Data != null) targetFaction = allianceUnit.Data.faction;
                    else if (enemyUnit != null && enemyUnit.Data != null) targetFaction = enemyUnit.Data.faction;

                    if (targetFaction == Faction.Enemy)
                    {
                        CancelPendingSingleClick();

                        for (int i = 0; i < selectedUnits.Count; i++)
                        {
                            if (selectedUnits[i] != null)
                            {
                                MeleeAttack melee = selectedUnits[i].GetComponent<MeleeAttack>();
                                if (melee != null) melee.AttackTarget(enemyHealth);

                                //RangedAttack ranged = selectedUnits[i].GetComponent<RangedAttack>();
                                //if (ranged != null) ranged.AttackTarget(enemyHealth);
                            }
                        }
                        return;
                    }
                }

                // Клик по стройплощадке
                ConstructionSite site = hit.collider.GetComponentInParent<ConstructionSite>();
                if (site != null && !site.IsCompleted)
                {
                    CancelPendingSingleClick();
                    if (ConstructionContextMenu.Instance != null)
                        ConstructionContextMenu.Instance.OpenMenu(site, Input.mousePosition);
                    return;
                }

                // Клик по жиле ресурсов (руда, металл, дерево)
                ResourceVein vein = hit.collider.GetComponentInParent<ResourceVein>();
                if (vein != null && !vein.IsEmpty)
                {
                    CancelPendingSingleClick();
                    if (ConstructionContextMenu.Instance != null)
                        ConstructionContextMenu.Instance.OpenMenu(vein, Input.mousePosition);
                    return;
                }
            }

            // Закрываем открытое меню, если кликаем в новое место
            if (ConstructionContextMenu.Instance != null)
                ConstructionContextMenu.Instance.CloseMenu();

            // 2. Обработка клика по земле (Одиночный vs Двойной)
            if (selectedUnits.Count > 0 && Physics.Raycast(ray, out RaycastHit groundHit, Mathf.Infinity, groundLayer))
            {
                rightClickCount++;

                if (rightClickCount == 1)
                {
                    // Первый клик: ждем короткую паузу, не отдавая приказ движения сразу
                    singleClickCoroutine = StartCoroutine(SingleClickDelayRoutine(groundHit.point));
                }
                else if (rightClickCount >= 2)
                {
                    // Второй клик успел прийти: отменяем одиночное движение!
                    CancelPendingSingleClick();

                    // Открываем контекстное меню земли
                    if (ConstructionContextMenu.Instance != null)
                    {
                        ConstructionContextMenu.Instance.OpenGroundMenu(groundHit.point, Input.mousePosition);
                    }
                }
            }
        }
    }

    private System.Collections.IEnumerator SingleClickDelayRoutine(Vector3 destination)
    {
        yield return new WaitForSeconds(doubleClickThreshold);

        // Время ожидания истекло, второго клика не было — выполняем обычное движение:
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            if (selectedUnits[i] != null)
            {
                selectedUnits[i].MoveTo(destination);
            }
        }

        rightClickCount = 0;
        singleClickCoroutine = null;
    }

    private void CancelPendingSingleClick()
    {
        if (singleClickCoroutine != null)
        {
            StopCoroutine(singleClickCoroutine);
            singleClickCoroutine = null;
        }
        rightClickCount = 0;
    }

    private void SelectUnit(UnitsController unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.SetSelected(true); 
        }
    }

    private void DeselectAll()
    {
        for (int i = 0; i < selectedUnits.Count; i++)
        {
            if (selectedUnits[i] != null)
            {
                selectedUnits[i].SetSelected(false);
            }
        }
        selectedUnits.Clear();

        // Снимаем выделение с хаба и закрываем его меню
        if (selectedTownHall != null)
        {
            QuickOutline outline = selectedTownHall.GetComponent<QuickOutline>();
            if (outline != null) outline.enabled = false;
            selectedTownHall = null;

            if (TownHallUIManager.Instance != null)
                TownHallUIManager.Instance.SetVisible(false, null);
        }

        // Сброс красной панели при снятии выделения
        if (UnitSelectionUIManager.Instance != null)
            UnitSelectionUIManager.Instance.UpdateSelectionDisplay(selectedUnits);
    }

    private void SelectTownHall(BuildingTypeInfo townHall)
    {
        selectedTownHall = townHall;

        QuickOutline outline = selectedTownHall.GetComponent<QuickOutline>();
        if (outline != null) outline.enabled = true;

        if (TownHallUIManager.Instance != null)
            TownHallUIManager.Instance.SetVisible(true, townHall);
    }

    // Выделить вообще всех союзников на карте
    public void SelectAllUnits()
    {
        DeselectAll();
        UnitsController[] allUnits = Object.FindObjectsByType<UnitsController>(FindObjectsSortMode.None);
        foreach (var unit in allUnits)
        {
            if (unit != null)
            {
                selectedUnits.Add(unit);
                unit.SetSelected(true);
            }
        }

        if (UnitSelectionUIManager.Instance != null)
            UnitSelectionUIManager.Instance.UpdateSelectionDisplay(selectedUnits);
    }

    public void SelectOnly(UnitsController targetUnit)
    {
        DeselectAll();

        if (targetUnit != null)
        {
            selectedUnits.Add(targetUnit);
            targetUnit.SetSelected(true);
        }

        if (UnitSelectionUIManager.Instance != null)
        {
            UnitSelectionUIManager.Instance.UpdateSelectionDisplay(selectedUnits);
        }
    }

    private void OnGUI()
    {
        if (isDragging && Vector3.Distance(mouseStartScreenPos, Input.mousePosition) > 4f)
        {
            // Переводим координаты (в OnGUI начало координат слева-сверху, а у Input.mousePosition — слева-снизу)
            var rect = GetScreenRect(mouseStartScreenPos, Input.mousePosition);

            // Рисуем внутреннюю часть и границы через созданный TextureUtils
            TextureUtils.DrawScreenRect(rect, boxColor);
            TextureUtils.DrawScreenRectBorder(rect, borderThickness, borderColor);
        }
    }

    private Rect GetScreenRect(Vector3 screenPosition1, Vector3 screenPosition2)
    {
        screenPosition1.y = Screen.height - screenPosition1.y;
        screenPosition2.y = Screen.height - screenPosition2.y;

        var topLeft = Vector3.Min(screenPosition1, screenPosition2);
        var bottomRight = Vector3.Max(screenPosition1, screenPosition2);

        return new Rect(topLeft.x, topLeft.y, bottomRight.x - topLeft.x, bottomRight.y - topLeft.y);
    }

}
