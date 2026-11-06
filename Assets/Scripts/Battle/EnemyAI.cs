using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Статблок врага")]
    [SerializeField] private UnitData enemyData;

    // Публичное свойство, чтобы Health.cs и RTSController могли прочитать фракцию и статы врага
    public UnitData Data => enemyData;

    private void Awake()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        Health health = GetComponent<Health>();

        if (enemyData != null)
        {
            // Настраиваем скорость агента из статблока
            if (agent != null)
            {
                agent.speed = enemyData.moveSpeed;
                agent.angularSpeed = enemyData.angularSpeed;
                agent.acceleration = enemyData.acceleration;
            }

            // Настраиваем максимальное здоровье врага из статблока
            if (health != null)
            {
                health.SetupHealth(enemyData.maxHealth);
            }
        }
    }
}
