using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures every gameplay level has a LevelEnd trigger that loads the next
/// scene (and plays the SfxId.LevelEnd "next level" sound on touch).
///
/// Resolution order for the spawn position:
///   1. If the scene contains a GameObject named "LevelEndAnchor", that
///      transform's position is used (drop one wherever you want the
///      finish-line trigger).
///   2. Otherwise the per-scene fallback X coordinate below is used at
///      y = 7.5 (matches Level 1's original placement).
///
/// Why this exists:
///   L2 and L3 lost their original LevelEnd GameObjects when an earlier
///   batch edit corrupted those scenes. Restoring via a bootstrapper avoids
///   any further scene-file surgery and gives a single place to tune.
/// </summary>
public static class LevelEndBootstrapper
{
    private const string ResourcePath = "LevelEnd";
    private const string AnchorName   = "LevelEndAnchor";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnInitialScene()
    {
        EnsureLevelEnd(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureLevelEnd(scene);
    }

    private static void EnsureLevelEnd(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        // Only spawn in actual gameplay levels.
        Vector3? fallback = GetFallbackPosition(scene.name);
        if (fallback == null)
            return;

        // Don't double-spawn if the scene already has one.
        if (Object.FindFirstObjectByType<LevelEndTrigger>() != null)
            return;

        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning(
                "[LevelEndBootstrapper] No prefab at Resources/" + ResourcePath +
                ". Level transition won't work in '" + scene.name + "'.");
            return;
        }

        Vector3 pos = fallback.Value;
        GameObject anchor = GameObject.Find(AnchorName);
        if (anchor != null)
            pos = anchor.transform.position;

        GameObject instance = Object.Instantiate(prefab, pos, Quaternion.identity);
        instance.name = "LevelEnd (Bootstrapped)";
        SceneManager.MoveGameObjectToScene(instance, scene);

        Debug.Log(
            "[LevelEndBootstrapper] Spawned LevelEnd in '" + scene.name + "' at " + pos +
            (anchor != null ? " (LevelEndAnchor)" : " (fallback)"));
    }

    /// <summary>
    /// Fallback world position for each level. The X coordinates are sensible
    /// "far right of the level" defaults — adjust by dropping a
    /// GameObject named "LevelEndAnchor" in the scene to override.
    /// </summary>
    private static Vector3? GetFallbackPosition(string sceneName)
    {
        switch (sceneName)
        {
            case "01_Level1": return new Vector3(151.6f, 7.5f, 0f);
            case "02_Level2": return new Vector3(120f,   3f,   0f);
            case "03_Level3": return new Vector3(120f,   3f,   0f);
            // Boss level finishes via the boss death sequence, not a trigger.
            default: return null;
        }
    }
}
