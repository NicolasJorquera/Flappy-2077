using System.Collections.Generic;
using UnityEngine;

public enum PowerUp { None, Shield, Magnet, Slow, Double }

/// <summary>
/// Reglas del modo Arcade (y del Diario): chips de neón, power-ups, combo de "perfecto" y jefes.
///
/// - Chips: +1 (cian), +5 (rosa, cerca del borde del hueco) y +10 (dorado, pegado al borde).
///   También aparecen arcos de chips entre edificios.
/// - Power-ups cada 6–9 obstáculos: escudo (aguanta 1 choque), imán (8 s), ciudad lenta (5 s) y
///   puntos dobles (10 s).
/// - Combo: cruzar el hueco cerca del centro suma una racha; cada 3 "perfectos" seguidos sube el
///   multiplicador (×2, ×3). Un cruce normal la corta.
/// - Jefes: cada 25 edificios se despeja la ciudad y entra un jefe (<see cref="BossFight"/>).
/// </summary>
public class ArcadeDirector : MonoBehaviour
{
    private class Pickup
    {
        public Transform T;
        public SpriteRenderer R;
        public int Value;          // chips: 1, 5 o 10
        public PowerUp Power;      // power-ups
        public bool Pulled;        // atraído por el imán
    }

    public const float PerfectTolerance = 0.32f;
    public const int GapsPerBoss = 25;
    private const float CollectRadius = 0.62f;
    private const float MagnetRadius = 4f;

    private LogicScript _logic;
    private BuildingsSpawnScript _spawner;
    private Transform _car;
    private Collider2D _carCollider;
    private readonly List<Pickup> _pickups = new List<Pickup>();
    private Sprite _chip1, _chip5, _chip10;
    private readonly Dictionary<PowerUp, Sprite> _puSprites = new Dictionary<PowerUp, Sprite>();
    private Material _unlit;
    private AudioSource _audio;
    private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

    private int _streak;
    private int _obstaclesSincePower;
    private int _nextPowerAt;
    private int _gapsSinceBoss;
    private PowerUp _timed = PowerUp.None;
    private float _timedLeft, _timedTotal;
    private bool _shield;
    private float _invulnerable;
    private BossFight _boss;
    private int _bossIndex;
    private bool _waitingForBoss;

    public int Multiplier => 1 + Mathf.Min(2, _streak / 3);
    public int Streak => _streak;
    public bool HasShield => _shield;
    public PowerUp TimedPower => _timed;
    public float TimedFraction => _timedTotal > 0 ? _timedLeft / _timedTotal : 0f;
    public bool BossActive => _boss != null || _waitingForBoss;
    public BossFight Boss => _boss;
    public bool Invulnerable => _invulnerable > 0f;

    public static ArcadeDirector Create(LogicScript logic, BuildingsSpawnScript spawner)
    {
        var go = new GameObject("Arcade Director");
        var d = go.AddComponent<ArcadeDirector>();
        d._logic = logic;
        d._spawner = spawner;
        d._car = logic.car.transform;
        d._carCollider = logic.car.GetComponent<Collider2D>();
        d.Init();
        return d;
    }

    private void Init()
    {
        _unlit = Resources.Load<Material>("Materials/SpriteUnlit");
        _chip1 = Resources.Load<Sprite>("Pixel/chip_1");
        _chip5 = Resources.Load<Sprite>("Pixel/chip_5");
        _chip10 = Resources.Load<Sprite>("Pixel/chip_10");
        _puSprites[PowerUp.Shield] = Resources.Load<Sprite>("Pixel/pu_shield");
        _puSprites[PowerUp.Magnet] = Resources.Load<Sprite>("Pixel/pu_magnet");
        _puSprites[PowerUp.Slow] = Resources.Load<Sprite>("Pixel/pu_slow");
        _puSprites[PowerUp.Double] = Resources.Load<Sprite>("Pixel/pu_double");
        foreach (var n in new[] { "chip_1", "chip_5", "chip_10", "powerup", "shield_break", "perfect" })
            _clips[n] = Resources.Load<AudioClip>("Audio/" + n);
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _nextPowerAt = GameRandom.Range(4, 7);
        BuildingsSpawnScript.Spawned += OnSpawned;
    }

    private void OnDestroy()
    {
        BuildingsSpawnScript.Spawned -= OnSpawned;
        LogicScript.PowerSpeedFactor = 1f;
    }

    // ------------------------------------------------------------------ obstáculos y chips

