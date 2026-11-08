using UnityEngine;

public class UnitAnimationEvents : MonoBehaviour
{
    [Header("Звук удара молотом")]
    [SerializeField] private AudioClip hammerHitClip;

    [Header("Настройки случайного тона (Pitch)")]
    [SerializeField] private bool randomizePitch = true;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 1.1f;

    private AudioSource audioSource;

    private void Awake()
    {
        // Ищем AudioSource на модели или создаем его, если нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Настройки для 3D-звука (чтобы громкость зависела от положения камеры)
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D звук
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 25f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    // ВАЖНО: Этот метод будет вызываться из анимации
    public void PlayHammerHitSound()
    {
        if (hammerHitClip == null) return;

        // Корректируем тон, чтобы частые удары не звучали монотонно
        if (randomizePitch)
            audioSource.pitch = Random.Range(minPitch, maxPitch);
        else
            audioSource.pitch = 1f;

        // Если глобальный AudioManager готов, берем его громкость SFX в качестве базы
        float baseVolume = 0.8f;

        // Воспроизводим звук
        audioSource.PlayOneShot(hammerHitClip, baseVolume);
    }
}
