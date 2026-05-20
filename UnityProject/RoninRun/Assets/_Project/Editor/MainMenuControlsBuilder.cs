#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds a MainMenuControlsPanel prefab the user can drop into the
/// existing 00_MainMenu scene without touching the rest of their
/// menu UI. The prefab contains:
///   - a dim full-screen background
///   - a "CONTROLS" heading
///   - four labelled rows showing the keyboard mapping
///   - a Back button (auto-wired to MainMenuUI.OnControlsBackButton
///     once the user assigns the MainMenuUI reference on the
///     prefab's outer GameObject).
///
/// The user wires this prefab as the 'controlsPanel' field on
/// their existing MainMenuUI component.
/// </summary>
public static class MainMenuControlsBuilder
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
    private const string PrefabPath   = "Assets/_Project/Prefabs/UI/MainMenuControlsPanel.prefab";

    [MenuItem("RoninRun/Setup/Build Main Menu Controls Panel Prefab")]
    public static void Build()
    {
        EnsureFolder(PrefabFolder);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        // The prefab root is a single RectTransform that will be
        // dropped as a child of the existing main menu Canvas.
        GameObject root = new GameObject("MainMenuControlsPanel",
            typeof(RectTransform));
        RectTransform rrt = (RectTransform)root.transform;
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero;
        rrt.offsetMax = Vector2.zero;

        // Dim background.
        GameObject bg = NewUI("Background", root.transform, true);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);

        // Heading.
        AddText(root.transform, "Heading", "CONTROLS",
            anchorY: 0.82f, fontSize: 96, bold: true);

        // Rows.
        AddText(root.transform, "MoveRow",   "Move          A / D",                  anchorY: 0.62f, fontSize: 44);
        AddText(root.transform, "JumpRow",   "Jump          W or Space",             anchorY: 0.54f, fontSize: 44);
        AddText(root.transform, "AttackRow", "Attack        J or Left Mouse",        anchorY: 0.46f, fontSize: 44);
        AddText(root.transform, "PauseRow",  "Pause         Esc",                    anchorY: 0.38f, fontSize: 44);

        // Hint line for double-tap-jump combo.
        AddText(root.transform, "TipRow",
            "Tip: double-tap Jump for a double-jump.",
            anchorY: 0.26f, fontSize: 28, italic: true, color: new Color(1f,1f,1f,0.7f));

        // Back button.
        GameObject backBtn = AddTextButton(root.transform, "BackButton", "BACK", anchorY: 0.14f);

        // Try to find an existing MainMenuUI in the scene; if present,
        // pre-wire the Back button. Otherwise the user wires it manually.
        MainMenuUI sceneMenu = Object.FindFirstObjectByType<MainMenuUI>();
        if (sceneMenu != null)
        {
            Button btn = backBtn.GetComponent<Button>();
            UnityAction call = (UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityAction), sceneMenu, nameof(MainMenuUI.OnControlsBackButton));
            UnityEventTools.AddPersistentListener(btn.onClick, call);
            Debug.Log("[MainMenuControlsBuilder] Back button auto-wired to the MainMenuUI " +
                      "in the open scene.");
        }
        else
        {
            Debug.Log("[MainMenuControlsBuilder] No MainMenuUI found in the open scene; " +
                      "wire the Back button's OnClick to MainMenuUI.OnControlsBackButton manually.");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[MainMenuControlsBuilder] Saved {PrefabPath}. " +
                  "Drop it as a child of your main menu Canvas, then drag it to " +
                  "MainMenuUI.Controls Panel.");
    }

    // ---- primitives -------------------------------------------------------

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

    private static void AddText(
        Transform parent, string name, string text,
        float anchorY, int fontSize, bool bold = false, bool italic = false,
        Color? color = null)
    {
        GameObject go = NewUI(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1100, 90);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = (bold ? FontStyles.Bold : FontStyles.Normal)
                      | (italic ? FontStyles.Italic : FontStyles.Normal);
        tmp.color = color ?? Color.white;
    }

    private static GameObject AddTextButton(
        Transform parent, string name, string label, float anchorY)
    {
        GameObject go = NewUI(name, parent);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, anchorY);
        rt.anchorMax = new Vector2(0.5f, anchorY);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360, 80);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.17f, 0.95f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.22f, 0.22f, 0.24f, 1f);
        cb.highlightedColor = new Color(0.32f, 0.32f, 0.36f, 1f);
        cb.pressedColor     = new Color(0.45f, 0.10f, 0.10f, 1f);
        cb.selectedColor    = new Color(0.32f, 0.32f, 0.36f, 1f);
        btn.colors = cb;

        AddText(go.transform, "Label", label,
            anchorY: 0.5f, fontSize: 38, bold: true);

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
