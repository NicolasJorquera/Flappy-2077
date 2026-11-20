using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    English = 0,
    Spanish = 1,
}

/// <summary>
/// Textos del juego en inglés y español. Para agregar un idioma: sumar un valor a
/// <see cref="Language"/>, una columna a cada entrada y su nombre en <see cref="LanguageName"/>.
/// La fuente pixel-art solo tiene mayúsculas, así que los textos se muestran en mayúsculas.
/// </summary>
public static class Loc
{
    private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
    {
        // menú
        { "play", new[] { "PLAY", "JUGAR" } },
        { "options", new[] { "OPTIONS", "OPCIONES" } },
        { "best", new[] { "BEST", "RÉCORD" } },
        { "score", new[] { "SCORE", "PUNTOS" } },
        { "tagline", new[] { "NEO CITY NIGHT RUN", "CARRERA NOCTURNA EN NEO CITY" } },
        // listo / juego
        { "tap_to_fly", new[] { "TAP TO FLY", "TOCA PARA VOLAR" } },
        { "tap_hint_keys", new[] { "OR PRESS SPACE", "O PULSA ESPACIO" } },
        { "paused", new[] { "PAUSED", "PAUSA" } },
        { "resume", new[] { "RESUME", "SEGUIR" } },
        { "menu", new[] { "MENU", "MENÚ" } },
        // fin de partida
        { "game_over", new[] { "CRASHED!", "¡CHOCASTE!" } },
        { "retry", new[] { "RETRY", "OTRA VEZ" } },
        { "new_best", new[] { "NEW BEST!", "¡NUEVO RÉCORD!" } },
        { "rank", new[] { "RANK", "RANGO" } },
        { "rank_none", new[] { "ROOKIE", "NOVATO" } },
        { "rank_bronze", new[] { "STREET RUNNER", "CORREDOR" } },
        { "rank_silver", new[] { "SKY PILOT", "PILOTO" } },
        { "rank_gold", new[] { "NEON ACE", "AS DE NEÓN" } },
        { "rank_neon", new[] { "LEGEND 2077", "LEYENDA 2077" } },
        { "next_rank", new[] { "NEXT RANK AT {0}", "SIGUIENTE RANGO: {0}" } },
        { "continue", new[] { "CONTINUE", "CONTINUAR" } },
        { "continue_ad", new[] { "CONTINUE - WATCH AD", "CONTINUAR - VER ANUNCIO" } },
        { "ad_unavailable", new[] { "NO AD AVAILABLE", "NO HAY ANUNCIO" } },
        { "leaderboard", new[] { "RANKING", "RANKING" } },
        { "no_ads", new[] { "NO ADS", "SIN ANUNCIOS" } },
        { "no_ads_price", new[] { "NO ADS {0}", "SIN ANUNCIOS {0}" } },
        { "no_ads_owned", new[] { "ADS REMOVED", "SIN ANUNCIOS: LISTO" } },
        { "thanks", new[] { "THANK YOU!", "¡GRACIAS!" } },
        { "purchase_failed", new[] { "PURCHASE NOT COMPLETED", "COMPRA NO COMPLETADA" } },
        // opciones
        { "music", new[] { "MUSIC", "MÚSICA" } },
        { "sfx", new[] { "EFFECTS", "EFECTOS" } },
        { "track", new[] { "SONG", "CANCIÓN" } },
        { "shuffle", new[] { "SHUFFLE", "ALEATORIA" } },
        { "track_n", new[] { "TRACK {0:00}", "PISTA {0:00}" } },
        { "language", new[] { "LANGUAGE", "IDIOMA" } },
        { "vibration", new[] { "VIBRATION", "VIBRACIÓN" } },
        { "shake", new[] { "SCREEN SHAKE", "SACUDIDA" } },
        { "on", new[] { "ON", "SÍ" } },
        { "off", new[] { "OFF", "NO" } },
        { "privacy", new[] { "PRIVACY", "PRIVACIDAD" } },
        { "back", new[] { "BACK", "VOLVER" } },
        { "version", new[] { "VERSION {0}", "VERSIÓN {0}" } },
    };

    public static event System.Action Changed;

    public static Language Current => GameSettings.Language;

    public static void SetLanguage(Language language)
    {
        if (GameSettings.Language == language) return;
        GameSettings.Language = language;
        Changed?.Invoke();
    }

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out var row)) return key.ToUpperInvariant();
        int i = (int)Current;
        return i < row.Length && !string.IsNullOrEmpty(row[i]) ? row[i] : row[0];
    }

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);

    /// <summary>Nombre del idioma escrito en ese mismo idioma.</summary>
    public static string LanguageName(Language language)
    {
        switch (language)
        {
            case Language.Spanish: return "ESPAÑOL";
            default: return "ENGLISH";
        }
    }

    public static Language FromSystemLanguage(SystemLanguage system)
    {
        switch (system)
        {
            case SystemLanguage.Spanish:
            case SystemLanguage.Catalan:
            case SystemLanguage.Basque:
                return Language.Spanish;
            default:
                return Language.English;
        }
    }

    public static int LanguageCount => System.Enum.GetValues(typeof(Language)).Length;
}
