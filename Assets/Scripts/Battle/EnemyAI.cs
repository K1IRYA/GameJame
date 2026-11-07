using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("Статблок врага (Резерв для инспектора)")]
    [SerializeField] private UnitData enemyData;
    public UnitData Data => enemyData;

    [Header("Настройки луча марша")]
    [SerializeField] private float marchDetectionDistance = 4f;
    [SerializeField] private LayerMask targetLayers;

    private Vector3 targetBasePosition;
    private bool isBaseFound = false;
    private bool isRaycastModeActive = false;

    // ЭТАП 2: Список преград и текущая приоритетная цель по лазеру
    private List<Health> obstaclesList = new List<Health>();
    private Health currentTarget;

    // ЭТАП 3: Компоненты для проведения атак по ТЗ
    private MeleeAttack meleeAttack;
    private float nextAttackTime;

    private void Awake()
    {
        // Кэшируем твой компонент ближнего боя со старта игры
        meleeAttack = GetComponent<MeleeAttack>();
    }

    private void Start()
    {
        if (TownHallRegister.IsBaseRegistered)
        {
            targetBasePosition = TownHallRegister.BasePosition;
            isBaseFound = true;
            Debug.Log($"[EnemyAI] {gameObject.name}: Координаты Ратуши получены! Стартую прямой марш.");
        }
        else
        {
            TownHallRegister register = Object.FindAnyObjectByType<TownHallRegister>();
            if (register != null)
            {
                targetBasePosition = register.transform.position;
                isBaseFound = true;
            }
        }
    }

    private void Update()
    {
        // Каждую секунду проверяем регистрацию базы на случай, если кубы родились раньше неё
        if (!isBaseFound)
        {
            if (TownHallRegister.IsBaseRegistered)
            {
                targetBasePosition = TownHallRegister.BasePosition;
                isBaseFound = true;
            }
            return;
        }

        // Если сработал перехват — марш отключается, управление у Скрипта №2
        if (isRaycastModeActive)
        {
            ExecuteRaycastScannerMode();
            return;
        }

        // --- СКРИПТ №1: РЕЖИМ ПРЯМОГО МАРША ---
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 rayDir = transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, rayDir, out hit, marchDetectionDistance, targetLayers))
        {
            Health foundObstacle = hit.collider.GetComponent<Health>() ?? hit.collider.GetComponentInParent<Health>();
            if (foundObstacle != null && !foundObstacle.IsDead)
            {
                if (hit.collider.GetComponent<EnemyAI>() == null && hit.collider.GetComponentInParent<EnemyAI>() == null)
                {
                    Debug.Log($"[EnemyAI] {gameObject.name}: Включаю СКРИПТ №2 (Лучевой сканер pregrad).");
                    isRaycastModeActive = true;
                    RebuildRaycastObstaclesList(); // Запускаем первый сквозной просчет лазера
                    return;
                }
            }
        }

        // Прямое движение к базе
        Vector3 directionToBase = (targetBasePosition - transform.position).normalized;
        directionToBase.y = 0f;

        if (directionToBase != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToBase), Time.deltaTime * 10f);
        }
        transform.position += directionToBase * 10f * Time.deltaTime;
    }

    // =========================================================
    // ЭТАП 2: СКВОЗНОЙ ЛУЧЕВОЙ СКАНЕР ПРЕГРАД (СКРИПТ №2)
    // =========================================================
    private void RebuildRaycastObstaclesList()
    {
        obstaclesList.Clear();

        // Стреляем сквозным лазером строго из центра куба в центр зарегистрированной Ратуши
        Vector3 startPoint = transform.position + Vector3.up * 0.5f;
        Vector3 targetPoint = TownHallRegister.IsBaseRegistered ? TownHallRegister.BasePosition + Vector3.up * 0.5f : targetBasePosition;
        Vector3 direction = (targetPoint - startPoint).normalized;
        float totalDistance = Vector3.Distance(startPoint, targetPoint);

        // Собираем абсолютно ВСЕ попадания по пути лазера
        RaycastHit[] hits = Physics.RaycastAll(startPoint, direction, totalDistance, targetLayers);

        foreach (var hit in hits)
        {
            Health h = hit.collider.GetComponent<Health>() ?? hit.collider.GetComponentInParent<Health>();
            if (h == null || h.IsDead || h.gameObject == gameObject) continue;

            // Исключаем союзных кубов
            if (hit.collider.GetComponent<EnemyAI>() != null || hit.collider.GetComponentInParent<EnemyAI>() != null) continue;

            if (!obstaclesList.Contains(h))
            {
                obstaclesList.Add(h);
            }
        }

        // СОРТИРОВКА СПИСКА СТРОГО ПО ТЗ: Стена -> Ратуша -> Строители -> Башни -> Остальные постройки
        obstaclesList.Sort((a, b) =>
        {
            int priorityA = GetBuildingPriority(a);
            int priorityB = GetBuildingPriority(b);
            return priorityA.CompareTo(priorityB); // Сортировка от меньшего к большему (1 — топ приоритет)
        });

        // Назначаем текущую цель из отсортированного списка лазера
        if (obstaclesList.Count > 0)
        {
            currentTarget = obstaclesList[0];
            Debug.Log($"[EnemyAI] {gameObject.name}: Сквозной лазер отсортирован! Текущая приоритетная цель по ТЗ: {currentTarget.gameObject.name}");
        }
        else
        {
            // Если лазер абсолютно чист — возвращаемся к обычному маршу
            isRaycastModeActive = false;
        }
    }

    // Калькулятор приоритетов ТЗ
    private int GetBuildingPriority(Health target)
    {
        BuildingTypeInfo bInfo = target.GetComponent<BuildingTypeInfo>() ?? target.GetComponentInParent<BuildingTypeInfo>();
        if (bInfo != null)
        {
            switch (bInfo.Type)
            {
                case BuildingTypeInfo.BuildingType.Wall:
                    return 1; // 1. Стены и заборы
                case BuildingTypeInfo.BuildingType.TownHall:
                    return 2; // 2. Ратуша
                case BuildingTypeInfo.BuildingType.Tower:
                    return 4; // 4. Башни
                default:
                    return 5; // 5. Остальные постройки
            }
        }

        // Проверяем, является ли цель гномом-строителем
        if (target.GetComponent<BuilderUnit>() != null || target.GetComponentInParent<BuilderUnit>() != null)
        {
            return 3; // 3. Строители
        }

        return 6;
    }

    private void ExecuteRaycastScannerMode()
    {
        // Если текущую преграду сломали или стёрли, мгновенно пересчитываем сквозной лазер
        if (currentTarget == null || currentTarget.IsDead)
        {
            RebuildRaycastObstaclesList();
            return;
        }

        // ЭТАП 3: РАСЧЕТ ДИСТАНЦИИ С ЗАЩИТОЙ ОТ ПРОСАЧИВАНИЯ СКВОЗЬ КОЛЛАЙДЕРЫ
        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);
        float attackRange = (enemyData != null) ? enemyData.attackRange : 2f;

        // Если наша цель здание — считаем дистанцию до ближайшей внешней кромки его коллайдера, а не до центра!
        Collider targetCollider = currentTarget.GetComponent<Collider>() ?? currentTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null && currentTarget.GetComponent<BuildingTypeInfo>() != null)
        {
            Vector3 closestPoint = targetCollider.ClosestPoint(transform.position);
            distance = Vector3.Distance(transform.position, closestPoint);
        }

        // БОЕВОЙ ЦИКЛ: ЕСЛИ ПОДОШЛИ НА ДИСТАНЦИЮ УДАРА
        if (distance <= attackRange)
        {
            // Полностью прекращаем движение (Скорость сбрасывается в 0, просачивание физически невозможно!)
            Vector3 lookDir = (currentTarget.transform.position - transform.position).normalized;
            lookDir.y = 0f;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
            }

            // Наносим урон через твой MeleeAttack
            if (meleeAttack != null)
            {
                meleeAttack.AttackTargetFromAI(currentTarget);
            }
            else if (Time.time >= nextAttackTime)
            {
                // Резервный кулдаун атаки на случай, если компонент ближнего боя забыли повесить
                nextAttackTime = Time.time + ((enemyData != null) ? enemyData.attackCooldown : 1f);
                currentTarget.TakeDamage((enemyData != null) ? enemyData.attackDamage : 10f);

                Animator anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger("Attack");
            }
        }
        else
        {
            // Если до забора еще есть расстояние — уверенно сближаемся
            Vector3 directionToObstacle = (currentTarget.transform.position - transform.position).normalized;
            directionToObstacle.y = 0f;

            if (directionToObstacle != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToObstacle), Time.deltaTime * 10f);
            }

            // Бежим в упор к преграде со скоростью 10 м/с
            transform.position += directionToObstacle * 10f * Time.deltaTime;
        }
    }

    private void OnDrawGizmos()
    {
        // Зеленый — марш перед собой, Малиновый — активный сквозной лазер сквозь постройки до Ратуши
        Gizmos.color = (!isRaycastModeActive) ? Color.green : Color.magenta;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        if (!isRaycastModeActive)
        {
            Gizmos.DrawRay(rayStart, transform.forward * marchDetectionDistance);
        }
        else if (TownHallRegister.IsBaseRegistered)
        {
            Gizmos.DrawLine(rayStart, TownHallRegister.BasePosition + Vector3.up * 0.5f);
        }
    }
}