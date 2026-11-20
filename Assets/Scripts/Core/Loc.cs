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
        // modos
        { "mode_classic", new[] { "CLASSIC", "CLÁSICO" } },
        { "mode_arcade", new[] { "ARCADE", "ARCADE" } },
        { "mode_daily", new[] { "DAILY", "DIARIO" } },
        { "mode_bosses", new[] { "BOSSES", "JEFES" } },
        { "hint_bosses", new[] { "BOSS RUSH: DEFEAT THEM ALL", "BOSS RUSH: DERRÓTALOS A TODOS" } },
        { "bosses_title", new[] { "BOSS GALLERY", "GALERÍA DE JEFES" } },
        { "bosses_sub", new[] { "PICK WHERE TO START. THE REST FOLLOW, TOUGHER EACH ROUND.", "ELIGE CON CUÁL EMPEZAR. LUEGO VIENEN LOS DEMÁS, MÁS DUROS." } },
        { "fight", new[] { "FIGHT", "PELEAR" } },
        { "boss_hp", new[] { "HEALTH {0}", "VIDA {0}" } },
        { "boss_defeated", new[] { "DEFEATED", "DERROTADO" } },
        { "boss_new", new[] { "NOT DEFEATED YET", "SIN DERROTAR" } },
        { "boss_drone_desc", new[] { "SHOT FANS", "ABANICOS DE DISPAROS" } },
        { "boss_gunship_desc", new[] { "BURSTS AND LASER", "RÁFAGAS Y LÁSER" } },
        { "boss_worm_desc", new[] { "FLOATING MINES", "MINAS FLOTANTES" } },
        { "today_best", new[] { "TODAY {0}: {1}", "HOY {0}: {1}" } },
        { "hint_arcade", new[] { "GRAB CHIPS AND FLY THROUGH THE CENTER", "JUNTA CHIPS Y PASA POR EL CENTRO" } },
        { "hint_daily", new[] { "DAILY CHALLENGE {0}", "DESAFÍO DEL {0}" } },
        // arcade
        { "perfect", new[] { "PERFECT!", "¡PERFECTO!" } },
        { "combo_up", new[] { "COMBO X{0}!", "¡COMBO X{0}!" } },
        { "pu_shield", new[] { "SHIELD", "ESCUDO" } },
        { "pu_magnet", new[] { "MAGNET", "IMÁN" } },
        { "pu_slow", new[] { "SLOW CITY", "CIUDAD LENTA" } },
        { "pu_double", new[] { "DOUBLE POINTS", "PUNTOS DOBLES" } },
        { "boss_alert", new[] { "WARNING!", "¡ALERTA!" } },
        { "boss_drone", new[] { "PATROL DRONE", "DRON PATRULLA" } },
        { "boss_gunship", new[] { "CORP GUNSHIP", "CAÑONERA CORP" } },
        { "boss_worm", new[] { "SKYTRAIN WORM", "GUSANO DEL METRO" } },
        // piloto
        { "pilot", new[] { "PILOT", "PILOTO" } },
        { "pilot_level", new[] { "PILOT LV {0}", "PILOTO NV {0}" } },
        { "level_short", new[] { "LV {0}", "NV {0}" } },
        { "missions", new[] { "MISSIONS", "MISIONES" } },
        { "colors", new[] { "CAR COLORS", "COLORES DEL AUTO" } },
        { "skin_locked", new[] { "UNLOCKS AT LEVEL {0}", "SE DESBLOQUEA EN NIVEL {0}" } },
        { "mission_done", new[] { "DONE: {0} +{1} XP", "CUMPLIDA: {0} +{1} XP" } },
        { "missions_done", new[] { "{0} MISSIONS DONE +{1} XP", "{0} MISIONES CUMPLIDAS +{1} XP" } },
        { "level_up", new[] { "LEVEL {0}!", "¡NIVEL {0}!" } },
        { "level_up_skin", new[] { "LEVEL {0}! NEW COLOR: {1}", "¡NIVEL {0}! NUEVO COLOR: {1}" } },
        { "skin_original", new[] { "ORIGINAL", "ORIGINAL" } },
        { "skin_pink", new[] { "NEON PINK", "NEÓN ROSA" } },
        { "skin_ice", new[] { "ICE CYAN", "CIAN HIELO" } },
        { "skin_toxic", new[] { "TOXIC", "TÓXICO" } },
        { "skin_gold", new[] { "GOLD", "ORO" } },
        { "skin_midnight", new[] { "MIDNIGHT", "MEDIANOCHE" } },
        { "skin_crimson", new[] { "CRIMSON", "CARMESÍ" } },
        { "skin_holo", new[] { "HOLOGRAM", "HOLOGRAMA" } },
        // misiones
        { "m_rungaps", new[] { "PASS {0} BUILDINGS IN ONE RUN", "PASA {0} EDIFICIOS EN UNA PARTIDA" } },
        { "m_runscore", new[] { "SCORE {0} IN ONE RUN", "HAZ {0} PUNTOS EN UNA PARTIDA" } },
        { "m_runstreak", new[] { "{0} PERFECTS IN A ROW", "{0} PERFECTOS SEGUIDOS" } },
        { "m_totalchips", new[] { "GRAB {0} CHIPS", "JUNTA {0} CHIPS" } },
        { "m_totalpowerups", new[] { "TAKE {0} POWER-UPS", "TOMA {0} POWER-UPS" } },
        { "m_totalbosses", new[] { "DEFEAT {0} BOSSES", "DERROTA {0} JEFES" } },
        { "m_totalperfects", new[] { "MAKE {0} PERFECTS", "HAZ {0} PERFECTOS" } },
        { "m_playgames", new[] { "PLAY {0} RUNS", "JUEGA {0} PARTIDAS" } },
        { "m_playdaily", new[] { "PLAY THE DAILY CHALLENGE", "JUEGA EL DESAFÍO DIARIO" } },
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
