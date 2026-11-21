using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    RunGaps,       // pasar N edificios en una partida
    RunScore,      // hacer N puntos en una partida (cualquier modo)
    RunStreak,     // N perfectos seguidos en una partida
    TotalChips,    // juntar N chips (acumulado)
    TotalPowerUps, // tomar N power-ups (acumulado)
    TotalBosses,   // derrotar N jefes (acumulado)
    TotalPerfects, // N perfectos (acumulado)
    PlayGames,     // jugar N partidas
    PlayDaily,     // jugar el desafío diario
}

/// <summary>Una misión activa con su progreso.</summary>
[Serializable]
public class Mission
{
    public MissionType Type;
    public int Target;
    public int Progress;
    public int Xp;

    public bool Done => Progress >= Target;

    public string Description => Loc.Format("m_" + Type.ToString().ToLowerInvariant(), Target);
}

/// <summary>
/// Misiones: siempre hay 3 activas. Avanzan en cualquier modo (las de chips, power-ups y jefes
/// solo pueden avanzar en Arcade/Diario). Al cumplir una se gana XP, sube el nivel de piloto
/// (<see cref="GameSettings.Level"/>) y aparece otra. Se guardan en el dispositivo.
/// </summary>
public static class Missions
{
    [Serializable]
    private class SaveData
    {
        public List<Mission> Active = new List<Mission>();
    }

    private const string Key = "pilot.missions";
    public const int Slots = 3;

    /// <summary>Resultado del último reporte, para la pantalla final.</summary>
    public static int LastLevelBefore { get; private set; }
    public static int LastLevelAfter { get; private set; }
    public static int LastXpGained { get; private set; }

    private static SaveData _data;

    public static IReadOnlyList<Mission> Active
    {
        get { Load(); return _data.Active; }
    }

    private static void Load()
    {
        if (_data != null) return;
        string json = GameSettings.GetString(Key);
        try { _data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json); }
        catch (Exception) { _data = new SaveData(); }
        if (_data == null || _data.Active == null) _data = new SaveData();
        _data.Active.RemoveAll(m => m == null || m.Target <= 0);
        while (_data.Active.Count < Slots) _data.Active.Add(NewMission(_data.Active));
        Save();
    }

    private static void Save() => GameSettings.SetString(Key, JsonUtility.ToJson(_data));

    /// <summary>
    /// Aplica una partida. Se llama en cada choque; los acumulados solo suman lo nuevo desde el
    /// último reporte (una partida con "continuar" se reporta dos veces sin contar doble).
    /// Devuelve las misiones cumplidas en este reporte.
    /// </summary>
    public static List<Mission> Report(RunStats s)
    {
        Load();
        var done = new List<Mission>();
        LastLevelBefore = GameSettings.Level;
        LastXpGained = 0;

        int dChips = s.Chips - s.ReportedChips;
        int dPower = s.PowerUps - s.ReportedPowerUps;
        int dBosses = s.Bosses - s.ReportedBosses;
        int dPerfects = s.Perfects - s.ReportedPerfects;
        int dGames = s.ReportedGame ? 0 : 1;
        s.ReportedChips = s.Chips;
        s.ReportedPowerUps = s.PowerUps;
        s.ReportedBosses = s.Bosses;
        s.ReportedPerfects = s.Perfects;
        s.ReportedGame = true;

        for (int i = 0; i < _data.Active.Count; i++)
        {
            var m = _data.Active[i];
            switch (m.Type)
            {
                case MissionType.RunGaps: m.Progress = Mathf.Max(m.Progress, s.Gaps); break;
                case MissionType.RunScore: m.Progress = Mathf.Max(m.Progress, s.Score); break;
                case MissionType.RunStreak: m.Progress = Mathf.Max(m.Progress, s.BestStreak); break;
                case MissionType.TotalChips: m.Progress += dChips; break;
                case MissionType.TotalPowerUps: m.Progress += dPower; break;
                case MissionType.TotalBosses: m.Progress += dBosses; break;
                case MissionType.TotalPerfects: m.Progress += dPerfects; break;
                case MissionType.PlayGames: m.Progress += dGames; break;
                case MissionType.PlayDaily: if (s.Mode == GameMode.Daily) m.Progress += dGames; break;
            }
            if (m.Done)
            {
                m.Progress = m.Target;
                done.Add(m);
                LastXpGained += m.Xp;
                GameSettings.Xp += m.Xp;
                _data.Active[i] = NewMission(_data.Active);
            }
        }
        LastLevelAfter = GameSettings.Level;
        Save();
        return done;
    }

    /// <summary>Nueva misión que no repita el tipo de las activas; la meta crece con el nivel.</summary>
    private static Mission NewMission(List<Mission> current)
    {
        int level = GameSettings.Level;
        float k = 1f + (level - 1) * 0.25f;
        var types = (MissionType[])Enum.GetValues(typeof(MissionType));
        MissionType t;
        int guard = 0;
        do t = types[UnityEngine.Random.Range(0, types.Length)];
        while (current.Exists(m => m.Type == t) && guard++ < 50);

        int target, xp;
        switch (t)
        {
            case MissionType.RunGaps: target = Round(15 * k, 5); xp = 120; break;
            case MissionType.RunScore: target = Round(40 * k, 10); xp = 120; break;
            case MissionType.RunStreak: target = Mathf.Min(12, 3 + level / 2); xp = 150; break;
            case MissionType.TotalChips: target = Round(40 * k, 10); xp = 100; break;
            case MissionType.TotalPowerUps: target = Round(4 * k, 1); xp = 100; break;
            case MissionType.TotalBosses: target = Mathf.Max(1, Mathf.RoundToInt(k)); xp = 200; break;
            case MissionType.TotalPerfects: target = Round(10 * k, 5); xp = 120; break;
            case MissionType.PlayDaily: target = 1; xp = 150; break;
            default: target = Round(3 * k, 1); xp = 80; break;
        }
        return new Mission { Type = t, Target = target, Xp = xp };
    }

    private static int Round(float v, int step) => Mathf.Max(step, Mathf.RoundToInt(v / step) * step);
}
