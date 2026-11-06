using UnityEngine;

public class ResourceDropOff : MonoBehaviour
{
    [Header("Точка выгрузки")]
    [Tooltip("Радиус взаимодействия с гномами")]
    public float dropOffDistance = 5f;

    // Сдача переносимых ресурсов
    public void Deposit(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResource(type, amount);
        }
    }
}
