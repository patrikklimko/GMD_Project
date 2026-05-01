#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only helper that creates EnemyConfigSO assets for the
/// existing enemy variants with sensible starting values. Lives in
/// an Editor/ folder so it's stripped from runtime builds.
///
/// Why this exists: hand-creating a ScriptableObject and remembering
/// every numeric tunable is tedious and easy to get wrong. Two
/// clicks (menu -> action) and the asset is in the right folder
/// with the documented values. Re-running is safe -- existing
/// assets are not overwritten.
/// </summary>
public static class EnemyConfigCreator
{
    private const string ConfigFolder =
        "Assets/_Project/ScriptableObjects/Enemies";

    [MenuItem("RoninRun/Setup/Create Wizard Config Asset")]
    public static void CreateWizardConfig()
    {
        EnemyConfigSO config = CreateOrLoad("Wizard_Config");
        config.maxHp           = 3;
        config.contactDamage   = 1;
        config.rangedDamage    = 1;
        config.moveSpeed       = 1.8f;
        config.detectionRange  = 7f;
        config.attackRange     = 6f;
        config.attackCooldown  = 2.5f;
        config.attackWindUp    = 0.6f;
        Save(config);
    }

    [MenuItem("RoninRun/Setup/Create Slime Config Asset")]
    public static void CreateSlimeConfig()
    {
        EnemyConfigSO config = CreateOrLoad("Slime_Config");
        config.maxHp           = 2;
        config.contactDamage   = 1;
        config.rangedDamage    = 0;
        config.moveSpeed       = 2f;
        config.detectionRange  = 6f;
        config.attackRange     = 1.2f;
        config.attackCooldown  = 2f;
        config.attackWindUp    = 0.3f;
        Save(config);
    }

    [MenuItem("RoninRun/Setup/Create Boss Config Asset")]
    public static void CreateBossConfig()
    {
        EnemyConfigSO config = CreateOrLoad("Boss_Config");
        config.maxHp           = 25;
        config.contactDamage   = 2;
        config.rangedDamage    = 2;
        config.moveSpeed       = 2.4f;
        config.detectionRange  = 14f;
        config.attackRange     = 2.0f;
        config.attackCooldown  = 1.6f;
        config.attackWindUp    = 0.4f;
        Save(config);
    }

    [MenuItem("RoninRun/Setup/Create All Enemy Configs")]
    public static void CreateAll()
    {
        CreateWizardConfig();
        CreateSlimeConfig();
        CreateBossConfig();
    }

    private static EnemyConfigSO CreateOrLoad(string assetName)
    {
        EnsureFolder(ConfigFolder);

        string path = $"{ConfigFolder}/{assetName}.asset";
        EnemyConfigSO existing =
            AssetDatabase.LoadAssetAtPath<EnemyConfigSO>(path);
        if (existing != null)
        {
            Debug.Log($"[EnemyConfigCreator] Updating existing asset: {path}");
            return existing;
        }

        EnemyConfigSO created = ScriptableObject.CreateInstance<EnemyConfigSO>();
        AssetDatabase.CreateAsset(created, path);
        Debug.Log($"[EnemyConfigCreator] Created new asset: {path}");
        return created;
    }

    private static void Save(EnemyConfigSO config)
    {
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = config;
        EditorGUIUtility.PingObject(config);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string[] parts = folderPath.Split('/');
        string current = parts[0]; // "Assets"
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
