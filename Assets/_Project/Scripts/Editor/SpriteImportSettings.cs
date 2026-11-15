using UnityEditor;
using UnityEngine;

namespace Bnhon.EditorTools
{
    /// <summary>
    /// Imports every image in Resources/Sprites as crisp pixel art, so a PNG
    /// dropped into that folder works without touching the Inspector:
    /// single sprite, point filtering, no compression, no mipmaps.
    /// </summary>
    public sealed class SpriteImportSettings : AssetPostprocessor
    {
        private const string Folder = "/Resources/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains(Folder)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
