using UnityEngine;

/// <summary>
/// Vibración corta en Android (Vibrator nativo con duración en ms). En otras plataformas no hace nada.
/// Respeta la opción "Vibración".
/// </summary>
public static class Haptics
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject _vibrator;
    private static bool _resolved;
#endif

    public static void Pulse(long milliseconds)
    {
        if (!GameSettings.Vibration) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!_resolved)
            {
                _resolved = true;
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            if (_vibrator == null) return;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                if (version.GetStatic<int>("SDK_INT") >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    using (var oneShot = effect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1))
                    {
                        _vibrator.Call("vibrate", oneShot);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", milliseconds);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Vibración no disponible: " + e.Message);
        }
#endif
    }

#if UNITY_ANDROID
    // Nunca se llama: su sola referencia hace que Unity agregue el permiso VIBRATE al manifiesto.
    private static void EnsureVibratePermission() => Handheld.Vibrate();
#endif
}
