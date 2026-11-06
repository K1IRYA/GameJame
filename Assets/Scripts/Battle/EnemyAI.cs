using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Статблок врага")]
    [SerializeField] private UnitData enemyData;

    // Публичное свойство, чтобы Health.cs и RTSController могли прочитать фракцию и статы врага
    public UnitData Data => enemyData;
    public enum TargetPriority { TownHall, Builder, Tower, Wall, OtherBuilding }

    [Header("Настройки ИИ и сканирования")]
    [SerializeField] private float scanRadius = 15f;       // Радиус, в котором враг замечает гномов/башни
    [SerializeField] private LayerMask targetLayers;       // Слои для сканирования (например, Default, Building)

    private NavMeshAgent agent;
    private Health health;
    private Health currentTargetHealth;
    private float nextAttackTime;
    private RangedAttack rangedAttackComponent;
    private MeleeAttack meleeAttackComponent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        rangedAttackComponent = GetComponent<RangedAttack>();
        meleeAttackComponent = GetComponent<MeleeAttack>();

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
    private void Start()
    {
        StartCoroutine(TargetScanningRoutine());
    }
    private System.Collections.IEnumerator TargetScanningRoutine()
    {
        while (!health.IsDead)
        {
            FindBestPriorityTarget();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void Update()
    {
        if (health.IsDead) return;

        // Если цели нет или она погибла, останавливаемся
        if (currentTargetHealth == null || currentTargetHealth.IsDead)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, currentTargetHealth.transform.position);
        float attackRange = enemyData != null ? enemyData.attackRange : 2f;

        // Фаза атаки
        if (distance <= attackRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            // Поворот к цели
            Vector3 lookDir = (currentTargetHealth.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);
            }
            // Универсальная атака: Дальний бой или Ближний бой через профильные компоненты
            if (rangedAttackComponent != null)
            {
                rangedAttackComponent.AttackTargetFromAI(currentTargetHealth);
            }
            else if (meleeAttackComponent != null)
            {
                meleeAttackComponent.AttackTargetFromAI(currentTargetHealth);
            }
            else if (Time.time >= nextAttackTime)
            {
                // Резервный хардкод урона, если забыли повесить боевой компонент
                nextAttackTime = Time.time + (enemyData != null ? enemyData.attackCooldown : 1f);
                currentTargetHealth.TakeDamage(enemyData != null ? enemyData.attackDamage : 10f);

                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Attack");
            }
        }
        // Фаза преследования
        else
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;

                // Корректируем точку назначения: если цель — постройка, ищем ближайшую валидную точку на синей сетке NavMesh
                Vector3 targetPosition = currentTargetHealth.transform.position;
                if (currentTargetHealth.GetComponent<BuildingTypeInfo>() != null)
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(targetPosition, out hit, 20f, NavMesh.AllAreas))
                    {
                        targetPosition = hit.position;
                    }
                }

                agent.SetDestination(targetPosition);

                // ПРОВЕРКА НА ПРЕПЯТСТВИЯ: Если агент упёрся или путь частичный (заблокирован стеной)
                if (agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial || (agent.hasPath && agent.velocity.sqrMagnitude < 0.1f))
                {
                    // Пускаем короткий луч вперед на уровне пояса врага для поиска забора
                    Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
                    RaycastHit hit;

                    if (Physics.Raycast(rayOrigin, transform.forward, out hit, 2.5f, targetLayers))
                    {
                        BuildingTypeInfo buildingInfo = hit.collider.GetComponentInParent<BuildingTypeInfo>();
                        // Если впереди забор или обычная постройка, мешающая пройти
                        if (buildingInfo != null && (buildingInfo.Type == BuildingTypeInfo.BuildingType.Wall || buildingInfo.Type == BuildingTypeInfo.BuildingType.OtherBuilding))
                        {
                            Health obstacleHealth = buildingInfo.GetComponentInParent<Health>();
                            if (obstacleHealth != null && !obstacleHealth.IsDead)
                            {
                                // Переключаем ярость на забор перед носом
                                currentTargetHealth = obstacleHealth;
                                agent.isStopped = true;
                            }
                        }
                    }
                }
            }
        }
    }
    private void FindBestPriorityTarget()
    {
        // Удерживаем цель БЕЗ пересканирования ТОЛЬКО если это живой Гном или атакующая Башня
        if (currentTargetHealth != null && !currentTargetHealth.IsDead)
        {
            bool isTargetBuilder = currentTargetHealth.GetComponent<BuilderUnit>() != null || currentTargetHealth.GetComponentInParent<BuilderUnit>() != null;
            BuildingTypeInfo currentBuilding = currentTargetHealth.GetComponent<BuildingTypeInfo>() ?? currentTargetHealth.GetComponentInParent<BuildingTypeInfo>();
            bool isTargetTower = currentBuilding != null && currentBuilding.Type == BuildingTypeInfo.BuildingType.Tower;

            if (isTargetBuilder || isTargetTower)
            {
                return; // Спокойно продолжаем бить гнома/башню, не отвлекаясь ни на что другое
            }
        }

        Collider[] colliders = Physics.OverlapSphere(transform.position, scanRadius, targetLayers);

        Health bestBuilder = null;
        Health bestTower = null;
        Health globalBase = null;

        float minBuilderDist = float.MaxValue;
        float minTowerDist = float.MaxValue;

        foreach (var col in colliders)
        {
            Health h = col.GetComponentInParent<Health>();
            if (h == null || h.IsDead) continue;

            // 1. Проверяем, строитель ли это (Гном)
            if (col.GetComponentInParent<BuilderUnit>() != null)
            {
                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist < minBuilderDist) { minBuilderDist = dist; bestBuilder = h; }
                continue;
            }
            // 2. Проверяем тип постройки
            BuildingTypeInfo building = col.GetComponentInParent<BuildingTypeInfo>();
            if (building != null)
            {
                if (building.Type == BuildingTypeInfo.BuildingType.Tower)
                {
                    float dist = Vector3.Distance(transform.position, h.transform.position);
                    if (dist < minTowerDist) { minTowerDist = dist; bestTower = h; }
                }
                else if (building.Type == BuildingTypeInfo.BuildingType.TownHall)
                {
                    globalBase = h; // Ратуша — глобальная цель
                }
            }
        }
        // Выставляем цель строго по приоритету: Строитель -> Башня -> Ратуша
        if (bestBuilder != null) currentTargetHealth = bestBuilder;
        else if (bestTower != null) currentTargetHealth = bestTower;
        else if (globalBase != null) currentTargetHealth = globalBase;
        else
        {
            // 1. Сначала ищем стандартно по компоненту выгрузки
            var dropOff = Object.FindAnyObjectByType<ResourceDropOff>();
            if (dropOff != null)
            {
                currentTargetHealth = dropOff.GetComponent<Health>();
                if (currentTargetHealth == null) currentTargetHealth = dropOff.GetComponentInParent<Health>();
            }

            // 2. РЕЗЕРВНЫЙ ВАРИАНТ: Если компонент не нашелся, жестко ищем объект по имени "base"
            if (currentTargetHealth == null)
            {
                GameObject baseGo = GameObject.Find("base");
                if (baseGo != null)
                {
                    currentTargetHealth = baseGo.GetComponent<Health>();
                    if (currentTargetHealth == null) currentTargetHealth = baseGo.GetComponentInChildren<Health>();
                }
            }
        }
    }
    
}
