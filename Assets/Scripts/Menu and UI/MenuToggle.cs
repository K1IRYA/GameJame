using UnityEngine;

public class MenuToggle : MonoBehaviour
{
    [Header("Панель/Фон меню")]
    public GameObject targetPanel;

    public void ToggleMenu()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(!targetPanel.activeSelf);
        }
    }
}
