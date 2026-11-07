using UnityEngine;
using UnityEngine.SceneManagement;

public class mainmenu : MonoBehaviour
{
    public GameObject panelSettings;
    
    private void Start()
    {
        if (panelSettings != null)
        {
            panelSettings.SetActive(false);
        }
    }

    public void Play()
    {
        if (Application.CanStreamedLevelBeLoaded("MainScene"))
        {
            SceneManager.LoadScene("MainScene");
        }
    }
    public void Back()
    {
        SceneManager.LoadScene("MainMenu");
    }
    public void Settings()
    {
        if (Application.CanStreamedLevelBeLoaded("SettingsScene"))
        {
            SceneManager.LoadScene("SettingsScene");
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}