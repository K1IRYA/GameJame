using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitsController : MonoBehaviour
{
    [Header("Stats Configuration")]
    [SerializeField] private UnitData unitData;

    [Header("Movement & Input")]
    [Tooltip("Слой поверхности земли для кликов мышью")]
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Visual Feedback")]
    [Tooltip("Объект-круг под ногами")]
    [SerializeField] private GameObject selectionCircle;

    [Header("Movement Marker")]
    [Tooltip("Префаб маркера (например, кружок или стрелочка)")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [Tooltip("Дистанция до цели, при которой маркер скрывается")]
    [SerializeField] private float stopMarkerDistance = 0.5f;

    // Публичный статус выделения для BuilderUnit и других систем
    public bool isSelected { get; private set; } = false;

    private NavMeshAgent agent;
    private Animator animator;
    private GameObject currentMarker;
    private bool isMovingToTarget = false;
    private bool isAttackMoving = false;
    private Vector3 attackMoveDestination;
    private MeleeAttack meleeAttack;
    private float scanTimer = 0f;
    private const float ScanInterval = 0.25f;
    [SerializeField] private float aggroScanRadius = 10f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        meleeAttack = GetComponent<MeleeAttack>();

        ApplyUnitStats();
        SetSelected(false);
    }

    private void ApplyUnitStats()
    {
        if (unitData == null)
        {
            Debug.LogWarning($"[UnitController] На объекте {gameObject.name} не назначен UnitData!", this);
            return;
        }

        // Применяем параметры скорости и поворота из ScriptableObject в NavMeshAgent
        agent.speed = unitData.moveSpeed;
        agent.angularSpeed = unitData.angularSpeed;
        agent.acceleration = unitData.acceleration;

     
    }

    private void Update()
    {
        UpdateAnimations();
        CheckDestinationReached();
        UpdateAttackMove();
    }



    public void MoveTo(Vector3 destination)
    {
        isAttackMoving = false;
        if (meleeAttack != null)
        {
            meleeAttack.StopAttacking();
        }

        if (agent != null && agent.isOnNavMesh)
        {
            // Снимаем возможную паузу агента
            agent.isStopped = false;

            // Если на юните есть компонент строителя — отменяем задачу стройки при обычном движении
            BuilderUnit builder = GetComponent<BuilderUnit>();
            if (builder != null)
            {
                builder.CancelBuildTask();
            }

            agent.SetDestination(destination);
            isMovingToTarget = true;

            if (currentMarker == null && moveMarkerPrefab != null)
            {
                currentMarker = Instantiate(moveMarkerPrefab);
            }

            if (currentMarker != null)
            {
                currentMarker.transform.position = destination + new Vector3(0f, 0.05f, 0f);
                currentMarker.SetActive(true);
            }
        }
    }

    private void CheckDestinationReached()
    {
        if (!isMovingToTarget) return;

        // Когда юнит дошел до точки назначения
        if (!agent.pathPending && agent.remainingDistance <= stopMarkerDistance)
        {
            isMovingToTarget = false; // Юнит дошел, цель сброшена

            if (currentMarker != null)
            {
                currentMarker.SetActive(false); // Скрываем маркер
            }
        }
    }

    // Если юнита уничтожат, удаляем и его маркер
    private void OnDestroy()
    {
        if (currentMarker != null)
        {
            Destroy(currentMarker);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionCircle != null)
        {
            selectionCircle.SetActive(selected);
        }
        if (currentMarker != null)
        {
            currentMarker.SetActive(selected && isMovingToTarget);
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Нормализуем скорость относительно максимальной (диапазон 0..1)
        float currentSpeed = agent.velocity.magnitude / (agent.speed > 0f ? agent.speed : 1f);

        // Передаем скорость в Animator с плавным сглаживанием
        animator.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);
    }

    // Свойства для внешнего доступа
    public UnitData Data => unitData;

    public void AttackMove(Vector3 destination)
    {
        BuilderUnit builder = GetComponent<BuilderUnit>();
        if (builder != null)
        {
            builder.CancelBuildTask();
        }

        if (meleeAttack != null)
        {
            meleeAttack.StopAttacking();
        }

        isAttackMoving = true;
        attackMoveDestination = destination;
        scanTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
            isMovingToTarget = true;

            if (currentMarker == null && moveMarkerPrefab != null)
            {
                currentMarker = Instantiate(moveMarkerPrefab);
            }

            if (currentMarker != null)
            {
                currentMarker.transform.position = destination + new Vector3(0f, 0.05f, 0f);
                currentMarker.SetActive(true);
            }
        }
    }
    private void UpdateAttackMove()
    {
        if (!isAttackMoving) return;

        // Если в данный момент юнит уже занят атакой живого врага
        if (meleeAttack != null && meleeAttack.HasTarget)
        {
            return;
        }

        // Сканируем пространство вокруг с интервалом
        scanTimer += Time.deltaTime;
        if (scanTimer >= ScanInterval)
        {
            scanTimer = 0f;
            Health nearestEnemy = FindNearestEnemy(aggroScanRadius);

            if (nearestEnemy != null && meleeAttack != null)
            {
                // Враг найден — переключаемся на атаку
                meleeAttack.AttackTarget(nearestEnemy);
                return;
            }
        }

        // Если врагов поблизости нет, продолжаем путь к изначальной точке A-клика
        if (agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.destination != attackMoveDestination)
            {
                agent.isStopped = false;
                agent.SetDestination(attackMoveDestination);
                isMovingToTarget = true;
            }

            if (agent.remainingDistance <= stopMarkerDistance)
            {
                isAttackMoving = false;
                isMovingToTarget = false;
                if (currentMarker != null) currentMarker.SetActive(false);
            }
        }
    }
    private Health FindNearestEnemy(float radius)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        Health bestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            Health hp = hit.GetComponentInParent<Health>();
            if (hp == null || hp.IsDead) continue;

            EnemyAI enemyAI = hp.GetComponent<EnemyAI>();
            if (enemyAI == null) continue;

            float dist = Vector3.Distance(transform.position, hp.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestEnemy = hp;
            }
        }

        return bestEnemy;
    }
}
