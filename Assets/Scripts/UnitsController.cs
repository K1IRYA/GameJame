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

    private NavMeshAgent agent;
    private Animator animator;
    private Camera mainCamera;
    private float currentHealth;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        mainCamera = Camera.main;

        if (groundLayer == ~0 || groundLayer == 0)
        {
            LayerMask mask = LayerMask.GetMask("Ground");
            if (mask != 0) groundLayer = mask;
        }

        ApplyUnitStats();
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
        HandleInput();
        UpdateAnimations();
    }

    private void HandleInput()
    {
        // Нажатие правой кнопки мыши (ПКМ) для перемещения
        if (Input.GetMouseButtonDown(1))
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                // Проверяем валидность точки на навигационной сетке NavMesh
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 2.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(navHit.position);
                }
                else
                {
                    agent.SetDestination(hit.point);
                }
            }
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
