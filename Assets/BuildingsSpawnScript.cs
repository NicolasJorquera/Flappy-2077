using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera obstáculos. Alterna entre las 10 combinaciones originales (prefabs) y pares
/// armados en tiempo de ejecución con los edificios pixel-art nuevos
/// (Assets/Resources/Pixel/bld_* abajo y top_* arriba), con hueco a altura aleatoria.
/// El intervalo se acorta con la velocidad para que la distancia entre obstáculos se mantenga.
/// </summary>
public class BuildingsSpawnScript : MonoBehaviour
{
    public float spawnRate = 2;
    public bool spawn;

    public GameObject comb1;
    public GameObject comb2;
    public GameObject comb3;
    public GameObject comb4;
    public GameObject comb5;
    public GameObject comb6;
    public GameObject comb7;
    public GameObject comb8;
    public GameObject comb9;
    public GameObject comb10;

    [Range(0f, 1f)] public float newBuildingsChance = 0.5f;
    public float gapSize = 2.5f;
    public float gapMinY = -2.2f;
    public float gapMaxY = 2.4f;

    private static readonly Color BuildingTint = new Color(0.82f, 0.82f, 0.82f, 1f);

    private GameObject[] _combinations;
    private Sprite[] _bottoms, _tops;
    private Light2D _lightTemplate;
    private Material _litMaterial;
    private float _timer;
    private int _lastCombination = -1;
    private int _lastBottom = -1;

    private void Start()
    {
        _combinations = new[] { comb1, comb2, comb3, comb4, comb5, comb6, comb7, comb8, comb9, comb10 };
        _bottoms = new[]
        {
            Resources.Load<Sprite>("Pixel/bld_neon"),
            Resources.Load<Sprite>("Pixel/bld_holo"),
            Resources.Load<Sprite>("Pixel/bld_capsule"),
        };
        _tops = new[]
        {
            Resources.Load<Sprite>("Pixel/top_hanging"),
            Resources.Load<Sprite>("Pixel/top_bridge"),
        };
        // Los pares nuevos copian la luz y el material de los originales: misma iluminación.
        if (comb1 != null)
        {
            _lightTemplate = comb1.GetComponentInChildren<Light2D>(true);
            var sr = comb1.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) _litMaterial = sr.sharedMaterial;
        }
        spawn = false;
    }

    private void Update()
    {
        if (!spawn) return;
        _timer += Time.deltaTime;
        float interval = spawnRate / Mathf.Max(0.5f, LogicScript.DifficultySpeed);
        if (_timer >= interval)
        {
            _timer = 0f;
            SpawnBuildings();
        }
    }

    /// <summary>Primer obstáculo antes de tiempo: que la partida no empiece con 2 s vacíos.</summary>
    public void Prime(float fraction) => _timer = spawnRate * Mathf.Clamp01(fraction);

    private void SpawnBuildings()
    {
        if (Random.value < newBuildingsChance && _bottoms[0] != null)
        {
            SpawnGenerated();
            return;
        }
        int index;
        do index = Random.Range(0, _combinations.Length); while (index == _lastCombination && _combinations.Length > 1);
        _lastCombination = index;
        Instantiate(_combinations[index], transform.position, transform.rotation);
    }

    private void SpawnGenerated()
    {
        float gapY = Random.Range(gapMinY, gapMaxY);
        var root = new GameObject("Generated pair");
        root.transform.position = new Vector3(transform.position.x, 0f, 0f);
        root.AddComponent<BuildingsMoveScript>();

        int b;
        do b = Random.Range(0, _bottoms.Length); while (b == _lastBottom && _bottoms.Length > 1);
        _lastBottom = b;
        var bottom = AddBuilding(root.transform, "Bottom Building", _bottoms[b]);
        float bottomH = bottom.sprite.bounds.size.y;
        bottom.transform.localPosition = new Vector3(0f, gapY - gapSize / 2f - bottomH / 2f, 0f);

        var top = AddBuilding(root.transform, "Top Building", _tops[Random.Range(0, _tops.Length)]);
        float topH = top.sprite.bounds.size.y;
        top.transform.localPosition = new Vector3(Random.Range(-0.25f, 0.25f), gapY + gapSize / 2f + topH / 2f, 0f);

        var trigger = new GameObject("Middle Trigger");
        trigger.transform.SetParent(root.transform, false);
        trigger.transform.localPosition = new Vector3(0f, gapY, 0f);
        var box = trigger.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, gapSize + 0.2f);
        trigger.AddComponent<BuildingsMiddleScript>();
    }

    private SpriteRenderer AddBuilding(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = BuildingTint;
        if (_litMaterial != null) sr.sharedMaterial = _litMaterial;
        // Colisión con la silueta real del sprite (no castiga antenas ni bordes transparentes).
        go.AddComponent<PolygonCollider2D>();

        if (_lightTemplate != null)
        {
            var light = Instantiate(_lightTemplate, go.transform, false);
            light.transform.localPosition = Vector3.zero;
            light.transform.localRotation = Quaternion.identity;
            FitLight(light, sprite.bounds.size);
        }
        return sr;
    }

    /// <summary>Escala la luz freeform del original para que cubra el nuevo edificio.</summary>
    private static void FitLight(Light2D light, Vector2 target)
    {
        var path = light.shapePath;
        if (path == null || path.Length == 0) return;
        Vector2 min = path[0], max = path[0];
        foreach (var p in path)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }
        Vector2 size = max - min;
        if (size.x <= 0f || size.y <= 0f) return;
        light.transform.localScale = new Vector3(target.x * 1.15f / size.x, target.y * 1.05f / size.y, 1f);
        light.transform.localPosition = -(Vector3)((min + max) * 0.5f * (Vector2)light.transform.localScale);
    }
}
