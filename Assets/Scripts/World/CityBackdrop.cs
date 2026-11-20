using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fondo de Neo City: cielo con bruma, luna, dos capas de rascacielos con parallax, tráfico
/// aéreo lejano y estrellas que titilan. Se mueve con la velocidad del mundo
/// (<see cref="LogicScript.WorldSpeed"/>), así se detiene al chocar y acelera con la dificultad.
/// </summary>
public class CityBackdrop : MonoBehaviour
{
    private class Layer
    {
        public Transform Root;
        public float Factor;
        public float Width;
    }

    private class Car
    {
        public Transform T;
        public float Speed;
    }

    private readonly List<Layer> _layers = new List<Layer>();
    private readonly List<Car> _traffic = new List<Car>();
    private readonly List<SpriteRenderer> _twinkles = new List<SpriteRenderer>();
    private Sprite[] _trafficSprites;
    private Material _unlit;
    private float _nextCar;
    private Camera _cam;

    public static CityBackdrop Create(Camera cam)
    {
        var go = new GameObject("City Backdrop");
        var b = go.AddComponent<CityBackdrop>();
        b._cam = cam;
        b.Build();
        return b;
    }

    private void Build()
    {
        _unlit = Resources.Load<Material>("Materials/SpriteUnlit");
        float halfH = _cam.orthographicSize;
        float bottom = -halfH;

        // Cielo: cubre toda la vista (480x200 px = 24x10 unidades; se escala si la pantalla es más ancha).
        var sky = MakeSprite("Sky", "Pixel/bg_sky", -100, Color.white);
        float viewW = halfH * 2f * Mathf.Max(_cam.aspect, 16f / 9f);
        float sx = Mathf.Max(1f, viewW / sky.sprite.bounds.size.x);
        float sy = halfH * 2f / sky.sprite.bounds.size.y;
        sky.transform.localScale = new Vector3(sx * 1.1f, sy, 1f);

        var moon = MakeSprite("Moon", "Pixel/bg_moon", -95, new Color(1f, 0.85f, 0.95f, 0.9f));
        moon.transform.position = new Vector3(3.6f, 1.7f, 0f);

        AddLayer("Far skyline", "Pixel/bg_far", -90, 0.12f, bottom, new Color(0.75f, 0.75f, 0.9f, 1f));
        AddLayer("Mid skyline", "Pixel/bg_mid", -70, 0.3f, bottom - 0.6f, Color.white);

        // Bruma de neón al pie de la ciudad (por delante de los edificios, muy sutil).
        var haze = new GameObject("Neon haze").AddComponent<SpriteRenderer>();
        haze.transform.SetParent(transform, false);
        haze.sprite = GradientSprite(new Color(0.62f, 0.13f, 0.85f, 0.38f), new Color(0.62f, 0.13f, 0.85f, 0f));
        haze.sharedMaterial = _unlit;
        haze.sortingOrder = 5;
        haze.transform.position = new Vector3(0f, bottom + 0.9f, 0f);
        haze.transform.localScale = new Vector3(viewW * 1.2f, 1.8f, 1f);

        _trafficSprites = new[]
        {
            Resources.Load<Sprite>("Pixel/traffic_a"),
            Resources.Load<Sprite>("Pixel/traffic_b"),
            Resources.Load<Sprite>("Pixel/traffic_c"),
        };

        var square = Fx.Square;
        for (int i = 0; i < 16; i++)
        {
            var s = new GameObject("Twinkle").AddComponent<SpriteRenderer>();
            s.transform.SetParent(transform, false);
            s.sprite = square;
            s.sharedMaterial = _unlit;
            s.sortingOrder = -99;
            s.transform.position = new Vector3(Random.Range(-viewW / 2, viewW / 2), Random.Range(0.5f, halfH - 0.2f), 0f);
            s.transform.localScale = Vector3.one * (Random.value < 0.3f ? 0.1f : 0.05f);
            _twinkles.Add(s);
        }
    }

