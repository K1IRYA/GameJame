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

        // 2. ДОБАВЛЕНО: Если это враг со своим скриптом EnemyAI
        EnemyAI enemy = GetComponent<EnemyAI>();
        if (enemy != null && enemy.Data != null)
        {
            SetupHealth(enemy.Data.maxHealth);
            return;
        }

        // 3. Если это здание с настроенным ScriptableObject UnitData
        BuildingTypeInfo building = GetComponent<BuildingTypeInfo>();
        if (building != null && building.Data != null)
        {
            SetupHealth(building.Data.maxHealth);
            return;
        }
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

        if (IsDead)
        {
            Die();
        }
    }

    private void Die()
    {
        // Логика смерти (запуск анимации смерти, удаление со сцены и т.д.)
        Destroy(gameObject);
    }
}
