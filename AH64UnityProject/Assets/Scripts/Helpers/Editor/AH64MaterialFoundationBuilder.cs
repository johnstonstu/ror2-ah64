using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates a restrained, texture-ready AH-64 material set and assigns exactly
/// one material per renderer. This command never exports the FBX, builds the
/// asset bundle, copies files to Build/, or deploys to a mod profile.
/// </summary>
public static class AH64MaterialFoundationBuilder
{
    private const string BundleFolder = "Assets/AH64/Bundle";
    private const string FoundationFolder = BundleFolder + "/AH64Surface";
    private const string MaterialFolder = FoundationFolder + "/Materials";
    private const string TextureFolder = FoundationFolder + "/Textures";
    private const string SurfaceTexturePath = TextureFolder + "/texAH64SurfaceVariation.png";
    private const string SurfaceNormalTexturePath = TextureFolder + "/texAH64SurfaceNormal.png";
    private const string BodyAtlasTexturePath = TextureFolder + "/texAH64BodyPanelAtlas_0116a.png";
    private const string RotorTexturePath = TextureFolder + "/texAH64RotorBlur.png";

    private sealed class SurfaceSpec
    {
        public readonly string MaterialName;
        public readonly Color Color;
        public readonly float Metallic;
        public readonly float Smoothness;
        public readonly Vector2 Tiling;
        public readonly bool UsesDetailMaps;
        public readonly bool UsesBodyAtlas;

        public SurfaceSpec(string materialName, Color color, float metallic,
            float smoothness, float tiling, bool usesDetailMaps = true,
            bool usesBodyAtlas = false)
        {
            MaterialName = materialName;
            Color = color;
            Metallic = metallic;
            Smoothness = smoothness;
            Tiling = new Vector2(tiling, tiling);
            UsesDetailMaps = usesDetailMaps;
            UsesBodyAtlas = usesBodyAtlas;
        }
    }

