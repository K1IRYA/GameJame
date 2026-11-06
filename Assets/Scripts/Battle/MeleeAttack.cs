using UnityEngine;
using UnityEngine.AI;

public class MeleeAttack : MonoBehaviour
{
    private UnitsController boundUnit;
    private UnitsController targetEnemy;
    private Health targetEnemyHealth;
    private NavMeshAgent agent;
    private Animator animator;

    private float nextAttackTime = 0f;

    // Параметры, которые будут динамически подтягиваться из UnitData
    private float currentDamage;
    private float currentRange;
    private float currentCooldown;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        boundUnit = GetComponent<UnitsController>();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Инициализируем базовые дистанции из настроек данных юнита
        if (boundUnit != null && boundUnit.Data != null)
        {
            currentRange = boundUnit.Data.attackRange;
            currentDamage = boundUnit.Data.attackDamage;
            currentCooldown = boundUnit.Data.attackCooldown;
        }
    }

    private void Update()
    {
        // Если враг погиб, сбрасываем состояние
        if (targetEnemyHealth == null) return;

        // ИЗМЕНЕНО: проверяем смерть через компонент Health
        if (targetEnemyHealth.IsDead)
        {
            StopAttacking();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetEnemyHealth.transform.position);

        // Фаза нанесения урона
        if (distance <= currentRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            // Разворот к противнику
            Vector3 lookDir = (targetEnemyHealth.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }

            // Проверка перезарядки удара
            if (Time.time >= nextAttackTime)
            {
                ExecuteAttack();
            }
        }
        // Фаза погони за целью
        else
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetEnemyHealth.transform.position);
            }
        }
    }

    public void AttackTarget(Health enemyHealth)
    {
        if (enemyHealth == null || enemyHealth.IsDead) return;
        Debug.Log($"[MeleeAttack] Гном получил приказ атаковать: {enemyHealth.gameObject.name}");
        if (enemyHealth == null || enemyHealth.IsDead) return;

        BuilderUnit builder = GetComponent<BuilderUnit>();
        if (builder != null) builder.CancelAllTasks();

        targetEnemyHealth = enemyHealth;
    }

    private void ExecuteAttack()
    {
        nextAttackTime = Time.time + currentCooldown;

        if (animator != null)
        {
            animator.SetTrigger(AttackTriggerHash);
        }

        if (targetEnemyHealth != null)
        {
            targetEnemyHealth.TakeDamage(currentDamage);
        }
    }

    public void StopAttacking()
    {
        targetEnemy = null;
        if (agent != null && agent.isOnNavMesh && agent.isStopped)
        {
            agent.isStopped = false;
        }
    }
}
