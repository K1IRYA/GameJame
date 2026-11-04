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
    private bool isSelected = false;

    [Header("Movement Marker")]
    [Tooltip("Префаб маркера (например, кружок или стрелочка)")]
    [SerializeField] private GameObject moveMarkerPrefab;
    [Tooltip("Дистанция до цели, при которой маркер скрывается")]
    [SerializeField] private float stopMarkerDistance = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private Camera mainCamera;
    private float currentHealth;
    private GameObject currentMarker;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;

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

        // Инициализируем здоровье
        currentHealth = unitData.maxHealth;
    }

    private void Update()
    {
        UpdateAnimations();
        CheckDestinationReached();
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(destination);

            if (currentMarker == null && moveMarkerPrefab != null)
            {
                currentMarker = Instantiate(moveMarkerPrefab);
            }

            // Перемещаем маркер в новую точку (чуть приподнимаем над землей)
            if (currentMarker != null)
            {
                currentMarker.transform.position = destination + new Vector3(0f, 0.05f, 0f);
                currentMarker.SetActive(true);
            }
        }
    }

    private void CheckDestinationReached()
    {
        if (currentMarker == null || !currentMarker.activeSelf) return;

        // Проверяем, остался ли у агента путь, и дошел ли он до конечной точки
        if (!agent.pathPending && agent.remainingDistance <= stopMarkerDistance)
        {
            currentMarker.SetActive(false); // Прячем маркер, когда цель достигнута
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
    }

    private void UpdateAnimations()
    {
        // Нормализуем скорость относительно максимальной (диапазон 0..1)
        float currentSpeed = agent.velocity.magnitude / (agent.speed > 0f ? agent.speed : 1f);

        // Передаем скорость в Animator с плавным сглаживанием
        animator.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);
    }

    // Свойства для внешнего доступа
    public UnitData Data => unitData;
    public float CurrentHealth => currentHealth;
}
