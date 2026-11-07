using UnityEngine;
using UnityEngine.UI;

public class PopulationUI : MonoBehaviour
{
    [Header("Текстовое поле для вывода (например, 0/2)")]
    [SerializeField] private Text populationText;

    private void Awake()
    {
        if (populationText == null)
        {
            populationText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        // Подписываемся на изменение количества строителей и лимита
        PopulationManager.OnPopulationChanged += UpdateDisplay;

        // Первичная инициализация при старте/включении
        if (PopulationManager.Instance != null)
        {
            UpdateDisplay(PopulationManager.Instance.CurrentBuilders, PopulationManager.Instance.MaxBuilders);
        }
    }

    private void OnDisable()
    {
        PopulationManager.OnPopulationChanged -= UpdateDisplay;
    }

    private void Start()
    {
        if (PopulationManager.Instance != null)
        {
            UpdateDisplay(PopulationManager.Instance.CurrentBuilders, PopulationManager.Instance.MaxBuilders);
        }
    }

    private void UpdateDisplay(int current, int max)
    {
        if (populationText != null)
        {
            populationText.text = $"{current}/{max}";
        }
    }
}
