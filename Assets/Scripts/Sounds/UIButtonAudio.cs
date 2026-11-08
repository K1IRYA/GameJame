using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIButtonAudio : MonoBehaviour
{
    public static UIButtonAudio Instance { get; private set; }

    [Header("Звук клика по UI")]
    [SerializeField] private AudioClip uiClickClip;

    [Header("Громкость")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Header("Автоподписка")]
    [Tooltip("Автоматически добавить звук на абсолютно все кнопки на сцене")]
    [SerializeField] private bool autoBindAllButtons = true;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D звук
    }

    private void Start()
    {
        if (autoBindAllButtons)
        {
            RegisterAllButtonsOnScene();
        }
    }

    // Сканирует и привязывает звук ко всем кнопкам на сцене
    public void RegisterAllButtonsOnScene()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in buttons)
        {
            RegisterButton(btn);
        }
    }

    // Привязка одной конкретной кнопки (например, для динамически созданных)
    public void RegisterButton(Button button)
    {
        if (button == null) return;
        button.onClick.AddListener(PlayUIClick);
    }

    public void PlayUIClick()
    {
        if (uiClickClip == null || audioSource == null) return;
        audioSource.PlayOneShot(uiClickClip, volume);
    }
}
