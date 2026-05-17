using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only repair utilities for card prefabs.
/// Use this when a prefab cannot be saved because a ScriptableObject or another
/// non-MonoBehaviour script was accidentally attached as a component.
/// </summary>
public static class CardPrefabScriptCleaner
{
    private const string CardPrefabPath = "Assets/_Assets/Prefabs/CardPrefab.prefab";

    [MenuItem("Tools/CUSGA/Cards/Clean Invalid Scripts In Open Prefab")]
    public static void CleanInvalidScriptsInOpenPrefab()
    {
        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage == null || prefabStage.prefabContentsRoot == null)
        {
            Debug.LogWarning("CardPrefabScriptCleaner: No prefab is currently open in Prefab Mode.");
            return;
        }

        int removedCount = CleanInvalidScripts(prefabStage.prefabContentsRoot);
        if (removedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(prefabStage.scene);
            Debug.Log($"CardPrefabScriptCleaner: Removed {removedCount} invalid script component(s) from open prefab '{prefabStage.assetPath}'. Save the prefab again now.");
        }
        else
        {
            Debug.Log($"CardPrefabScriptCleaner: No invalid script components found in open prefab '{prefabStage.assetPath}'.");
        }
    }

    [MenuItem("Tools/CUSGA/Cards/Clean Invalid Scripts In CardPrefab Asset")]
    public static void CleanInvalidScriptsInCardPrefabAsset()
    {
        var prefabRoot = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"CardPrefabScriptCleaner: Cannot load prefab at {CardPrefabPath}.");
            return;
        }

        try
        {
            int removedCount = CleanInvalidScripts(prefabRoot);
            if (removedCount > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CardPrefabPath);
                Debug.Log($"CardPrefabScriptCleaner: Removed {removedCount} invalid script component(s) from {CardPrefabPath}.");
            }
            else
            {
                Debug.Log($"CardPrefabScriptCleaner: No invalid script components found in {CardPrefabPath}.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static int CleanInvalidScripts(GameObject root)
    {
        int removedCount = 0;
        var transforms = root.GetComponentsInChildren<Transform>(true);

        foreach (var child in transforms)
        {
            removedCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);

            var behaviours = child.GetComponents<MonoBehaviour>();
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                var script = MonoScript.FromMonoBehaviour(behaviour);
                var scriptClass = script != null ? script.GetClass() : null;
                if (scriptClass != null && typeof(MonoBehaviour).IsAssignableFrom(scriptClass))
                {
                    continue;
                }

                string scriptName = scriptClass != null ? scriptClass.Name : script != null ? script.name : "<unresolved>";
                Debug.LogWarning(
                    $"CardPrefabScriptCleaner: Removing invalid script '{scriptName}' from '{child.name}'. " +
                    "Only MonoBehaviour scripts can be attached to GameObjects.",
                    child.gameObject);

                UnityEngine.Object.DestroyImmediate(behaviour, true);
                removedCount++;
            }
        }

        return removedCount;
    }
}
