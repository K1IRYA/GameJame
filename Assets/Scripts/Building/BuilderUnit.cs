using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BuilderUnit : MonoBehaviour
{
    [Header("Параметры стройки")]
    public float buildDistance = 2.5f;
    public float buildSpeed = 25f;

    private NavMeshAgent agent;
    private ConstructionSite targetSite;
    private Collider targetCollider;
    private UnitsController unitController;

    public bool IsSelected
    {
        get
        {
            if (unitController != null)
                return unitController.isSelected;
            return false;
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        unitController = GetComponent<UnitsController>();
    }

    private void Update()
    {
        if (targetSite == null) return;

        // Если здание уже достроено
        if (targetSite.IsCompleted)
        {
            CancelBuildTask();
            return;
        }

        // Вычисляем точку у внешнего края фундамента
        Vector3 targetPoint = GetClosestBuildPoint();
        float distance = Vector3.Distance(transform.position, targetPoint);

        if (distance <= buildDistance)
        {
            // Подошли — останавливаемся и строим
            agent.isStopped = true;

            // Поворачиваемся лицом к стройке
            Vector3 lookDirection = (targetSite.transform.position - transform.position).normalized;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 5f);
            }

            targetSite.AddProgress(buildSpeed * Time.deltaTime);
        }
        else
        {
            // Бежим к внешнему краю
            agent.isStopped = false;
            agent.SetDestination(targetPoint);
        }
    }

    public void AssignBuildTask(ConstructionSite site)
    {
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

    // Метод отмены задачи постройки при обычном клике ПКМ
    public void CancelBuildTask()
    {
        targetSite = null;
        targetCollider = null;
        if (agent != null)
        {
            agent.isStopped = false;
        }
    }

    private Vector3 GetClosestBuildPoint()
    {
        if (targetSite == null) return transform.position;

        if (targetCollider != null)
        {
            Vector3 closest = targetCollider.ClosestPoint(transform.position);
            Vector3 dir = (transform.position - closest).normalized;
            dir.y = 0f;
            return closest + dir * 1.2f;
        }

        Vector3 fromCenter = (transform.position - targetSite.transform.position).normalized;
        fromCenter.y = 0f;
        return targetSite.transform.position + fromCenter * 3f;
    }
}
