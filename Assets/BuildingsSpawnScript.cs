using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Genera obstáculos. Alterna entre las 10 combinaciones originales (prefabs) y pares armados en
/// tiempo de ejecución con los edificios pixel-art (Assets/Resources/Pixel/bld_* abajo y top_*
/// arriba), con hueco a altura aleatoria.
///
/// Se generan por distancia recorrida (no por tiempo): así la separación entre obstáculos se
/// mantiene aunque el mundo acelere, se frene (ciudad lenta) o se detenga (jefes).
/// Todo el azar pasa por <see cref="GameRandom"/> para que el desafío diario sea igual para todos.
/// </summary>
public class BuildingsSpawnScript : MonoBehaviour
{
    public float spawnRate = 2;   // segundos a velocidad base: separación = spawnRate × 5 unidades
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

    [Range(0f, 1f)] public float newBuildingsChance = 0.55f;
    public float gapSize = 2.5f;
    public float gapMinY = -2.2f;
    public float gapMaxY = 2.4f;

    /// <summary>Se generó un obstáculo: raíz, centro y alto del hueco (para chips y power-ups).</summary>
    public static event Action<GameObject, float, float> Spawned;

    private static readonly Color BuildingTint = new Color(0.82f, 0.82f, 0.82f, 1f);
    private static readonly string[] BottomNames = { "bld_neon", "bld_holo", "bld_capsule", "bld_pagoda", "bld_corp", "bld_shops", "bld_antenna" };
    private static readonly string[] TopNames = { "top_hanging", "top_bridge", "top_billboard", "top_cargo", "top_monorail" };

    private GameObject[] _combinations;
    private Sprite[] _bottoms, _tops;
    private Light2D _lightTemplate;
    private Material _litMaterial;
    private float _distance;
    private int _lastCombination = -1, _lastBottom = -1, _lastTop = -1;

    private float Spacing => spawnRate * LogicScript.BaseWorldSpeed;

    private void Start()
    {
        _combinations = new[] { comb1, comb2, comb3, comb4, comb5, comb6, comb7, comb8, comb9, comb10 };
        _bottoms = Array.ConvertAll(BottomNames, n => Resources.Load<Sprite>("Pixel/" + n));
        _tops = Array.ConvertAll(TopNames, n => Resources.Load<Sprite>("Pixel/" + n));
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
        _distance += LogicScript.WorldSpeed * Time.deltaTime;
        if (_distance >= Spacing)
        {
            _distance -= Spacing;
            SpawnBuildings();
        }
    }

    /// <summary>Adelanta el primer obstáculo: que la partida no empiece con un tramo vacío.</summary>
    public void Prime(float fraction) => _distance = Spacing * Mathf.Clamp01(fraction);

    private void SpawnBuildings()
    {
        if (GameRandom.Chance(newBuildingsChance) && _bottoms[0] != null)
        {
            SpawnGenerated();
            return;
        }
        int index;
        do index = GameRandom.Range(0, _combinations.Length); while (index == _lastCombination && _combinations.Length > 1);
        _lastCombination = index;
        var root = Instantiate(_combinations[index], transform.position, transform.rotation);

        // Hueco de los prefabs originales: el trigger "Middle Trigger".
        var mid = root.GetComponentInChildren<BuildingsMiddleScript>();
        var box = mid != null ? mid.GetComponent<BoxCollider2D>() : null;
        if (box != null)
        {
            Vector3 c = box.transform.TransformPoint(box.offset);
            float h = box.size.y * Mathf.Abs(box.transform.lossyScale.y);
            Spawned?.Invoke(root, c.y, h);
        }
    }

    private void SpawnGenerated()
    {
        float gapY = GameRandom.Range(gapMinY, gapMaxY);
        var root = new GameObject("Generated pair");
        root.transform.position = new Vector3(transform.position.x, 0f, 0f);
        root.AddComponent<BuildingsMoveScript>();

        var bottom = AddBuilding(root.transform, "Bottom Building", _bottoms[Pick(_bottoms.Length, ref _lastBottom)]);
        float bottomH = bottom.sprite.bounds.size.y;
        bottom.transform.localPosition = new Vector3(0f, gapY - gapSize / 2f - bottomH / 2f, 0f);

        var top = AddBuilding(root.transform, "Top Building", _tops[Pick(_tops.Length, ref _lastTop)]);
        float topH = top.sprite.bounds.size.y;
        top.transform.localPosition = new Vector3(GameRandom.Range(-0.25f, 0.25f), gapY + gapSize / 2f + topH / 2f, 0f);

        var trigger = new GameObject("Middle Trigger");
        trigger.transform.SetParent(root.transform, false);
        trigger.transform.localPosition = new Vector3(0f, gapY, 0f);
        var box = trigger.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(0.5f, gapSize + 0.2f);
        trigger.AddComponent<BuildingsMiddleScript>();

        Spawned?.Invoke(root, gapY, gapSize);
    }

    private static int Pick(int count, ref int last)
    {
        int i;
        do i = GameRandom.Range(0, count); while (i == last && count > 1);
        last = i;
        return i;
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
