#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Observe the unmodified MainScene, capture evidence, and leave the Editor playing.</summary>
[InitializeOnLoad]
public static class EnemyAIMainSceneObservation
{
    private const string ActiveKey = "EnemyAIMainSceneObservation.Active";
    private const string StartedKey = "EnemyAIMainSceneObservation.Started";
    private const string ErrorsKey = "EnemyAIMainSceneObservation.Errors";
    private const string CaptureKey = "EnemyAIMainSceneObservation.CaptureRequested";
    private const string SnapshotKey = "EnemyAIMainSceneObservation.Snapshot";
    private const string CaptureTimeKey = "EnemyAIMainSceneObservation.CaptureTime";
    private const string ScenePath = "Assets/Scenes/MainScene.unity";

    [Serializable] private sealed class ErrorList { public List<string> errors = new List<string>(); }
    [Serializable] private sealed class EnemySnapshot
    {
        public string name;
        public Vector3 position;
        public string state;
        public string currentTarget;
        public float health;
        public bool dead;
        public bool agentPresent;
        public bool agentEnabled;
        public bool isOnNavMesh;
        public bool hasPath;
        public bool isStopped;
        public Vector3 velocity;
    }
    [Serializable] private sealed class Snapshot
    {
        public string capturedUtc;
        public string scene;
        public string scenePath;
        public string unityVersion;
        public int frame;
        public float elapsedPlaySeconds;
        public float timeScale;
        public bool paused;
        public int healthCount;
        public int livingHealthCount;
        public int deadHealthCount;
        public int inactiveHealthCount;
        public string screenshot;
        public bool screenshotExists;
        public string observation;
        public List<EnemySnapshot> enemies = new List<EnemySnapshot>();
        public List<string> errors = new List<string>();
    }

    static EnemyAIMainSceneObservation()
    {
        if (SessionState.GetBool(ActiveKey, false)) Attach();
    }

    [MenuItem("Tools/Checks/Observe MainScene Enemy AI")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(ActiveKey, false))
            throw new InvalidOperationException("MainScene observation requires idle Edit Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save or discard scene edits before MainScene observation.");
        if (SessionState.GetBool("EnemyAIPlayModeChecks.Active", false))
            throw new InvalidOperationException("Wait for the synthetic Enemy AI suite to finish first.");

        SessionState.SetBool(ActiveKey, true);
        SessionState.SetString(StartedKey, "");
        SessionState.SetString(ErrorsKey, JsonUtility.ToJson(new ErrorList()));
        SessionState.SetBool(CaptureKey, false);
        SessionState.SetString(SnapshotKey, "");
        Attach();
        Application.runInBackground = true;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }

    private static void Attach()
    {
        EditorApplication.update -= Update;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
            SessionState.SetString(StartedKey, Time.realtimeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            OnLog("MainScene observation was interrupted before capture completed.", "", LogType.Error);
            WriteSnapshot("Observation interrupted; no gameplay settings were changed.");
            Detach();
        }
    }

    private static void Update()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying) return;
        if (!float.TryParse(SessionState.GetString(StartedKey, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float started)) return;
        if (!SessionState.GetBool(CaptureKey, false))
        {
            if (Time.realtimeSinceStartup - started < 15f) return;
            WriteSnapshot("Observational snapshot of the original MainScene; no enemies, spawners, stats, or scene assets were modified.");
            SessionState.SetBool(CaptureKey, true);
            SessionState.SetString(CaptureTimeKey, Time.realtimeSinceStartup.ToString("R", CultureInfo.InvariantCulture));
            ScreenCapture.CaptureScreenshot(ScreenshotPath);
        }
        else
        {
            float.TryParse(SessionState.GetString(CaptureTimeKey, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out float requested);
            Snapshot snapshot = JsonUtility.FromJson<Snapshot>(SessionState.GetString(SnapshotKey, ""));
            bool freshScreenshot = File.Exists(ScreenshotPath) &&
                File.GetLastWriteTimeUtc(ScreenshotPath) >= DateTime.Parse(snapshot.capturedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
            if (!freshScreenshot && Time.realtimeSinceStartup - requested < 8f) return;
            snapshot.screenshotExists = freshScreenshot;
            snapshot.errors = ReadErrors().errors;
            if (!snapshot.screenshotExists) snapshot.errors.Add("ScreenCapture did not produce the requested PNG within 8 seconds.");
            File.WriteAllText(JsonPath, JsonUtility.ToJson(snapshot, true));
            Debug.Log("[EnemyAI MainScene observation] JSON: " + JsonPath + "; screenshot: " + snapshot.screenshotExists + "; Editor remains in Play Mode.");
            Detach();
        }
    }

    private static string JsonPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/enemy-ai-main-scene.json"));
    private static string ScreenshotPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/enemy-ai-main-scene.png"));

    private static void WriteSnapshot(string observation)
    {
        Scene scene = SceneManager.GetActiveScene();
        float.TryParse(SessionState.GetString(StartedKey, "0"), NumberStyles.Float, CultureInfo.InvariantCulture, out float started);
        var snapshot = new Snapshot
        {
            capturedUtc = DateTime.UtcNow.ToString("O"), scene = scene.name, scenePath = scene.path,
            unityVersion = Application.unityVersion, frame = Time.frameCount,
            elapsedPlaySeconds = Time.realtimeSinceStartup - started, timeScale = Time.timeScale,
            paused = EditorApplication.isPaused, screenshot = ScreenshotPath, observation = observation,
            errors = ReadErrors().errors
        };
        foreach (Health health in Object.FindObjectsOfType<Health>(true))
        {
            snapshot.healthCount++;
            if (!health.gameObject.activeInHierarchy) snapshot.inactiveHealthCount++;
            if (health.IsDead) snapshot.deadHealthCount++; else snapshot.livingHealthCount++;
        }
        foreach (EnemyAI enemy in Object.FindObjectsOfType<EnemyAI>(true))
        {
            Health health = enemy.GetComponent<Health>();
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            bool bound = agent != null && agent.enabled && agent.gameObject.activeInHierarchy && agent.isOnNavMesh;
            snapshot.enemies.Add(new EnemySnapshot
            {
                name = enemy.name, position = enemy.transform.position, state = enemy.State,
                currentTarget = enemy.CurrentTarget != null ? enemy.CurrentTarget.name : null,
                health = health != null ? health.currentHealth : -1f, dead = health != null && health.IsDead,
                agentPresent = agent != null, agentEnabled = agent != null && agent.enabled,
                isOnNavMesh = bound, hasPath = bound && agent.hasPath, isStopped = bound && agent.isStopped,
                velocity = bound ? agent.velocity : Vector3.zero
            });
        }
        Directory.CreateDirectory(Path.GetDirectoryName(JsonPath));
        string json = JsonUtility.ToJson(snapshot, true);
        SessionState.SetString(SnapshotKey, json);
        File.WriteAllText(JsonPath, json);
    }

    private static ErrorList ReadErrors()
    {
        string json = SessionState.GetString(ErrorsKey, "");
        return string.IsNullOrEmpty(json) ? new ErrorList() : JsonUtility.FromJson<ErrorList>(json);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        ErrorList errors = ReadErrors();
        errors.errors.Add(message + "\n" + stack);
        SessionState.SetString(ErrorsKey, JsonUtility.ToJson(errors));
    }

    private static void Detach()
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        Application.logMessageReceived -= OnLog;
    }
}
#endif
