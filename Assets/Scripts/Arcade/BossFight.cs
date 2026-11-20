using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelea contra un jefe en el Arcade. El auto dispara solo (pulsos de neón): el jugador apunta
/// con la altura y esquiva, con el mismo control de un toque.
///
/// Jefes (se repiten en ciclo, cada vuelta con más vida y más rápidos):
///  0. Dron patrulla: sube y baja; abanicos de 3 y luego 5 disparos.
///  1. Cañonera corporativa: sigue tu altura; ráfagas rectas y un láser horizontal con aviso.
///  2. Gusano del metro: recorre la derecha en "ocho"; suelta minas que flotan hacia ti.
/// Las colisiones son geométricas (distancias y rectángulos), no de física.
/// </summary>
public class BossFight : MonoBehaviour
{
    private class Shot
    {
        public Transform T;
        public Vector2 Vel;
        public float Radius;
        public bool Mine;
    }

    private static readonly string[] NameKeys = { "boss_drone", "boss_gunship", "boss_worm" };
    private const float Scale = 1.5f;
    private const float FireInterval = 0.22f;

    private ArcadeDirector _director;
    private LogicScript _logic;
    private int _kind, _cycle;
    private float _hp, _maxHp;
    private SpriteRenderer _body;
    private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();
    private readonly List<Vector3> _trail = new List<Vector3>();
    private readonly List<Shot> _enemyShots = new List<Shot>();
    private readonly List<Transform> _playerShots = new List<Transform>();
    private Sprite _shotPlayer, _shotEnemy, _mine;
    private Material _unlit;
    private AudioSource _audio;
    private AudioClip _shootClip, _hitClip, _downClip, _alarmClip;

    private enum Phase { Entering, Fighting, Dying }
    private Phase _phase = Phase.Entering;
    private float _t, _attackTimer, _fireTimer, _flash, _laserTimer, _volley;
    private float _homeX = 5.2f;
    private SpriteRenderer _laser;
    private float _laserState; // >0 avisando, <0 disparando
    private float _laserY;
    private bool _frozen;

    public string NameKey => NameKeys[_kind];
    public float HealthFraction => _maxHp > 0 ? _hp / _maxHp : 0f;

    public static BossFight Begin(ArcadeDirector director, LogicScript logic, int index)
    {
        var go = new GameObject("Boss");
        var b = go.AddComponent<BossFight>();
        b._director = director;
        b._logic = logic;
        b._kind = index % 3;
        b._cycle = index / 3;
        b.Build();
        return b;
    }

    private float Speed => 1f + _cycle * 0.2f;

    private void Build()
    {
        _unlit = Resources.Load<Material>("Materials/SpriteUnlit");
        _shotPlayer = Resources.Load<Sprite>("Pixel/shot_player");
        _shotEnemy = Resources.Load<Sprite>("Pixel/shot_enemy");
        _mine = Resources.Load<Sprite>("Pixel/mine");
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _shootClip = Resources.Load<AudioClip>("Audio/shoot");
        _hitClip = Resources.Load<AudioClip>("Audio/boss_hit");
        _downClip = Resources.Load<AudioClip>("Audio/boss_down");
        _alarmClip = Resources.Load<AudioClip>("Audio/alarm");

        _maxHp = _hp = (_kind == 0 ? 18f : _kind == 1 ? 30f : 40f) * (1f + _cycle * 0.35f);
        string sprite = _kind == 0 ? "boss_drone" : _kind == 1 ? "boss_gunship" : "boss_worm_head";
        _body = MakeSprite(transform, Resources.Load<Sprite>("Pixel/" + sprite), 6);
        _body.transform.localScale = Vector3.one * Scale;
        transform.position = new Vector3(13f, 0.5f, 0f);
        if (_kind == 2)
        {
            var seg = Resources.Load<Sprite>("Pixel/boss_worm_seg");
            for (int i = 0; i < 5; i++)
            {
                var s = MakeSprite(null, seg, 5);
                s.transform.localScale = Vector3.one * Scale;
                s.transform.position = transform.position + Vector3.right * (i + 1) * 1.2f;
                _segments.Add(s);
            }
            _homeX = 4.4f;
        }
        Sfx.Play(_audio, _alarmClip, 0.9f);
        _logic.UI?.ShowBossWarning(Loc.Get(NameKey));
        Fx.Instance?.Shake(0.15f, 0.6f);
    }

