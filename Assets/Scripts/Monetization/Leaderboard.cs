using UnityEngine;
#if FLAPPY_PLAY_GAMES && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Tablero mundial de puntajes con Google Play Games Services (gratis, sin servidor).
///
/// Queda desactivado hasta configurar Play Games en Play Console. Para activarlo:
///   1. Play Console → Play Games Services → crear el proyecto y un tablero "Best score".
///   2. Poner su ID en <see cref="LeaderboardId"/>.
///   3. Window → Google Play Games → Setup → Android setup, pegando los recursos que da Play Console
///      (escribe el App ID en el manifiesto).
///   4. Agregar FLAPPY_PLAY_GAMES a Scripting Define Symbols de Android.
/// Sin esos pasos el plugin no se incluye y el botón de ranking no aparece (ver docs/MONETIZACION.md).
/// </summary>
public static class Leaderboard
{
    /// <summary>ID del tablero de Play Console (formato "CgkI...").</summary>
    public const string LeaderboardId = "CgkIm7bXxasGEAIQAQ";

#if FLAPPY_PLAY_GAMES && UNITY_ANDROID && !UNITY_EDITOR
    private static bool _started;
    private static int _pendingScore;

    public static bool Available => !string.IsNullOrEmpty(LeaderboardId);

    /// <summary>Al abrir el juego: inicio de sesión silencioso (Play Games v2 lo hace solo si puede).</summary>
    public static void Init()
    {
        if (_started || !Available) return;
        _started = true;
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(OnSignIn);
    }

    /// <summary>Sube el puntaje (Google guarda el mejor de cada jugador).</summary>
    public static void Submit(int score)
    {
        if (!Available || score <= 0) return;
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            _pendingScore = Mathf.Max(_pendingScore, score);
            return;
        }
        PlayGamesPlatform.Instance.ReportScore(score, LeaderboardId, ok =>
        {
            if (!ok) Debug.LogWarning("Play Games: no se pudo subir el puntaje");
        });
    }

    /// <summary>Abre el ranking de Google; si no hay sesión, la pide primero.</summary>
    public static void Show()
    {
        if (!Available) return;
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI(LeaderboardId);
            return;
        }
        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            OnSignIn(status);
            if (status == SignInStatus.Success) PlayGamesPlatform.Instance.ShowLeaderboardUI(LeaderboardId);
        });
    }

    private static void OnSignIn(SignInStatus status)
    {
        if (status != SignInStatus.Success) return;
        // El récord local también cuenta (pudo lograrse sin sesión iniciada).
        int best = Mathf.Max(_pendingScore, GameSettings.BestScore);
        _pendingScore = 0;
        Submit(best);
    }
#else
    public static bool Available => false;
    public static void Init() { }
    public static void Submit(int score) { }
    public static void Show() { }
#endif
}
