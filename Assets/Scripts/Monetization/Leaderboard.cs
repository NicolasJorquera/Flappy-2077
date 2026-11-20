using System.Collections.Generic;
using UnityEngine;
#if FLAPPY_PLAY_GAMES && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Rankings mundiales con Google Play Games Services (gratis, sin servidor), uno por modo.
/// Configuración en docs/MONETIZACION.md (App ID 217847290651, proyecto vinculado a SmartMirror).
/// Un modo sin ID de tablero simplemente no sube puntajes; el botón de ranking abre la lista de
/// todos los tableros publicados.
/// </summary>
public static class Leaderboard
{
    /// <summary>ID del tablero clásico ("Best score").</summary>
    public const string LeaderboardId = "CgkIm7bXxasGEAIQAQ";
    /// <summary>Tableros de Arcade y Diario: pegar los IDs al crearlos en Play Console.</summary>
    public const string ArcadeLeaderboardId = "CgkIm7bXxasGEAIQAw";
    public const string DailyLeaderboardId = "CgkIm7bXxasGEAIQBA";

    public static string IdFor(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.Arcade: return ArcadeLeaderboardId;
            case GameMode.Daily: return DailyLeaderboardId;
            default: return LeaderboardId;
        }
    }

#if FLAPPY_PLAY_GAMES && UNITY_ANDROID && !UNITY_EDITOR
    private static bool _started;
    private static readonly Dictionary<GameMode, int> Pending = new Dictionary<GameMode, int>();

    public static bool Available => !string.IsNullOrEmpty(LeaderboardId);

    /// <summary>Al abrir el juego: inicio de sesión silencioso (Play Games v2 lo hace solo si puede).</summary>
    public static void Init()
    {
        if (_started || !Available) return;
        _started = true;
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(OnSignIn);
    }

    public static void Submit(int score) => Submit(GameMode.Classic, score);

    /// <summary>Sube el puntaje al tablero del modo (Google guarda el mejor de cada jugador).</summary>
    public static void Submit(GameMode mode, int score)
    {
        string id = IdFor(mode);
        if (!Available || string.IsNullOrEmpty(id) || score <= 0) return;
        if (!PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Pending[mode] = Mathf.Max(Pending.TryGetValue(mode, out var p) ? p : 0, score);
            return;
        }
        PlayGamesPlatform.Instance.ReportScore(score, id, ok =>
        {
            if (!ok) Debug.LogWarning("Play Games: no se pudo subir el puntaje");
        });
    }

    /// <summary>Abre la lista de rankings de Google; si no hay sesión, la pide primero.</summary>
    public static void Show()
    {
        if (!Available) return;
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            PlayGamesPlatform.Instance.ShowLeaderboardUI();
            return;
        }
        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            OnSignIn(status);
            if (status == SignInStatus.Success) PlayGamesPlatform.Instance.ShowLeaderboardUI();
        });
    }

    private static void OnSignIn(SignInStatus status)
    {
        if (status != SignInStatus.Success) return;
        // Los récords locales también cuentan (pudieron lograrse sin sesión iniciada).
        foreach (GameMode mode in System.Enum.GetValues(typeof(GameMode)))
        {
            int best = Mathf.Max(Pending.TryGetValue(mode, out var p) ? p : 0, GameSettings.BestFor(mode));
            Submit(mode, best);
        }
        Pending.Clear();
    }
#else
    public static bool Available => false;
    public static void Init() { }
    public static void Submit(int score) { }
    public static void Submit(GameMode mode, int score) { }
    public static void Show() { }
#endif
}
