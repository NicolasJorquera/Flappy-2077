using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Prueba automática de juego: abre la escena, entra en Play y deja que el piloto automático
/// juegue (ver PlaytestDriver). Sirve para comprobar que se puede puntuar, que la dificultad
/// sube y que el ciclo choque → otra vez funciona, sin teléfono ni navegador.
///
///   Unity -batchmode -projectPath . -executeMethod Playtest.Run -logFile -
///   (sin -quit: la prueba cierra Unity al terminar)
/// </summary>
public static class Playtest
{
    private const string RunningKey = "flappy.playtest";

    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Hook()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false))
            {
                SessionState.SetBool(RunningKey, false);
                new GameObject("Playtest Driver").AddComponent<PlaytestDriver>();
            }
        };
    }
}
