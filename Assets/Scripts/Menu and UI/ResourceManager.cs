using UnityEngine;
using UnityEngine.UI;

public class ResourceManager : MonoBehaviour
{
    // Синглтон, чтобы любой скрипт (например, стройка) мог проверить ресурсы через ResourceManager.Instance
    public static ResourceManager Instance { get; private set; }

    [Header("UI Текстовые поля")]
    [SerializeField] private Text goldText;
    [SerializeField] private Text woodText;

    [Header("Стартовые ресурсы")]
    [SerializeField] private int gold = 500;
    [SerializeField] private int wood = 200;

    // Публичные свойства для чтения текущих ресурсов
    public int Gold => gold;
    public int Wood => wood;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UpdateResourceUI();
    }

    // Метод добавления ресурсов (например, при добыче гномами)
    public void AddResources(int goldAmount, int woodAmount)
    {
        gold += goldAmount;
        wood += woodAmount;
        UpdateResourceUI();
    }

    // Метод траты ресурсов (вызывается перед началом строительства башни)
    public bool TrySpendResources(int goldCost, int woodCost)
    {
        if (gold >= goldCost && wood >= woodCost)
        {
            gold -= goldCost;
            wood -= woodCost;
            UpdateResourceUI();
            return true; // Успешно потрачено
        }
        
        Debug.Log("[Ресурсы] Недостаточно ресурсов для постройки!");
        return false; // Ресурсов не хватает
    }

    private void UpdateResourceUI()
    {
        if (goldText != null) goldText.text = gold.ToString();
        if (woodText != null) woodText.text = wood.ToString();
    }
}
