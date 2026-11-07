#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Real Play Mode smoke checks. Run with -executeMethod EnemyAIPlayModeChecks.Run (without -quit).</summary>
[InitializeOnLoad]
public static class EnemyAIPlayModeChecks
{
    private const string ActiveKey = "EnemyAIPlayModeChecks.Active";
    private const string ResultKey = "EnemyAIPlayModeChecks.Result";
    private const string ScenesKey = "EnemyAIPlayModeChecks.Scenes";
    private const string StartedKey = "EnemyAIPlayModeChecks.Started";
    private const float SuiteTimeout = 180f;
    private static readonly string[] CaseNames =
    {
        "navmesh_march_without_teleport", "blocking_wall_breach_then_town_hall",
        "wait_without_base_then_find_late_base", "allied_enemies_are_not_targets", "own_death_stops_navigation",
        "melee_default_layer_blocker_prevents_damage", "ranged_default_layer_blocker_prevents_projectiles"
    };

    [Serializable] private sealed class Report
    {
        public string startedUtc;
        public string finishedUtc;
        public string unityVersion;
        public bool passed;
        public List<CheckResult> checks = new List<CheckResult>();
        public List<string> errors = new List<string>();
    }

    [Serializable] private sealed class CheckResult
    {
        public string name;
        public bool passed;
        public float durationSeconds;
        public string details;
    }

    [Serializable] private sealed class SavedScene
    {
        public string path;
        public bool loaded;
        public bool active;
    }
    [Serializable] private sealed class SavedScenes { public SavedScene[] scenes; }

    private static Report report;
    private static CheckResult current;
    private static IEnumerator scenario;
    private static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private static readonly List<UnitData> dataObjects = new List<UnitData>();
    private static GameObject root;
    private static NavMeshData navMeshData;
    private static NavMeshDataInstance navMeshInstance;
    private static EnemyAI enemy;
    private static Health hall;
    private static float caseStart;
    private static int caseIndex;
    private static int lastFrame = -1;
    private static bool finishing;
    private static Vector3 previousPosition;
    private static float previousSampleTime;
    private static float maximumStep;
    private static float travelled;

    static EnemyAIPlayModeChecks()
    {
        if (SessionState.GetBool(ActiveKey, false)) Attach();
    }

    [MenuItem("Tools/Checks/Enemy AI Play Mode smoke checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(ActiveKey, false))
            throw new InvalidOperationException("Enemy AI checks require idle Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or discard scene edits before running Enemy AI checks.");
        }

        SceneSetup[] originalScenes = EditorSceneManager.GetSceneManagerSetup();
        var savedScenes = new SavedScene[originalScenes.Length];
        for (int i = 0; i < originalScenes.Length; i++)
            savedScenes[i] = new SavedScene { path = originalScenes[i].path, loaded = originalScenes[i].isLoaded, active = originalScenes[i].isActive };
        SessionState.SetString(ScenesKey, JsonUtility.ToJson(new SavedScenes { scenes = savedScenes }));
        SessionState.SetString(StartedKey, DateTime.UtcNow.ToString("O"));
        SessionState.SetString(ResultKey, "");
        // The blank scene is created before Play Mode so the user's scene scripts never start.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(ActiveKey, true);
        Application.runInBackground = true;
        Time.timeScale = 1f;
        EditorApplication.isPaused = false;
        Attach();
        EditorApplication.EnterPlaymode();
    }

