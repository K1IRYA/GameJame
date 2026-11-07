using System.Collections.Generic;
using UnityEngine;

public class ConstructionSite : MonoBehaviour
{
    [Header("Финальное готовое здание")]
    public GameObject completedBuildingPrefab;

    [Header("Случайные вариации (если нужно несколько, например для забора)")]
    [SerializeField] private List<GameObject> randomVariants = new List<GameObject>();

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
        GameObject prefabToSpawn = completedBuildingPrefab;

        // Если в списке есть вариации, выбираем одну случайно
        if (randomVariants != null && randomVariants.Count > 0)
        {
            int randomIndex = Random.Range(0, randomVariants.Count);
            prefabToSpawn = randomVariants[randomIndex];
        }

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}
