#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Observation check: -executeMethod EnemyAIEnclosedBuilderCheck.Run, without -quit.</summary>
[InitializeOnLoad]
public static class EnemyAIEnclosedBuilderCheck
{
    private const string Prefix = "EnemyAIEnclosedBuilderCheck.";
    private const float WallTimeout = 12f;
    private const float AfterBreachTimeout = 10f;
    private const float SuiteTimeout = 45f;
    private const float BuilderHP = 10000f;
    private const float WallHP = 30f;
    private const string BuilderPrefab = "Assets/Prefabs/Dwarf_bulder.prefab";
    private const string EnemyPrefab = "Assets/Prefabs/Enemy_test.prefab";

    [Serializable] private sealed class Report
    {
        public string startedUtc;
        public string finishedUtc;
        public string unityVersion;
        public bool passed;
        public string purpose = "Regression: breach one wall then attack the accessible builder; preserve accessible town hall priority.";
        public string fixture = "Real Dwarf_bulder and Enemy_test; flat 48x32 runtime NavMesh; four primitive Wall buildings, inner edges +/-6, thickness1, height3; enemy(-12,0,0); optional hall(-20,0,10).";
        public string overrides = "Enemy scan20, production target mask576; enemy UnitData cloned without stat edits; stationary builder task scripts disabled; builder/hall HP10000, wall HP30.";
        public string screenshot;
        public List<Observation> scenarios = new List<Observation>();
        public List<string> errors = new List<string>();
    }

    [Serializable] private sealed class Observation
    {
        public string name;
        public bool sanityPassed;
        public string failure;
        public float durationSeconds;
        public string firstChosenTarget;
        public List<string> targetChanges = new List<string>();
        public List<string> damagedWalls = new List<string>();
        public List<string> destroyedWalls = new List<string>();
        public float enemyScanDistance;
        public float enemyAttackRange;
        public float enemyDamage;
        public float enemySpeed;
        public float enemyCooldown;
        public float builderInitialHP;
        public float builderHPBeforeBreach;
        public float builderHPAtBreach;
        public float builderFinalHP;
        public bool breached;
        public float breachSeconds = -1f;
        public bool builderReached;
        public bool builderDamaged;
        public bool builderDamagedBeforeBreach;
        public bool hallAccessible;
        public bool hallDamaged;
        public float hallFinalHP;
        public bool initialInteriorPathComplete;
        public bool builderStationary = true;
        public string finalTarget;
        public string finalState;
        public Vector3 finalPosition;
        public string finalPathStatus;
        public string interpretation;
    }

    [Serializable] private sealed class SavedScene
    {
        public string path;
        public bool loaded;
        public bool active;
    }
    [Serializable] private sealed class SavedScenes { public SavedScene[] scenes; }

    private static Report report;
    private static Observation current;
    private static GameObject root;
    private static GameObject inactiveRoot;
    private static EnemyAI enemy;
    private static Health builder;
    private static Health hall;
    private static readonly List<Health> walls = new List<Health>();
    private static readonly List<string> wallNames = new List<string>();
    private static readonly List<Object> temporaryAssets = new List<Object>();
    private static NavMeshData mesh;
    private static NavMeshDataInstance meshInstance;
    private static int caseIndex;
    private static int phase;
    private static int lastFrame = -1;
    private static float phaseStart;
    private static float caseStart;
    private static string previousTarget;
    private static bool finishing;
    private static bool interruptedReload;

    static EnemyAIEnclosedBuilderCheck()
    {
        if (!SessionState.GetBool(Prefix + "Active", false)) return;
        interruptedReload = SessionState.GetBool(Prefix + "RuntimeStarted", false)
            && string.IsNullOrEmpty(SessionState.GetString(Prefix + "Result", ""));
        Attach();
    }

