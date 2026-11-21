using UnityEngine;

/// <summary>
/// El juego es solo horizontal, pero debe poder girarse 180° (con la cámara o los botones a
/// cualquier lado) aunque el jugador tenga bloqueada la rotación automática del teléfono.
/// Unity pide "userLandscape", que en Android respeta ese bloqueo y deja la pantalla fija;
/// aquí se pide "sensorLandscape", que sigue al sensor entre los dos horizontales.
/// </summary>
public static class LandscapeRotation
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const int SensorLandscape = 6; // ActivityInfo.SCREEN_ORIENTATION_SENSOR_LANDSCAPE

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            // Sin "using": el runnable se ejecuta después en el hilo de interfaz y necesita la actividad viva.
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                activity.Call("setRequestedOrientation", SensorLandscape);
                activity.Dispose();
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo activar la rotación por sensor: " + e.Message);
        }
    }
#endif
}