    private SpriteRenderer MakeSprite(Transform parent, Sprite sprite, int order)
    {
        var go = new GameObject(sprite != null ? sprite.name : "sprite");
        if (parent != null) go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = _unlit;
        sr.sortingOrder = order;
        return sr;
    }

    private void OnDestroy()
    {
        foreach (var s in _segments) if (s != null) Destroy(s.gameObject);
        foreach (var s in _enemyShots) if (s.T != null) Destroy(s.T.gameObject);
        foreach (var s in _playerShots) if (s != null) Destroy(s.gameObject);
        if (_laser != null) Destroy(_laser.gameObject);
    }

    public void OnPlayerCrash() => _frozen = true;

    public void OnPlayerRevive()
    {
        _frozen = false;
        foreach (var s in _enemyShots) if (s.T != null) Destroy(s.T.gameObject);
        _enemyShots.Clear();
        _laserState = 0f;
        if (_laser != null) _laser.enabled = false;
        _attackTimer = 1.5f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        _t += dt;
        bool playing = _logic.State == GameState.Playing && _logic.carScript.carIsAlive;

        switch (_phase)
        {
            case Phase.Entering:
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(_homeX, transform.position.y, 0f), 5f * dt);
                if (Mathf.Abs(transform.position.x - _homeX) < 0.01f)
                {
                    _phase = Phase.Fighting;
                    _attackTimer = 1.2f;
                    _logic.UI?.ShowBossBar(Loc.Get(NameKey));
                }
                break;
            case Phase.Fighting:
                Move(dt);
                if (playing && !_frozen) Attack(dt);
                break;
            case Phase.Dying:
                transform.position += new Vector3(0.5f, -2.2f, 0f) * dt;
                transform.Rotate(0, 0, 90f * dt);
                if (Random.value < 0.25f) Fx.Instance?.Explosion(transform.position + (Vector3)Random.insideUnitCircle * 1.2f);
                if (_t > 1.4f) Destroy(gameObject);
                break;
        }

        UpdateSegments(dt);
        if (_phase == Phase.Fighting && playing) PlayerFire(dt);
        UpdatePlayerShots(dt);
        UpdateEnemyShots(dt, playing);
        UpdateLaser(dt, playing);
        CheckBodyContact(playing);