    // Optional interactive observation entry point; deliberately does not quit the Editor.
    public static void OpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(ActiveKey, false))
            throw new InvalidOperationException("MainScene observation requires idle Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or discard scene edits before opening MainScene.");
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived -= LogReceived;
        Application.logMessageReceived += LogReceived;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(ActiveKey, false))
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            EditorApplication.isPaused = false;
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(ActiveKey, false))
        {
            if (string.IsNullOrEmpty(SessionState.GetString(ResultKey, "")))
            {
                EnsureReport();
                report.errors.Add("Play Mode ended before the suite completed.");
                WriteReport();
            }
            FinalizeInEditMode();
        }
    }

    private static void EnsureReport()
    {
        if (report != null) return;
        report = new Report { startedUtc = SessionState.GetString(StartedKey, ""), unityVersion = Application.unityVersion };
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        EnsureReport();
        if (DateTime.TryParse(report.startedUtc, out DateTime started) && (DateTime.UtcNow - started.ToUniversalTime()).TotalSeconds > SuiteTimeout)
        {
            report.errors.Add("Suite exceeded the 180 second wall-clock watchdog.");
            Finish();
            return;
        }
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || finishing) return;
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;

        try
        {
            if (scenario == null)
            {
                if (caseIndex >= CaseNames.Length) { Finish(); return; }
                BeginCase();
            }
            SampleMovement();
            while (routines.Count > 0)
            {
                IEnumerator top = routines.Peek();
                if (!top.MoveNext()) { routines.Pop(); continue; }
                if (top.Current is IEnumerator nested) { routines.Push(nested); continue; }
                return;
            }
            CompleteCase(true, "All timed assertions passed; maximum sampled step=" + maximumStep.ToString("F3") + "m, travelled=" + travelled.ToString("F2") + "m.");
        }
        catch (Exception exception)
        {
            CompleteCase(false, exception.ToString());
        }
    }

    private static void BeginCase()
    {
        CleanupWorld();
        current = new CheckResult { name = CaseNames[caseIndex] };
        caseStart = Time.realtimeSinceStartup;
        root = new GameObject("EnemyAI smoke check - " + current.name);
        BuildNavMesh();
        switch (caseIndex)
        {
            case 0: scenario = March(); break;
            case 1: scenario = WallBreach(); break;
            case 2: scenario = LateBase(); break;
            case 3: scenario = Allies(); break;
            case 4: scenario = OwnDeath(); break;
            case 5: scenario = DefaultLayerBlocker(false); break;
            default: scenario = DefaultLayerBlocker(true); break;
        }
        routines.Push(scenario);
        maximumStep = travelled = 0f;
        previousSampleTime = -1f;
    }

    private static void CompleteCase(bool passed, string details)
    {
        if (current == null)
        {
            report.errors.Add(details);
            Finish();
            return;
        }
        current.passed = passed;
        current.durationSeconds = Time.realtimeSinceStartup - caseStart;
        current.details = details + Diagnostics();
        report.checks.Add(current);
        Debug.Log("[EnemyAI checks] " + current.name + ": " + (passed ? "PASS" : "FAIL") + " " + current.details);
        current = null;
        scenario = null;
        routines.Clear();
        caseIndex++;
    }

    private static void BuildNavMesh()
    {
        Require(NavMesh.GetSettingsCount() > 0, "Project has no NavMesh agent build settings.");
        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, 0f), Quaternion.identity, Vector3.one),
                size = new Vector3(48f, 0.2f, 12f), area = 0
            }
        };
        navMeshData = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings, sources,
            new Bounds(Vector3.zero, new Vector3(54f, 10f, 18f)), Vector3.zero, Quaternion.identity);
        Require(navMeshData != null, "Runtime NavMeshBuilder returned null.");
        navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
        Require(navMeshInstance.valid, "Runtime NavMesh data could not be registered.");
        Require(NavMesh.SamplePosition(new Vector3(-14f, 0f, 0f), out _, 1f, NavMesh.AllAreas), "Runtime NavMesh has no spawn surface.");
    }

    private static UnitData MakeData(bool building = false)
    {
        UnitData data = ScriptableObject.CreateInstance<UnitData>();
        data.name = "Temporary smoke check stats";
        data.IsBuild = building;
        data.moveSpeed = 4f;
        data.acceleration = 20f;
        data.angularSpeed = 720f;
        data.maxHealth = building ? 10000f : 100f;
        data.attackDamage = 20f;
        data.attackRange = 1.6f;
        data.attackCooldown = 0.25f;
        dataObjects.Add(data);
        return data;
    }

    private static GameObject MakeObject(string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.SetActive(false);
        go.transform.SetParent(root.transform);
        go.transform.position = position;
        return go;
    }

    private static EnemyAI MakeEnemy(Vector3 position, string name = "Enemy under test", bool ranged = false, bool losCheck = false)
    {
        GameObject go = MakeObject(name, position);
        var collider = go.AddComponent<CapsuleCollider>();
        collider.radius = 0.35f;
        collider.height = 1.8f;
        collider.center = Vector3.up * 0.9f;
        var agent = go.AddComponent<NavMeshAgent>();
        agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.speed = 4f;
        agent.acceleration = 20f;
        agent.angularSpeed = 720f;
        go.AddComponent<Health>();
        EnemyAI ai = go.AddComponent<EnemyAI>();
        UnitData stats = MakeData();
        if (losCheck) { stats.attackRange = 6f; stats.moveSpeed = 0f; }
        SetField(ai, "enemyData", stats);
        SetField(ai, "marchDetectionDistance", 4f);
        SetField(ai, "targetLayers", (LayerMask)(losCheck ? 576 : ~0));
        if (ranged)
        {
            RangedAttack attack = go.AddComponent<RangedAttack>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Test_bullet.prefab");
            Require(prefab != null && prefab.GetComponent<Projectile>() != null, "Real Test_bullet projectile prefab is missing.");
            var firePoint = new GameObject("Fire point").transform;
            firePoint.SetParent(go.transform, false);
            firePoint.localPosition = Vector3.up * 0.5f;
            SetField(attack, "projectilePrefab", prefab);
            SetField(attack, "firePoint", firePoint);
            SetField(attack, "targetLayers", (LayerMask)576);
        }
        else go.AddComponent<MeleeAttack>();
        go.SetActive(true);
        return ai;
    }

    private static Health MakeBuilding(string name, Vector3 position, Vector3 size, BuildingTypeInfo.BuildingType type)
    {
        GameObject go = MakeObject(name, position);
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.center = Vector3.up * size.y * 0.5f;
        Health health = go.AddComponent<Health>();
        BuildingTypeInfo info = go.AddComponent<BuildingTypeInfo>();
        SetField(info, "buildingType", type);
        SetField(info, "buildingData", MakeData(true));
        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = size;
        obstacle.center = box.center;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        if (type == BuildingTypeInfo.BuildingType.TownHall) go.AddComponent<TownHallRegister>();
        go.SetActive(true);
        return health;
    }

    private static Health MakeHall()
    {
        return MakeBuilding("Town hall", new Vector3(14f, 0f, 0f), new Vector3(4f, 3f, 4f), BuildingTypeInfo.BuildingType.TownHall);
    }

    private static IEnumerator March()
    {
        hall = MakeHall();
        yield return Wait(0.6f);
        enemy = MakeEnemy(new Vector3(-14f, 0f, 0f));
        yield return Wait(0.2f);
        Require(enemy.Data != null, "EnemyAI.Data lost its serialized stats.");
        Require(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "Enemy did not bind to the runtime NavMesh.");
        yield return Until(() => hall.currentHealth < hall.maxHealth, 14f, "Enemy never reached and damaged the distant hall.");
        Require(travelled > 18f, "Enemy did not perform sustained navigation toward the hall.");
        Require(EdgeDistance(enemy, hall) <= enemy.Data.attackRange + 0.4f, "Hall was damaged outside collider-edge attack range.");
    }

    private static IEnumerator WallBreach()
    {
        hall = MakeHall();
        Health wall = MakeBuilding("Blocking wall", Vector3.zero, new Vector3(1f, 3f, 20f), BuildingTypeInfo.BuildingType.Wall);
        yield return Wait(0.6f);
        wall.SetupHealth(80f);
        enemy = MakeEnemy(new Vector3(-10f, 0f, 0f));
        bool damagedWall = false;
        yield return Until(() => wall == null || wall.IsDead, 12f, "Enemy never destroyed the wall blocking the complete corridor.", () =>
        {
            Require(hall != null && Mathf.Approximately(hall.currentHealth, hall.maxHealth), "Hall was hit through a living wall.");
            Require(enemy.transform.position.x < 0.6f, "Enemy crossed a living full-width wall.");
            if (wall != null && wall.currentHealth < wall.maxHealth) damagedWall = true;
        });
        Require(damagedWall, "No intermediate real Health damage to the wall was observed.");
        yield return Until(() => hall.currentHealth < hall.maxHealth, 12f, "Enemy failed to resume navigation and damage the hall after the wall died.");
        Require(enemy.transform.position.x > 8f, "Enemy did not travel beyond the destroyed wall.");
    }

    private static IEnumerator LateBase()
    {
        enemy = MakeEnemy(new Vector3(-14f, 0f, 0f));
        yield return Wait(0.2f);
        Vector3 start = enemy.transform.position;
        yield return Wait(1.5f);
        Require(Vector3.Distance(start, enemy.transform.position) < 0.2f, "Enemy wandered without any base or target.");
        Require(CurrentTarget() == null, "Enemy has a target in an empty scene.");
        hall = MakeHall();
        yield return Until(() => enemy.transform.position.x > start.x + 2f, 5f, "Enemy did not discover the late-spawned hall.");
        yield return Until(() => hall.currentHealth < hall.maxHealth, 14f, "Late-spawned hall was not reached and attacked.");
    }

    private static IEnumerator Allies()
    {
        hall = MakeHall();
        yield return Wait(0.6f);
        EnemyAI ally = MakeEnemy(new Vector3(-11f, 0f, 0f), "Stationary allied enemy");
        ally.enabled = false;
        var allyAgent = ally.GetComponent<NavMeshAgent>();
        allyAgent.isStopped = true;
        enemy = MakeEnemy(new Vector3(-14f, 0f, 0f));
        yield return Wait(0.2f);
        Health allyHealth = ally.GetComponent<Health>();
        allyHealth.SetupHealth(1000f);
        yield return Until(() => hall.currentHealth < hall.maxHealth, 15f, "Enemy failed to pass its ally and attack the hall.", () =>
        {
            Require(CurrentTarget() != allyHealth, "Enemy selected its allied EnemyAI as a target.");
            Require(allyHealth != null && Mathf.Approximately(allyHealth.currentHealth, 1000f), "Enemy damaged its ally.");
        });
    }

    private static IEnumerator OwnDeath()
    {
        hall = MakeHall();
        yield return Wait(0.6f);
        enemy = MakeEnemy(new Vector3(-14f, 0f, 0f));
        yield return Until(() => enemy.transform.position.x > -11f, 5f, "Enemy did not move before the own-death check.");
        // TakeDamage destroys the object; SetupHealth(0) keeps a dead Health observable.
        enemy.GetComponent<Health>().SetupHealth(0f);
        yield return Wait(0.25f);
        Vector3 stopped = enemy.transform.position;
        float hp = hall.currentHealth;
        yield return Wait(1f);
        Require(Vector3.Distance(stopped, enemy.transform.position) < 0.08f, "Dead enemy kept moving.");
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        Require(!agent.enabled || (agent.isStopped && agent.velocity.sqrMagnitude < 0.01f), "Dead enemy retained an active moving NavMeshAgent.");
        Require(Mathf.Approximately(hall.currentHealth, hp), "Dead enemy continued attacking.");
        Require(CurrentTarget() == null, "Dead enemy retained its target.");
    }

    private static IEnumerator DefaultLayerBlocker(bool ranged)
    {
        hall = MakeBuilding("LOS town hall", Vector3.zero, new Vector3(2f, 3f, 2f), BuildingTypeInfo.BuildingType.TownHall);
        hall.gameObject.layer = 6;
        GameObject blocker = MakeObject("Healthless Default-layer LOS blocker", new Vector3(-2f, 0f, 0f));
        blocker.layer = 0;
        BoxCollider box = blocker.AddComponent<BoxCollider>();
        box.size = new Vector3(0.5f, 4f, 20f);
        box.center = Vector3.up * 2f;
        blocker.SetActive(true);
        Physics.SyncTransforms();
        yield return Wait(0.6f);
        enemy = MakeEnemy(new Vector3(-4f, 0f, 0f), ranged ? "Ranged LOS enemy" : "Melee LOS enemy", ranged, true);
        yield return null;
        yield return null;
        Require(EdgeDistance(enemy, hall) < enemy.Data.attackRange, "LOS target must start inside attack range.");
        Require(blocker.GetComponent<Health>() == null && blocker.layer == 0, "LOS fixture must have no Health and use Default layer.");
        float end = Time.realtimeSinceStartup + 1.5f;
        while (Time.realtimeSinceStartup < end)
        {
            Require(Mathf.Approximately(hall.currentHealth, hall.maxHealth), "Enemy damaged an in-range hall through the healthless Default-layer blocker.");
            Require(Object.FindObjectsOfType<Projectile>().Length == 0, "Enemy launched a real projectile through the Default-layer blocker.");
            Require(EdgeDistance(enemy, hall) < enemy.Data.attackRange, "LOS target moved out of range; blocked-fire assertion would be vacuous.");
            yield return null;
        }
        Object.DestroyImmediate(blocker);
        Physics.SyncTransforms();
        bool sawProjectile = false;
        yield return Until(() => hall.currentHealth < hall.maxHealth, 5f, "Enemy did not deal real damage after the LOS blocker was removed.", () =>
        {
            if (Object.FindObjectsOfType<Projectile>().Length > 0) sawProjectile = true;
        });
        if (ranged) Require(sawProjectile, "Ranged damage occurred without observing a real Test_bullet projectile.");
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    private static IEnumerator Until(Func<bool> condition, float timeout, string failure, Action everyFrame = null)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            everyFrame?.Invoke();
            Require(Time.realtimeSinceStartup < end, failure);
            yield return null;
        }
    }

    private static void SampleMovement()
    {
        if (enemy == null) { previousSampleTime = -1f; return; }
        Vector3 position = enemy.transform.position;
        if (previousSampleTime >= 0f)
        {
            float step = Vector3.Distance(previousPosition, position);
            float elapsed = Mathf.Max(Time.time - previousSampleTime, 0f);
            maximumStep = Mathf.Max(maximumStep, step);
            travelled += step;
            Require(step <= Mathf.Max(enemy.Data.moveSpeed, enemy.GetComponent<NavMeshAgent>().speed) * elapsed + 0.25f,
                "Movement discontinuity/teleport: " + step.ToString("F3") + "m in " + elapsed.ToString("F3") + "s.");
        }
        previousPosition = position;
        previousSampleTime = Time.time;
    }

    private static Health CurrentTarget()
    {
        PropertyInfo property = typeof(EnemyAI).GetProperty("CurrentTarget", BindingFlags.Instance | BindingFlags.Public);
        Require(property != null, "EnemyAI must expose public Health CurrentTarget.");
        return (Health)property.GetValue(enemy);
    }

    private static float EdgeDistance(EnemyAI ai, Health target)
    {
        Collider collider = target.GetComponent<Collider>();
        return Vector3.Distance(ai.transform.position, collider.ClosestPoint(ai.transform.position));
    }

    private static string Diagnostics()
    {
        if (enemy == null) return "";
        PropertyInfo state = typeof(EnemyAI).GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
        return " Enemy position=" + enemy.transform.position + "; State=" + (state == null ? "<missing>" : state.GetValue(enemy)) + ".";
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Require(field != null, target.GetType().Name + "." + name + " field is missing.");
        field.SetValue(target, value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void LogReceived(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        EnsureReport();
        report.errors.Add(message + "\n" + stack);
    }

    private static void CleanupWorld()
    {
        enemy = null;
        hall = null;
        if (root != null) Object.DestroyImmediate(root);
        if (navMeshInstance.valid) navMeshInstance.Remove();
        if (navMeshData != null) Object.DestroyImmediate(navMeshData);
        foreach (UnitData data in dataObjects) if (data != null) Object.DestroyImmediate(data);
        dataObjects.Clear();
    }

    private static void Finish()
    {
        if (finishing) return;
        finishing = true;
        if (current != null) CompleteCase(false, "Suite stopped before the current scenario completed.");
        CleanupWorld();
        WriteReport();
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinalizeInEditMode();
    }

    private static void WriteReport()
    {
        EnsureReport();
        report.finishedUtc = DateTime.UtcNow.ToString("O");
        report.passed = report.errors.Count == 0 && report.checks.Count == CaseNames.Length && report.checks.TrueForAll(check => check.passed);
        string json = JsonUtility.ToJson(report, true);
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/enemy-ai-checks.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json);
        SessionState.SetString(ResultKey, json);
        Debug.Log("[EnemyAI checks] " + (report.passed ? "PASS" : "FAIL") + "; JSON: " + path);
    }

    private static void FinalizeInEditMode()
    {
        string json = SessionState.GetString(ResultKey, "");
        Report completed = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<Report>(json);
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        Application.logMessageReceived -= LogReceived;
        try
        {
            string saved = SessionState.GetString(ScenesKey, "");
            if (!string.IsNullOrEmpty(saved))
            {
                SavedScenes setup = JsonUtility.FromJson<SavedScenes>(saved);
                if (setup.scenes != null && Array.TrueForAll(setup.scenes, scene => !string.IsNullOrEmpty(scene.path)))
                {
                    var restore = new SceneSetup[setup.scenes.Length];
                    for (int i = 0; i < restore.Length; i++)
                        restore[i] = new SceneSetup { path = setup.scenes[i].path, isLoaded = setup.scenes[i].loaded, isActive = setup.scenes[i].active };
                    EditorSceneManager.RestoreSceneManagerSetup(restore);
                }
            }
        }
        finally
        {
            report = null;
            scenario = null;
            current = null;
            routines.Clear();
            caseIndex = 0;
            lastFrame = -1;
            finishing = false;
            if (Application.isBatchMode) EditorApplication.Exit(completed != null && completed.passed ? 0 : 1);
        }
    }
}
#endif
