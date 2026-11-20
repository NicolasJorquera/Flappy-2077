using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Texto de UI con la fuente <see cref="PixelFont"/>. Dibuja sombra y/o contorno de neón,
/// admite varias líneas y se encoge para caber en su rectángulo (útil con textos en español,
/// que suelen ser más largos).
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class PixelText : MaskableGraphic
{
    public enum Align { Left, Center, Right }

    [SerializeField] private string _text = "";
    [SerializeField] private float _pixel = 6f;
    [SerializeField] private Align _align = Align.Center;
    [SerializeField] private bool _shrinkToFit = true;
    [SerializeField] private int _lineGap = 4;

    public Color ShadowColor = new Color(0.04f, 0.0f, 0.12f, 0.9f);
    public bool Shadow = true;
    public Color GlowColor = new Color(1f, 0.2f, 0.63f, 0.55f);
    public bool Glow;

    private string _locKey;
    private object[] _locArgs;

    public override Texture mainTexture => PixelFont.Texture;

    public string text
    {
        get => _text;
        set { if (_text == value) return; _text = value ?? ""; SetVerticesDirty(); }
    }

    public float PixelSize
    {
        get => _pixel;
        set { _pixel = value; SetVerticesDirty(); }
    }

    public Align Alignment
    {
        get => _align;
        set { _align = value; SetVerticesDirty(); }
    }

    /// <summary>Muestra la traducción de <paramref name="key"/> y la actualiza al cambiar el idioma.</summary>
    public void SetKey(string key, params object[] args)
    {
        _locKey = key;
        _locArgs = args;
        Refresh();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
        Loc.Changed += Refresh;
        Refresh();
    }

    protected override void OnDisable()
    {
        Loc.Changed -= Refresh;
        base.OnDisable();
    }

    private void Refresh()
    {
        if (string.IsNullOrEmpty(_locKey)) return;
        text = _locArgs != null && _locArgs.Length > 0 ? Loc.Format(_locKey, _locArgs) : Loc.Get(_locKey);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (string.IsNullOrEmpty(_text)) return;

        string[] lines = _text.Split('\n');
        int maxW = 0;
        foreach (var l in lines) maxW = Mathf.Max(maxW, PixelFont.MeasureLine(l));
        int totalH = lines.Length * PixelFont.CapHeight + (lines.Length - 1) * _lineGap;

        Rect r = rectTransform.rect;
        float px = _pixel;
        float extra = (Glow ? 2 : 0) + (Shadow ? 1 : 0);
        if (_shrinkToFit && maxW > 0)
        {
            float fitW = r.width / (maxW + extra);
            float fitH = r.height / (totalH + PixelFont.AccentRows + extra);
            px = Mathf.Min(px, fitW, fitH);
        }
        // Tamaño de píxel entero cuando cabe: bordes más nítidos.
        if (px >= 2f) px = Mathf.Floor(px);

        float blockTop = r.center.y + totalH * px * 0.5f;
        for (int li = 0; li < lines.Length; li++)
        {
            string line = lines[li];
            float w = PixelFont.MeasureLine(line) * px;
            float x0 = _align == Align.Left ? r.xMin : _align == Align.Right ? r.xMax - w : r.center.x - w * 0.5f;
            float baseY = blockTop - (li * (PixelFont.CapHeight + _lineGap) + PixelFont.CapHeight) * px;

            if (Glow)
            {
                var gc = GlowColor;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (dx != 0 || dy != 0) EmitLine(vh, line, x0 + dx * px, baseY + dy * px, px, gc);
            }
            if (Shadow) EmitLine(vh, line, x0 + px, baseY - px, px, ShadowColor * new Color(1, 1, 1, color.a));
            EmitLine(vh, line, x0, baseY, px, color);
        }
    }

    private static void EmitLine(VertexHelper vh, string line, float x, float baseY, float px, Color32 c)
    {
        foreach (char raw in line)
        {
            char ch = PixelFont.Normalize(raw);
            if (ch == ' ') { x += (PixelFont.SpaceWidth + 1) * px; continue; }
            if (!PixelFont.TryGet(ch, out var g)) continue;

            float w = PixelFont.CellW * px;
            float h = PixelFont.CellH * px;
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(x, baseY), c, new Vector2(g.Uv.xMin, g.Uv.yMin));
            vh.AddVert(new Vector3(x, baseY + h), c, new Vector2(g.Uv.xMin, g.Uv.yMax));
            vh.AddVert(new Vector3(x + w, baseY + h), c, new Vector2(g.Uv.xMax, g.Uv.yMax));
            vh.AddVert(new Vector3(x + w, baseY), c, new Vector2(g.Uv.xMax, g.Uv.yMin));
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
            x += (g.Width + 1) * px;
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetVerticesDirty();
    }
}
