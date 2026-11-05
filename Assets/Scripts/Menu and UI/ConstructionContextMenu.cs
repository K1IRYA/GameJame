using UnityEngine;
using UnityEngine.UI;

public class ConstructionContextMenu : MonoBehaviour
{
    public static ConstructionContextMenu Instance { get; private set; }

    [Header("Панель меню")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button buildButton;
    [SerializeField] private Button demolishButton;

    private ConstructionSite currentSite;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        if (buildButton != null)
        {
            buildButton.onClick.AddListener(OnBuildClicked);
        }

        if (demolishButton != null)
        {
            demolishButton.onClick.AddListener(OnDemolishClicked);
        }
    }

    private void Update()
    {
        // Закрываем меню при клике ЛКМ в пустоту или по клавише Escape
        if (menuPanel != null && menuPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseMenu();
            }
        }
    }

    public void OpenMenu(ConstructionSite site, Vector2 screenPosition)
    {
        currentSite = site;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);

            // Получаем RectTransform меню и канваса
            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            Canvas parentCanvas = menuPanel.GetComponentInParent<Canvas>();

            if (parentCanvas != null && panelRect != null)
            {
                // Для Canvas в режиме Screen Space - Overlay:
                if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    panelRect.position = screenPosition;
                }
                else // Для режима Screen Space - Camera:
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

            // Гарантируем, что панель отрисуется поверх всех остальных UI элементов
            menuPanel.transform.SetAsLastSibling();
        }
    }


    public void CloseMenu()
    {
        currentSite = null;
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    private void OnBuildClicked()
    {
        if (currentSite == null) return;

        // Ищем всех строителей и отправляем тех, кто выделен
        BuilderUnit[] builders = FindObjectsOfType<BuilderUnit>();
        bool anyAssigned = false;

        foreach (var builder in builders)
        {
            if (builder.IsSelected)
            {
                builder.AssignBuildTask(currentSite);
                anyAssigned = true;
            }
        }

        if (!anyAssigned)
        {
            Debug.Log("[Контекстное меню] Ни один строитель не выбран!");
        }

        CloseMenu();
    }

    private void OnDemolishClicked()
    {
        if (currentSite != null)
        {
            // Отменяем задачу у всех строителей, которые шли строить этот объект
            BuilderUnit[] builders = FindObjectsOfType<BuilderUnit>();
            foreach (var builder in builders)
            {
                builder.CancelBuildTask();
            }

            // Удаляем саму стройплощадку
            Destroy(currentSite.gameObject);
            currentSite = null;
        }

        CloseMenu();
    }
}
