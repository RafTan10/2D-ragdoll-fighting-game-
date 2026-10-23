using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Import settings for everything under Assets/Game/Art: sprite, 32 PPU, point filter, no compression,
/// and a pivot chosen from the file name (see CLAUDE.md "Pixel art conventions").
/// </summary>
public class PixelArtImporter : AssetPostprocessor
{
    public const string ArtRoot = "Assets/Game/Art/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ArtRoot)) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = Fighter.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        // Fighters and weapons are small; keeping them readable lets the rig fit hitboxes to the visible pixels.
        importer.isReadable = !assetPath.Contains("/Arenas/");

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = PivotFor(assetPath);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    public static Vector2 PivotFor(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        string p = path.Replace('\\', '/');

        if (p.Contains("/Arenas/")) return new Vector2(0.5f, 0f);
        if (p.Contains("/Weapons/")) return name.Contains("shield") ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);
        if (name == "head") return new Vector2(0.5f, 0f);              // neck
        if (name == "cape") return new Vector2(0.5f, 1f);              // hangs from the neck
        if (name == "skirt") return new Vector2(0.5f, 1f);             // hangs from the hips
        if (name == "hand_prop") return new Vector2(0.5f, 1f);         // hangs from the hand
        if (name == "torso") return new Vector2(0.5f, 0.5f);
        if (name.Contains("arm") || name.Contains("leg")) return new Vector2(0.5f, 1f); // joint at the top
        return new Vector2(0.5f, 0.5f);
    }
}
