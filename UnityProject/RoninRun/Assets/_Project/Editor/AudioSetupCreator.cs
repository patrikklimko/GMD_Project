#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor menu items that bootstrap the audio system: create the
/// SfxLibrary asset (with one entry per SfxId so the inspector is
/// pre-populated) and the AudioManager prefab. The AudioMixer
/// itself still has to be created by hand because Unity's mixer
/// asset format is not friendly to programmatic generation -- but
/// the setup guide walks through it in 30 seconds.
/// </summary>
public static class AudioSetupCreator
{
    private const string SfxLibraryFolder = "Assets/_Project/ScriptableObjects/Audio";
    private const string SfxLibraryPath   = "Assets/_Project/ScriptableObjects/Audio/SfxLibrary.asset";

    private const string PrefabFolder = "Assets/_Project/Prefabs/Core";
    private const string PrefabPath   = "Assets/_Project/Prefabs/Core/AudioManager.prefab";

    [MenuItem("RoninRun/Setup/Create SFX Library Asset")]
    public static void CreateSfxLibrary()
    {
        EnsureFolder(SfxLibraryFolder);

        SfxLibrarySO library =
            AssetDatabase.LoadAssetAtPath<SfxLibrarySO>(SfxLibraryPath);

        bool isNew = library == null;
        if (isNew)
        {
            library = ScriptableObject.CreateInstance<SfxLibrarySO>();
            AssetDatabase.CreateAsset(library, SfxLibraryPath);
        }

        // Use SerializedObject so the underlying private List<Entry> is
        // populated correctly without making the field public.
        SerializedObject so = new SerializedObject(library);
        SerializedProperty entries = so.FindProperty("entries");

        // Reset to the canonical SfxId list so re-runs match the enum.
        entries.ClearArray();
        int idx = 0;
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            if (id == SfxId.None) continue;
            entries.InsertArrayElementAtIndex(idx);
            SerializedProperty entry = entries.GetArrayElementAtIndex(idx);
            entry.FindPropertyRelative("id").enumValueIndex = (int)id;
            entry.FindPropertyRelative("clip").objectReferenceValue = null;
            entry.FindPropertyRelative("defaultVolume").floatValue = 1f;
            idx++;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = library;
        EditorGUIUtility.PingObject(library);

        Debug.Log(isNew
            ? $"[AudioSetupCreator] Created SfxLibrary at {SfxLibraryPath}. " +
              "Drop your AudioClips into the Clip column."
            : $"[AudioSetupCreator] Updated SfxLibrary at {SfxLibraryPath}. " +
              "Existing clip assignments may have been reset to match the SfxId enum.");
    }

    [MenuItem("RoninRun/Setup/Create AudioManager Prefab")]
    public static void CreateAudioManagerPrefab()
    {
        EnsureFolder(PrefabFolder);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(PrefabPath);
        }

        GameObject root = new GameObject("AudioManager");
        AudioManager manager = root.AddComponent<AudioManager>();

        // Try to auto-link the SfxLibrary if it already exists.
        SfxLibrarySO library =
            AssetDatabase.LoadAssetAtPath<SfxLibrarySO>(SfxLibraryPath);
        if (library != null)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("sfxLibrary").objectReferenceValue = library;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        string libNote = library != null
            ? "SfxLibrary auto-linked."
            : "SfxLibrary not found yet -- run 'Create SFX Library Asset' first, then re-run this to link it.";
        Debug.Log($"[AudioSetupCreator] Saved AudioManager prefab to {PrefabPath}. {libNote} " +
                  "Drop the prefab into the 00_MainMenu scene; it persists across scenes via DontDestroyOnLoad.");
    }

    [MenuItem("RoninRun/Setup/Create Audio System (Library + Manager)")]
    public static void CreateBoth()
    {
        CreateSfxLibrary();
        CreateAudioManagerPrefab();
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
