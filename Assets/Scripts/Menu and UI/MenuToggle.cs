using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    [Header("Панель/Фон меню")]
    public GameObject targetPanel;

    public void ToggleMenu()
    {
        if (targetPanel != null)
        {
            bool willOpen = !targetPanel.activeSelf;
            targetPanel.SetActive(willOpen);

            // Если открываем меню стройки — гасим меню хаба
            if (willOpen)
            {
                RTSController rts = Object.FindAnyObjectByType<RTSController>();
                if (rts != null) rts.DeselectTownHall();
            }
        }
    }

}
