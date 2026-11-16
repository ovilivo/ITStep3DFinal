using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor only (this file lives in an Editor folder, so it is never part of a build).
/// Pressing Play always starts from the first scene in Build Settings (the Intro Scene),
/// whichever scene is currently open. Builds already start from that same scene.
/// To play the open scene directly instead, delete this file or clear Editor Scene Manager's Play Mode Start Scene.
/// </summary>
[InitializeOnLoad]
public static class PlayFromIntroScene
{
    static PlayFromIntroScene()
    {
        Apply();
        EditorBuildSettings.sceneListChanged += Apply;
    }

    private static void Apply()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0 || !scenes[0].enabled)
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.LogWarning("[PlayFromIntroScene] Build Settings has no enabled scene at index 0; Play starts from the open scene.");
            return;
        }

        var startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[0].path);
        if (EditorSceneManager.playModeStartScene != startScene)
            EditorSceneManager.playModeStartScene = startScene;
    }
}
