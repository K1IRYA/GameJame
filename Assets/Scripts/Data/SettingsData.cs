using UnityEngine;

[CreateAssetMenu(fileName = "GameSettingsData", menuName = "Settings/Game Settings Data")]
public class GameSettingsData : ScriptableObject
{
    private const string MasterVolumeKey = "Settings_MasterVolume";
    private const string MusicVolumeKey = "Settings_MusicVolume";
    private const string SfxVolumeKey = "Settings_SfxVolume";
    private const string FullscreenKey = "Settings_Fullscreen";

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("Display Settings")]
    public bool isFullscreen = true;

    public void InitializeSettings()
    {
        // Загружаем сохраненные значения
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 0.3f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);
        isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        ApplyAllSettings();
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        // Системный глобальный микшер Unity
        AudioListener.volume = masterVolume;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVolume);
        }

        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(musicVolume);
        }
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = volume;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSfxVolume(sfxVolume);
        }
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ApplyAllSettings()
    {
        AudioListener.volume = masterVolume;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(masterVolume);
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSfxVolume(sfxVolume);
        }

        Screen.fullScreen = isFullscreen;
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}
