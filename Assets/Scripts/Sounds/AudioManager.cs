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

    [Header("Настройки громкости")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.5f;

    private AudioSource sfxSource;
    private AudioSource musicSource;

    private void Awake()
    {
        // Паттерн Singleton с защитой от дублирования при смене сцен
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
        // Подписываемся на загрузку любых сцен в проекте
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeAudioSources()
    {
        // Создаем два отдельных источника: для музыки и для звуковых эффектов
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = sfxVolume;

        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // При переходе на новую сцену автоматически привязываем клик ко всем новым кнопкам
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
            sfxSource.PlayOneShot(uiClickClip, sfxVolume);
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void SetVolume(float volume)
    {
        sfxVolume = volume;
        musicVolume = volume;
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (musicSource != null) musicSource.volume = musicVolume;
    }
}
