using UnityEngine;
using UnityEngine.UI;

public class UnitPortraitUI : MonoBehaviour
{
    [Header("UI Элементы портрета")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Button selectButton;

    [Header("Полоска здоровья")]
    [Tooltip("Зеленая полоска с Image Type = Filled")]
    [SerializeField] private Image healthBarFill;

    private UnitsController boundUnit;

    public void Setup(UnitsController unit, Sprite portraitIcon)
    {
        boundUnit = unit;

        if (iconImage != null && portraitIcon != null)
        {
            iconImage.sprite = portraitIcon;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(OnPortraitClicked);
        }

        UpdateHealthDisplay();
    }

    private void Update()
    {
        // Обновляем шкалу здоровья в реальном времени, если юнит ранен
        if (boundUnit != null)
        {
            UpdateHealthDisplay();
        }
    }

    private void UpdateHealthDisplay()
    {
        if (healthBarFill == null || boundUnit == null) return;

        // Если у вас в скрипте юнита есть поля здоровья (например, currentHealth / maxHealth)
        float healthPercent = GetUnitHealthPercent();
        healthBarFill.fillAmount = Mathf.Clamp01(healthPercent);
    }

    private float GetUnitHealthPercent()
    {
        if (boundUnit != null)
        {
            Health h = boundUnit.GetComponent<Health>();
            if (h != null && h.maxHealth > 0f)
            {
                return h.currentHealth / h.maxHealth;
            }
        }

        return 1.0f;
    }

    private void OnPortraitClicked()
    {
        if (boundUnit == null)
        {
            Debug.LogWarning("[UnitPortraitUI] Клик сработал, но boundUnit равен null!");
            return;
        }

        Debug.Log($"[UnitPortraitUI] Нажат портрет юнита: {boundUnit.name}");

        RTSController rts = Object.FindAnyObjectByType<RTSController>();
        if (rts != null)
        {
            rts.SelectOnly(boundUnit);
        }
        else
        {
            Debug.LogError("[UnitPortraitUI] RTSController не найден на сцене!");
        }
    }

}
