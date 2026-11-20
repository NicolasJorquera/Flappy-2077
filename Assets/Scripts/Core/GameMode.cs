using System;
using UnityEngine;

/// <summary>Modos de juego.</summary>
public enum GameMode
{
    Classic = 0, // endless puro (el original)
    Arcade = 1,  // chips, power-ups, combo y jefes
    Daily = 2,   // arcade con la misma ciudad para todos ese día
    Bosses = 3,  // galería de jefes: boss rush desde el jefe elegido
}

public static class GameModes
{
    public static string Key(GameMode m) =>
        m == GameMode.Arcade ? "mode_arcade" : m == GameMode.Daily ? "mode_daily" : m == GameMode.Bosses ? "mode_bosses" : "mode_classic";

    /// <summary>Reglas de arcade (chips, power-ups, jefes) en Arcade y Diario.</summary>
    public static bool HasArcadeRules(GameMode m) => m != GameMode.Classic;

    /// <summary>Rangos por puntaje según el modo (el arcade suma mucho más rápido).</summary>
    public static int[] RankThresholds(GameMode m) =>
        m == GameMode.Classic ? new[] { 0, 10, 25, 50, 100 }
        : m == GameMode.Bosses ? new[] { 0, 25, 75, 150, 300 }
        : new[] { 0, 40, 100, 200, 400 };

    /// <summary>Semilla del desafío diario: fecha UTC (aaaammdd), igual para todos.</summary>
    public static int DailySeed => int.Parse(DateTime.UtcNow.ToString("yyyyMMdd"));
    public static string DailyLabel => DateTime.UtcNow.ToString("dd/MM");
}

/// <summary>
/// Azar del juego (obstáculos, chips, power-ups, jefes). Con semilla fija en el desafío diario
/// para que todos jueguen la misma ciudad; los efectos visuales usan UnityEngine.Random aparte.
/// </summary>
public static class GameRandom
{
    private static System.Random _rnd = new System.Random();

    public static void Reset(int? seed) => _rnd = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

    public static float Value => (float)_rnd.NextDouble();
    public static float Range(float min, float max) => min + (max - min) * Value;
    /// <summary>Entero en [min, max).</summary>
    public static int Range(int min, int max) => _rnd.Next(min, max);
    public static bool Chance(float p) => Value < p;
}

/// <summary>Estadísticas de una partida (para misiones y la pantalla final).</summary>
public class RunStats
{
    public GameMode Mode;
    public int Score;
    public int Gaps;
    public int Chips;
    public int Perfects;
    public int BestStreak;
    public int PowerUps;
    public int Bosses;
    public bool Continued;

    // Lo ya reportado a las misiones (una partida con "continuar" se reporta dos veces).
    public int ReportedChips, ReportedPowerUps, ReportedBosses, ReportedPerfects;
    public bool ReportedGame;
}
