using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BuilderUnit : MonoBehaviour
{
    [Header("Параметры стройки")]
    public float buildDistance = 3.5f;
    public float buildSpeed = 25f;

    [Header("Параметры добычи ресурсов")]
    public float gatherDistance = 3.5f;
    public float gatherSpeed = 5f; // Количество ресурса в секунду
    public int maxCarryCapacity = 50;
    private int currentCarriedAmount = 0;
    private ResourceType carriedResourceType;
    private bool isReturningToDropOff = false;
    private ResourceDropOff targetDropOff;
    private Vector3 chopAreaCenter;
    private float chopAreaRadius;
    private bool isChoppingArea = false;

    private NavMeshAgent agent;
    private ConstructionSite targetSite;
    private Collider targetCollider;
    private ResourceVein targetVein;
    private UnitsController unitController;
    private float gatherTimer = 0f;
    private Animator animator;
    private static readonly int IsBuildingHash = Animator.StringToHash("IsBuilding");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

    public bool IsSelected => unitController != null && unitController.isSelected;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        unitController = GetComponent<UnitsController>();
    }

    private void Start()
    {
        // Учитываем нового строителя в глобальном менеджере населения
        if (PopulationManager.Instance != null)
        {
            PopulationManager.Instance.RegisterBuilder();
        }
    }

    private void OnDestroy()
    {
        // Освобождаем слот населения при гибели строителя
        if (gameObject.scene.isLoaded && PopulationManager.Instance != null)
        {
            PopulationManager.Instance.UnregisterBuilder();
        }
    }

    private void Update()
    {
        // 1. ЛОГИКА ДОБЫЧИ РЕСУРСОВ
        // 1.1 ФАЗА ВОЗВРАТА НА СКЛАД (работает даже если дерево уже срублено/уничтожено)
        if (isReturningToDropOff)
        {
            if (targetDropOff == null)
            {
                targetDropOff = FindClosestDropOff();
                if (targetDropOff == null)
                {
                    Debug.LogWarning("[BuilderUnit] Склад/База (ResourceDropOff) не найдены на сцене!");
                    return;
                }
            }

            Collider dropCol = targetDropOff.GetComponentInChildren<Collider>();
            Vector3 dropPoint = dropCol != null ? dropCol.ClosestPoint(transform.position) : targetDropOff.transform.position;
            float distToDrop = Vector3.Distance(transform.position, dropPoint);

            if (distToDrop <= targetDropOff.dropOffDistance)
            {
                // Сдаем ресурсы в хаб
                targetDropOff.Deposit(carriedResourceType, currentCarriedAmount);
                currentCarriedAmount = 0;
                isReturningToDropOff = false;
                targetDropOff = null;

                // Если рубим зону деревьев — ищем следующее живое дерево в роще
                if (isChoppingArea)
                {
                    targetVein = FindClosestTreeInArea(chopAreaCenter, chopAreaRadius);
                    if (targetVein == null)
                    {
                        CancelAllTasks(); // Все деревья в округе вырублены
                        return;
                    }

                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        Collider col = targetVein.GetComponentInChildren<Collider>();
                        Vector3 p = col != null ? col.ClosestPoint(transform.position) : targetVein.transform.position;
                        agent.SetDestination(p);
                    }
                }
                // Если копали рудник, но он почему-то пуст (или не бесконечен)
                else if (targetVein == null || targetVein.IsEmpty)
                {
                    CancelAllTasks();
                    return;
                }
                else
                {
                    // Возвращаемся к бесконечной рудной жиле
                    if (agent != null && agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        Collider col = targetVein.GetComponentInChildren<Collider>();
                        Vector3 p = col != null ? col.ClosestPoint(transform.position) : targetVein.transform.position;
                        agent.SetDestination(p);
                    }
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    Vector3 dir = (transform.position - dropPoint).normalized;
                    dir.y = 0f;
                    agent.SetDestination(dropPoint + dir * 1.5f);
                }
            }
            return; // Выходим из Update, пока идем на склад
        }

        // 2. ФАЗА ДОБЫЧИ НА МЕСТЕ (только если жила/дерево существует)
        if (targetVein != null)
        {
            // Если дерево внезапно опустело прямо перед добычей
            if (targetVein.IsEmpty)
            {
                if (currentCarriedAmount > 0)
                {
                    isReturningToDropOff = true;
                    targetDropOff = FindClosestDropOff();
                    return;
                }

                if (isChoppingArea)
                {
                    targetVein = FindClosestTreeInArea(chopAreaCenter, chopAreaRadius);
                    if (targetVein == null)
                    {
                        CancelAllTasks();
                        return;
                    }
                }
                else
                {
                    CancelAllTasks();
                    return;
                }
            }

            Collider veinCol = targetVein.GetComponentInChildren<Collider>();
            Vector3 veinPoint = veinCol != null ? veinCol.ClosestPoint(transform.position) : targetVein.transform.position;
            float distToVein = Vector3.Distance(transform.position, veinPoint);

            if (distToVein <= gatherDistance)
            {
                if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

                Vector3 lookDir = (targetVein.transform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
                }

                gatherTimer += Time.deltaTime;
                if (gatherTimer >= 1f)
                {
                    gatherTimer = 0f;

                    if (animator != null)
                    {
                        animator.SetTrigger(AttackTriggerHash);
                    }

                    int needed = maxCarryCapacity - currentCarriedAmount;
                    int mined = targetVein.ExtractResource(Mathf.Min((int)gatherSpeed, needed));

                    if (mined > 0)
                    {
                        currentCarriedAmount += mined;
                        carriedResourceType = targetVein.resourceType;
                    }

                    // Если рюкзак заполнен (50 ед.) ИЛИ дерево срублено под ноль
                    if (currentCarriedAmount >= maxCarryCapacity || (targetVein != null && targetVein.IsEmpty))
                    {
                        isReturningToDropOff = true;
                        targetDropOff = FindClosestDropOff();
                        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
                    }
                }
            }
            else
            {
                if (agent != null && agent.isOnNavMesh)
                {
                    if (animator != null) animator.SetBool(IsBuildingHash, false);
                    agent.isStopped = false;
                    Vector3 dir = (transform.position - veinPoint).normalized;
                    dir.y = 0f;
                    agent.SetDestination(veinPoint + dir * 0.8f);
                }
            }
            return;
        }

        // 3. ЛОГИКА СТРОИТЕЛЬСТВА ЗДАНИЙ
        if (targetSite == null)
        {
            if (animator != null && animator.GetBool(IsBuildingHash))
            {
                animator.SetBool(IsBuildingHash, false);
            }
            return;
        }

        if (targetSite.IsCompleted)
        {
            CancelAllTasks();
            return;
        }

        Vector3 targetPoint = GetClosestBuildPoint();

        // Расстояние до целевой точки стройки
        float distance = Vector3.Distance(transform.position, targetPoint);

        // Также считаем реальное расстояние до ближайшей внешней грани коллайдера здания
        float distToCollider = float.MaxValue;
        if (targetCollider != null)
        {
            distToCollider = Vector3.Distance(transform.position, targetCollider.ClosestPoint(transform.position));
        }

        // Строим, если подошли к точке ИЛИ если подошли к коллайдеру здания на дистанцию стройки
        bool inRange = distance <= buildDistance || distToCollider <= (buildDistance + 0.5f);

        // Защита от тупика: если агент уперся в забор и больше не может идти вперед
        if (!inRange && agent != null && agent.isOnNavMesh && !agent.pathPending)
        {
            if (agent.remainingDistance <= (agent.stoppingDistance + 0.3f) && distToCollider <= (buildDistance + 2.5f))
            {
                inRange = true;
            }
        }

        if (inRange)
        {
            if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

            if (animator != null) animator.SetBool(IsBuildingHash, true);

            Vector3 lookDir = (targetSite.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }

            targetSite.AddProgress(buildSpeed * Time.deltaTime);
        }
        else
        {
            if (animator != null) animator.SetBool(IsBuildingHash, false);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(targetPoint);
            }
        }
    }


    public void AssignBuildTask(ConstructionSite site)
    {
        CancelAllTasks();
        targetSite = site;
        if (site != null)
        {
            targetCollider = site.GetComponentInChildren<Collider>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(GetClosestBuildPoint());
            }
        }
    }

    public void AssignGatherTask(ResourceVein vein)
    {
        CancelAllTasks();
        targetVein = vein;
        if (agent != null && vein != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            Collider col = vein.GetComponentInChildren<Collider>();
            Vector3 targetPos = col != null ? col.ClosestPoint(transform.position) : vein.transform.position;
            Vector3 dir = (transform.position - targetPos).normalized;
            dir.y = 0f;
            agent.SetDestination(targetPos + dir * 0.8f);
        }
    }

    public void CancelAllTasks()
    {
        if (animator != null) animator.SetBool(IsBuildingHash, false);
        targetSite = null;
        targetCollider = null;
        targetVein = null;
        gatherTimer = 0f;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    // Для совместимости с UnitsController
    public void CancelBuildTask() => CancelAllTasks();

    private Vector3 GetClosestBuildPoint()
    {
        if (targetSite == null) return transform.position;

        Vector3 rawPoint;
        if (targetCollider != null)
        {
            Vector3 closest = targetCollider.ClosestPoint(transform.position);
            Vector3 dir = (transform.position - closest).normalized;
            dir.y = 0f;
            if (dir == Vector3.zero) dir = transform.forward;
            rawPoint = closest + dir * 2.0f;
        }
        else
        {
            Vector3 fromCenter = (transform.position - targetSite.transform.position).normalized;
            fromCenter.y = 0f;
            rawPoint = targetSite.transform.position + fromCenter * 5f;
        }

        // Проецируем точку строго на доступный край NavMesh вокруг повернутого забора
        if (NavMesh.SamplePosition(rawPoint, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return rawPoint;
    }

    // Поиск ближайшей базы со скриптом ResourceDropOff
    private ResourceDropOff FindClosestDropOff()
    {
        ResourceDropOff[] dropOffs = FindObjectsByType<ResourceDropOff>(FindObjectsSortMode.None);
        ResourceDropOff closest = null;
        float minDst = Mathf.Infinity;

        foreach (var drop in dropOffs)
        {
            float dst = Vector3.Distance(transform.position, drop.transform.position);
            if (dst < minDst)
            {
                minDst = dst;
                closest = drop;
            }
        }

        return closest;
    }

    public void AssignChopAreaTask(Vector3 center, float radius)
    {
        CancelAllTasks();
        chopAreaCenter = center;
        chopAreaRadius = radius;
        isChoppingArea = true;

        targetVein = FindClosestTreeInArea(center, radius);
        if (targetVein != null && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            Collider col = targetVein.GetComponentInChildren<Collider>();
            Vector3 p = col != null ? col.ClosestPoint(transform.position) : targetVein.transform.position;
            agent.SetDestination(p);
        }
    }

    private ResourceVein FindClosestTreeInArea(Vector3 center, float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(center, radius);
        ResourceVein closest = null;
        float minDst = Mathf.Infinity;

        foreach (var col in colliders)
        {
            ResourceVein vein = col.GetComponentInParent<ResourceVein>();
            if (vein != null && vein.resourceType == ResourceType.Wood && !vein.IsEmpty)
            {
                float dst = Vector3.Distance(transform.position, vein.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closest = vein;
                }
            }
        }

        return closest;
    }
}
