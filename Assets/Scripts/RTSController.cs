using System.Collections.Generic;
using UnityEngine;

public class RTSController : MonoBehaviour
{
    [Header("Layer Masks")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask unitLayer;

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
        // Клик ЛКМ - выделение
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // Клик по юниту
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

            // Клик по пустому месту (сброс выделения)
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                DeselectAll();
            }
        }
    }

    private void HandleOrders()
    {
        // Клик ПКМ - движение выбранных юнитов
        if (Input.GetMouseButtonDown(1) && selectedUnits.Count > 0)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
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
}
