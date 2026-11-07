using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth { get; private set; }
    public float currentHealth { get; private set; }

    public bool IsDead => currentHealth <= 0f;

    private void Start()
    {
        // 1. Если это юнит игрока
        UnitsController unit = GetComponent<UnitsController>();
        if (unit != null && unit.Data != null)
        {
            SetupHealth(unit.Data.maxHealth);
            return;
        }

        // 2. Если это враг со своим скриптом EnemyAI
        EnemyAI enemy = GetComponent<EnemyAI>();
        if (enemy != null && enemy.Data != null)
        {
            SetupHealth(enemy.Data.maxHealth);
            return;
        }

        // 3. Если это здание (ищем компонент, даже если он выключен в инспекторе)
        BuildingTypeInfo building = GetComponent<BuildingTypeInfo>() ?? GetComponentInChildren<BuildingTypeInfo>();
        if (building != null)
        {
            // Если дата настроена — берем из SO, если нет или компонент пустой — даем резервные 250 HP забора
            float targetHP = (building.Data != null) ? building.Data.maxHealth : 250f;
            SetupHealth(targetHP);
            return;
        }

        // Резервный хардкод здоровья для любых забытых объектов на сцене
        SetupHealth(100f);
    }

    public void SetupHealth(float maxHP)
    {
        maxHealth = maxHP;
        currentHealth = maxHP;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        // Отладка: пишем в консоль, кто получил урон и сколько осталось HP
        Debug.Log($"[{gameObject.name}] Получил урон: {amount}. Осталось HP: {currentHealth}/{maxHealth}");

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"[{gameObject.name}] ПОЛНОСТЬЮ УНИЧТОЖЕН!");
        Destroy(gameObject);
    }
}