    // Names deliberately retain the Body/Dark/Glass/Radar prefixes consumed by
    // AH64Survivor.PolishAirframeMaterials. Never load the bare "matAH64".
    private static readonly Dictionary<string, SurfaceSpec> RendererSpecs =
        new Dictionary<string, SurfaceSpec>(StringComparer.Ordinal)
        {
            // Albedo lifted ~2.4x across the Dark* family (playtest v0.1.17.7: the
            // airframe collapsed to a flat black silhouette on night stages).
            // A real Apache genuinely is this dark, but RoR2's flat stylised lighting
            // has no bounce or exposure to recover shadow detail — anything under ~0.20
            // reflectance reads as pure black in game.
            // The value ORDER is what sells the material read, so it's preserved:
            // rotor darkest, then hellfire, static, pod, gun, airframe lightest.
            // Second lift, 2026-08-03: the body still read "really, really dark" in game
            // even though it looks mid-olive in Blender. That gap is RoR2's deferred hopoo
            // shading, not the material — the conversion never touches _Color, so albedo
            // is the only lever. Airframe 0.44 -> 0.60, and the dark family follows so the
            // value ORDER (the thing that sells the panel hierarchy) is preserved.
            { "Airframe", new SurfaceSpec("matAH64BodySurface",
                new Color(0.60f, 0.67f, 0.43f), 0.00f, 0.26f, 1.0f, true, true) },
            { "AirframeDark", new SurfaceSpec("matAH64DarkStatic",
                new Color(0.340f, 0.370f, 0.272f), 0.00f, 0.24f, 4.0f) },
            // Declutter pass 2026-08-03. Canopy was (0.100, 0.220, 0.280) — a bright
            // cyan that read as a cartoon windscreen against the olive hull. Apache
            // glass is near-black with a cool sky reflection, so this is darker and
            // pulled well off pure cyan.
            { "Canopy", new SurfaceSpec("matAH64GlassCanopy",
                new Color(0.055f, 0.120f, 0.150f), 0.00f, 0.80f, 1.0f, false) },
            // Markings were (0.60, 0.36, 0.085) — safety orange, and there were seven
            // marked features per side wearing it. One band remains and it is muted
            // toward dull rust, so it reads as a stencil rather than a hazard stripe.
            { "AirframeMarkings", new SurfaceSpec("matAH64Markings",
                new Color(0.38f, 0.24f, 0.095f), 0.00f, 0.40f, 1.0f, false) },
            // Optics stay near-black on purpose — a lens should read as a dark hole.
            { "NoseOptics", new SurfaceSpec("matAH64Optics",
                new Color(0.030f, 0.130f, 0.200f), 0.05f, 0.88f, 1.0f, false) },
            { "MainRotor", new SurfaceSpec("matAH64DarkRotor",
                new Color(0.218f, 0.238f, 0.205f), 0.00f, 0.14f, 5.0f, false) },
            { "TailRotor", new SurfaceSpec("matAH64DarkRotor",
                new Color(0.218f, 0.238f, 0.205f), 0.00f, 0.14f, 5.0f, false) },
            { "ChinTurret", new SurfaceSpec("matAH64DarkGun",
                new Color(0.375f, 0.400f, 0.310f), 0.25f, 0.44f, 4.0f) },
            { "ChinBarrel", new SurfaceSpec("matAH64DarkGun",
                new Color(0.375f, 0.400f, 0.310f), 0.25f, 0.44f, 4.0f) },
            // Roughly 60% of the M230's albedo. The cluster is a much larger unbroken
            // metal mass than the M230's thin tube, so an identical value reads clearly
            // lighter on it — reported as "too light coloured" in playtest 2026-08-03.
            // Slightly more metallic and less rough as well, so it still catches a
            // highlight while it spins rather than going flat black.
            { "ChinGatling", new SurfaceSpec("matAH64DarkGatling",
                new Color(0.225f, 0.245f, 0.192f), 0.34f, 0.38f, 4.0f) },
            { "ChinGatlingHousing", new SurfaceSpec("matAH64DarkGatling",
                new Color(0.225f, 0.245f, 0.192f), 0.34f, 0.38f, 4.0f) },
            { "ChinCannon", new SurfaceSpec("matAH64DarkGun",
                new Color(0.375f, 0.400f, 0.310f), 0.25f, 0.44f, 4.0f) },
            { "PodRocketL", new SurfaceSpec("matAH64DarkRocketPod",
                new Color(0.355f, 0.400f, 0.245f), 0.04f, 0.32f, 2.0f, false) },
            { "PodRocketR", new SurfaceSpec("matAH64DarkRocketPod",
                new Color(0.355f, 0.400f, 0.245f), 0.04f, 0.32f, 2.0f, false) },
            { "PodMissileL", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "PodMissileR", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            // The eight individual Hellfires share the rack's material exactly — they
            // are the same ordnance, just split into separate renderers so they can be
            // hidden one at a time.
            { "MissileL0", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileL1", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileL2", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileL3", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileR0", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileR1", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileR2", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "MissileR3", new SurfaceSpec("matAH64DarkHellfire",
                new Color(0.298f, 0.330f, 0.250f), 0.04f, 0.26f, 4.0f) },
            { "RadarDome", new SurfaceSpec("matAH64RadarDome",
                new Color(0.180f, 0.260f, 0.300f), 0.08f, 0.55f, 2.0f, false) },
        };

    [MenuItem("AH64/Prepare Material Foundation (prefabs only, no bundle build)")]
    public static void PrepareExistingPrefabs()
    {
        string[] paths =
        {
            BundleFolder + "/mdlAH64.prefab",
            BundleFolder + "/AH64Display.prefab",
        };

        foreach (string path in paths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (!root)
            {
                Debug.LogWarning("[AH64MaterialFoundation] Prefab not found: " + path);
                continue;
            }

            try
            {
                string error = PrepareAndAssign(root);
                if (error != null)
                    throw new InvalidOperationException(path + ": " + error);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AH64MaterialFoundation] Prepared prefab materials only; " +
                  "no FBX export, bundle build, Build/ copy, or deployment was performed.");
    }

    public static string PrepareAndAssign(GameObject root)
    {
        if (!root)
            return "Model root is null.";

        EnsureFolders();
        EnsureTexture(SurfaceTexturePath, BuildSurfaceTexture());
        EnsureTexture(SurfaceNormalTexturePath, BuildSurfaceNormalTexture(), true);
        EnsureTexture(RotorTexturePath, BuildRotorTexture());
        Texture2D surfaceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SurfaceTexturePath);
        Texture2D surfaceNormalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(SurfaceNormalTexturePath);
        Texture2D bodyAtlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BodyAtlasTexturePath);
        if (!bodyAtlasTexture)
        {
            Debug.LogWarning("[AH64MaterialFoundation] Body atlas is missing; using the neutral surface texture.");
            bodyAtlasTexture = surfaceTexture;
        }

        var materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        foreach (SurfaceSpec spec in RendererSpecs.Values)
        {
            if (materials.ContainsKey(spec.MaterialName))
                continue;
            Texture2D colorTexture = spec.UsesBodyAtlas ? bodyAtlasTexture : surfaceTexture;
            Material material = EnsureSurfaceMaterial(spec, colorTexture, surfaceNormalTexture);
            if (!material)
                return "Could not create " + spec.MaterialName + ".";
            materials.Add(spec.MaterialName, material);
        }

        foreach (KeyValuePair<string, SurfaceSpec> pair in RendererSpecs)
        {
            Transform child = FindDeep(root.transform, pair.Key);
            if (!child)
                return "Missing renderer transform " + pair.Key + ".";
            Renderer renderer = child.GetComponent<Renderer>();
            if (!renderer)
                return pair.Key + " has no Renderer.";
            renderer.sharedMaterials = new[] { materials[pair.Value.MaterialName] };
            if (renderer.sharedMaterials.Length != 1)
                return pair.Key + " did not retain exactly one material slot.";
        }