    private void OnSpawned(GameObject root, float gapY, float gapSize)
    {
        float half = gapSize * 0.5f;
        _obstaclesSincePower++;
        if (_obstaclesSincePower >= _nextPowerAt)
        {
            _obstaclesSincePower = 0;
            _nextPowerAt = GameRandom.Range(6, 10);
            AddPowerUp(root.transform, new Vector3(0f, gapY, 0f), RandomPower());
        }
        else
        {
            float r = GameRandom.Value;
            if (r < 0.08f)
                AddChip(root.transform, new Vector3(0f, gapY + Sign() * (half - 0.32f), 0f), 10);
            else if (r < 0.30f)
                AddChip(root.transform, new Vector3(0f, gapY + Sign() * (half - 0.5f), 0f), 5);
            else if (r < 0.85f)
                AddChip(root.transform, new Vector3(0f, gapY, 0f), 1);
        }

        // Arco de chips entre este obstáculo y el siguiente (a mitad de camino).
        if (GameRandom.Chance(0.45f))
        {
            var arc = new GameObject("Chip arc");
            arc.transform.position = root.transform.position + Vector3.right * (_spawner.spawnRate * LogicScript.BaseWorldSpeed * 0.5f);
            arc.AddComponent<BuildingsMoveScript>();
            float baseY = Mathf.Clamp(gapY + GameRandom.Range(-1.2f, 1.2f), -3f, 3.2f);
            float amp = GameRandom.Range(0.4f, 1.1f);
            int n = GameRandom.Range(4, 7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)(n - 1);
                float x = (t - 0.5f) * 3.2f;
                float y = baseY + Mathf.Sin(t * Mathf.PI) * amp;
                AddChip(arc.transform, new Vector3(x, y, 0f), i == n / 2 && GameRandom.Chance(0.3f) ? 5 : 1);
            }
        }
    }

    private static float Sign() => GameRandom.Chance(0.5f) ? 1f : -1f;

    private PowerUp RandomPower()
    {
        while (true)
        {
            var p = (PowerUp)GameRandom.Range(1, 5);
            if (p == PowerUp.Shield && _shield) continue;
            return p;
        }
    }

    private void AddChip(Transform parent, Vector3 local, int value)
    {
        var sr = MakeSprite(parent, local, value == 10 ? _chip10 : value == 5 ? _chip5 : _chip1, "Chip +" + value);
        _pickups.Add(new Pickup { T = sr.transform, R = sr, Value = value });
    }

    private void AddPowerUp(Transform parent, Vector3 local, PowerUp p)
    {
        var sr = MakeSprite(parent, local, _puSprites[p], "PowerUp " + p);
        sr.transform.localScale = Vector3.one * 1.15f;
        _pickups.Add(new Pickup { T = sr.transform, R = sr, Power = p });
    }

    private SpriteRenderer MakeSprite(Transform parent, Vector3 local, Sprite sprite, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = _unlit;
        sr.sortingOrder = 3;
        return sr;
    }

    // ------------------------------------------------------------------ bucle

    private Vector2 CarCenter =>
        _carCollider != null && _carCollider.enabled ? (Vector2)_carCollider.bounds.center : (Vector2)_car.position;

    private void Update()
    {
        float dt = Time.deltaTime;
        bool alive = _logic.carScript.carIsAlive;
        Vector2 car = CarCenter;
        float t = Time.time;

        for (int i = _pickups.Count - 1; i >= 0; i--)
        {
            var p = _pickups[i];
            if (p.T == null) { _pickups.RemoveAt(i); continue; }

            // Flotación y brillo intermitente.
            p.R.transform.localScale = Vector3.one * ((p.Power != PowerUp.None ? 1.15f : 1f) * (1f + Mathf.Sin(t * 6f + i) * 0.06f));
            if (!alive) continue;

            Vector2 pos = p.T.position;
            float dist = Vector2.Distance(pos, car);
            if (_timed == PowerUp.Magnet && p.Power == PowerUp.None && dist < MagnetRadius) p.Pulled = true;
            if (p.Pulled)
            {
                p.T.position = Vector2.MoveTowards(pos, car, (14f + LogicScript.WorldSpeed) * dt);
                dist = Vector2.Distance(p.T.position, car);
            }
            if (dist < CollectRadius) Collect(p, i);
        }

        // Power-up temporal.
        if (_timed != PowerUp.None)
        {
            _timedLeft -= dt;
            if (_timedLeft <= 0f) EndTimed();
        }
        float targetSlow = _timed == PowerUp.Slow ? 0.6f : 1f;
        LogicScript.PowerSpeedFactor = Mathf.MoveTowards(LogicScript.PowerSpeedFactor, targetSlow, dt * 1.5f);

        // Invulnerabilidad tras romper el escudo: parpadeo y luego vuelve la colisión.
        if (_invulnerable > 0f)
        {
            _invulnerable -= dt;
            _logic.carScript.spriteRenderer.enabled = Mathf.Repeat(t, 0.16f) < 0.1f || _invulnerable <= 0f;
            if (_invulnerable <= 0f && _carCollider != null && alive) _carCollider.enabled = true;
        }

        UpdateBossSchedule();
        _logic.UI?.SetArcadeHud(this);
    }

    private void Collect(Pickup p, int index)
    {
        _pickups.RemoveAt(index);
        Vector3 pos = p.T.position;
        Destroy(p.T.gameObject);

        if (p.Power != PowerUp.None)
        {
            _logic.Stats.PowerUps++;
            Play("powerup", 0.9f);
            Fx.Instance?.ScoreSparks(pos, Fx.Amber, 16);
            _logic.UI?.PopText(pos, Loc.Get("pu_" + p.Power.ToString().ToLowerInvariant()), GameUI.Amber);
            if (p.Power == PowerUp.Shield) _shield = true;
            else
            {
                _timed = p.Power;
                _timedTotal = _timedLeft = p.Power == PowerUp.Magnet ? 8f : p.Power == PowerUp.Slow ? 5f : 10f;
            }
            return;
        }

        _logic.Stats.Chips++;
        int points = p.Value * Multiplier * (_timed == PowerUp.Double ? 2 : 1);
        Play(p.Value == 10 ? "chip_10" : p.Value == 5 ? "chip_5" : "chip_1", 0.9f);
        Color c = p.Value == 10 ? Fx.Amber : p.Value == 5 ? Fx.Pink : Fx.Cyan;
        Fx.Instance?.ScoreSparks(pos, c, p.Value == 1 ? 4 : 10);
        _logic.AddPoints(points, pos, popup: points > 1);
    }

    private void EndTimed()
    {
        _timed = PowerUp.None;
        _timedLeft = _timedTotal = 0f;
    }

    /// <summary>Al cruzar un hueco: actualiza la racha de perfectos y devuelve los puntos.</summary>
    public int OnGapPassed(Collider2D gap)
    {
        float dy = gap != null ? Mathf.Abs(CarCenter.y - gap.bounds.center.y) : 99f;
        if (dy <= PerfectTolerance)
        {
            _streak++;
            _logic.Stats.Perfects++;
            _logic.Stats.BestStreak = Mathf.Max(_logic.Stats.BestStreak, _streak);
            Play("perfect", 0.8f);
            bool levelUp = _streak % 3 == 0 && _streak <= 6;
            _logic.UI?.PopText(_car.position + Vector3.up * 0.8f,
                levelUp ? Loc.Format("combo_up", Multiplier) : Loc.Get("perfect"), levelUp ? GameUI.Pink : GameUI.Cyan);
        }
        else
        {
            _streak = 0;
        }
        _gapsSinceBoss++;
        return Multiplier * (_timed == PowerUp.Double ? 2 : 1);
    }

    /// <summary>El auto chocó: si hay escudo, lo consume y evita el fin de partida.</summary>
    public bool TryAbsorbHit()
    {
        if (_invulnerable > 0f) return true;
        if (!_shield) return false;
        _shield = false;
        _invulnerable = 1.4f;
        if (_carCollider != null) _carCollider.enabled = false;
        Play("shield_break", 1f);
        Fx.Instance?.Explosion(_car.position);
        Fx.Instance?.Shake(0.2f, 0.25f);
        Haptics.Pulse(30);
        return true;
    }

    /// <summary>Puntos con los multiplicadores actuales (para jefes).</summary>
    public int Scaled(int basePoints) => basePoints * Multiplier * (_timed == PowerUp.Double ? 2 : 1);

    public void OnCrash()
    {
        EndTimed();
        _streak = 0;
        _invulnerable = 0f;
        _logic.carScript.spriteRenderer.enabled = true;
        _boss?.OnPlayerCrash();
    }

    public void OnRevive()
    {
        _shield = true; // pequeño regalo al continuar
        _boss?.OnPlayerRevive();
        foreach (var p in _pickups)
            if (p.T != null && p.T.position.x < _car.position.x + 9f) Destroy(p.T.gameObject);
    }

    private void Play(string clip, float vol)
    {
        if (_clips.TryGetValue(clip, out var c)) Sfx.Play(_audio, c, vol);
    }

    // ------------------------------------------------------------------ jefes

    private void UpdateBossSchedule()
    {
        if (_logic.State != GameState.Playing) return;
        if (_boss == null && !_waitingForBoss && _gapsSinceBoss >= GapsPerBoss)
        {
            _waitingForBoss = true;
            _spawner.spawn = false;
        }
        if (_waitingForBoss)
        {
            // Espera a que pasen los edificios que quedan en pantalla.
            foreach (var b in FindObjectsByType<BuildingsMoveScript>())
                if (b.transform.position.x > _car.position.x - 2f) return;
            _waitingForBoss = false;
            _boss = BossFight.Begin(this, _logic, _bossIndex++);
        }
    }

    /// <summary>El jefe fue derrotado: suma puntos, suelta un power-up y vuelve la ciudad.</summary>
    public void OnBossDefeated(Vector3 pos, int basePoints)
    {
        _boss = null;
        _gapsSinceBoss = 0;
        _logic.Stats.Bosses++;
        _logic.AddPoints(Scaled(basePoints), pos, popup: true);
        var drop = new GameObject("Boss drop");
        drop.transform.position = pos;
        drop.AddComponent<BuildingsMoveScript>();
        AddPowerUp(drop.transform, Vector3.zero, RandomPower());
        _spawner.spawn = true;
        _spawner.Prime(0.3f);
    }

    /// <summary>Daño al jugador por un proyectil/jefe: escudo o fin de partida.</summary>
    public void HitPlayer()
    {
        if (!_logic.carScript.carIsAlive) return;
        if (TryAbsorbHit()) return;
        _logic.carScript.CrashFromHazard();
    }

    public Vector2 PlayerCenter => CarCenter;
}
