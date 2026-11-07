using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingCostUI : MonoBehaviour
{
    [Serializable]
    public struct ResourceRequirement
    {
        public ResourceType resourceType; // ScrapMetal, IronOre или Wood
        public Sprite icon;               // Иконка ресурса
        public int amount;                // Требуемое количество
    }

    [Header("Список требуемых ресурсов")]
    [SerializeField] private List<ResourceRequirement> costList = new List<ResourceRequirement>();
    public List<ResourceRequirement> CostList => costList;
    private readonly List<Text> costTextList = new List<Text>();

    [Header("Контейнер под кнопкой")]
    [Tooltip("Если оставить пустым, объект с HorizontalLayoutGroup создастся автоматически")]
    [SerializeField] private Transform costContainer;

    [Header("Настройки визуала стоимости")]
    [SerializeField] private Font textFont;
    [SerializeField] private int fontSize = 14;
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color notAffordableColor = Color.red;

    private Button buildButton;

    private void Awake()
    {
        buildButton = GetComponent<Button>();
        SetupContainerAndItems();
    }

    private void OnEnable()
    {
        UpdateCostVisuals();
    }

    private void Update()
    {
        UpdateCostVisuals();
    }

    private void SetupContainerAndItems()
    {
        // 1. Создаем или настраиваем контейнер под кнопкой
        if (costContainer == null)
        {
            GameObject containerObj = new GameObject("CostContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerObj.transform.SetParent(transform, false);

            RectTransform rt = containerObj.GetComponent<RectTransform>();
            // Привязываем к нижней границе кнопки
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -4f);
            rt.sizeDelta = new Vector2(0f, 32f);

            HorizontalLayoutGroup hlg = containerObj.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            costContainer = containerObj.transform;
        }

        // Очищаем старые дочерние элементы
        for (int i = costContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(costContainer.GetChild(i).gameObject);
        }
        costTextList.Clear();

        // 2. Генерируем элементы стоимости
        foreach (var req in costList)
        {
            GameObject itemObj = new GameObject($"Cost_{req.resourceType}", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            itemObj.transform.SetParent(costContainer, false);

            RectTransform itemRt = itemObj.GetComponent<RectTransform>();
            itemRt.sizeDelta = new Vector2(55f, 28f);

            HorizontalLayoutGroup itemHlg = itemObj.GetComponent<HorizontalLayoutGroup>();
            itemHlg.childAlignment = TextAnchor.MiddleCenter;
            itemHlg.spacing = 4f;
            itemHlg.childControlWidth = false;
            itemHlg.childControlHeight = false;
            itemHlg.childForceExpandWidth = false;
            itemHlg.childForceExpandHeight = false;

            // Иконка ресурса
            if (req.icon != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(itemObj.transform, false);
                Image img = iconObj.GetComponent<Image>();
                img.sprite = req.icon;
                img.preserveAspect = true;
                img.rectTransform.sizeDelta = new Vector2(22f, 22f);
            }

            // Текст количества ресурса
            GameObject textObj = new GameObject("AmountText", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(itemObj.transform, false);
            Text txt = textObj.GetComponent<Text>();
            txt.text = req.amount.ToString();
            txt.fontSize = fontSize;
            txt.color = affordableColor;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.font = (textFont != null) ? textFont : Resources.GetBuiltinResource<Font>("Silkscreen-Regular.ttf");

            // Защита от скрытия текста: разрешаем тексту выходить за рамки RectTransform
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.rectTransform.sizeDelta = new Vector2(30f, 26f);

            costTextList.Add(txt);
        }
    }


    private void UpdateCostVisuals()
    {
        bool hasEnoughResources = true;

        for (int i = 0; i < costList.Count; i++)
        {
            var req = costList[i];
            int currentStock = 0;

            if (ResourceManager.Instance != null)
            {
                currentStock = ResourceManager.Instance.GetResourceAmount(req.resourceType);
            }

            bool canAfford = currentStock >= req.amount;
            if (!canAfford)
            {
                hasEnoughResources = false;
            }

            // Подкрашиваем текст в красный, если ресурса не хватает
            if (i < costTextList.Count && costTextList[i] != null)
            {
                costTextList[i].color = canAfford ? affordableColor : notAffordableColor;
            }
        }

        // Блокируем саму кнопку, делая её некликабельной
        if (buildButton != null)
        {
            // Не активируем кнопку, если на ней висит TownHallBuildBlocker и хаб уже существует
            TownHallBuildBlocker blocker = GetComponent<TownHallBuildBlocker>();
            if (blocker != null && !hasEnoughResources)
            {
                buildButton.interactable = false;
            }
            else if (blocker == null)
            {
                buildButton.interactable = hasEnoughResources;
            }
        }
    }

    // Метод списания ресурсов (вызывается при установке фундамента)
    public bool TrySpendResources()
    {
        if (ResourceManager.Instance == null) return true;

        int scrapNeeded = 0;
        int ironNeeded = 0;
        int woodNeeded = 0;

        foreach (var req in costList)
        {
            switch (req.resourceType)
            {
                case ResourceType.ScrapMetal: scrapNeeded += req.amount; break;
                case ResourceType.IronOre: ironNeeded += req.amount; break;
                case ResourceType.Wood: woodNeeded += req.amount; break;
            }
        }

        return ResourceManager.Instance.TrySpendResources(scrapNeeded, ironNeeded, woodNeeded);
    }
}