    [MenuItem("Tools/Checks/Enclosed builder test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(Prefix + "Active", false))
            throw new InvalidOperationException("Enclosed builder check requires idle Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or discard scene edits before running the check.");
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        var saved = new SavedScene[setup.Length];
        for (int i = 0; i < setup.Length; i++)
            saved[i] = new SavedScene { path = setup[i].path, loaded = setup[i].isLoaded, active = setup[i].isActive };
        SessionState.SetString(Prefix + "Scenes", JsonUtility.ToJson(new SavedScenes { scenes = saved }));
        SessionState.SetString(Prefix + "Started", DateTime.UtcNow.ToString("O"));
        SessionState.SetString(Prefix + "Progress", "");
        SessionState.SetString(Prefix + "Result", "");
        SessionState.SetBool(Prefix + "RuntimeStarted", false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Prefix + "Active", true);
        Attach();
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
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

    private static void EnsureReport()
    {
        if (report != null) return;
        string saved = SessionState.GetString(Prefix + "Progress", "");
        report = string.IsNullOrEmpty(saved)
            ? new Report { startedUtc = SessionState.GetString(Prefix + "Started", ""), unityVersion = Application.unityVersion }
            : JsonUtility.FromJson<Report>(saved);
    }

    private static void Persist() { SessionState.SetString(Prefix + "Progress", JsonUtility.ToJson(report)); }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Prefix + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Time.timeScale = 1f;
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
        }
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EnsureReport();
        if (string.IsNullOrEmpty(SessionState.GetString(Prefix + "Result", "")))
        {
            report.errors.Add("Play Mode ended before the observation completed.");
            WriteReport();
        }
        FinalizeInEditMode();
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Prefix + "Active", false) || finishing) return;
        EnsureReport();
        try
        {
            Require(!interruptedReload, "Scripts reloaded during observation; saved partial report retained, not silently restarted.");
            if (DateTime.TryParse(report.startedUtc, out DateTime started))
                Require((DateTime.UtcNow - started.ToUniversalTime()).TotalSeconds < SuiteTimeout, "45 second wall-clock watchdog expired.");
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            if (current == null)
            {
                if (caseIndex == 2) { Finish(); return; }
                BeginCase();
            }
            if (phase == 0 && Time.realtimeSinceStartup - phaseStart >= 0.7f) StartEnemy();
            else if (phase == 1 && Time.realtimeSinceStartup - phaseStart >= 0.1f) StartObservation();
            else if (phase == 2)
            {
                Sample();
                float elapsed = Time.realtimeSinceStartup - phaseStart;
                if (current.breached ? elapsed - current.breachSeconds >= AfterBreachTimeout : elapsed >= WallTimeout)
                    CompleteCase();
            }
            Persist();
        }
        catch (Exception exception)
        {
            if (current != null) { current.failure = exception.ToString(); SnapshotFinal(); }
            report.errors.Add(exception.ToString());
            Persist();
            Finish();
        }
    }

    private static void BeginCase()
    {
        CleanupWorld();
        SessionState.SetBool(Prefix + "RuntimeStarted", true);
        current = new Observation { name = caseIndex == 0 ? "closed_ring_without_hall" : "closed_ring_accessible_outside_hall" };
        report.scenarios.Add(current);
        caseStart = phaseStart = Time.realtimeSinceStartup;
        phase = 0;
        previousTarget = null;
        root = new GameObject("Enclosed builder observation - " + current.name);
        inactiveRoot = new GameObject("Inactive prefab staging");
        inactiveRoot.transform.SetParent(root.transform);
        inactiveRoot.SetActive(false);
        BuildNavMesh();
        MakeBuilding("West wall", new Vector3(-6.5f, 0f, 0f), new Vector3(1f, 3f, 14f), false);
        MakeBuilding("East wall", new Vector3(6.5f, 0f, 0f), new Vector3(1f, 3f, 14f), false);
        MakeBuilding("North wall", new Vector3(0f, 0f, 6.5f), new Vector3(14f, 3f, 1f), false);
        MakeBuilding("South wall", new Vector3(0f, 0f, -6.5f), new Vector3(14f, 3f, 1f), false);
        GameObject unit = InstantiatePrefab(BuilderPrefab, Vector3.zero);
        Require(unit.GetComponent<BuilderUnit>() != null && unit.GetComponent<UnitsController>() != null,
            "Real builder prefab must retain BuilderUnit and UnitsController identity.");
        Require(unit.GetComponent<UnitsController>().Data != null && unit.GetComponent<UnitsController>().Data.faction == Faction.Player,
            "Builder prefab must retain real Player UnitData.");
        foreach (MonoBehaviour script in unit.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(script is Health)) script.enabled = false;
        NavMeshAgent agent = unit.GetComponent<NavMeshAgent>();
        Require(agent != null, "Builder prefab has no NavMeshAgent.");
        agent.enabled = false;
        builder = unit.GetComponent<Health>();
        Require(builder != null, "Builder prefab has no Health.");
        unit.transform.SetParent(root.transform, true);
        unit.SetActive(true);
        MakeMarker("Builder marker (green)", unit.transform, new Color(0.2f, 0.9f, 0.35f));
        if (caseIndex == 1)
            hall = MakeBuilding("Accessible outside town hall", new Vector3(-20f, 0f, 10f), new Vector3(3f, 3f, 3f), true);
        Physics.SyncTransforms();
    }

    private static void BuildNavMesh()
    {
        Require(NavMesh.GetSettingsCount() > 0, "Project has no NavMesh settings.");
        var sources = new List<NavMeshBuildSource>
        {
            new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, 0f), Quaternion.identity, Vector3.one), size = new Vector3(48f, 0.2f, 32f), area = 0 }
        };
        mesh = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
            new Bounds(Vector3.zero, new Vector3(50f, 10f, 34f)), Vector3.zero, Quaternion.identity);
        Require(mesh != null, "Runtime BuildNavMeshData returned null.");
        meshInstance = NavMesh.AddNavMeshData(mesh);
        Require(meshInstance.valid, "Runtime NavMesh registration failed.");
        GameObject ground = Cube("Runtime flat ground", new Vector3(0f, -0.15f, 0f), new Vector3(48f, 0.2f, 32f), new Color(0.16f, 0.2f, 0.23f));
        ground.layer = 0;
    }

    private static GameObject InstantiatePrefab(string path, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        Require(prefab != null, "Missing real prefab: " + path);
        GameObject instance = Object.Instantiate(prefab, inactiveRoot.transform);
        instance.SetActive(false);
        instance.transform.position = position;
        return instance;
    }

    private static Health MakeBuilding(string name, Vector3 position, Vector3 size, bool townHall)
    {
        GameObject go = Cube(name, position + Vector3.up * size.y * 0.5f, size,
            townHall ? new Color(0.25f, 0.55f, 0.95f) : new Color(0.95f, 0.6f, 0.15f));
        go.SetActive(false);
        go.layer = 9;
        // Keep unscaled physics dimensions and building origin at ground level.
        go.transform.localScale = Vector3.one;
        go.transform.position = position;
        go.GetComponent<MeshFilter>().sharedMesh = null;
        Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        BoxCollider box = go.GetComponent<BoxCollider>();
        box.size = size;
        box.center = Vector3.up * size.y * 0.5f;
        GameObject visual = Cube(name + " visual", position + box.center, size, townHall ? new Color(0.25f, 0.55f, 0.95f) : new Color(0.95f, 0.6f, 0.15f));
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, true);
        Health hp = go.AddComponent<Health>();
        BuildingTypeInfo info = go.AddComponent<BuildingTypeInfo>();
        SetField(info, "buildingType", townHall ? BuildingTypeInfo.BuildingType.TownHall : BuildingTypeInfo.BuildingType.Wall);
        UnitData stats = ScriptableObject.CreateInstance<UnitData>();
        temporaryAssets.Add(stats);
        stats.IsBuild = true;
        stats.faction = Faction.Player;
        stats.maxHealth = townHall ? BuilderHP : WallHP;
        SetField(info, "buildingData", stats);
        var obstacle = go.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = size;
        obstacle.center = box.center;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        if (townHall) go.AddComponent<TownHallRegister>();
        else { walls.Add(hp); wallNames.Add(name); }
        go.SetActive(true);
        return hp;
    }

    private static void StartEnemy()
    {
        builder.SetupHealth(BuilderHP);
        current.builderInitialHP = current.builderHPBeforeBreach = BuilderHP;
        foreach (Health wall in walls) wall.SetupHealth(WallHP);
        if (hall != null) hall.SetupHealth(BuilderHP);
        Require(NavMesh.SamplePosition(Vector3.zero, out NavMeshHit inside, 1f, NavMesh.AllAreas), "Interior NavMesh missing.");
        var path = new NavMeshPath();
        NavMesh.CalculatePath(new Vector3(-12f, 0f, 0f), inside.position, NavMesh.AllAreas, path);
        current.initialInteriorPathComplete = path.status == NavMeshPathStatus.PathComplete;
        Require(!current.initialInteriorPathComplete, "Closed ring has a complete path into its interior before any breach.");
        GameObject go = InstantiatePrefab(EnemyPrefab, new Vector3(-12f, 0f, 0f));
        enemy = go.GetComponent<EnemyAI>();
        Require(enemy != null && enemy.Data != null && go.GetComponent<MeleeAttack>() != null, "Enemy_test must contain production EnemyAI, UnitData and MeleeAttack.");
        UnitData copy = Object.Instantiate(enemy.Data);
        temporaryAssets.Add(copy);
        SetField(enemy, "enemyData", copy);
        SetField(enemy, "marchDetectionDistance", 20f);
        SetField(enemy, "targetLayers", (LayerMask)576);
        current.enemyScanDistance = 20f;
        current.enemyAttackRange = copy.attackRange;
        current.enemyDamage = copy.attackDamage;
        current.enemySpeed = copy.moveSpeed;
        current.enemyCooldown = copy.attackCooldown;
        NavMeshAgent agent = go.GetComponent<NavMeshAgent>();
        agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
        go.transform.SetParent(root.transform, true);
        go.SetActive(true);
        MakeMarker("Enemy marker (red)", go.transform, new Color(0.95f, 0.2f, 0.2f));
        phase = 1;
        phaseStart = Time.realtimeSinceStartup;
    }

    private static void StartObservation()
    {
        Require(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "Enemy failed to bind to runtime NavMesh.");
        if (hall != null)
        {
            var path = new NavMeshPath();
            NavMesh.CalculatePath(enemy.transform.position, new Vector3(-17f, 0f, 10f), NavMesh.AllAreas, path);
            current.hallAccessible = path.status == NavMeshPathStatus.PathComplete;
            Require(current.hallAccessible, "Outside hall fixture is not accessible.");
        }
        if (caseIndex == 0) CaptureSnapshot();
        phase = 2;
        phaseStart = Time.realtimeSinceStartup;
        Sample();
    }

    private static void Sample()
    {
        Require(enemy != null && builder != null, "Enemy or high-HP builder unexpectedly destroyed.");
        float elapsed = Time.realtimeSinceStartup - phaseStart;
        string target = TargetName();
        if (target != previousTarget)
        {
            current.targetChanges.Add(elapsed.ToString("F2") + "s: " + target);
            previousTarget = target;
        }
        if (string.IsNullOrEmpty(current.firstChosenTarget) && enemy.CurrentTarget != null) current.firstChosenTarget = target;
        for (int i = 0; i < walls.Count; i++)
        {
            Health wall = walls[i];
            if ((wall == null || wall.currentHealth < WallHP) && !current.damagedWalls.Contains(wallNames[i]))
                current.damagedWalls.Add(wallNames[i]);
            if (wall != null && !wall.IsDead) continue;
            if (!current.destroyedWalls.Contains(wallNames[i])) current.destroyedWalls.Add(wallNames[i]);
        }
        if (!current.breached && current.destroyedWalls.Count > 0)
        {
            current.breached = true;
            current.breachSeconds = elapsed;
            current.builderHPAtBreach = builder.currentHealth;
        }
        if (!current.breached)
        {
            current.builderHPBeforeBreach = builder.currentHealth;
            current.builderDamagedBeforeBreach |= builder.currentHealth < BuilderHP;
            Require(!current.builderDamagedBeforeBreach, "Builder received damage before any enclosing wall was breached.");
            Require(Mathf.Abs(enemy.transform.position.x) >= 6f || Mathf.Abs(enemy.transform.position.z) >= 6f,
                "Enemy entered closed interior before any wall was breached.");
        }
        current.builderStationary &= Vector3.Distance(builder.transform.position, Vector3.zero) < 0.05f;
        Require(current.builderStationary, "Stationary builder fixture moved.");
        current.builderDamaged |= builder.currentHealth < BuilderHP;
        current.builderReached |= Mathf.Abs(enemy.transform.position.x) < 6f && Mathf.Abs(enemy.transform.position.z) < 6f
            && Vector3.Distance(enemy.transform.position, builder.GetComponent<Collider>().ClosestPoint(enemy.transform.position)) <= current.enemyAttackRange + 0.2f;
        current.hallDamaged |= hall != null && hall.currentHealth < BuilderHP;
        SnapshotFinal();
    }

    private static string TargetName() { return enemy != null && enemy.CurrentTarget != null ? enemy.CurrentTarget.name : "<none>"; }

    private static void SnapshotFinal()
    {
        if (current == null) return;
        current.durationSeconds = Time.realtimeSinceStartup - caseStart;
        if (builder != null) current.builderFinalHP = builder.currentHealth;
        if (hall != null) current.hallFinalHP = hall.currentHealth;
        if (enemy == null) return;
        current.finalTarget = TargetName();
        current.finalState = enemy.State;
        current.finalPosition = enemy.transform.position;
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        current.finalPathStatus = agent.enabled && agent.isOnNavMesh ? agent.pathStatus.ToString() : "OffNavMesh";
    }

    private static void CompleteCase()
    {
        SnapshotFinal();
        Require(!string.IsNullOrEmpty(current.firstChosenTarget), "Enemy never selected any fixture target.");
        if (caseIndex == 0)
        {
            Require(current.breached, "Enemy did not breach the enclosing fence.");
            Require(current.builderDamaged, "Enemy did not switch to the builder after opening a passage.");
            Require(current.destroyedWalls.Count == 1 && current.damagedWalls.Count == 1,
                "Enemy kept attacking unrelated walls after opening a passage to the builder.");
        }
        else
        {
            Require(current.hallDamaged, "Enemy did not preserve accessible town hall priority.");
            Require(current.damagedWalls.Count == 0, "Enemy attacked unrelated walls despite an accessible hall.");
        }
        current.sanityPassed = true;
        current.interpretation = current.hallDamaged
            ? "Accessible outside hall was attacked; builder pursuit is not asserted."
            : current.builderDamaged ? "Builder was damaged after a wall breach."
            : current.breached ? "Wall breach observed, but builder was not damaged within the post-breach window."
            : "No wall breach within observation window; final target/state retained as observed.";
        Debug.Log("[Enclosed builder] " + current.name + ": " + current.interpretation);
        current = null;
        caseIndex++;
    }

    private static GameObject Cube(string name, Vector3 position, Vector3 size, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(root.transform);
        go.transform.position = position;
        go.transform.localScale = size;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Require(shader != null, "No supported unlit shader for evidence snapshot.");
        var material = new Material(shader);
        temporaryAssets.Add(material);
        material.color = color;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static void MakeMarker(string name, Transform parent, Color color)
    {
        GameObject marker = Cube(name, parent.position + Vector3.up * 3f, new Vector3(1.5f, 0.15f, 1.5f), color);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
        marker.transform.SetParent(parent, true);
    }

    private static void CaptureSnapshot()
    {
        GameObject go = new GameObject("Initial overhead evidence camera");
        go.transform.SetParent(root.transform);
        go.transform.position = new Vector3(-5f, 38f, 0f);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 17f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);
        camera.enabled = false;
        RenderTexture previous = RenderTexture.active;
        var target = new RenderTexture(1280, 960, 24);
        var image = new Texture2D(1280, 960, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            string path = LogPath("enemy-ai-enclosed-builder.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            report.screenshot = path;
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
        }
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Require(field != null, target.GetType().Name + "." + name + " missing.");
        field.SetValue(target, value);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void LogReceived(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        EnsureReport();
        report.errors.Add(message + "\n" + stack);
        Persist();
    }

    private static string LogPath(string file)
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, file);
    }

    private static void CleanupWorld()
    {
        enemy = null;
        builder = hall = null;
        walls.Clear();
        wallNames.Clear();
        if (root != null) Object.DestroyImmediate(root);
        if (meshInstance.valid) meshInstance.Remove();
        if (mesh != null) Object.DestroyImmediate(mesh);
        foreach (Object asset in temporaryAssets) if (asset != null) Object.DestroyImmediate(asset);
        temporaryAssets.Clear();
    }

    private static void Finish()
    {
        if (finishing) return;
        finishing = true;
        CleanupWorld();
        WriteReport();
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        else FinalizeInEditMode();
    }

    private static void WriteReport()
    {
        EnsureReport();
        report.finishedUtc = DateTime.UtcNow.ToString("O");
        report.passed = report.errors.Count == 0 && report.scenarios.Count == 2 && report.scenarios.TrueForAll(item => item.sanityPassed);
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText(LogPath("enemy-ai-enclosed-builder.json"), json);
        SessionState.SetString(Prefix + "Result", json);
        Persist();
    }

    private static void FinalizeInEditMode()
    {
        Report completed = JsonUtility.FromJson<Report>(SessionState.GetString(Prefix + "Result", "{}"));
        SessionState.SetBool(Prefix + "Active", false);
        SessionState.SetBool(Prefix + "RuntimeStarted", false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        Application.logMessageReceived -= LogReceived;
        try
        {
            SavedScenes saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(Prefix + "Scenes", "{}"));
            if (saved.scenes != null && Array.TrueForAll(saved.scenes, scene => !string.IsNullOrEmpty(scene.path)))
            {
                var setup = new SceneSetup[saved.scenes.Length];
                for (int i = 0; i < setup.Length; i++)
                    setup[i] = new SceneSetup { path = saved.scenes[i].path, isLoaded = saved.scenes[i].loaded, isActive = saved.scenes[i].active };
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
        finally
        {
            report = null;
            current = null;
            phase = caseIndex = 0;
            lastFrame = -1;
            interruptedReload = finishing = false;
            if (Application.isBatchMode) EditorApplication.Exit(completed != null && completed.passed ? 0 : 1);
        }
    }
}
#endif
