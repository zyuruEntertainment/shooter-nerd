using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PauseDiagnostic
{
    static PauseDiagnostic()
    {
        EditorApplication.pauseStateChanged += state =>
        {
            Debug.Log(
                $"[PAUSE DIAGNOSTIC] {state} | " +
                $"Playing: {EditorApplication.isPlaying}"
            );
        };

        EditorApplication.playModeStateChanged += state =>
        {
            Debug.Log(
                $"[PLAY MODE] {state}"
            );
        };
    }
}
