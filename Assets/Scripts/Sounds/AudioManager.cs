using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Звуки интерфейса")]
    [SerializeField] private AudioClip uiClickClip;

    [Header("Фоновая музыка (необязательно)")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Настройки громкости каналов")]
    private float masterVolume = 1f;
    private float baseMusicVolume = 0.3f;
    private float baseSfxVolume = 0.8f;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeAudioSources()
    {
        // Считываем все сохраненные настройки
        masterVolume = PlayerPrefs.GetFloat("Settings_MasterVolume", 1f);
        baseMusicVolume = PlayerPrefs.GetFloat("Settings_MusicVolume", 0.3f);
        baseSfxVolume = PlayerPrefs.GetFloat("Settings_SfxVolume", 0.8f);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = masterVolume * baseMusicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = masterVolume * baseSfxVolume;

        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindAllSceneButtons();
    }

    public void BindAllSceneButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            RegisterButton(btn);
        }
    }

    public void RegisterButton(Button button)
    {
        if (button == null) return;
        button.onClick.RemoveListener(PlayUIClick);
        button.onClick.AddListener(PlayUIClick);
    }

    public void PlayUIClick()
    {
        if (uiClickClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(uiClickClip, masterVolume * baseSfxVolume);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, masterVolume * baseSfxVolume * volumeMultiplier);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    // Методы динамического изменения громкости из слайдеров настроек
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        UpdateCurrentVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        baseMusicVolume = volume;
        UpdateCurrentVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        baseSfxVolume = volume;
        UpdateCurrentVolumes();
    }

    private void UpdateCurrentVolumes()
    {
        if (musicSource != null) musicSource.volume = masterVolume * baseMusicVolume;
        if (sfxSource != null) sfxSource.volume = masterVolume * baseSfxVolume;
    }
}
