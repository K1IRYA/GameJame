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

        // Удаляем старый фантом перед спавном нового
        CancelPlacement();

        activeGhost = Instantiate(ghostPrefab);

        BuildingGhost ghostScript = activeGhost.GetComponent<BuildingGhost>();
        if (ghostScript != null && ghostScript.groundLayer == 0)
        {
            ghostScript.groundLayer = groundLayer;
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
