using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptCleaner
{
    [MenuItem("Tools/Remove Missing Scripts")]
    private static void RemoveMissingScripts()
    {
        int removed = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                removed += CleanHierarchy(root);
        }

        if (removed > 0)
            EditorSceneManager.SaveOpenScenes();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = PrefabUtility.LoadPrefabContents(path);
            int n = CleanHierarchy(prefab);
            if (n > 0)
            {
                removed += n;
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        Debug.Log($"[MissingScriptCleaner] Removed {removed} missing script(s).");
    }

    private static int CleanHierarchy(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
            count += CleanHierarchy(child.gameObject);
        return count;
    }
}
