using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de "game feel": partículas de píxeles (estela del propulsor, chispas, explosión)
/// y sacudida de cámara. Las partículas usan un pool para no crear basura en cada frame.
/// </summary>
public class Fx : MonoBehaviour
{
    private struct Particle
    {
        public SpriteRenderer R;
        public Vector2 Vel;
        public float Life, MaxLife, Size, Gravity, Drag;
        public Color Color;
    }

    public static Fx Instance { get; private set; }

    private static Sprite _square;
    private readonly List<Particle> _live = new List<Particle>();
    private readonly Stack<SpriteRenderer> _pool = new Stack<SpriteRenderer>();
    private Material _unlit;

    private Camera _cam;
    private Vector3 _camHome;
    private float _shakeTime, _shakeDuration, _shakeAmount;

    public static readonly Color Pink = new Color(1f, 0.2f, 0.63f);
    public static readonly Color Cyan = new Color(0.25f, 0.96f, 0.82f);
    public static readonly Color Amber = new Color(1f, 0.83f, 0.42f);
    public static readonly Color Coral = new Color(1f, 0.36f, 0.36f);
    public static readonly Color Lilac = new Color(0.92f, 0.51f, 0.9f);

    /// <summary>Sprite blanco de 1x1 unidad (para partículas y destellos).</summary>
    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                _square = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            }
            return _square;
        }
    }

    public static Fx Create(Camera cam)
    {
        var go = new GameObject("Fx");
        var fx = go.AddComponent<Fx>();
        fx._cam = cam;
        fx._camHome = cam.transform.position;
        fx._unlit = Resources.Load<Material>("Materials/SpriteUnlit");
        Instance = fx;
        return fx;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Emit(Vector2 pos, Vector2 vel, Color color, float size, float life, float gravity = 0f, float drag = 1.5f, int order = 2)
    {
        var r = _pool.Count > 0 ? _pool.Pop() : NewRenderer();
        r.gameObject.SetActive(true);
        r.sortingOrder = order;
        r.transform.position = pos;
        r.transform.localScale = Vector3.one * size;
        r.color = color;
        _live.Add(new Particle { R = r, Vel = vel, Life = life, MaxLife = life, Size = size, Gravity = gravity, Drag = drag, Color = color });
    }

    /// <summary>Explosión de choque: chispas de neón en todas direcciones.</summary>
    public void Explosion(Vector2 pos)
    {
        Color[] palette = { Pink, Cyan, Amber, Coral, Color.white, Lilac };
        for (int i = 0; i < 46; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float s = Random.Range(2f, 9f);
            Emit(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, palette[Random.Range(0, palette.Length)],
                Random.Range(0.08f, 0.2f), Random.Range(0.4f, 1.0f), gravity: -6f, drag: 2.5f, order: 6);
        }
        for (int i = 0; i < 10; i++) // humo oscuro
        {
            Emit(pos + Random.insideUnitCircle * 0.3f, new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 2f)),
                new Color(0.16f, 0.06f, 0.3f, 0.8f), Random.Range(0.25f, 0.45f), Random.Range(0.6f, 1.2f), gravity: 0f, drag: 1f, order: 5);
        }
    }

    /// <summary>Destello de puntaje: unos pocos píxeles que suben.</summary>
    public void ScoreSparks(Vector2 pos, Color color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Emit(pos + Random.insideUnitCircle * 0.2f, new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(1.5f, 4f)),
                color, Random.Range(0.06f, 0.12f), Random.Range(0.3f, 0.6f), gravity: -4f, drag: 2f, order: 6);
        }
    }

    public void Shake(float amount, float duration)
    {
        if (!GameSettings.ScreenShake) return;
        _shakeAmount = Mathf.Max(_shakeAmount * (_shakeTime / Mathf.Max(0.001f, _shakeDuration)), amount);
        _shakeDuration = duration;
        _shakeTime = duration;
    }

    private SpriteRenderer NewRenderer()
    {
        var r = new GameObject("px").AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = Square;
        r.sharedMaterial = _unlit;
        return r;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var p = _live[i];
            p.Life -= dt;
            if (p.Life <= 0f)
            {
                p.R.gameObject.SetActive(false);
                _pool.Push(p.R);
                _live.RemoveAt(i);
                continue;
            }
            p.Vel.y += p.Gravity * dt;
            p.Vel *= Mathf.Exp(-p.Drag * dt);
            p.R.transform.position += (Vector3)(p.Vel * dt);
            float t = p.Life / p.MaxLife;
            var c = p.Color;
            c.a *= Mathf.Clamp01(t * 1.6f);
            p.R.color = c;
            // Tamaño en escalones: se ve "pixelado" en vez de encogerse suave.
            p.R.transform.localScale = Vector3.one * (p.Size * Mathf.Ceil(t * 3f) / 3f);
            _live[i] = p;
        }

        if (_shakeTime > 0f)
        {
            _shakeTime -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(_shakeTime / _shakeDuration);
            Vector2 off = Random.insideUnitCircle * _shakeAmount * k * k;
            _cam.transform.position = _camHome + new Vector3(off.x, off.y, 0f);
        }
        else
        {
            _cam.transform.position = _camHome;
        }
    }
}
