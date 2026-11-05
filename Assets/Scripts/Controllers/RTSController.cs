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

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleSelection();
        HandleOrders();
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

        // Отпустили ЛКМ — завершаем выделение и собираем юнитов
        if (Input.GetMouseButtonUp(0))
        {
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

        if (!Input.GetKey(KeyCode.LeftShift))
        {
            DeselectAll();
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
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 1. Проверяем, кликнули ли мы по стройплощадке (работает ВСЕГДА, даже без выделенных юнитов)
            if (Physics.Raycast(ray, out RaycastHit siteHit, Mathf.Infinity))
            {
                ConstructionSite site = siteHit.collider.GetComponentInParent<ConstructionSite>();
                if (site != null && !site.IsCompleted)
                {
                    if (ConstructionContextMenu.Instance != null)
                    {
                        ConstructionContextMenu.Instance.OpenMenu(site, Input.mousePosition);
                    }
                    return; // Прерываем обычный приказ идти, так как открыли меню площадки
                }
            }

            // 2. Если кликнули не по стройплощадке, закрываем открытое меню, если оно было
            if (ConstructionContextMenu.Instance != null)
            {
                ConstructionContextMenu.Instance.CloseMenu();
            }

            // 3. Обычное перемещение выделенных юнитов по земле
            if (selectedUnits.Count > 0 && Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                for (int i = 0; i < selectedUnits.Count; i++)
                {
                    if (selectedUnits[i] != null)
                    {
                        selectedUnits[i].MoveTo(hit.point);
                    }
                }
            }
        }
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
