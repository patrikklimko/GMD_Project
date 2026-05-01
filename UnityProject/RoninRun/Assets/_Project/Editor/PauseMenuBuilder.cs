#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor-only helper that builds the pause-menu canvas hierarchy
/// (with a Pause panel, a Controls subpanel, and all OnClick
/// listeners wired to the matching PauseMenu method) and saves it
/// as a prefab at Assets/_Project/Prefabs/UI/PauseMenuCanvas.prefab.
///
/// Manually building this canvas is roughly 30 clicks plus four
/// drag-drop OnClick wirings -- error-prone, and trivial to miss
/// one. Running this once produces an identical, repeatable result.
///
/// Re-running is safe: any existing prefab at the target path is
/// deleted first so you get a fresh build.
/// </summary>
public static class PauseMenuBuilder
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
    private const string PrefabPath   = "Assets/_Project/Prefabs/UI/PauseMenuCanvas.prefab";

    [MenuItem("RoninRun/Setup/Build Pause Menu Canvas Prefab")]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);

        // Tear down a stale prefab if present so we don't merge state.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        // Construct the live hierarchy in a temporary scene root.
        GameObject root = new GameObject(
            "PauseMenuCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // above HUD

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Add the PauseMenu component now so we can hook references.
        PauseMenu pauseMenu = root.AddComponent<PauseMenu>();

        // Build the panels.
        GameObject pausePanel    = BuildPausePanel(root.transform);
        GameObject controlsPanel = BuildControlsPanel(root.transform);

        pausePanel.SetActive(false);
        controlsPanel.SetActive(false);

        // Wire panel references via SerializedObject so the
        // [SerializeField] privates are set correctly.
        SerializedObject so = new SerializedObject(pauseMenu);
        so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        so.FindProperty("controlsPanel").objectReferenceValue = controlsPanel;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Wire the buttons' OnClicks now that the PauseMenu reference exists.
        WireButton(pausePanel,    "ResumeButton",   pauseMenu, nameof(PauseMenu.OnResumeButton));
        WireButton(pausePanel,    "RestartButton",  pauseMenu, nameof(PauseMenu.OnRestartButton));
        WireButton(pausePanel,    "ControlsButton", pauseMenu, nameof(PauseMenu.OnControlsButton));
        WireButton(pausePanel,    "MainMenuButton", pauseMenu, nameof(PauseMenu.OnMainMenuButton));
        WireButton(controlsPanel, "BackButton",     pauseMenu, nameof(PauseMenu.OnControlsBackButton));

        // Ensure the scene has an EventSystem; if not, add one to the prefab.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            es.transform.SetParent(root.transform, false);
        }

        // Save as prefab.
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[PauseMenuBuilder] Saved prefab to {PrefabPath}. " +
                  "Drag this into each gameplay scene (Level1-3 + Boss).");
    }

    // --- panel builders ----------------------------------------------------

    private static GameObject BuildPausePanel(Transform parent)
    {
        GameObject panel = NewUI("PausePanel", parent, full: true);
        AddDimBackground(panel.transform);

        AddText(panel.transform, "Title", "PAUSED",
            anchorY: 0.78f, fontSize: 96, bold: true);

        float topY = 0.62f;
        float gap  = 0.10f;

        AddTextButton(panel.transform, "ResumeButton",   "Resume",      anchorY: topY - 0 * gap);
        AddTextButton(panel.transform, "RestartButton",  "Restart",     anchorY: topY - 1 * gap);
        AddTextButton(panel.transform, "ControlsButton", "Controls",    anchorY: topY - 2 * gap);
        AddTextButton(panel.transform, "MainMenuButton", "Main Menu",   anchorY: topY - 3 * gap);

        return panel;
    }

    private static GameObject BuildControlsPanel(Transform parent)
    {
        GameObject panel = NewUI("ControlsPanel", parent, full: true);
        AddDimBackground(panel.transform);

        AddText(panel.transform, "Title", "CONTROLS",
            anchorY: 0.82f, fontSize: 84, bold: true);

        AddText(panel.transform, "MoveRow",   "Move        A / D or arrows",      anchorY: 0.66f, fontSize: 40);
        AddText(panel.transform, "JumpRow",   "Jump        Space (double-tap)",   anchorY: 0.58f, fontSize: 40);
        AddText(panel.transform, "AttackRow", "Attack      J or Left Mouse",      anchorY: 0.50f, fontSize: 40);
        AddText(panel.transform, "PauseRow",  "Pause       Esc",                  anchorY: 0.42f, fontSize: 40);

        AddTextButton(panel.transform, "BackButton", "Back", anchorY: 0.20f);

        return panel;
    }

    // --- primitives --------------------------------------------------------

    private static GameObject NewUI(string name, Transform parent, bool full = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        if (full)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        return go;
    }

    private static void AddDimBackground(Transform parent)
    {
        GameObject bg = NewUI("Background", parent, full: true);
        Image img = bg.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.65f);
        img.raycastTarget = true;
    }

    private static void AddText(
        Transform parent, string name, string text,
        float anchorY, int fontSize = 36, bool bold = false)
    {
        GameObject go = NewUI(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(900, 90);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.color = Color.white;
    }

    private static GameObject AddTextButton(
        Transform parent, string name, string label, float anchorY)
    {
        GameObject go = NewUI(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420, 80);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.20f, 0.20f, 0.22f, 1f);
        cb.highlightedColor = new Color(0.30f, 0.30f, 0.34f, 1f);
        cb.pressedColor     = new Color(0.45f, 0.10f, 0.10f, 1f);
        cb.selectedColor    = new Color(0.30f, 0.30f, 0.34f, 1f);
        cb.disabledColor    = new Color(0.10f, 0.10f, 0.10f, 0.6f);
        btn.colors = cb;

        AddText(go.transform, "Label", label, anchorY: 0.5f, fontSize: 40, bold: true);

        return go;
    }

    private static void WireButton(
        GameObject panel, string buttonName,
        PauseMenu pauseMenu, string methodName)
    {
        Button btn = FindChild<Button>(panel.transform, buttonName);
        if (btn == null)
        {
            Debug.LogWarning($"[PauseMenuBuilder] Button '{buttonName}' not found.");
            return;
        }

        UnityAction call =
            (UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityAction), pauseMenu, methodName);

        UnityEventTools.AddPersistentListener(btn.onClick, call);
    }

    private static T FindChild<T>(Transform parent, string childName) where T : Component
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName)
            {
                T comp = t.GetComponent<T>();
                if (comp != null) return comp;
            }
        }
        return null;
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