    private SpriteRenderer MakeSprite(string name, string path, int order, Color color)
    {
        var r = new GameObject(name).AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = Resources.Load<Sprite>(path);
        r.sharedMaterial = _unlit;
        r.sortingOrder = order;
        r.color = color;
        return r;
    }

    private void AddLayer(string name, string path, int order, float factor, float bottom, Color color)
    {
        var sprite = Resources.Load<Sprite>(path);
        var root = new GameObject(name).transform;
        root.SetParent(transform, false);
        float w = sprite.bounds.size.x, h = sprite.bounds.size.y;
        // 3 copias: cubre pantallas muy anchas y permite repetir sin huecos.
        for (int i = -1; i <= 1; i++)
        {
            var r = new GameObject(name + " " + i).AddComponent<SpriteRenderer>();
            r.transform.SetParent(root, false);
            r.sprite = sprite;
            r.sharedMaterial = _unlit;
            r.sortingOrder = order;
            r.color = color;
            r.transform.localPosition = new Vector3(i * w, bottom + h / 2f, 0f);
        }
        _layers.Add(new Layer { Root = root, Factor = factor, Width = w });
    }

    private static Sprite GradientSprite(Color bottom, Color top)
    {
        var tex = new Texture2D(1, 16, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
        for (int y = 0; y < 16; y++)
        {
            // Escalones de 2 px: degradado "pixelado" coherente con el resto del arte.
            float t = Mathf.Floor(y / 2f) / 7f;
            tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 16), new Vector2(0.5f, 0.5f), 16f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float world = LogicScript.WorldSpeed;

        foreach (var l in _layers)
        {
            var p = l.Root.localPosition;
            p.x -= world * l.Factor * dt;
            if (p.x <= -l.Width) p.x += l.Width;
            l.Root.localPosition = p;
        }

        _nextCar -= dt;
        if (_nextCar <= 0f && _traffic.Count < 7)
        {
            SpawnCar();
            _nextCar = Random.Range(0.5f, 1.8f);
        }

        float limit = _cam.orthographicSize * _cam.aspect + 2f;
        for (int i = _traffic.Count - 1; i >= 0; i--)
        {
            var c = _traffic[i];
            var p = c.T.position;
            p.x += (c.Speed - world * 0.2f) * dt;
            p.y += Mathf.Sin(Time.time * 2f + i) * 0.05f * dt;
            c.T.position = p;
            if (Mathf.Abs(p.x) > limit)
            {
                Destroy(c.T.gameObject);
                _traffic.RemoveAt(i);
            }
        }

        float t = Time.time;
        for (int i = 0; i < _twinkles.Count; i++)
        {
            float a = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(t * (0.6f + i * 0.13f) + i));
            _twinkles[i].color = new Color(0.85f, 0.9f, 1f, a);
        }
    }

    private void SpawnCar()
    {
        bool toRight = Random.value < 0.5f;
        bool near = Random.value < 0.35f;
        float limit = _cam.orthographicSize * _cam.aspect + 1.5f;
        var r = new GameObject("Traffic").AddComponent<SpriteRenderer>();
        r.transform.SetParent(transform, false);
        r.sprite = _trafficSprites[Random.Range(0, _trafficSprites.Length)];
        r.sharedMaterial = _unlit;
        r.sortingOrder = near ? -65 : -85;
        r.flipX = !toRight;
        float scale = near ? 1f : 0.6f;
        r.color = near ? Color.white : new Color(0.7f, 0.7f, 0.85f, 0.85f);
        r.transform.localScale = Vector3.one * scale;
        r.transform.position = new Vector3(toRight ? -limit : limit, Random.Range(near ? -1.5f : -0.5f, 3.6f), 0f);
        float speed = Random.Range(near ? 2.5f : 1f, near ? 4.5f : 2.2f);
        _traffic.Add(new Car { T = r.transform, Speed = toRight ? speed : -speed });
    }
}
