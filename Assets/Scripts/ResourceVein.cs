using UnityEngine;
using static ResourceType;

public class ResourceVein : MonoBehaviour
{
    [Header("Тип ресурса")]
    public ResourceType resourceType = ResourceType.ScrapMetal;

    [Header("Запас сырья")]
    public bool isInfinite = true;
    public int maxResources = 500;
    public int currentResources = 500;

    [Header("Эффект истощения (для деревьев)")]
    [SerializeField] private GameObject stumpPrefab;
    public bool IsEmpty => !isInfinite && currentResources <= 0;

    public int ExtractResource(int amount)
    {
        if (isInfinite) return amount;

        if (IsEmpty) return 0;

        int extracted = Mathf.Min(amount, currentResources);
        currentResources -= extracted;

        if (IsEmpty)
        {
            if (stumpPrefab != null && resourceType == ResourceType.Wood)
            {
                Instantiate(stumpPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }

        return extracted;
    }
}