        // Destello al recibir daño.
        _flash = Mathf.MoveTowards(_flash, 0f, dt * 6f);
        _body.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.8f), _flash);
        if (_phase == Phase.Fighting) _logic.UI?.SetBossHealth(HealthFraction);
    }

    // ------------------------------------------------------------------ movimiento y ataques

    private void Move(float dt)
    {
        Vector3 p = transform.position;
        switch (_kind)
        {
            case 0:
                p.y = Mathf.Sin(_t * 1.2f * Speed) * 2.4f + 0.4f;
                p.x = _homeX + Mathf.Sin(_t * 0.7f) * 0.4f;
                break;
            case 1:
                if (_laserState == 0f)
                    p.y = Mathf.MoveTowards(p.y, Mathf.Clamp(_director.PlayerCenter.y, -3f, 3.5f), 1.6f * Speed * dt);
                break;
            case 2:
                p.x = _homeX + Mathf.Cos(_t * 0.9f * Speed) * 1.6f;
                p.y = Mathf.Sin(_t * 1.8f * Speed) * 2.6f + 0.3f;
                break;
        }
        transform.position = p;
    }

    private void Attack(float dt)
    {
        _attackTimer -= dt;
        switch (_kind)
        {
            case 0:
                if (_attackTimer <= 0f)
                {
                    _volley++;
                    int n = _volley % 3 == 0 ? 5 : 3;
                    float spread = n == 5 ? 56f : 32f; // huecos claros entre disparos
                    for (int i = 0; i < n; i++)
                    {
                        float a = 180f - spread / 2f + spread * i / (n - 1);
                        FireEnemy(Muzzle(), Dir(a) * 5.5f * Speed);
                    }
                    _attackTimer = 1.8f / Speed;
                }
                break;
            case 1:
                _laserTimer += dt;
                if (_laserTimer > 5f / Speed && _laserState == 0f)
                {
                    _laserTimer = 0f;
                    _laserState = 0.9f; // aviso
                    _laserY = transform.position.y;
                }
                if (_attackTimer <= 0f && _laserState == 0f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float off = (i - 1) * 0.55f;
                        FireEnemy(Muzzle() + new Vector2(0f, off), Vector2.left * 7f * Speed);
                    }
                    _attackTimer = 1.3f / Speed;
                }
                break;
            case 2:
                if (_attackTimer <= 0f)
                {
                    var s = FireEnemy(Muzzle(), new Vector2(-2.6f * Speed, 0f));
                    s.Mine = true;
                    s.Radius = 0.42f;
                    s.T.GetComponent<SpriteRenderer>().sprite = _mine;
                    _attackTimer = 1.1f / Speed;
                }
                break;
        }
    }

    private Vector2 Muzzle()
    {
        var b = _body.bounds;
        return new Vector2(b.min.x + 0.2f, b.center.y);
    }

    private static Vector2 Dir(float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
    }

    private Shot FireEnemy(Vector2 pos, Vector2 vel)
    {
        var sr = MakeSprite(null, _shotEnemy, 7);
        sr.transform.position = pos;
        sr.transform.localScale = Vector3.one * 1.3f;
        var s = new Shot { T = sr.transform, Vel = vel, Radius = 0.2f };
        _enemyShots.Add(s);
        return s;
    }

    private void UpdateSegments(float dt)
    {
        if (_segments.Count == 0) return;
        _trail.Insert(0, transform.position);
        if (_trail.Count > 120) _trail.RemoveAt(_trail.Count - 1);
        for (int i = 0; i < _segments.Count; i++)
        {
            int idx = Mathf.Min(_trail.Count - 1, (i + 1) * 9);
            var target = _trail.Count > 0 ? _trail[idx] + Vector3.right * 0.35f : transform.position;
            _segments[i].transform.position = Vector3.Lerp(_segments[i].transform.position, target, 1f - Mathf.Exp(-12f * dt));
            _segments[i].color = _body.color;
            if (_phase == Phase.Dying)
            {
                _segments[i].transform.position += Vector3.down * (2f + i) * dt;
            }
        }
    }

    // ------------------------------------------------------------------ disparos del jugador

    private void PlayerFire(float dt)
    {
        _fireTimer -= dt;
        if (_fireTimer > 0f) return;
        _fireTimer = FireInterval;
        var sr = MakeSprite(null, _shotPlayer, 7);
        Vector2 car = _director.PlayerCenter;
        sr.transform.position = car + new Vector2(0.65f, 0.02f);
        sr.transform.localScale = Vector3.one * 1.4f;
        _playerShots.Add(sr.transform);
        Sfx.Play(_audio, _shootClip, 0.25f, Random.Range(0.95f, 1.1f));
    }

    private void UpdatePlayerShots(float dt)
    {
        var hitBox = _body.bounds;
        hitBox.Expand(new Vector3(-hitBox.size.x * 0.15f, -hitBox.size.y * 0.2f, 0f));
        for (int i = _playerShots.Count - 1; i >= 0; i--)
        {
            var s = _playerShots[i];
            if (s == null) { _playerShots.RemoveAt(i); continue; }
            s.position += Vector3.right * 14f * dt;
            bool hit = _phase == Phase.Fighting && (Contains(hitBox, s.position) || HitsSegment(s.position));
            if (hit)
            {
                Damage(1f, s.position);
                Destroy(s.gameObject);
                _playerShots.RemoveAt(i);
            }
            else if (s.position.x > 13f)
            {
                Destroy(s.gameObject);
                _playerShots.RemoveAt(i);
            }
        }
    }

    private bool HitsSegment(Vector3 p)
    {
        foreach (var s in _segments)
            if (Contains(s.bounds, p)) return true;
        return false;
    }

    private static bool Contains(Bounds b, Vector3 p) => p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y && p.y <= b.max.y;

    private void Damage(float amount, Vector3 where)
    {
        _hp -= amount;
        _flash = 1f;
        Sfx.Play(_audio, _hitClip, 0.4f, Random.Range(0.9f, 1.2f));
        Fx.Instance?.ScoreSparks(where, Fx.Cyan, 3);
        if (_hp <= 0f) Die();
    }

    private void Die()
    {
        _phase = Phase.Dying;
        _t = 0f;
        Sfx.Play(_audio, _downClip, 1f);
        Fx.Instance?.Explosion(transform.position);
        Fx.Instance?.Shake(0.4f, 0.6f);
        Haptics.Pulse(80);
        foreach (var s in _enemyShots) if (s.T != null) Destroy(s.T.gameObject);
        _enemyShots.Clear();
        if (_laser != null) _laser.enabled = false;
        _logic.UI?.HideBossBar();
        _director.OnBossDefeated(transform.position, 25 * (1 + _cycle));
    }

    // ------------------------------------------------------------------ peligros para el jugador

    private void UpdateEnemyShots(float dt, bool playing)
    {
        Vector2 car = _director.PlayerCenter;
        for (int i = _enemyShots.Count - 1; i >= 0; i--)
        {
            var s = _enemyShots[i];
            if (s.T == null) { _enemyShots.RemoveAt(i); continue; }
            if (!_frozen)
            {
                Vector2 v = s.Vel;
                if (s.Mine) v.y = Mathf.Sin(Time.time * 3f + i) * 0.8f;
                s.T.position += (Vector3)(v * dt);
                if (s.Mine) s.T.Rotate(0, 0, 120f * dt);
            }
            Vector2 p = s.T.position;
            if (playing && !_director.Invulnerable && Vector2.Distance(p, car) < s.Radius + 0.22f)
            {
                Fx.Instance?.ScoreSparks(p, Fx.Pink, 6);
                Destroy(s.T.gameObject);
                _enemyShots.RemoveAt(i);
                _director.HitPlayer();
                continue;
            }
            if (p.x < -13f || p.y < -7f || p.y > 7f)
            {
                Destroy(s.T.gameObject);
                _enemyShots.RemoveAt(i);
            }
        }
    }

    private void UpdateLaser(float dt, bool playing)
    {
        if (_laserState == 0f) return;
        if (_laser == null)
        {
            _laser = MakeSprite(null, Fx.Square, 4);
        }
        _laser.enabled = true;
        float left = -14f, right = Muzzle().x;
        _laser.transform.position = new Vector3((left + right) / 2f, _laserY, 0f);

        if (_laserState > 0f)
        {
            // Aviso: línea fina que parpadea.
            _laserState -= dt;
            _laser.transform.localScale = new Vector3(right - left, 0.06f, 1f);
            _laser.color = new Color(1f, 0.2f, 0.4f, Mathf.Repeat(Time.time, 0.16f) < 0.08f ? 0.9f : 0.3f);
            if (_laserState <= 0f) _laserState = -0.5f; // dispara
        }
        else
        {
            _laserState += dt;
            _laser.transform.localScale = new Vector3(right - left, 0.45f, 1f);
            _laser.color = new Color(1f, 0.35f, 0.75f, 0.85f);
            Vector2 car = _director.PlayerCenter;
            if (playing && !_director.Invulnerable && Mathf.Abs(car.y - _laserY) < 0.3f && car.x < right) _director.HitPlayer();
            if (_laserState >= 0f)
            {
                _laserState = 0f;
                _laser.enabled = false;
            }
        }
    }

    private void CheckBodyContact(bool playing)
    {
        if (!playing || _phase != Phase.Fighting || _director.Invulnerable) return;
        Vector2 car = _director.PlayerCenter;
        var b = _body.bounds;
        b.Expand(new Vector3(-b.size.x * 0.2f, -b.size.y * 0.3f, 0f));
        if (Contains(b, car) || HitsSegment(car)) _director.HitPlayer();
    }
}
