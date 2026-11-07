using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeyRebindButton : MonoBehaviour
{
    [Header("Какое действие настраивает эта кнопка")]
    [SerializeField] private GameAction action;

    [Header("Текстовое поле на кнопке")]
    [SerializeField] private Text keyDisplayText;

    private Button button;
    private bool isWaitingForKey = false;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (keyDisplayText == null)
        {
            keyDisplayText = GetComponentInChildren<Text>();
        }
    }

    private void Start()
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(StartRebinding);
        UpdateVisual();
    }

    private void OnEnable()
    {
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (InputManager.Instance != null && keyDisplayText != null)
        {
            keyDisplayText.text = InputManager.Instance.GetKey(action).ToString();
        }
    }

    public void StartRebinding()
    {
        StopAllCoroutines();
        StartCoroutine(WaitForKeyPressRoutine());
    }

    private System.Collections.IEnumerator WaitForKeyPressRoutine()
    {
        isWaitingForKey = true;
        if (keyDisplayText != null)
        {
            keyDisplayText.text = "...";
        }

        yield return null;

        while (isWaitingForKey)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isWaitingForKey = false;
                UpdateVisual();
                yield break;
            }

            if (Input.anyKeyDown)
            {
                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (kcode >= KeyCode.Mouse0 && kcode <= KeyCode.Mouse6) continue;

                    if (Input.GetKeyDown(kcode))
                    {
                        if (InputManager.Instance != null)
                        {
                            InputManager.Instance.SetKey(action, kcode);
                        }
                        isWaitingForKey = false;
                        UpdateVisual();
                        yield break;
                    }
                }
            }

            yield return null;
        }
    }
}
