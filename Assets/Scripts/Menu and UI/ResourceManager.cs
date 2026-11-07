using UnityEngine;
using UnityEngine.UI;
using static ResourceType;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("UI Счётчики (Legacy Text или TMP_Text)")]
    [SerializeField] private Text scrapText;
    [SerializeField] private Text ironText;
    [SerializeField] private Text woodText;

    [Header("Текущие запасы")]
    [SerializeField] private int scrapMetal = 0;
    [SerializeField] private int ironOre = 0;
    [SerializeField] private int wood = 0;

    public int ScrapMetal => scrapMetal;
    public int IronOre => ironOre;
    public int Wood => wood;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        UpdateResourceUI();
    }

    public void AddResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.ScrapMetal:
                scrapMetal += amount;
                break;
            case ResourceType.IronOre:
                ironOre += amount;
                break;
            case ResourceType.Wood:
                wood += amount;
                break;
        }

        UpdateResourceUI();
    }

    public bool TrySpendResources(int scrapCost, int ironCost, int woodCost)
    {
        if (scrapMetal >= scrapCost && ironOre >= ironCost && wood >= woodCost)
        {
            scrapMetal -= scrapCost;
            ironOre -= ironCost;
            wood -= woodCost;
            UpdateResourceUI();
            return true;
        }

        return false;
    }
    public int GetResourceAmount(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.ScrapMetal: return scrapMetal;
            case ResourceType.IronOre: return ironOre;
            case ResourceType.Wood: return wood;
            default: return 0;
        }
    }

    private void UpdateResourceUI()
    {
        if (scrapText != null) scrapText.text = scrapMetal.ToString();
        if (ironText != null) ironText.text = ironOre.ToString();
        if (woodText != null) woodText.text = wood.ToString();
    }
}
