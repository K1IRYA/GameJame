using UnityEngine;
using UnityEngine.UI;

public class WaveTimerUI : MonoBehaviour
{
    public static WaveTimerUI Instance { get; private set; }

    [Header("UI Элементы текста")]
    [SerializeField] private GameObject uiRoot;
    [SerializeField] private Text timerText;
    [SerializeField] private Text statusText;

    [Header("Стрелка направления")]
    [SerializeField] private Image directionArrow; // Спрайт стрелочки
    [SerializeField] private float arrowDuration = 10f; // Сколько секунд показывать стрелку

    private Vector3 targetSpawnPosition;
    private bool isShowingArrow = false;
    private float arrowTimer = 0f;
    private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        mainCamera = Camera.main;
        if (directionArrow != null) directionArrow.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isShowingArrow && directionArrow != null && mainCamera != null)
        {
            arrowTimer -= Time.deltaTime;
            if (arrowTimer <= 0f)
            {
                directionArrow.gameObject.SetActive(false);
                isShowingArrow = false;
                return;
            }

            // Рассчитываем направление от центра экрана до 3D-точки спавна на карте
            Vector3 screenPos = mainCamera.WorldToScreenPoint(targetSpawnPosition);

            // Если точка позади камеры или на экране
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 direction = ((Vector2)screenPos - screenCenter).normalized;

            // Поворачиваем UI стрелочку
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            directionArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 если стрелка смотрит вверх по умолчанию
        }
    }

    public void UpdateTimerDisplay(float timeRemaining, int nextWaveNumber)
    {
        if (timerText == null) return;

        if (timeRemaining < 0f) timeRemaining = 0f;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"{minutes:00}:{seconds:00}";
        if (statusText != null) statusText.text = $"Until the wave: {nextWaveNumber}:";
    }

    public void SetStatusText(string text)
    {
        if (statusText != null) statusText.text = text;
        if (timerText != null) timerText.text = "";
    }

    public void ShowSpawnDirection(Vector3 spawnWorldPos)
    {
        targetSpawnPosition = spawnWorldPos;
        arrowTimer = arrowDuration;
        isShowingArrow = true;

        if (directionArrow != null) directionArrow.gameObject.SetActive(true);
    }

    public void SetVisible(bool isVisible)
    {
        if (uiRoot != null) uiRoot.SetActive(isVisible);
    }
}
