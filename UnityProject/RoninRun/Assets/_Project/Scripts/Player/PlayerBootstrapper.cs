using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures a Player exists in every gameplay scene, auto-detects existing
/// scene-placed Players, wires the camera, applies per-scene HP overrides,
/// and configures ground detection.
/// </summary>
public static class PlayerBootstrapper
{
    private const string PlayerResourcePath = "Player";
    private const string SpawnAnchorName    = "PlayerSpawn";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnInitialScene()
    {
        EnsurePlayer(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsurePlayer(scene);
    }

    private static void EnsurePlayer(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        string n = scene.name;
        if (n == "00_MainMenu" || n == "00b_Intro" || n == "05_Victory")
            return;

        GameObject existing = FindExistingPlayer();
        if (existing != null)
        {
            try { existing.tag = "Player"; } catch { }
            bool cameraWired = WireCameraFollow(existing);
            Debug.Log("[PlayerBootstrapper] Found existing Player '" + existing.name +
                      "' in scene '" + scene.name + "'. Camera wired: " + cameraWired);
            return;
        }

        Debug.Log("[PlayerBootstrapper] No existing Player in '" + scene.name +
                  "'. Will spawn from Resources/" + PlayerResourcePath + ".");

        GameObject prefab = Resources.Load<GameObject>(PlayerResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("[PlayerBootstrapper] No Player prefab at Resources/" +
                             PlayerResourcePath + ". Player cannot be spawned.");
            return;
        }

        Vector3 spawnPos = prefab.transform.position;
        GameObject anchor = GameObject.Find(SpawnAnchorName);
        if (anchor != null)
            spawnPos = anchor.transform.position;

        GameObject instance = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
        instance.name = "Player";
        SceneManager.MoveGameObjectToScene(instance, scene);

        int sceneHp = GetSceneMaxHp(scene.name);
        if (sceneHp > 0)
            ApplyMaxHp(instance, sceneHp);

        ConfigureGroundDetection(instance);
        WireCameraFollow(instance);

        Debug.Log("[PlayerBootstrapper] Spawned Player in scene '" + scene.name +
                  (anchor != null ? "' at PlayerSpawn anchor." : "' at prefab origin.") +
                  (sceneHp > 0 ? " HP=" + sceneHp : ""));
    }

    private static void ConfigureGroundDetection(GameObject playerInstance)
    {
        GroundDetector2D detector = playerInstance.GetComponent<GroundDetector2D>();
        if (detector == null)
            detector = playerInstance.AddComponent<GroundDetector2D>();

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogWarning("[PlayerBootstrapper] No 'Ground' layer exists.");
            return;
        }

        LayerMask mask = 1 << groundLayer;
        var t = typeof(GroundDetector2D);
        var bf = System.Reflection.BindingFlags.NonPublic |
                 System.Reflection.BindingFlags.Instance;
        var layerField = t.GetField("groundLayer", bf);
        if (layerField != null)
            layerField.SetValue(detector, mask);
    }

    private static GameObject FindExistingPlayer()
    {
        GameObject tagged = null;
        try { tagged = GameObject.FindGameObjectWithTag("Player"); } catch { }
        if (tagged != null)
            return tagged;

        PlayerMovement2D mv = Object.FindFirstObjectByType<PlayerMovement2D>(FindObjectsInactive.Include);
        if (mv != null)
            return mv.gameObject;

        PlayerHealth hp = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (hp != null)
            return hp.gameObject;

        return null;
    }

    private static bool WireCameraFollow(GameObject playerInstance)
    {
        CameraFollow2D follower = Object.FindFirstObjectByType<CameraFollow2D>();
        if (follower == null)
        {
            Debug.LogWarning("[PlayerBootstrapper] No CameraFollow2D in scene.");
            return false;
        }
        follower.SetTarget(playerInstance.transform);
        return true;
    }

    private static int GetSceneMaxHp(string sceneName)
    {
        switch (sceneName)
        {
            case "04_BossLevel": return 25;
            case "01_Level1":
            case "02_Level2":
            case "03_Level3":   return 10;
            default:            return 0;
        }
    }

    private static void ApplyMaxHp(GameObject playerInstance, int hp)
    {
        PlayerHealth health = playerInstance.GetComponentInChildren<PlayerHealth>(true);
        if (health == null) return;

        var t = typeof(PlayerHealth);
        var bf = System.Reflection.BindingFlags.NonPublic |
                 System.Reflection.BindingFlags.Instance;
        var maxField = t.GetField("maxHp", bf);
        var hpField  = t.GetField("_hp",   bf);
        if (maxField != null) maxField.SetValue(health, hp);
        if (hpField  != null) hpField.SetValue(health, hp);
    }
}
