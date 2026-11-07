using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionUIManager : MonoBehaviour
{
    public static UnitSelectionUIManager Instance { get; private set; }

    [Header("Желтая зона (Все союзники)")]
    [SerializeField] private Button selectAllButton;
    [SerializeField] private Text totalUnitsText;

    [Header("Красная зона (Выделенные)")]
    [SerializeField] private GameObject selectionContainer; // Объект красной зоны
    [SerializeField] private Transform portraitListRoot;      // Куда спавнить карточки
    [SerializeField] private GameObject portraitPrefab;       // Префаб карточки с UnitPortraitUI
    [SerializeField] private Sprite defaultPortraitIcon;

    private List<UnitPortraitUI> activePortraits = new List<UnitPortraitUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (selectAllButton != null)
        {
            selectAllButton.onClick.AddListener(OnSelectAllClicked);
        }
    }

    private void Start()
    {
        UpdateTotalCount();
        UpdateSelectionDisplay(new List<UnitsController>());
    }

    // Обновление счетчика в желтой кнопке
    public void UpdateTotalCount()
    {
        UnitsController[] allUnits = Object.FindObjectsByType<UnitsController>(FindObjectsSortMode.None);
        if (totalUnitsText != null)
        {
            totalUnitsText.text = $"ALL ({allUnits.Length})";
        }
    }

    // Обновление портретов в красной зоне
    private List<UnitsController> lastSelectedUnits = new List<UnitsController>();
    public void UpdateSelectionDisplay(List<UnitsController> selectedUnits)
    {
        // Проверяем, изменился ли состав выделения, чтобы не плодить и не уничтожать объекты в один и тот же кадр
        bool selectionChanged = false;
        if (selectedUnits == null || selectedUnits.Count != lastSelectedUnits.Count)
        {
            selectionChanged = true;
        }
        else
        {
            for (int i = 0; i < selectedUnits.Count; i++)
            {
                if (selectedUnits[i] != lastSelectedUnits[i])
                {
                    selectionChanged = true;
                    break;
                }
            }
        }

        // Если состав выделения не изменился (например, мы кликнули на того же единственного гнома), ничего не перерисовываем
        if (!selectionChanged) return;

        // Сохраняем текущее состояние для следующей проверки
        lastSelectedUnits = new List<UnitsController>(selectedUnits ?? new List<UnitsController>());

        // Очищаем старые портреты
        foreach (var p in activePortraits)
        {
            if (p != null) Destroy(p.gameObject);
        }
        activePortraits.Clear();

        // Если никто не выделен — скрываем красную панель
        if (selectedUnits == null || selectedUnits.Count == 0)
        {
            if (selectionContainer != null) selectionContainer.SetActive(false);
            return;
        }

        if (selectionContainer != null) selectionContainer.SetActive(true);

        // Создаем портреты заново только при реальном изменении состава
        foreach (var unit in selectedUnits)
        {
            if (unit == null) continue;

            GameObject go = Instantiate(portraitPrefab, portraitListRoot);
            UnitPortraitUI portrait = go.GetComponent<UnitPortraitUI>();
            if (portrait != null)
            {
                Sprite icon = (unit.Data != null && unit.Data.portraitIcon != null) ? unit.Data.portraitIcon : defaultPortraitIcon;
                portrait.Setup(unit, icon);
                activePortraits.Add(portrait);
            }
        }
    }

    private void OnSelectAllClicked()
    {
        RTSController rts = Object.FindAnyObjectByType<RTSController>();
        if (rts != null)
        {
            rts.SelectAllUnits();
        }
    }
}
