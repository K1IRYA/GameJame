using UnityEngine;
using UnityEngine.SceneManagement;

public class pause : MonoBehaviour
{
    public GameObject pausemenu;

    public static bool isPaused;

    void Start()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausemenu != null)
        {
            pausemenu.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                resumegame();
            }
            else
            {
                pausegame();
            }
        }
    }

    public void pausegame()
    {
        if (pausemenu != null)
        {
            pausemenu.SetActive(true);
        }

        Time.timeScale = 0f;
        isPaused = true;

        // Освобождаем курсор для кликов по кнопкам меню
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void resumegame()
    {
        if (pausemenu != null)
        {
            pausemenu.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;

        // Возвращаем курсор в режим игры
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void gotomain()
    {
        Time.timeScale = 1f;
        isPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void exitgame()
    {
        Debug.Log("Выход из игры...");
        Time.timeScale = 1f;
        isPaused = false;

        Application.Quit();

#if UNITY_EDITOR
        // Чтобы кнопка выхода работала и при тестировании в редакторе Unity:
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}