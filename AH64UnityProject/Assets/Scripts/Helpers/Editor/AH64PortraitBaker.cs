using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes the character-select portrait from <c>AH64Display</c>, so the lobby icon matches the airframe.
///
/// <para>Two-pass render + composite (colour, then unlit coverage mask) — opaque forward shaders don't
/// write useful destination alpha, so a single pass can't cut out cleanly.</para>
///
/// <para>Menu: <c>AH64 -> Bake Character Portrait</c>. Also runnable via
/// <c>-executeMethod AH64PortraitBaker.RunFromCommandLine</c>. Rebuild the assetbundle afterwards.</para>
/// </summary>
public static class AH64PortraitBaker
{
    private const string DisplayPrefabPath = "Assets/AH64/Bundle/AH64Display.prefab";
    private const string OutputPath = "Assets/AH64/Bundle/Icons/texAH64Icon.png";
    private const string TempRootName = "__AH64PortraitRig";

    private const int RenderSize = 1024;
    private const int ImportSize = 256;

    // Mild zoom for the CSS tile (was 0.12 full-airframe pad). Keep airframe in frame.
    private const float Azimuth = 34f;
    private const float Elevation = 16f;
    private const float CameraDistance = 8.5f;

    private const float FramePadding = 0.02f;

    // Olive drab clear colour so edge AA blends into the hull rather than into black/cream.
    private static readonly Color ArmorColor = new Color(0.22f, 0.28f, 0.18f);
    private static readonly Color RimColor = new Color(0.55f, 0.75f, 0.55f);
    private const int RimWidthPixels = 8;

    [MenuItem("AH64/Bake Character Portrait")]
    public static void Bake()
    {
        string error = BakeInternal();
        if (error != null)
            EditorUtility.DisplayDialog("Portrait bake failed", error, "OK");
        else
            EditorUtility.DisplayDialog("Portrait baked",
                "Wrote " + OutputPath + ".\nRebuild the assetbundle (Ctrl+Alt+B) to ship it.", "OK");
    }

    /// <summary>Batchmode entry: Unity -batchmode -executeMethod AH64PortraitBaker.RunFromCommandLine</summary>
    public static void RunFromCommandLine()
    {
        string error = BakeInternal();
        if (error != null)
        {
            Debug.LogError("[AH64PortraitBaker] " + error);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[AH64PortraitBaker] Baked " + OutputPath);
        EditorApplication.Exit(0);
    }

    private static string BakeInternal()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DisplayPrefabPath);
        if (prefab == null)
            return "Could not load " + DisplayPrefabPath + ". Run Phase 4 Setup first.";

