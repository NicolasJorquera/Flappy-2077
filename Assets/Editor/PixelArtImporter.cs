using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajustes de importación para el arte generado por Tools/art/generate.py (Assets/Resources/Pixel):
/// sprite sin compresión, filtro Point (pixel nítido), 20 px por unidad de mundo y bordes 9-slice
/// para los paneles y botones de UI.
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Resources/Pixel/";
    public const float PixelsPerUnit = 20f;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder)) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = true;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);

        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (file == "ui_panel") importer.spriteBorder = new Vector4(6, 6, 6, 6);
        else if (file.StartsWith("ui_button")) importer.spriteBorder = new Vector4(4, 4, 4, 4);
        else importer.spriteBorder = Vector4.zero;
    }
}
