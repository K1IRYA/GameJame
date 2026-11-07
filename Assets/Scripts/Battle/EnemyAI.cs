using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private UnitData enemyData;
    public UnitData Data => enemyData;

    [Header("Поиск целей")]
    [SerializeField, Min(1f)] private float marchDetectionDistance = 10f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField, Min(0.1f)] private float decisionInterval = 0.4f;

    public Health CurrentTarget => currentTarget;
    public string State { get; private set; } = "Waiting";

    private NavMeshAgent agent;
    private Health ownHealth;
    private MeleeAttack meleeAttack;
    private RangedAttack rangedAttack;
    private Animator animator;
    private Health currentTarget;
    private Vector3 destination;
    private float nextDecision;
    private float nextAttack;
    private NavMeshPath path;
    private readonly HashSet<Health> candidates = new HashSet<Health>();

    private float AttackRange => Mathf.Max(0.2f, enemyData != null ? enemyData.attackRange : 2f);
    private int TargetMask => targetLayers.value != 0 ? targetLayers.value : (1 << 6) | (1 << 9);
    private bool CanNavigate => agent != null && agent.enabled && agent.isOnNavMesh;

    private void Awake()
    {
        path = new NavMeshPath();
        agent = GetComponent<NavMeshAgent>();
        ownHealth = GetComponent<Health>();
        meleeAttack = GetComponent<MeleeAttack>();
        rangedAttack = GetComponent<RangedAttack>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (enemyData != null)
        {
            agent.speed = Mathf.Max(0f, enemyData.moveSpeed);
            agent.angularSpeed = Mathf.Max(0f, enemyData.angularSpeed);
            agent.acceleration = Mathf.Max(0.1f, enemyData.acceleration);
        }
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = true;
        agent.autoRepath = true;
        nextDecision = Time.time;
    }

    private void Update()
    {
        if (ownHealth != null && ownHealth.IsDead)
        {
            currentTarget = null;
            StopMoving();
            if (agent != null) agent.enabled = false;
            State = "Dead";
            return;
        }
        if (!CanNavigate)
        {
            State = "OffNavMesh";
            return;
        }

        if (Time.time >= nextDecision || !IsHostile(currentTarget))
        {
            // An empty battlefield must not trigger a scene-wide search every frame.
            if (Time.time >= nextDecision)
            {
                nextDecision = Time.time + Mathf.Max(0.1f, decisionInterval);
                ChooseTarget();
            }
            if (!IsHostile(currentTarget))
            {
                currentTarget = null;
                if (State != "ApproachingBlockade" || !agent.hasPath)
                {
                    StopMoving();
                    State = "Waiting";
                }
                return;
            }
        }

        Vector3 point = ClosestPoint(currentTarget, transform.position);
        if (FlatDistance(transform.position, point) <= AttackRange && HasClearAttack(currentTarget))
        {
            StopMoving();
            State = "Attacking";
            Vector3 direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), agent.angularSpeed * Time.deltaTime);

            if (rangedAttack != null && rangedAttack.enabled)
                rangedAttack.AttackTargetFromAI(currentTarget);
            else if (meleeAttack != null && meleeAttack.enabled)
                meleeAttack.AttackTargetFromAI(currentTarget);
            else if (Time.time >= nextAttack)
            {
                nextAttack = Time.time + Mathf.Max(0.1f, enemyData != null ? enemyData.attackCooldown : 1f);
                currentTarget.TakeDamage(enemyData != null ? enemyData.attackDamage : 10f);
            }
        }
        else
        {
            State = "Moving";
            agent.isStopped = false;
        }
    }

    private void ChooseTarget()
    {
        candidates.Clear();
        Health townHall = TownHallRegister.BaseHealth;
        if (IsHostile(townHall)) candidates.Add(townHall);
        foreach (Collider col in Physics.OverlapSphere(transform.position, marchDetectionDistance, TargetMask, QueryTriggerInteraction.Ignore))
        {
            Health health = col.GetComponentInParent<Health>();
            if (IsHostile(health)) candidates.Add(health);
        }

        Health best = null;
        Vector3 bestPoint = transform.position;
        float bestScore = float.PositiveInfinity;
        bool baseReachable = IsHostile(townHall) && TryApproach(townHall, out _);
        if (IsHostile(townHall) && !baseReachable)
        {
            Vector3 start = transform.position + Vector3.up * 0.5f;
            Vector3 delta = townHall.transform.position + Vector3.up * 0.5f - start;
            foreach (RaycastHit hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, TargetMask, QueryTriggerInteraction.Ignore))
            {
                Health blocker = hit.collider.GetComponentInParent<Health>();
                if (IsHostile(blocker)) candidates.Add(blocker);
            }
        }
        foreach (Health candidate in candidates)
        {
            BuildingTypeInfo info = candidate.GetComponent<BuildingTypeInfo>() ?? candidate.GetComponentInParent<BuildingTypeInfo>();
            bool wall = info != null && info.Type == BuildingTypeInfo.BuildingType.Wall;
            // Prefer an open route over demolishing a wall that is merely nearby.
            if (wall && baseReachable) continue;
            if (!TryApproach(candidate, out Vector3 approach)) continue;
            // Walls are fallback targets only while no primary target can be reached.
            int priority = wall ? 4 : candidate == townHall ? 1 : candidate.GetComponent<BuilderUnit>() != null ? 2 : 3;
            float score = priority * 10000f + FlatDistance(transform.position, approach);
            // Keep the current target when alternatives are nearly equivalent.
            if (candidate == currentTarget) score -= 2f;
            if (score < bestScore)
            {
                best = candidate;
                bestPoint = approach;
                bestScore = score;
            }
        }

        currentTarget = best;
        if (best == null)
        {
            State = "Waiting";
            // A partial path lets the squad approach a distant blockade before scanning it.
            if (IsHostile(townHall) && agent.CalculatePath(townHall.transform.position, path) && path.corners.Length > 1)
            {
                agent.isStopped = false;
                agent.SetPath(path);
                State = "ApproachingBlockade";
            }
            return;
        }
        destination = bestPoint;
        if (FlatDistance(transform.position, ClosestPoint(best, transform.position)) > AttackRange || !HasClearAttack(best))
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }
    }

    private bool TryApproach(Health target, out Vector3 point)
    {
        point = transform.position;
        if (FlatDistance(transform.position, ClosestPoint(target, transform.position)) <= AttackRange && HasClearAttack(target))
            return true;

        float bestLength = float.PositiveInfinity;
        Vector3 center = target.transform.position;
        Collider collider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        if (collider != null) center = collider.bounds.center;
        for (int i = 0; i < 9; i++)
        {
            Vector3 direction;
            if (i == 0)
            {
                direction = transform.position - center;
                direction.y = 0f;
                direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            }
            else
            {
                float angle = (i - 1) * Mathf.PI / 4f;
                direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            }
            Vector3 outside = center + direction * (collider != null ? collider.bounds.extents.magnitude + 2f : 2f);
            outside.y = transform.position.y;
            Vector3 edge = ClosestPoint(target, outside);
            edge.y = transform.position.y;
            Vector3 desired = edge + direction * Mathf.Max(agent.radius + 0.15f, AttackRange * 0.7f);
            if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, Mathf.Max(1f, agent.radius * 2f), agent.areaMask)) continue;
            if (FlatDistance(hit.position, ClosestPoint(target, hit.position)) > AttackRange) continue;
            if (!HasClearAttackFrom(target, hit.position)) continue;
            if (!NavMesh.CalculatePath(transform.position, hit.position, agent.areaMask, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            float length = 0f;
            Vector3[] corners = path.corners;
            for (int c = 1; c < corners.Length; c++) length += Vector3.Distance(corners[c - 1], corners[c]);
            if (length >= bestLength) continue;
            bestLength = length;
            point = hit.position;
        }
        return !float.IsPositiveInfinity(bestLength);
    }

    private bool IsHostile(Health target)
    {
        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy || target == ownHealth) return false;
        if (target.GetComponentInParent<EnemyAI>() != null || target.GetComponentInParent<MiniCubeKamikaze>() != null) return false;
        UnitsController unit = target.GetComponent<UnitsController>();
        if (unit != null && unit.Data != null && unit.Data.faction != Faction.Player) return false;
        BuildingTypeInfo building = target.GetComponent<BuildingTypeInfo>();
        if (building != null && building.Data != null && building.Data.faction != Faction.Player) return false;
        return true;
    }

    private bool HasClearAttack(Health target)
    {
        return HasClearAttackFrom(target, transform.position);
    }

    private bool HasClearAttackFrom(Health target, Vector3 position)
    {
        Vector3 start = position + Vector3.up * 0.5f;
        Vector3 end = ClosestPoint(target, start);
        Vector3 delta = end - start;
        if (delta.sqrMagnitude < 0.001f) return true;
        foreach (RaycastHit hit in Physics.RaycastAll(start, delta.normalized, delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == target || health == ownHealth) continue;
            if (health != null && health.IsDead) continue;
            if (hit.collider.GetComponentInParent<EnemyAI>() != null) continue;
            return false;
        }
        return true;
    }

    private static Vector3 ClosestPoint(Health target, Vector3 from)
    {
        Collider collider = target.GetComponent<Collider>() ?? target.GetComponentInChildren<Collider>();
        return collider != null && collider.enabled ? collider.ClosestPoint(from) : target.transform.position;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void StopMoving()
    {
        if (!CanNavigate) return;
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    private void OnDisable()
    {
        StopMoving();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, marchDetectionDistance);
        if (currentTarget != null) Gizmos.DrawLine(transform.position, destination);
    }
}