        GameObject rig = new GameObject(TempRootName);
        AmbientSettings savedAmbient = AmbientSettings.Capture();
        try
        {
            AmbientSettings.ApplyPortraitAmbient();

            GameObject subject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            subject.transform.SetParent(rig.transform, false);

            // Rigid airframe — no idle pose to sample. Disable any leftover animator so it can't
            // overwrite transforms on the next update.
            Animator animator = subject.GetComponentInChildren<Animator>();
            if (animator != null) animator.enabled = false;

            List<Renderer> visible = new List<Renderer>();
            foreach (Renderer r in subject.GetComponentsInChildren<Renderer>(false))
                if (r.enabled) visible.Add(r);

            if (visible.Count == 0)
                return "AH64Display has no enabled renderers.";

            Camera cam = BuildCamera(rig, visible);
            BuildLights(rig);

            Texture2D color = Capture(cam, ArmorColor, null);
            Texture2D mask = Capture(cam, Color.black, visible);

            Texture2D portrait = Composite(color, mask);
            UnityEngine.Object.DestroyImmediate(color);
            UnityEngine.Object.DestroyImmediate(mask);

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Assets/AH64/Bundle/Icons");
            File.WriteAllBytes(OutputPath, portrait.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(portrait);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter();

            Debug.Log($"[AH64PortraitBaker] Baked {OutputPath} ({visible.Count} parts).");
            return null;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rig);
            savedAmbient.Restore();
        }
    }

    private struct AmbientSettings
    {
        private UnityEngine.Rendering.AmbientMode mode;
        private Color color;
        private float intensity;

        public static AmbientSettings Capture()
        {
            return new AmbientSettings
            {
                mode = RenderSettings.ambientMode,
                color = RenderSettings.ambientLight,
                intensity = RenderSettings.ambientIntensity,
            };
        }

        public static void ApplyPortraitAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.12f);
            RenderSettings.ambientIntensity = 1f;
        }

        public void Restore()
        {
            RenderSettings.ambientMode = mode;
            RenderSettings.ambientLight = color;
            RenderSettings.ambientIntensity = intensity;
        }
    }

    private static Camera BuildCamera(GameObject rig, List<Renderer> visible)
    {
        Bounds bounds = visible[0].bounds;
        for (int i = 1; i < visible.Count; i++) bounds.Encapsulate(visible[i].bounds);

        Vector3 focus = bounds.center;
        float frameSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * (0.5f + FramePadding);
        frameSize = Mathf.Max(frameSize, 1.5f);

        GameObject camGo = new GameObject("PortraitCamera");
        camGo.transform.SetParent(rig.transform, false);

        Quaternion orbit = Quaternion.Euler(Elevation, Azimuth + 180f, 0f);
        camGo.transform.position = focus + orbit * new Vector3(0f, 0f, -CameraDistance);
        camGo.transform.rotation = orbit;

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = frameSize;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 40f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.enabled = false;
        return cam;
    }

    private static void BuildLights(GameObject rig)
    {
        AddLight(rig, new Vector3(35f, 205f, 0f), 0.85f, Color.white);
        AddLight(rig, new Vector3(15f, 60f, 0f), 0.28f, new Color(0.70f, 0.85f, 1f));
        AddLight(rig, new Vector3(-10f, 20f, 0f), 0.35f, new Color(1f, 0.95f, 0.80f));
    }

    private static void AddLight(GameObject rig, Vector3 euler, float intensity, Color color)
    {
        GameObject go = new GameObject("PortraitLight");
        go.transform.SetParent(rig.transform, false);
        go.transform.rotation = Quaternion.Euler(euler);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.None;
    }

    private static Texture2D Capture(Camera cam, Color background, List<Renderer> maskTargets)
    {
        Material flat = null;
        Material[][] saved = null;

        if (maskTargets != null)
        {
            flat = new Material(Shader.Find("Unlit/Color"));
            flat.color = Color.white;
            saved = new Material[maskTargets.Count][];
            for (int i = 0; i < maskTargets.Count; i++)
            {
                saved[i] = maskTargets[i].sharedMaterials;
                Material[] replacement = new Material[saved[i].Length];
                for (int m = 0; m < replacement.Length; m++) replacement[m] = flat;
                maskTargets[i].sharedMaterials = replacement;
            }
        }

        RenderTexture rt = new RenderTexture(RenderSize, RenderSize, 24, RenderFormat()) { antiAliasing = 8 };
        RenderTexture previous = RenderTexture.active;
        try
        {
            cam.backgroundColor = background;
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            Texture2D result = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0);
            result.Apply();
            return result;
        }
        finally
        {
            RenderTexture.active = previous;
            cam.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            if (maskTargets != null)
            {
                for (int i = 0; i < maskTargets.Count; i++) maskTargets[i].sharedMaterials = saved[i];
                UnityEngine.Object.DestroyImmediate(flat);
            }
        }
    }

    private static RenderTextureFormat RenderFormat()
    {
        return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32)
            ? RenderTextureFormat.ARGB32
            : RenderTextureFormat.Default;
    }

    private static Texture2D Composite(Texture2D color, Texture2D mask)
    {
        int size = RenderSize;
        Color32[] src = color.GetPixels32();
        Color32[] cov = mask.GetPixels32();

        float[] alpha = new float[src.Length];
        for (int i = 0; i < alpha.Length; i++) alpha[i] = cov[i].r / 255f;

        float[] glow = Dilate(alpha, size, RimWidthPixels);

        Color[] outPx = new Color[src.Length];
        for (int i = 0; i < src.Length; i++)
        {
            float a = alpha[i];
            float rim = Mathf.Clamp01(glow[i] - a);
            Color body = new Color(src[i].r / 255f, src[i].g / 255f, src[i].b / 255f, 1f);

            Color rgb = Color.Lerp(RimColor, body, a);
            outPx[i] = new Color(rgb.r, rgb.g, rgb.b, Mathf.Clamp01(a + rim * 0.55f));
        }

        Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
        result.SetPixels(outPx);
        result.Apply();
        return result;
    }

    private static float[] Dilate(float[] source, int size, int radius)
    {
        float[] horizontal = new float[source.Length];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float max = 0f;
                int from = Mathf.Max(0, x - radius), to = Mathf.Min(size - 1, x + radius);
                for (int i = from; i <= to; i++) max = Mathf.Max(max, source[y * size + i]);
                horizontal[y * size + x] = max;
            }
        }

        float[] result = new float[source.Length];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float max = 0f;
                int from = Mathf.Max(0, y - radius), to = Mathf.Min(size - 1, y + radius);
                for (int i = from; i <= to; i++) max = Mathf.Max(max, horizontal[i * size + x]);
                result[y * size + x] = max;
            }
        }
        return result;
    }

    private static void ConfigureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = ImportSize;
        importer.SaveAndReimport();
    }
}
