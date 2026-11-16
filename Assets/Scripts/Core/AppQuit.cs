using UnityEngine;

/// <summary>Leaves the game. In a build this quits the application; in the Editor it stops Play Mode.</summary>
public static class AppQuit
{
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
