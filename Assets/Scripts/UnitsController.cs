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
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
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
