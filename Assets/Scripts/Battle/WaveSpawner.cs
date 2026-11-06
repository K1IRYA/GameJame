using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [System.Serializable]
    public class EnemySquad
    {
        public GameObject enemyPrefab; // Префаб врага с EnemyAI
        public int count;              // Сколько спавнить в этой пачке
    }

    [System.Serializable]
    public class WavePattern
    {
        public string waveName = "Волна";
        public List<EnemySquad> squads; // Состав волны
    }

    [Header("Настройки времени")]
    [SerializeField] private float timeBeforeFirstWave = 120f; // Пару минут до первой волны
    [SerializeField] private float timeBetweenWaves = 90f;    // Время между последующими волнами

    [Header("Точки спавна врагов")]
    [SerializeField] private Transform[] spawnPoints; // 3 спота спавна

    [Header("Шаблоны волн")]
    [SerializeField] private List<WavePattern> predefinedWaves;

    private int currentWaveIndex = 0;
    private float waveTimer;
    private bool isTimerRunning = false;
    private bool isWaveActive = false;

    private List<Health> activeEnemies = new List<Health>();
    private Transform activeTownHall;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        // Ищем ратушу на сцене (объект с тегом или специальным скриптом)
        // Предполагаем, что ратуша имеет скрипт ResourceDropOff или имя "base"
        var dropOff = Object.FindAnyObjectByType<ResourceDropOff>();
        if (dropOff != null)
        {
            StartTimer(timeBeforeFirstWave);
            activeTownHall = dropOff.transform;
        }
        else
        {
            Debug.LogWarning("[WaveSpawner] Ратуша не найдена при старте. Ожидание появления ратуши...");
        }
    }

    private void Update()
    {
        if (isTimerRunning)
        {
            waveTimer -= Time.deltaTime;

            // Обновляем UI таймера
            if (WaveTimerUI.Instance != null)
            {
                WaveTimerUI.Instance.UpdateTimerDisplay(waveTimer, currentWaveIndex + 1);
            }

            if (waveTimer <= 0f)
            {
                isTimerRunning = false;
                StartCoroutine(SpawnWaveRoutine());
            }
        }

        // Отслеживаем завершение волны
        if (isWaveActive)
        {
            activeEnemies.RemoveAll(item => item == null || item.IsDead);

            if (activeEnemies.Count == 0)
            {
                EndWave();
            }
        }
    }

    // Метод для вызова из скрипта постройки ратуши, если она ставится в процессе игры
    public void OnTownHallBuilt(Transform townHallTransform)
    {
        activeTownHall = townHallTransform;
        if (!isTimerRunning && !isWaveActive)
        {
            StartTimer(timeBeforeFirstWave);
        }
    }

    private void StartTimer(float duration)
    {
        waveTimer = duration;
        isTimerRunning = true;
        if (WaveTimerUI.Instance != null) WaveTimerUI.Instance.SetVisible(true);
    }

    private IEnumerator SpawnWaveRoutine()
    {
        isWaveActive = true;
        if (WaveTimerUI.Instance != null) WaveTimerUI.Instance.SetStatusText("Wawe here!");

        // Выбираем случайную точку спавна из трех доступных
        Transform chosenSpawnPoint = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[Random.Range(0, spawnPoints.Length)] : null;

        // Показываем стрелочку UI в направлении выбранной точки
        if (WaveTimerUI.Instance != null && chosenSpawnPoint != null)
        {
            WaveTimerUI.Instance.ShowSpawnDirection(chosenSpawnPoint.position);
        }

        // Берем заготовленный шаблон или генерируем нарастающую сложность, если шаблоны кончились
        WavePattern wave = null;
        if (currentWaveIndex < predefinedWaves.Count)
        {
            wave = predefinedWaves[currentWaveIndex];
        }
        else
        {
            wave = GenerateProceduralWave(currentWaveIndex);
        }

        foreach (var squad in wave.squads)
        {
            for (int i = 0; i < squad.count; i++)
            {
                if (squad.enemyPrefab == null) continue;

                // Сначала спавним в базовой точке
                GameObject enemyGo = Instantiate(squad.enemyPrefab, chosenSpawnPoint.position, chosenSpawnPoint.rotation);

                // Смещаем позицию через Warp, чтобы NavMeshAgent не возвращал врага обратно в центр
                UnityEngine.AI.NavMeshAgent enemyAgent = enemyGo.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (enemyAgent != null)
                {
                    Vector3 spawnOffset = new Vector3(Random.Range(-2.5f, 2.5f), 0f, Random.Range(-2.5f, 2.5f));
                    enemyAgent.Warp(chosenSpawnPoint.position + spawnOffset);
                }

                Health enemyHealth = enemyGo.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    activeEnemies.Add(enemyHealth);
                }

                yield return new WaitForSeconds(0.2f); // Небольшая задержка, чтобы они плавно выходили кучкой
            }
        }
    }

    private void EndWave()
    {
        isWaveActive = false;

        currentWaveIndex++;

        // Запускаем новый таймер по кругу
        StartTimer(timeBetweenWaves);
    }

    private WavePattern GenerateProceduralWave(int index)
    {
        // Процедурная генерация на случай, если заготовленные шаблоны закончатся
        WavePattern wave = new WavePattern();
        wave.waveName = $"Волна {index + 1} (Нарастающая сложность)";
        wave.squads = new List<EnemySquad>();

        if (predefinedWaves.Count > 0)
        {
            var lastWave = predefinedWaves[predefinedWaves.Count - 1];
            if (lastWave.squads != null && lastWave.squads.Count > 0)
            {
                EnemySquad squad = new EnemySquad();
                squad.enemyPrefab = lastWave.squads[0].enemyPrefab;
                squad.count = 3 + (index * 2); // С каждой волной на 2 врага больше
                wave.squads.Add(squad);
            }
        }

        return wave;
    }
}
