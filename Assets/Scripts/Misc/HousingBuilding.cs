using UnityEngine;

public class HousingBuilding : MonoBehaviour
{
    [Header("Сколько мест строителей дает эта постройка")]
    [SerializeField] private int buildersCapacity = 2;

    private bool isRegistered = false;

    private void Start()
    {
        RegisterHousing();
    }

    private void RegisterHousing()
    {
        if (isRegistered) return;

        if (PopulationManager.Instance != null)
        {
            PopulationManager.Instance.AddMaxBuilders(buildersCapacity);
            isRegistered = true;
        }
    }

    private void OnDestroy()
    {
        // Списываем лимит только во время активной игры (не при выходе из сцены)
        if (isRegistered && gameObject.scene.isLoaded)
        {
            if (PopulationManager.Instance != null)
            {
                PopulationManager.Instance.RemoveMaxBuilders(buildersCapacity);
            }
        }
    }
}
