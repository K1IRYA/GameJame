using UnityEngine;
using UnityEngine.UI;

public class ConstructionContextMenu : MonoBehaviour
{
    public static ConstructionContextMenu Instance { get; private set; }

    [Header("UI Элементы меню")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button demolishButton;
    [SerializeField] private Text actionButtonText;
    [SerializeField] private Text demolishButtonText;

    private ConstructionSite currentSite;
    private ResourceVein currentVein;
    private Vector3 targetGroundPoint;
    private bool isGroundContextMenu = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Перепривязываем слушатели кнопок на универсальные методы
        if (buildButton != null)
        {
            buildButton.onClick.RemoveAllListeners();
            buildButton.onClick.AddListener(OnPrimaryActionClicked);
        }

        if (demolishButton != null)
        {
            demolishButton.onClick.RemoveAllListeners();
            demolishButton.onClick.AddListener(OnSecondaryActionClicked);
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    // 1. Открытие для стройплощадки (BUILD / DEMOLISH)
    public void OpenMenu(ConstructionSite site, Vector2 screenPosition)
    {
        currentSite = site;
        currentVein = null;
        isGroundContextMenu = false;

        if (actionButtonText != null) actionButtonText.text = "Build";
        if (demolishButtonText != null) demolishButtonText.text = "Remove";
        if (demolishButton != null) demolishButton.gameObject.SetActive(true);

        OpenMenuInternal(screenPosition);
    }

    // 2. Открытие для жилы руды (GATHER)
    public void OpenMenu(ResourceVein vein, Vector2 screenPosition)
    {
        currentVein = vein;
        currentSite = null;
        isGroundContextMenu = false;

        if (actionButtonText != null) actionButtonText.text = "Gather";
        if (demolishButton != null) demolishButton.gameObject.SetActive(false);

        OpenMenuInternal(screenPosition);
    }

    // 3. Открытие по двойному клику ПКМ по земле (CHOP WOOD / MOVE TO)
    public void OpenGroundMenu(Vector3 groundPoint, Vector2 screenPosition)
    {
        targetGroundPoint = groundPoint;
        isGroundContextMenu = true;
        currentSite = null;
        currentVein = null;

        if (actionButtonText != null) actionButtonText.text = "Chop wood";
        if (demolishButtonText != null) demolishButtonText.text = "Move to";
        if (demolishButton != null) demolishButton.gameObject.SetActive(true);

        OpenMenuInternal(screenPosition);
    }

    private void OpenMenuInternal(Vector2 screenPosition)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            Canvas parentCanvas = menuPanel.GetComponentInParent<Canvas>();

            if (parentCanvas != null && panelRect != null)
            {
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    panelRect.position = screenPosition;
                }
                else
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentCanvas.transform as RectTransform,
                        screenPosition,
                        parentCanvas.worldCamera,
                        out Vector2 localPoint
                    );
                    panelRect.localPosition = localPoint;
                }
            }
            else
            {
                menuPanel.transform.position = screenPosition;
            }

            menuPanel.transform.SetAsLastSibling();
        }
    }

    // Обработка верхней кнопки (CHOP WOOD / BUILD / GATHER)
    private void OnPrimaryActionClicked()
    {
        if (isGroundContextMenu)
        {
            BuilderUnit[] builders = Object.FindObjectsByType<BuilderUnit>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null && builder.IsSelected)
                {
                    builder.AssignChopAreaTask(targetGroundPoint, 20f);
                }
            }
        }
        else if (currentSite != null)
        {
            BuilderUnit[] builders = Object.FindObjectsByType<BuilderUnit>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null && builder.IsSelected)
                {
                    builder.AssignBuildTask(currentSite);
                }
            }
        }
        else if (currentVein != null)
        {
            BuilderUnit[] builders = Object.FindObjectsByType<BuilderUnit>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null && builder.IsSelected)
                {
                    builder.AssignGatherTask(currentVein);
                }
            }
        }

        CloseMenu();
    }

    // Обработка нижней кнопки (MOVE TO / DEMOLISH)
    private void OnSecondaryActionClicked()
    {
        if (isGroundContextMenu)
        {
            UnitsController[] units = Object.FindObjectsByType<UnitsController>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (unit != null && unit.isSelected)
                {
                    unit.MoveTo(targetGroundPoint);
                }
            }
        }
        else if (currentSite != null)
        {
            BuilderUnit[] builders = Object.FindObjectsByType<BuilderUnit>(FindObjectsSortMode.None);
            foreach (var builder in builders)
            {
                if (builder != null) builder.CancelAllTasks();
            }

            Destroy(currentSite.gameObject);
            currentSite = null;
        }

        CloseMenu();
    }

    public void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        currentSite = null;
        currentVein = null;
        isGroundContextMenu = false;
    }
}
