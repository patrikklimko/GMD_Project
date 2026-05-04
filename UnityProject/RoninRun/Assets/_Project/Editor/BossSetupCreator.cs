#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor menu items that bootstrap the boss arena: build the boss
/// HP bar canvas prefab, build a black fade-overlay canvas prefab
/// (used by BossDeathSequence), and spawn four teleport anchors in
/// the currently open scene as children of a BossArenaAnchors empty.
///
/// The actual boss GameObject is left manual because it depends on
/// imported sprite art and an animator.
/// </summary>
public static class BossSetupCreator
{
    private const string PrefabFolder    = "Assets/_Project/Prefabs/UI";
    private const string HpBarPrefab     = "Assets/_Project/Prefabs/UI/BossHealthBarCanvas.prefab";
    private const string FadePrefab      = "Assets/_Project/Prefabs/UI/FadeOverlayCanvas.prefab";

    [MenuItem("RoninRun/Setup/Build Boss HP Bar Canvas")]
    public static void BuildBossHealthBarCanvas()
    {
        EnsureFolder(PrefabFolder);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(HpBarPrefab) != null)
        {
            AssetDatabase.DeleteAsset(HpBarPrefab);
        }

        GameObject root = new GameObject(
            "BossHealthBarCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // Bar container at the top.
        GameObject container = NewUI("Container", root.transform);
        RectTransform crt = (RectTransform)container.transform;
        crt.anchorMin = new Vector2(0.5f, 1f);
        crt.anchorMax = new Vector2(0.5f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.anchoredPosition = new Vector2(0f, -40f);
        crt.sizeDelta = new Vector2(1200, 110);

        // Title.
        GameObject titleGo = NewUI("Title", container.transform);
        RectTransform trt = (RectTransform)titleGo.transform;
        trt.anchorMin = new Vector2(0f, 0.55f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "BRINGER OF DEATH";
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        title.fontSize = 42;
        title.color = Color.white;

        // Background frame.
        GameObject frame = NewUI("Frame", container.transform);
        RectTransform frt = (RectTransform)frame.transform;
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(1f, 0.5f);
        frt.offsetMin = new Vector2(20f, 0f);
        frt.offsetMax = new Vector2(-20f, 0f);
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);

        // Fill image (uses Filled type, Horizontal, Left origin).
        GameObject fillGo = NewUI("Fill", frame.transform);
        RectTransform fillRt = (RectTransform)fillGo.transform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(6f, 6f);
        fillRt.offsetMax = new Vector2(-6f, -6f);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.78f, 0.10f, 0.10f, 1f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        // Wire BossHealthBar.
        BossHealthBar bar = root.AddComponent<BossHealthBar>();
        SerializedObject so = new SerializedObject(bar);
        so.FindProperty("fillImage").objectReferenceValue = fillImg;
        so.FindProperty("titleLabel").objectReferenceValue = title;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HpBarPrefab);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[BossSetupCreator] Saved {HpBarPrefab}. Drop it into 04_BossLevel " +
                  "and assign the boss's Health component to its 'Boss Health' field.");
    }

    [MenuItem("RoninRun/Setup/Build Fade Overlay Canvas")]
    public static void BuildFadeOverlayCanvas()
    {
        EnsureFolder(PrefabFolder);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FadePrefab) != null)
        {
            AssetDatabase.DeleteAsset(FadePrefab);
        }

        GameObject root = new GameObject(
            "FadeOverlayCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // above HUD + boss bar

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject overlay = NewUI("Black", root.transform);
        RectTransform rt = (RectTransform)overlay.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = overlay.AddComponent<Image>();
        img.color = Color.black;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, FadePrefab);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[BossSetupCreator] Saved {FadePrefab}. Drop it into 04_BossLevel and " +
                  "assign its CanvasGroup to BossDeathSequence's 'Fade Overlay' field.");
    }

    [MenuItem("RoninRun/Setup/Spawn Boss Teleport Anchors (current scene)")]
    public static void SpawnTeleportAnchors()
    {
        // Place 4 anchors in a rectangle around the world origin so the
        // user only has to drag them to actual arena positions.
        GameObject parent = GameObject.Find("BossArena_Anchors");
        if (parent == null)
        {
            parent = new GameObject("BossArena_Anchors");
        }
        else
        {
            // Clear existing children to keep this idempotent.
            for (int i = parent.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }
        }

        Vector2[] defaults = {
            new Vector2(-6f,  0f),
            new Vector2( 6f,  0f),
            new Vector2(-3f,  3f),
            new Vector2( 3f,  3f),
        };

        for (int i = 0; i < defaults.Length; i++)
        {
            GameObject anchor = new GameObject($"TeleportAnchor_{i + 1}");
            anchor.transform.SetParent(parent.transform, false);
            anchor.transform.position = defaults[i];
        }

        Selection.activeObject = parent;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[BossSetupCreator] Spawned 4 anchors under '{parent.name}' " +
                  "in the current scene. Drag each into position around the boss arena, " +
                  "then assign them to BringerOfDeath.teleportAnchors[].");
    }

    [MenuItem("RoninRun/Setup/Build Boss Scene Helpers (HP bar + Fade + Anchors)")]
    public static void BuildAll()
    {
        BuildBossHealthBarCanvas();
        BuildFadeOverlayCanvas();
        SpawnTeleportAnchors();
    }

    // ---- helpers ----------------------------------------------------------

    private static GameObject NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
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
