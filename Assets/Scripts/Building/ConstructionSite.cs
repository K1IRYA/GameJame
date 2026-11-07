using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    [Header("Финальное готовое здание")]
    public GameObject completedBuildingPrefab;

    [Header("Параметры стройки")]
    public float maxBuildProgress = 100f;
    public float currentProgress = 0f;

    public bool IsCompleted => currentProgress >= maxBuildProgress;

    public void AddProgress(float amount)
    {
        if (IsCompleted) return;

        currentProgress += amount;

        if (currentProgress >= maxBuildProgress)
        {
            CompleteBuilding();
        }
    }

    private void CompleteBuilding()
    {
        if (completedBuildingPrefab != null)
        {
            Instantiate(completedBuildingPrefab, transform.position, transform.rotation);
        }
        Destroy(gameObject);
    }
}
