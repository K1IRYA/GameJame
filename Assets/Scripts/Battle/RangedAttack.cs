using UnityEngine;

public class RangedAttack : MonoBehaviour
{
    [Header("Настройки снаряда")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask targetLayers;

    private UnitData unitData;
    private Health currentTarget;
    private float nextAttackTime;

    private void Start()
    {
        // 1. Пытаемся вытащить данные, если это башня
        var buildingInfo = GetComponent<BuildingTypeInfo>();
        if (buildingInfo != null && buildingInfo.Data != null)
        {
            unitData = buildingInfo.Data;
            StartCoroutine(TowerScanningRoutine());
            return;
        }

        // 2. Пытаемся вытащить данные, если это враг
        var enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null && enemyAI.Data != null)
        {
            unitData = enemyAI.Data;
            return;
        }
    }

    private void Update()
    {
        if (unitData == null) return;
        // Mobile enemies fire only when EnemyAI has checked range and line of sight.
        if (GetComponent<EnemyAI>() != null) return;

        // Если это здание (башня), проверяем дистанцию и здоровье текущей цели
        if (unitData.IsBuild)
        {
            if (currentTarget == null || currentTarget.IsDead || Vector3.Distance(transform.position, currentTarget.transform.position) > unitData.attackRange)
            {
                currentTarget = null;
                return;
            }
        }

        // Поворачиваем к цели только мобильных юнитов. Здания (IsBuild == true) стоят монолитно.
        if (!unitData.IsBuild && currentTarget != null && !currentTarget.IsDead)
        {
            Vector3 lookDir = (currentTarget.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);
            }
        }

        // Выполняем атаку по кулдауну, если цель валидна
        if (currentTarget != null && !currentTarget.IsDead && Time.time >= nextAttackTime)
        {
            ExecuteRangedAttack(currentTarget);
        }
    }

    // Этот метод будет вызывать EnemyAI.cs для стреляющих врагов, передавая туда свою приоритетную цель
    public void AttackTargetFromAI(Health target)
    {
        if (unitData == null || target == null || target.IsDead) return;

        currentTarget = target;

        if (Time.time >= nextAttackTime)
        {
            ExecuteRangedAttack(currentTarget);
        }
    }

    private void ExecuteRangedAttack(Health target)
    {
        nextAttackTime = Time.time + unitData.attackCooldown;

        // Стреляем, только если задан префаб снаряда и точка огня
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projGo = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Projectile projectile = projGo.GetComponent<Projectile>();
            if (projectile != null)
            {
                // Передаем снаряду цель и урон из нашего ScriptableObject
                projectile.MoveTo(target, unitData.attackDamage);
            }
        }

        // Запуск анимации, если есть аниматор
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Attack");
    }

    // Корутина сканирования целей только для башен
    private System.Collections.IEnumerator TowerScanningRoutine()
    {
        while (true)
        {
            if (currentTarget == null)
            {
                Collider[] colliders = Physics.OverlapSphere(transform.position, unitData.attackRange, targetLayers);
                float minDistance = float.MaxValue;
                Health closestTarget = null;

                foreach (var col in colliders)
                {
                    Health h = col.GetComponentInParent<Health>();
                    if (h == null || h.IsDead) continue;

                    // Башни игрока стреляют только по врагам
                    var enemyCheck = col.GetComponentInParent<EnemyAI>();
                    if (enemyCheck != null)
                    {
                        float dist = Vector3.Distance(transform.position, h.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestTarget = h;
                        }
                    }
                }

                if (closestTarget != null) currentTarget = closestTarget;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (unitData != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, unitData.attackRange);
        }
    }
}