using UnityEditor;
using UnityEngine;

// Only configures this generated pack; other project textures are untouched.
public sealed class GeneratedUISpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        bool isTargetDir = assetPath.StartsWith("Assets/Art/UI/Generated/", System.StringComparison.OrdinalIgnoreCase) ||
                           assetPath.StartsWith("Assets/Art/UI/Новое/", System.StringComparison.OrdinalIgnoreCase) ||
                           assetPath.StartsWith("Assets/Art/UI/New/", System.StringComparison.OrdinalIgnoreCase);

        if (!isTargetDir || !assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100;
        importer.maxTextureSize = 2048;
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (name == "menu_background" ||
            name.StartsWith("button_pause", System.StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Pause_", System.StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("icon_", System.StringComparison.OrdinalIgnoreCase))
        {
            importer.spriteBorder = Vector4.zero;
            return;
        }
        float border = name.StartsWith("panel_") ? 28 :
            (name.StartsWith("button_") || name.Contains("TargetGraphic") || name.Contains("HighlightedSprite") || name.Contains("PressedSprite") || name.Contains("DisabledSprite") || name.Contains("SelectedSprite")) ? 14 :
            (name.StartsWith("build_slot_")) ? 16 : 0;
        importer.spriteBorder = new Vector4(border, border, border, border);
    }
}
