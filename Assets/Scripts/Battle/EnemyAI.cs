using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Статблок врага")]
    [SerializeField] private UnitData enemyData;
    public UnitData Data => enemyData;

    [Header("Настройки сканирования")]
    [SerializeField] private float scanRadius = 15f;
    [SerializeField] private LayerMask targetLayers;

    private NavMeshAgent agent;
    private Health health;
    private Health currentTarget;
    private float nextAttackTime;

    private MeleeAttack meleeAttack;
    private RangedAttack rangedAttack;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        meleeAttack = GetComponent<MeleeAttack>();
        rangedAttack = GetComponent<RangedAttack>();

        if (enemyData != null)
        {
            if (agent != null)
            {
                agent.speed = enemyData.moveSpeed;
                agent.angularSpeed = enemyData.angularSpeed;
                agent.acceleration = enemyData.acceleration;
                agent.stoppingDistance = enemyData.attackRange - 0.2f;
            }
            if (health != null) health.SetupHealth(enemyData.maxHealth);
        }
    }

    private void Start()
    {
        StartCoroutine(ScanningRoutine());
    }

    private System.Collections.IEnumerator ScanningRoutine()
    {
        while (!health.IsDead)
        {
            ChooseBestTarget();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void ChooseBestTarget()
    {
        // 1. УДЕРЖАНИЕ СТЕН И РАТУШИ: Если уже штурмуем базу или ломаем забор — не пересчитываем цель, пока не уничтожим
        if (currentTarget != null && !currentTarget.IsDead)
        {
            BuildingTypeInfo bInfo = currentTarget.GetComponent<BuildingTypeInfo>() ?? currentTarget.GetComponentInParent<BuildingTypeInfo>();
            if (bInfo != null && (bInfo.Type == BuildingTypeInfo.BuildingType.Wall || bInfo.Type == BuildingTypeInfo.BuildingType.OtherBuilding || bInfo.Type == BuildingTypeInfo.BuildingType.TownHall))
            {
                return;
            }
        }

        // 2. СКАНИРОВАНИЕ ОКРУЖЕНИЯ: Собираем объекты по маске слоев
        Collider[] colliders = Physics.OverlapSphere(transform.position, scanRadius, targetLayers);
        Health bestBuilder = null;
        Health bestTower = null;
        Health globalBase = null;

        float minBuilderDist = float.MaxValue;
        float minTowerDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            Health h = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;

            // Ищем гномов-строителей
            if (col.GetComponent<BuilderUnit>() != null || col.GetComponentInParent<BuilderUnit>() != null)
            {
                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist < minBuilderDist) { minBuilderDist = dist; bestBuilder = h; }
                continue;
            }

            // Ищем здания по типам
            BuildingTypeInfo building = col.GetComponent<BuildingTypeInfo>() ?? col.GetComponentInParent<BuildingTypeInfo>();
            if (building != null)
            {
                if (building.Type == BuildingTypeInfo.BuildingType.Tower)
                {
                    float dist = Vector3.Distance(transform.position, h.transform.position);
                    if (dist < minTowerDist) { minTowerDist = dist; bestTower = h; }
                }
                else if (building.Type == BuildingTypeInfo.BuildingType.TownHall)
                {
                    globalBase = h;
                }
            }
        }

        // 3. СТРОГИЙ ВЫБОР ПРИОРИТЕТОВ ИЗ ТЗ: Ратуша -> Строители -> Башни
        if (globalBase != null)
        {
            currentTarget = globalBase;
        }
        else
        {
            // Резервный глобальный поиск Ратуши, если она за пределами радиуса OverlapSphere
            var dropOff = Object.FindAnyObjectByType<ResourceDropOff>();
            if (dropOff != null) globalBase = dropOff.GetComponent<Health>() ?? dropOff.GetComponentInParent<Health>();

            if (globalBase == null)
            {
                GameObject baseGo = GameObject.Find("base") ?? GameObject.Find("base(Clone)");
                if (baseGo != null) globalBase = baseGo.GetComponent<Health>() ?? baseGo.GetComponentInChildren<Health>();
            }

            if (globalBase != null && !globalBase.IsDead)
            {
                currentTarget = globalBase;
            }
            else if (bestBuilder != null)
            {
                currentTarget = bestBuilder;
            }
            else if (bestTower != null)
            {
                currentTarget = bestTower;
            }
        }

        Debug.Log($"[EnemyAI] Моя текущая цель по ТЗ: {(currentTarget != null ? currentTarget.gameObject.name : "НИКОГО НЕ НАШЕЛ")}");
    }

    private void Update()
    {
        if (health.IsDead || currentTarget == null || currentTarget.IsDead)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        // ДИНАМИЧЕСКИЙ ПЕРЕХВАТ СТЕН И ЗАБОРОВ НА ПУТИ К ЦЕЛИ
        bool hasObstacle = false;
        Vector3 checkPoint = transform.position;

        // ИЗМЕНЕНО: Включаем логику проламывания стен ТОЛЬКО когда физически подошли к базе/гному ближе scanRadius (15 метров)
        float currentDistanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (currentDistanceToTarget <= scanRadius)
        {
            Vector3 startPoint = transform.position + Vector3.up * 0.5f;
            Vector3 endPoint = currentTarget.transform.position + Vector3.up * 0.5f;
            RaycastHit hit;

            // 1. Проверяем прямую физическую линию видимости вблизи
            if (Physics.Linecast(startPoint, endPoint, out hit, ~0))
            {
                Health obsHealth = hit.collider.GetComponent<Health>() ?? hit.collider.GetComponentInParent<Health>();
                if (obsHealth != null && obsHealth.gameObject != currentTarget.gameObject && !obsHealth.IsDead)
                {
                    if (hit.collider.GetComponent<EnemyAI>() == null && hit.collider.GetComponentInParent<EnemyAI>() == null)
                    {
                        hasObstacle = true;
                        checkPoint = hit.point;
                    }
                }
            }

            // 2. РЕЗЕРВНЫЙ АВАРИЙНЫЙ ТРИГГЕР: Если буксуем на месте вплотную к забору
            if (!hasObstacle && agent != null && agent.isOnNavMesh && agent.hasPath && agent.velocity.sqrMagnitude < 0.1f)
            {
                float currentAttackRange = enemyData != null ? enemyData.attackRange : 2f;
                if (currentDistanceToTarget > currentAttackRange)
                {
                    hasObstacle = true;
                    checkPoint = transform.position + transform.forward * 1.5f;
                }
            }

            // Если преграда зафиксирована под носом — переключаемся на забор
            if (hasObstacle)
            {
                Collider[] hitColliders = Physics.OverlapSphere(checkPoint, 3.5f, ~0);
                foreach (var col in hitColliders)
                {
                    if (col.gameObject == gameObject) continue;

                    Health wallHealth = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();
                    if (wallHealth != null && !wallHealth.IsDead && wallHealth.gameObject != currentTarget.gameObject)
                    {
                        if (col.GetComponent<EnemyAI>() == null && col.GetComponentInParent<EnemyAI>() == null)
                        {
                            currentTarget = wallHealth;
                            if (agent != null && agent.isOnNavMesh)
                            {
                                agent.isStopped = false;
                                agent.SetDestination(currentTarget.transform.position);
                            }
                            break;
                        }
                    }
                }
            }
        }

        // БОЕВОЙ ЦИКЛ: ДВИЖЕНИЕ И АТАКА
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float attackRange = enemyData != null ? enemyData.attackRange : 2f;
        // Если бьем здание — считаем расстояние до ближайшей точки его физического коллайдера
        Collider targetCollider = currentTarget.GetComponent<Collider>() ?? currentTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null && currentTarget.GetComponent<BuildingTypeInfo>() != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            distance = Vector3.Distance(transform.position, closestPoint);
        }

        if (distance <= attackRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            Vector3 lookDir = (currentTarget.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            if (rangedAttack != null)
            {
                rangedAttack.AttackTargetFromAI(currentTarget);
            }
            else if (meleeAttack != null)
            {
                meleeAttack.AttackTargetFromAI(currentTarget);
            }
            else if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + (enemyData != null ? enemyData.attackCooldown : 1f);
                currentTarget.TakeDamage(enemyData != null ? enemyData.attackDamage : 10f);

                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Attack");
            }
        }
        else
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;

                Vector3 targetPosition = currentTarget.transform.position;

                // КОРРЕКЦИЯ ДЛЯ ЗДАНИЙ: Если цель — Ратуша или башня, бежим к ближайшей к ней точке на синей сетке NavMesh
                BuildingTypeInfo targetBuilding = currentTarget.GetComponent<BuildingTypeInfo>() ?? currentTarget.GetComponentInParent<BuildingTypeInfo>();
                if (targetBuilding != null && targetBuilding.Type == BuildingTypeInfo.BuildingType.TownHall)
                {
                    NavMeshHit navHit;
                    if (NavMesh.SamplePosition(targetPosition, out navHit, 30f, NavMesh.AllAreas))
                    {
                        targetPosition = navHit.position;
                    }
                }

                agent.SetDestination(targetPosition);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}
