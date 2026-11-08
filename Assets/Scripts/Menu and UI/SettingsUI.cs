using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("Data Reference")]
    [SerializeField] private GameSettingsData settingsData;

    [Header("UI References")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Navigation")]
    [SerializeField] private string returnSceneName = "MainMenu";

    private void Start()
    {
        if (settingsData != null)
        {
            settingsData.InitializeSettings();
        }

        UpdateUIElements();

        // Подписываемся на события UI
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
    }

    private void UpdateUIElements()
    {
        if (settingsData == null) return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(settingsData.masterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(settingsData.musicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settingsData.sfxVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(settingsData.isFullscreen);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (settingsData != null) settingsData.SetMasterVolume(value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (settingsData != null) settingsData.SetMusicVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (settingsData != null) settingsData.SetSfxVolume(value);
    }

    private void OnFullscreenChanged(bool value)
    {
        if (settingsData != null) settingsData.SetFullscreen(value);
    }

    public void OnBackButtonClicked()
    {
        if (settingsData != null)
        {
            settingsData.SaveSettings();
        }

        if (!string.IsNullOrEmpty(returnSceneName))
        {
            SceneManager.LoadScene(returnSceneName);
        }
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveAllListeners();
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveAllListeners();
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveAllListeners();
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveAllListeners();
    }
}
