using System;
using UnityEngine;

/// <summary>
/// Opciones del jugador, guardadas en PlayerPrefs (solo en el dispositivo).
/// Cualquier cambio dispara <see cref="Changed"/> para que audio y UI se actualicen al momento.
/// </summary>
public static class GameSettings
{
    public const int VolumeSteps = 10;

    private const string KeyMusic = "opt.music";
    private const string KeySfx = "opt.sfx";
    private const string KeyLang = "opt.lang";
    private const string KeyVibration = "opt.vibration";
    private const string KeyShake = "opt.shake";
    private const string KeyBest = "score.best";
    private const string KeyGames = "stats.games";
    private const string KeyTrack = "opt.track";
    private const string KeyNoAds = "iap.noads";

    /// <summary>Valor de <see cref="MusicTrack"/> que significa "orden aleatorio".</summary>
    public const int Shuffle = -1;

    public static event Action Changed;

    private static bool _loaded;
    private static int _music, _sfx, _best, _games, _track;
    private static Language _language;
    private static bool _vibration, _shake, _noAds;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _music = PlayerPrefs.GetInt(KeyMusic, 6);
        _sfx = PlayerPrefs.GetInt(KeySfx, 8);
        _language = PlayerPrefs.HasKey(KeyLang)
            ? (Language)PlayerPrefs.GetInt(KeyLang)
            : Loc.FromSystemLanguage(Application.systemLanguage);
        if (!Enum.IsDefined(typeof(Language), _language)) _language = Language.English;
        _vibration = PlayerPrefs.GetInt(KeyVibration, 1) == 1;
        _shake = PlayerPrefs.GetInt(KeyShake, 1) == 1;
        _best = PlayerPrefs.GetInt(KeyBest, 0);
        _games = PlayerPrefs.GetInt(KeyGames, 0);
        _track = PlayerPrefs.GetInt(KeyTrack, Shuffle);
        _noAds = PlayerPrefs.GetInt(KeyNoAds, 0) == 1;
    }

    /// <summary>Canción de fondo elegida: índice 0..n-1, o <see cref="Shuffle"/>.</summary>
    public static int MusicTrack
    {
        get { EnsureLoaded(); return _track; }
        set { EnsureLoaded(); _track = Mathf.Max(Shuffle, value); Save(KeyTrack, _track); }
    }

    /// <summary>Volumen de música en pasos 0..10.</summary>
    public static int MusicSteps
    {
        get { EnsureLoaded(); return _music; }
        set { EnsureLoaded(); _music = Mathf.Clamp(value, 0, VolumeSteps); Save(KeyMusic, _music); }
    }

    /// <summary>Volumen de efectos en pasos 0..10.</summary>
    public static int SfxSteps
    {
        get { EnsureLoaded(); return _sfx; }
        set { EnsureLoaded(); _sfx = Mathf.Clamp(value, 0, VolumeSteps); Save(KeySfx, _sfx); }
    }

    /// <summary>Volumen lineal 0..1 con curva perceptual (los pasos bajos suenan bajos de verdad).</summary>
    public static float MusicVolume => Curve(MusicSteps);
    public static float SfxVolume => Curve(SfxSteps);

    public static Language Language
    {
        get { EnsureLoaded(); return _language; }
        set { EnsureLoaded(); _language = value; Save(KeyLang, (int)value); }
    }

    public static bool Vibration
    {
        get { EnsureLoaded(); return _vibration; }
        set { EnsureLoaded(); _vibration = value; Save(KeyVibration, value ? 1 : 0); }
    }

    public static bool ScreenShake
    {
        get { EnsureLoaded(); return _shake; }
        set { EnsureLoaded(); _shake = value; Save(KeyShake, value ? 1 : 0); }
    }

    public static int BestScore
    {
        get { EnsureLoaded(); return _best; }
    }

    public static int GamesPlayed
    {
        get { EnsureLoaded(); return _games; }
    }

    /// <summary>
    /// Copia local de la compra "Sin anuncios" (para que funcione sin conexión). La fuente de verdad
    /// es Google Play: <see cref="NoAdsStore"/> la confirma o la revoca (reembolso) al iniciar.
    /// </summary>
    public static bool AdsRemoved
    {
        get { EnsureLoaded(); return _noAds; }
        set { EnsureLoaded(); if (_noAds == value) return; _noAds = value; Save(KeyNoAds, value ? 1 : 0); }
    }

    /// <summary>Guarda el puntaje si es récord. Devuelve true si lo es.</summary>
    public static bool SubmitScore(int score)
    {
        EnsureLoaded();
        if (score <= _best) return false;
        _best = score;
        PlayerPrefs.SetInt(KeyBest, _best);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Cuenta una partida terminada (una sola vez por partida, aunque haya continuado).</summary>
    public static void CountGame()
    {
        EnsureLoaded();
        _games++;
        PlayerPrefs.SetInt(KeyGames, _games);
        PlayerPrefs.Save();
    }

    private static float Curve(int steps)
    {
        float t = steps / (float)VolumeSteps;
        return t * t;
    }

    private static void Save(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
