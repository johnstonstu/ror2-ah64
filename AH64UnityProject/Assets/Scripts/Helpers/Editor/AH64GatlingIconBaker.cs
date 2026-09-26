using System;
using UnityEditor;
using UnityEngine;

/// <summary>Imports approved illustrated icons. Never replaces authored art with procedural placeholders.</summary>
public static class AH64GatlingIconBaker
{
    // Retain the existing entry points for editor callers; authoring now happens in the PNGs.
    [MenuItem("AH64/Import Primary Skill Icons")]
    public static void BakeFromMenu() { BakeAll(); }

    public static string BakeAll()
    {
        foreach (string name in new[] { "texAH64PrimaryIcon", "texAH64GatlingIcon", "texAH64CannonIcon" })
        {
            string path = "Assets/AH64/Bundle/Icons/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) throw new InvalidOperationException("Missing approved primary icon: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite || sprite.rect.width < 256 || sprite.rect.width != sprite.rect.height)
                throw new InvalidOperationException("Expected square illustrated Sprite of at least 256px: " + path);
        }
        Debug.Log("[AH64Icons] Approved illustrated primary sprites imported.");
        return null;
    }

    public static string Bake() => BakeAll();
}
