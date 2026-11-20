using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fuente pixel-art propia (5x7, mayúsculas) dibujada en código: sin archivos de fuente ni
/// licencias de terceros. Incluye Á É Í Ó Ú Ñ Ü ¿ ¡ para el español.
/// La textura se genera una vez al primer uso.
/// </summary>
public static class PixelFont
{
    public const int CapHeight = 7;
    public const int AccentRows = 3;            // 2 filas de tilde + 1 de separación
    public const int CellW = 5, CellH = CapHeight + AccentRows;
    private const int Pad = 1, Cols = 16;
    public const int SpaceWidth = 3;

    public struct Glyph
    {
        public int Width;
        public Rect Uv;   // celda completa (CellW x CellH)
    }

    private static Texture2D _texture;
    private static Dictionary<char, Glyph> _glyphs;

    public static Texture2D Texture
    {
        get { Build(); return _texture; }
    }

    public static bool TryGet(char c, out Glyph g)
    {
        Build();
        return _glyphs.TryGetValue(c, out g);
    }

    /// <summary>Ancho en píxeles de fuente de una línea (sin el espacio final).</summary>
    public static int MeasureLine(string line)
    {
        int w = 0;
        foreach (char raw in line)
        {
            char c = Normalize(raw);
            if (c == ' ') { w += SpaceWidth + 1; continue; }
            if (TryGet(c, out var g)) w += g.Width + 1;
        }
        return Mathf.Max(0, w - 1);
    }

    public static char Normalize(char c)
    {
        c = char.ToUpperInvariant(c);
        switch (c)
        {
            case 'À': case 'Â': case 'Ä': return 'Á';
            case 'È': case 'Ê': case 'Ë': return 'É';
            case 'Ì': case 'Î': case 'Ï': return 'Í';
            case 'Ò': case 'Ô': case 'Ö': return 'Ó';
            case 'Ù': case 'Û': return 'Ú';
            case '×': return 'X';
            default: return c;
        }
    }

    // ------------------------------------------------------------------ datos

    private static readonly Dictionary<char, string[]> Bitmaps = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'B', new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." } },
        { 'C', new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." } },
        { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
        { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
        { 'F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
        { 'G', new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" } },
        { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'I', new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
        { 'J', new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." } },
        { 'K', new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" } },
        { 'L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" } },
        { 'M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
        { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
        { 'O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
        { 'P', new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." } },
        { 'Q', new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" } },
        { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
        { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
        { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
        { 'U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
        { 'V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
        { 'W', new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" } },
        { 'X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" } },
        { 'Y', new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." } },
        { 'Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" } },
        { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
        { '1', new[] { ".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###" } },
        { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
        { '3', new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." } },
        { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
        { '5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
        { '6', new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." } },
        { '7', new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
        { '8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
        { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." } },
        { '.', new[] { ".", ".", ".", ".", ".", ".", "#" } },
        { ',', new[] { "..", "..", "..", "..", "..", ".#", "#." } },
        { ':', new[] { ".", ".", "#", ".", ".", "#", "." } },
        { '!', new[] { "#", "#", "#", "#", "#", ".", "#" } },
        { '¡', new[] { "#", ".", "#", "#", "#", "#", "#" } },
        { '?', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." } },
        { '¿', new[] { "..#..", ".....", "..#..", ".#...", "#....", "#...#", ".###." } },
        { '-', new[] { "...", "...", "...", "###", "...", "...", "..." } },
        { '+', new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." } },
        { '=', new[] { "....", "....", "####", "....", "####", "....", "...." } },
        { '/', new[] { "....#", "...#.", "...#.", "..#..", ".#...", ".#...", "#...." } },
        { '\'', new[] { "#", "#", ".", ".", ".", ".", "." } },
        { '"', new[] { "#.#", "#.#", "...", "...", "...", "...", "..." } },
        { '(', new[] { ".#", "#.", "#.", "#.", "#.", "#.", ".#" } },
        { ')', new[] { "#.", ".#", ".#", ".#", ".#", ".#", "#." } },
        { '<', new[] { "...#", "..#.", ".#..", "#...", ".#..", "..#.", "...#" } },
        { '>', new[] { "#...", ".#..", "..#.", "...#", "..#.", ".#..", "#..." } },
        { '%', new[] { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" } },
        { '#', new[] { ".#.#.", "#####", ".#.#.", ".#.#.", ".#.#.", "#####", ".#.#." } },
        { '_', new[] { ".....", ".....", ".....", ".....", ".....", ".....", "#####" } },
        { '*', new[] { ".....", "#.#.#", ".###.", "#####", ".###.", "#.#.#", "....." } },
    };

    // Tildes: 2 filas sobre la letra base.
    private static readonly string[] Acute = { "..#", ".#." };
    private static readonly string[] Tilde = { ".##.#", "#.##." };
    private static readonly string[] Diaeresis = { ".....", ".#.#." };

    private static readonly (char ch, char baseCh, string[] accent)[] Composed =
    {
        ('Á', 'A', Acute), ('É', 'E', Acute), ('Í', 'I', Acute), ('Ó', 'O', Acute), ('Ú', 'U', Acute),
        ('Ñ', 'N', Tilde), ('Ü', 'U', Diaeresis),
    };

    private static void Build()
    {
        if (_texture != null) return;

        var all = new List<(char, string[], string[])>();
        foreach (var kv in Bitmaps) all.Add((kv.Key, kv.Value, null));
        foreach (var c in Composed) all.Add((c.ch, Bitmaps[c.baseCh], c.accent));

        int rows = Mathf.CeilToInt(all.Count / (float)Cols);
        int texW = Cols * (CellW + Pad) + Pad;
        int texH = rows * (CellH + Pad) + Pad;
        _texture = new Texture2D(texW, texH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelFont",
        };
        var clear = new Color32[texW * texH];
        _texture.SetPixels32(clear);
        _glyphs = new Dictionary<char, Glyph>();

        var white = new Color32(255, 255, 255, 255);
        for (int i = 0; i < all.Count; i++)
        {
            var (ch, rowsBmp, accent) = all[i];
            int cx = Pad + (i % Cols) * (CellW + Pad);
            // y de textura crece hacia arriba: la celda i/Cols empieza desde arriba.
            int cyTop = texH - Pad - (i / Cols) * (CellH + Pad) - 1;

            int width = 1;
            for (int r = 0; r < CapHeight; r++)
            {
                string row = rowsBmp[r];
                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] != '#') continue;
                    _texture.SetPixel(cx + x, cyTop - (AccentRows + r), white);
                    width = Mathf.Max(width, x + 1);
                }
                width = Mathf.Max(width, row.TrimEnd('.').Length);
            }
            width = Mathf.Max(width, rowsBmp[0].Length);
            if (accent != null)
            {
                int off = Mathf.Max(0, (width - accent[0].Length + 1) / 2);
                for (int r = 0; r < accent.Length; r++)
                    for (int x = 0; x < accent[r].Length; x++)
                        if (accent[r][x] == '#') _texture.SetPixel(cx + off + x, cyTop - r, white);
            }

            float u0 = cx / (float)texW, u1 = (cx + CellW) / (float)texW;
            float v1 = (cyTop + 1) / (float)texH, v0 = (cyTop + 1 - CellH) / (float)texH;
            _glyphs[ch] = new Glyph { Width = width, Uv = Rect.MinMaxRect(u0, v0, u1, v1) };
        }
        _texture.Apply(false, true);
    }
}
