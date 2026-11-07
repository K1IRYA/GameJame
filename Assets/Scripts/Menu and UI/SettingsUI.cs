using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private GameSettingsData settingsData;

    [Header("UI References")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    private void Start()
    {
        if (settingsData != null)
        {
            settingsData.InitializeSettings();
            UpdateUIElements();
        }

        // Подписываемся на события UI
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void UpdateUIElements()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = settingsData.masterVolume;

        if (qualityDropdown != null)
            qualityDropdown.value = settingsData.qualityLevel;

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = settingsData.isFullscreen;
    }

    private void OnVolumeChanged(float value)
    {
        settingsData.SetVolume(value);
    }

    private void OnQualityChanged(int value)
    {
        settingsData.SetQuality(value);
    }
    private void OnFullscreenChanged(bool value)
    {
        settingsData.SetFullscreen(value);
    }
    public void OnBackButtonClicked()
    {
        if (settingsData != null)
        {
            settingsData.SaveSettings();
        }
        SceneManager.LoadScene("MainMenu");
    }
    private void OnDestroy()
    {
        // Отписываемся для чистоты памяти
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveAllListeners();
    }
}
