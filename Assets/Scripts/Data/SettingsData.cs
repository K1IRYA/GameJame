using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettingsData", menuName = "Settings/Game Settings Data")]
public class GameSettingsData : ScriptableObject
{
    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("Graphics")]
    public int qualityLevel = 2; // По умолчанию среднее/высокое
    public bool isFullscreen = true;

    private const string VolumeKey = "Settings_MasterVolume";
    private const string QualityKey = "Settings_QualityLevel";
    private const string FullscreenKey = "Settings_Fullscreen";

    public void InitializeSettings()
    {
        // Загрузка сохраненных значений
        masterVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        qualityLevel = PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel());
        isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;

        ApplyAllSettings();
    }

    public void SetVolume(float volume)
    {
        masterVolume = volume;
        AudioListener.volume = masterVolume;
        PlayerPrefs.SetFloat(VolumeKey, masterVolume);
    }

    public void SetQuality(int qualityIndex)
    {
        qualityLevel = qualityIndex;
        QualitySettings.SetQualityLevel(qualityLevel);
        PlayerPrefs.SetInt(QualityKey, qualityLevel);
    }

    public void SetFullscreen(bool fullscreen)
    {
        isFullscreen = fullscreen;
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    public void ApplyAllSettings()
    {
        AudioListener.volume = masterVolume;
        QualitySettings.SetQualityLevel(qualityLevel);
        Screen.fullScreen = isFullscreen;
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}
