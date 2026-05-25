using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ensures every gameplay scene has a player HP bar canvas.
///
/// On every scene load, destroys any pre-existing PlayerHealthUI components
/// (their canvas wiring can get orphaned when scenes are edited) and builds
/// a fresh, self-contained HUD canvas with a new PlayerHealthUI on it.
/// The PlayerHealthUI then auto-builds the bar visuals in Start().
/// </summary>
public static class HudBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnInitialScene()
    {
        EnsureHud(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureHud(scene);
    }

    private static void EnsureHud(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        string n = scene.name;
        if (n == "00_MainMenu" || n == "00b_Intro" || n == "05_Victory")
            return;

        // Destroy any pre-existing PlayerHealthUI components — scene-baked
        // ones can get orphaned from their canvas after edits, so we always
        // start clean.
        int destroyed = 0;
        PlayerHealthUI[] existing = Object.FindObjectsByType<PlayerHealthUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            if (existing[i] == null) continue;
            Object.Destroy(existing[i]);
            destroyed++;
        }

        BuildHud(scene);

        if (destroyed > 0)
            Debug.Log("[HudBootstrapper] Replaced " + destroyed +
                      " pre-existing PlayerHealthUI(s) in '" + scene.name + "'.");
    }

    private static void BuildHud(Scene scene)
    {
        GameObject root = new GameObject(
            "PlayerHUD (Bootstrapped)",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        SceneManager.MoveGameObjectToScene(root, scene);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = false;
        group.interactable = false;

        root.AddComponent<PlayerHealthUI>();

        Debug.Log("[HudBootstrapper] Spawned PlayerHUD in scene '" + scene.name + "'.");
    }
}