        Material blurMaterial = EnsureRotorBlurMaterial();
        if (!blurMaterial)
            return "Could not create rotor blur material.";
        foreach (string name in new[] { "RotorBlurMain", "RotorBlurTail" })
        {
            Transform child = FindDeep(root.transform, name);
            if (!child)
                return "Missing renderer transform " + name + ".";
            Renderer renderer = child.GetComponent<Renderer>();
            if (!renderer)
                return name + " has no Renderer.";
            renderer.sharedMaterials = new[] { blurMaterial };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        return null;
    }

    public static Material EnsureRotorBlurMaterial()
    {
        EnsureFolders();
        EnsureTexture(RotorTexturePath, BuildRotorTexture());
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (!shader)
        {
            Debug.LogError("[AH64MaterialFoundation] Rotor blur shader not found.");
            return null;
        }

        string path = MaterialFolder + "/matAH64RotorBlur.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RotorTexturePath);
        material.SetColor("_TintColor", new Color(0.28f, 0.29f, 0.30f, 0.18f));
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material EnsureSurfaceMaterial(SurfaceSpec spec, Texture2D texture,
        Texture2D normalTexture)
    {
        Shader shader = Shader.Find("Standard");
        if (!shader)
        {
            Debug.LogError("[AH64MaterialFoundation] Standard shader not found.");
            return null;
        }

        string path = MaterialFolder + "/" + spec.MaterialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;
        material.SetColor("_Color", spec.Color);
        material.SetFloat("_Metallic", spec.Metallic);
        material.SetFloat("_Glossiness", spec.Smoothness);
        material.SetTexture("_MainTex", spec.UsesDetailMaps ? texture : null);
        material.SetTextureScale("_MainTex", spec.Tiling);
        material.SetTexture("_BumpMap", spec.UsesDetailMaps ? normalTexture : null);
        material.SetFloat("_BumpScale", spec.UsesDetailMaps ? 0.12f : 0f);
        if (spec.UsesDetailMaps)
            material.EnableKeyword("_NORMALMAP");
        else
            material.DisableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureTexture(string path, Texture2D generated, bool normalMap = false)
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string absolutePath = Path.Combine(projectRoot ?? "", path.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolutePath))
        {
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }
        UnityEngine.Object.DestroyImmediate(generated);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer)
            return;
        importer.sRGBTexture = !normalMap;
        importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.maxTextureSize = 256;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static Texture2D BuildSurfaceTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float wave = Mathf.Sin(x * Mathf.PI * 2f / 47f) *
                         Mathf.Sin(y * Mathf.PI * 2f / 59f);
            float grain = Mathf.PerlinNoise(x / 19f, y / 23f) - 0.5f;
            int localX = x % 64;
            int localY = y % 48;
            bool seam = localX < 2 || localY < 2;
            float panel = ((x / 64 + y / 48) & 1) == 0 ? -3.5f : 2.5f;
            float value = 228f + panel + wave * 3f + grain * 12f - (seam ? 16f : 0f);
            byte channel = (byte)Mathf.Clamp(value, 196f, 242f);
            pixels[y * size + x] = new Color32(channel, channel, channel, 215);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildSurfaceNormalTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float h = Mathf.PerlinNoise(x / 17f, y / 17f);
            float hx = Mathf.PerlinNoise((x + 1) / 17f, y / 17f) - h;
            float hy = Mathf.PerlinNoise(x / 17f, (y + 1) / 17f) - h;
            Vector3 normal = new Vector3(-hx * 1.2f, -hy * 1.2f, 1f).normalized;
            pixels[y * size + x] = new Color32(
                (byte)Mathf.Clamp((normal.x * 0.5f + 0.5f) * 255f, 0f, 255f),
                (byte)Mathf.Clamp((normal.y * 0.5f + 0.5f) * 255f, 0f, 255f),
                (byte)Mathf.Clamp((normal.z * 0.5f + 0.5f) * 255f, 0f, 255f),
                255);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static Texture2D BuildRotorTexture()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px = (x + 0.5f) / size * 2f - 1f;
            float py = (y + 0.5f) / size * 2f - 1f;
            float radius = Mathf.Sqrt(px * px + py * py);
            float angle = Mathf.Atan2(py, px);
            float ring = Mathf.Clamp01(1f - Mathf.Abs(radius - 0.70f) / 0.16f);
            float hubFade = Mathf.SmoothStep(0f, 1f, (radius - 0.20f) / 0.25f);
            float arcs = Mathf.Pow(Mathf.Clamp01(0.45f + 0.55f *
                Mathf.Cos(angle * 4f + radius * 6f)), 2.5f);
            float alpha = ring * hubFade * arcs * Mathf.Clamp01((0.98f - radius) * 12f);
            pixels[y * size + x] = new Color32(205, 212, 216,
                (byte)Mathf.Clamp(alpha * 150f, 0f, 150f));
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static void EnsureFolders()
    {
        EnsureFolder(BundleFolder, "AH64Surface");
        EnsureFolder(FoundationFolder, "Materials");
        EnsureFolder(FoundationFolder, "Textures");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name)
                return child;
        return null;
    }
}
