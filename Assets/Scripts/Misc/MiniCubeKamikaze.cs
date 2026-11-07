using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MiniCubeKamikaze : MonoBehaviour
{
    // Глобальный счетчик камикадзе на сцене
    public static int CurrentActiveCount { get; private set; } = 0;

    [Header("Параметры камикадзе")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float detonateDistance = 1.5f; // Дистанция подрыва вплотную

    private NavMeshAgent agent;
    private Health targetWall;
    private bool exploded = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
            agent.stoppingDistance = 0.1f;
        }
    }

    private void Start()
    {
        CurrentActiveCount++; // Увеличиваем счетчик при появлении
        FindNearestWall();
    }

    private void FindNearestWall()
    {
        BuildingTypeInfo[] allBuildings = Object.FindObjectsByType<BuildingTypeInfo>(FindObjectsSortMode.None);
        float minDistance = float.MaxValue;
        BuildingTypeInfo nearestBuilding = null;

        foreach (var b in allBuildings)
        {
            if (b.Type == BuildingTypeInfo.BuildingType.Wall || b.Type == BuildingTypeInfo.BuildingType.OtherBuilding)
            {
                Health wHealth = b.GetComponent<Health>() ?? b.GetComponentInParent<Health>();
                if (wHealth != null && !wHealth.IsDead)
                {
                    float dist = Vector3.Distance(transform.position, b.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearestBuilding = b;
                    }
                }
            }
        }

        if (nearestBuilding != null)
        {
            targetWall = nearestBuilding.GetComponent<Health>() ?? nearestBuilding.GetComponentInParent<Health>();
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(nearestBuilding.transform.position);
            }
        }
        else
        {
            GameObject baseGo = GameObject.Find("base") ?? GameObject.Find("base(Clone)");
            if (baseGo != null)
            {
                targetWall = baseGo.GetComponent<Health>();
                if (agent != null && agent.isOnNavMesh) agent.SetDestination(baseGo.transform.position);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void Update()
    {
        if (exploded) return;

        if (targetWall == null || targetWall.IsDead)
        {
            FindNearestWall();
            return;
        }

        // БРОНЕБОЙНАЯ ПРОВЕРКА ДИСТАНЦИИ: Если подбежали к нашей цели вплотную
        float distToWall = Vector3.Distance(transform.position, targetWall.transform.position);
        if (distToWall <= detonateDistance)
        {
            ExplodeAndDestroyTarget();
        }
    }

    private void ExplodeAndDestroyTarget()
    {
        if (exploded) return;
        exploded = true;

        // Находим самый корень префаба цели, чтобы удалить секцию целиком вместе с бордюром
        int buildingLayerIndex = LayerMask.NameToLayer("Building");
        GameObject objectToDestroy = targetWall.gameObject;

        if (targetWall.transform.parent != null && targetWall.transform.parent.gameObject.layer == buildingLayerIndex)
        {
            objectToDestroy = targetWall.transform.parent.gameObject;
        }
        else if (targetWall.transform.root != null && targetWall.transform.root.gameObject != targetWall.gameObject)
        {
            // Проверяем, чтобы корнем не оказался глобальный контейнер всей карты
            if (!targetWall.transform.root.name.Contains("Container") && !targetWall.transform.root.name.Contains("Root"))
            {
                objectToDestroy = targetWall.transform.root.gameObject;
            }
        }

        Debug.Log($"[Kamikaze] ДЕТОНАЦИЯ ПО ДИСТАНЦИИ! Уничтожаю цель: {objectToDestroy.name}");

        // Полностью дезинтегрируем забор и стираем мини-куб
        Destroy(objectToDestroy);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        CurrentActiveCount = Mathf.Max(0, CurrentActiveCount - 1);
    }
}
