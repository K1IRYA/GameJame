using UnityEngine;

public class BuildSystem : MonoBehaviour
{
    // Статическое свойство синглтона, к которому обращается BuildingGhost
    public static BuildSystem Instance { get; private set; }

    [Header("Слой земли по умолчанию")]
    public LayerMask groundLayer;

    private GameObject activeGhost;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // Отмена постройки по ПКМ или Escape
        if (activeGhost != null)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }
    }

    // Вызывается кнопкой из UI
    public void StartPlacement(GameObject ghostPrefab)
    {
        if (ghostPrefab == null) return;

        // Автоматически ищем скрипт стоимости на той кнопке, на которую сейчас кликнул игрок
        BuildingCostUI costUI = null;
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
        {
            costUI = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.GetComponent<BuildingCostUI>();
        }

        StartPlacementWithCost(ghostPrefab, costUI);
    }

    // Перегруженный метод для явной передачи стоимости
    public void StartPlacementWithCost(GameObject ghostPrefab, BuildingCostUI costUI)
    {
        if (ghostPrefab == null) return;

        // Удаляем старый фантом перед спавном нового
        CancelPlacement();

        activeGhost = Instantiate(ghostPrefab);

        BuildingGhost ghostScript = activeGhost.GetComponent<BuildingGhost>();
        if (ghostScript != null)
        {
            if (ghostScript.groundLayer == 0)
            {
                ghostScript.groundLayer = groundLayer;
            }

            // Передаем ссылку на стоимость кнопки в призрак здания
            ghostScript.SetOriginCostUI(costUI);
        }
    }

    public void CancelPlacement()
    {
        if (activeGhost != null)
        {
            Destroy(activeGhost);
            activeGhost = null;
        }
    }

    public void ClearCurrentGhost()
    {
        activeGhost = null;
    }
}
