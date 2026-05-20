#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the lore intro scene (00b_Intro.unity) from scratch with
/// a black background, a TMP body text element styled for long
/// paragraphs, a "Press SPACE to begin" prompt, an EventSystem,
/// and a LoreIntroController already wired to the body text and
/// prompt. The scene is opened automatically so the user can
/// inspect and tweak before saving.
///
/// Safe to re-run: an existing 00b_Intro.unity is overwritten.
/// Adds the scene to both the legacy EditorBuildSettings.scenes
/// list and (via reflection) to the active Unity 6 Build Profile.
/// </summary>
public static class IntroSceneBuilder
{
    private const string ScenesFolder = "Assets/_Project/Scenes";
    private const string ScenePath    = "Assets/_Project/Scenes/00b_Intro.unity";

    [MenuItem("RoninRun/Setup/Build Intro Scene")]
    public static void BuildIntroScene()
    {
        EnsureFolder(ScenesFolder);

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[IntroSceneBuilder] Build cancelled — user chose not to save current scene.");
            return;
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- camera -------------------------------------------------------
        GameObject camGo = new GameObject("Main Camera",
            typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        Camera cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // --- canvas root --------------------------------------------------
        GameObject root = new GameObject("IntroCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // --- background image (full screen, black) ------------------------
        GameObject bg = NewUI("Background", root.transform, true);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = Color.black;

        // --- body text ----------------------------------------------------
        GameObject bodyGo = NewUI("BodyText", root.transform);
        RectTransform brt = (RectTransform)bodyGo.transform;
        brt.anchorMin = new Vector2(0.1f, 0.25f);
        brt.anchorMax = new Vector2(0.9f, 0.75f);
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;

        TextMeshProUGUI body = bodyGo.AddComponent<TextMeshProUGUI>();
        body.text = "A wandering Ronin, his master slain, his lands burnt to ash.";
        body.fontSize = 56;
        body.alignment = TextAlignmentOptions.Center;
        body.color = new Color(1f, 0.95f, 0.85f, 1f);
        // textWrappingMode replaces the obsolete enableWordWrapping in newer TMP versions.
        body.textWrappingMode = TextWrappingModes.Normal;

        // --- continue prompt ---------------------------------------------
        GameObject promptGo = new GameObject("ContinuePrompt",
            typeof(RectTransform), typeof(CanvasGroup));
        promptGo.transform.SetParent(root.transform, false);
        RectTransform prt = (RectTransform)promptGo.transform;
        prt.anchorMin = new Vector2(0.5f, 0.08f);
        prt.anchorMax = new Vector2(0.5f, 0.08f);
        prt.pivot     = new Vector2(0.5f, 0.5f);
        prt.sizeDelta = new Vector2(900, 80);

        CanvasGroup promptGroup = promptGo.GetComponent<CanvasGroup>();
        promptGroup.alpha = 0f;

        GameObject promptLabel = NewUI("Label", promptGo.transform);
        RectTransform plrt = (RectTransform)promptLabel.transform;
        plrt.anchorMin = Vector2.zero;
        plrt.anchorMax = Vector2.one;
        plrt.offsetMin = Vector2.zero;
        plrt.offsetMax = Vector2.zero;
        TextMeshProUGUI promptText = promptLabel.AddComponent<TextMeshProUGUI>();
        promptText.text = "Press SPACE to begin";
        promptText.fontSize = 36;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = new Color(1f, 1f, 1f, 0.7f);
        promptText.fontStyle = FontStyles.Italic;

        // --- controller ---------------------------------------------------
        GameObject controllerGo = new GameObject("LoreIntroController");
        LoreIntroController controller = controllerGo.AddComponent<LoreIntroController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("continuePromptGroup").objectReferenceValue = promptGroup;
        so.ApplyModifiedPropertiesWithoutUndo();

        // --- event system -------------------------------------------------
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Save the scene.
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        if (!saved)
        {
            Debug.LogError($"[IntroSceneBuilder] Failed to save scene at {ScenePath}.");
            return;
        }

        AddSceneToBuildSettings(ScenePath);

        Debug.Log($"[IntroSceneBuilder] Saved {ScenePath}. " +
                  "Verify scene order in File > Build Profiles: " +
                  "00_MainMenu, 00b_Intro, 01_Level1, ...");
    }

    // ---- build settings registration -------------------------------------

    private static void AddSceneToBuildSettings(string path)
    {
        // Legacy / shared scene list. Still works in Unity 6 when no
        // build profile overrides it.
        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        bool alreadyInShared = false;
        foreach (var s in existing)
        {
            if (s.path == path) { alreadyInShared = true; break; }
        }
        if (!alreadyInShared)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(existing);
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // Unity 6 introduced Build Profiles which can carry their own
        // per-profile scene list that overrides the shared list. We try
        // to add to the active profile via reflection so we don't take
        // a hard dependency on the API (which has moved between
        // Unity versions).
        bool addedToProfile = TryAddToActiveBuildProfile(path);

        if (addedToProfile)
        {
            Debug.Log($"[IntroSceneBuilder] Added {path} to the active Build Profile's " +
                      "scene list AND the shared scene list. " +
                      "Verify order in File > Build Profiles.");
        }
        else
        {
            Debug.LogWarning($"[IntroSceneBuilder] Added {path} to the shared scene list, " +
                             "but could NOT confirm it was added to the active Build Profile. " +
                             "Open File > Build Profiles and drag 00b_Intro into the scene list " +
                             "between 00_MainMenu and 01_Level1 if Play prints a 'scene not found' error.");
        }
    }

    /// <summary>
    /// Best-effort: add the scene to the active Unity 6 Build Profile's
    /// scene list. Uses reflection so this script keeps compiling on
    /// older Unity versions that don't have UnityEditor.Build.Profile.
    /// </summary>
    private static bool TryAddToActiveBuildProfile(string scenePath)
    {
        System.Type profileType = System.Type.GetType(
            "UnityEditor.Build.Profile.BuildProfile, UnityEditor.Build.Profile");
        if (profileType == null) return false;

        System.Reflection.MethodInfo getActive = profileType.GetMethod(
            "GetActiveBuildProfile",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (getActive == null) return false;

        object profile = getActive.Invoke(null, null);
        if (profile == null) return false;

        System.Reflection.PropertyInfo scenesProp = profileType.GetProperty(
            "scenes",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (scenesProp == null) return false;

        EditorBuildSettingsScene[] currentScenes = scenesProp.GetValue(profile) as EditorBuildSettingsScene[];
        if (currentScenes == null) return false;

        foreach (var s in currentScenes)
        {
            if (s != null && s.path == scenePath) return true;
        }

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);
        list.Add(new EditorBuildSettingsScene(scenePath, true));

        try
        {
            scenesProp.SetValue(profile, list.ToArray());
            EditorUtility.SetDirty((Object)profile);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[IntroSceneBuilder] Reflection write to BuildProfile.scenes failed: {e.Message}");
            return false;
        }
    }

    // ---- UI primitives ---------------------------------------------------

    private static GameObject NewUI(string name, Transform parent, bool fillParent = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (fillParent)
        {
            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return go;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
#endif
