using UnityEngine;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    private Health baseHealth;
    private bool isGameOverTriggered = false;

    private void Awake()
    {
        baseHealth = GetComponent<Health>();
    }

    private void Update()
    {
        if (!isGameOverTriggered && baseHealth != null && baseHealth.IsDead)
        {
            TriggerDefeat();
        }
    }

    private void OnDestroy()
    {
        if (!isGameOverTriggered && gameObject.scene.isLoaded)
        {
            TriggerDefeat();
        }
    }

    private void TriggerDefeat()
    {
        if (isGameOverTriggered) return;
        isGameOverTriggered = true;

        Debug.Log("[GameOverManager] База уничтожена! Активирую экран DEFEAT.");

        pause.isPaused = true;
        pause.isGameOver = true;
        Time.timeScale = 0f;

        if (WaveTimerUI.Instance != null)
        {
            WaveTimerUI.Instance.SetVisible(false);
        }

        GameObject pausePanel = null;

        pause pauseCtrl = Object.FindAnyObjectByType<pause>(FindObjectsInactive.Include);
        if (pauseCtrl != null && pauseCtrl.pausemenu != null)
        {
            pausePanel = pauseCtrl.pausemenu;
        }

        if (pausePanel == null)
        {
            GameObject inGameHud = GameObject.Find("InGameHud");
            if (inGameHud != null)
            {
                Transform pTransform = inGameHud.transform.Find("Pause");
                if (pTransform != null) pausePanel = pTransform.gameObject;
            }
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);

            // ИСПРАВЛЕНО: Text[] со скобками
            Text[] allTexts = pausePanel.GetComponentsInChildren<Text>(true);
            foreach (var txt in allTexts)
            {
                if (txt.name.ToLower().Contains("text") && (txt.text.ToUpper().Contains("PAUSE") || txt.transform.parent == pausePanel.transform))
                {
                    txt.text = "DEFEAT";
                    break;
                }
            }

            // ИСПРАВЛЕНО: Button[] со скобками
            Button[] allButtons = pausePanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string btnName = btn.gameObject.name.ToLower();
                Text btnText = btn.GetComponentInChildren<Text>(true);
                string textContent = (btnText != null) ? btnText.text.ToLower() : "";

                // Скрываем Continue и Settings
                if (btnName.Contains("continue") || textContent.Contains("continue") ||
                    btnName.Contains("settings") || textContent.Contains("settings"))
                {
                    btn.gameObject.SetActive(false);
                }
                // Находим кнопку главного меню: активируем и поднимаем на Y = 106
                else if (btnName.Contains("main") || textContent.Contains("main") || btnName.Contains("menu"))
                {
                    btn.gameObject.SetActive(true);

                    RectTransform rt = btn.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 106f);
                    }
                }
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
