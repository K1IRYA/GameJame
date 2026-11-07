using UnityEngine;
using UnityEngine.SceneManagement;

public class pause : MonoBehaviour
{
    public GameObject pausemenu;

    public static bool isPaused;
    public static bool isGameOver; // Флаг поражения, доступный из любого скрипта

    void Start()
    {
        isPaused = false;
        isGameOver = false; // Сбрасываем при перезапуске/старте сцены
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
            // Если база уничтожена, Escape больше не закрывает меню, а сразу выходит в главное меню
            if (isGameOver)
            {
                gotomain();
                return;
            }

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

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void resumegame()
    {
        // Дополнительная защита: если игра окончена, метод отмены паузы блокируется
        if (isGameOver) return;

        if (pausemenu != null)
        {
            pausemenu.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;

        Cursor.lockState = CursorLockMode.Confined;
    }

    public void gotomain()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void exitgame()
    {
        Debug.Log("Выход из игры...");
        Time.timeScale = 1f;
        isPaused = false;
        isGameOver = false;

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
