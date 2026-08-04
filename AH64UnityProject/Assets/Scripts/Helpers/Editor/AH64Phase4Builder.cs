using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Atomic Phase 4 setup: build <c>mdlAH64</c> + <c>AH64Display</c> from the imported FBX, wire the
/// ChildLocator (14 renderers + 14 anchors), add hurtbox colliders, retag the assetbundle to
/// <c>ah64</c>, and build it. Run from the menu or via
/// <c>-executeMethod AH64Phase4Builder.RunFromCommandLine</c>.
/// ChinBarrel is a separate renderer so the M230 can yaw/pitch independently.
///
/// <para>All three of code constant / Unity folder tag / on-disk bundle file must change together —
/// this script owns the Unity-side half. The mod-side half is in <c>AH64Survivor.cs</c>.</para>
/// </summary>
public static class AH64Phase4Builder
{
    private const string FbxPath = "Assets/AH64/Source/FBX/AH64.fbx";
    private const string BundleFolder = "Assets/AH64/Bundle";
    private const string ModelPrefabPath = BundleFolder + "/mdlAH64.prefab";
    private const string DisplayPrefabPath = BundleFolder + "/AH64Display.prefab";
    // Named so Materials.LoadMaterial's Contains matching can find it — but never request the bare
    // "matAH64" from the mod side; it would match this and the three airframe materials alike.
    private const string BundleName = "ah64";

    // Must match Art/Blender/build_ah64.py — one renderer = one material (RoR2 hard constraint).
    // RotorBlurMain/RotorBlurTail get ChildLocator entries but are deliberately NOT in the mod's
    // customRendererInfos: CharacterModel must never manage their transparent FX material.
    private static readonly string[] RendererNames =
    {
        "Airframe", "AirframeDark", "Canopy", "AirframeMarkings", "NoseOptics",
        "MainRotor", "TailRotor", "ChinTurret", "ChinBarrel", "ChinGatling",
        "PodRocketL", "PodRocketR", "PodMissileL", "PodMissileR",
        // Individual Hellfires — own renderers so AH64PylonMissiles can deplete them.
        "MissileL0", "MissileL1", "MissileL2", "MissileL3",
        "MissileR0", "MissileR1", "MissileR2", "MissileR3",
        "RadarDome",
        "RotorBlurMain", "RotorBlurTail",
    };

    // Anchors authored as Blender empties and exported with the FBX.
    // RadarDome is a mesh renderer (listed above), not an empty.
    private static readonly string[] AnchorNames =
    {
        "Chest", "Head", "NoseTip", "WingL", "WingR", "TailTip",
        "MainHurtbox", "HeadHurtbox", "AimOrigin", "Muzzle",
        "MuzzleRocketL", "MuzzleRocketR", "MuzzleMissileL", "MuzzleMissileR",
    };

    [MenuItem("AH64/Phase 4 Setup (mdlAH64 + bundle rename)")]
    public static void RunFromMenu()
    {
        string error = Run();
        if (error != null)
            EditorUtility.DisplayDialog("Phase 4 Setup failed", error, "OK");
        else
            EditorUtility.DisplayDialog("Phase 4 Setup", "mdlAH64 + AH64Display built, bundle tagged ah64, and AssetBundle built.", "OK");
    }

    /// <summary>
    /// Rebuilds the authored model/display prefabs from the current FBX while
    /// deliberately leaving bundle generation and profile deployment untouched.
    /// Use this to inspect art iterations safely in the Unity editor.
    /// </summary>
    [MenuItem("AH64/Refresh Prefabs From FBX (no bundle build)")]
    public static void RefreshPrefabsFromFbxMenu()
    {
        string error = RefreshPrefabsFromFbx();
        if (error != null)
            EditorUtility.DisplayDialog("AH-64 prefab refresh failed", error, "OK");
        else
            Debug.Log("[AH64Phase4Builder] Refreshed AH-64 prefabs from FBX; no bundle build or deployment was performed.");
    }

    /// <summary>
    /// Drop a <c>Phase4.request</c> file next to Assets/ (or touch this script while that file exists)
    /// to run setup in an already-open Editor without batchmode.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        string flag = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Phase4.request");
        if (!File.Exists(flag))
            return;

        try { File.Delete(flag); }
        catch (IOException) { return; }

        EditorApplication.delayCall += () =>
        {
            string error = Run();
            if (error != null)
                Debug.LogError("[AH64Phase4Builder] Auto-run failed: " + error);
            else
                Debug.Log("[AH64Phase4Builder] Auto-run complete.");
        };
    }

    /// <summary>Batchmode entry point: Unity -batchmode -executeMethod AH64Phase4Builder.RunFromCommandLine</summary>
    public static void RunFromCommandLine()
    {
        string error = Run();
        if (error != null)
        {
            Debug.LogError("[AH64Phase4Builder] " + error);
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("[AH64Phase4Builder] Phase 4 setup complete.");
        EditorApplication.Exit(0);
    }

    public static string Run()
    {
        // Only the bundle folder carries the tag. The FBX under Assets/AH64/Source is deliberately
        // untagged — its meshes reach the bundle as dependencies of mdlAH64/AH64Display, so the
        // raw source asset never ships as a standalone bundle entry.
        TagFolderBundle(BundleFolder, BundleName);

        string audioError = AH64AudioFoundationBuilder.PrepareAudioAsset();
        if (audioError != null)
            return "Audio foundation failed: " + audioError;

        string buildError = RefreshPrefabsFromFbx();
        if (buildError != null)
            return buildError;

        string bundleError = BuildAh64BundleQuiet();
        if (bundleError != null)
            return bundleError;

        return null;
    }

    public static string RefreshPrefabsFromFbx()
    {
        GameObject fbxRoot = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (!fbxRoot)
            return "Could not load " + FbxPath + ". Is the FBX imported?";

        string buildError = BuildModelPrefabs(fbxRoot);
        if (buildError != null)
            return buildError;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return null;
    }

    private static string BuildAh64BundleQuiet()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();

        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "AssetBundles");
        Directory.CreateDirectory(outputPath);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.StandaloneWindows64);

        if (manifest == null)
            return "AssetBundle build failed. Check the Console.";

        string builtFile = Path.Combine(outputPath, BundleName);
        if (!File.Exists(builtFile))
            return "Build succeeded but '" + BundleName + "' was not produced at " + builtFile;

        string pluginFolder = Environment.ExpandEnvironmentVariables(
            @"%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\demo time\BepInEx\plugins\Stu-AH64");
        if (Directory.Exists(pluginFolder))
        {
            string destFolder = Path.Combine(pluginFolder, "AssetBundles");
            Directory.CreateDirectory(destFolder);
            File.Copy(builtFile, Path.Combine(destFolder, BundleName), true);
            Debug.Log("[AH64Phase4Builder] Installed ah64 bundle to profile.");
        }
        else
        {
            Debug.LogWarning("[AH64Phase4Builder] Profile plugin folder missing; bundle built but not installed.\n" + pluginFolder);
        }

        var info = new FileInfo(builtFile);
        Debug.Log($"[AH64Phase4Builder] Built '{BundleName}' - {info.Length / 1024f / 1024f:F2} MB\n{builtFile}");
        return null;
    }

    private static void TagFolderBundle(string folderPath, string bundleName)
    {
        AssetImporter importer = AssetImporter.GetAtPath(folderPath);
        if (importer == null)
        {
            Debug.LogWarning("[AH64Phase4Builder] No importer for " + folderPath);
            return;
        }

        if (importer.assetBundleName != bundleName)
        {
            importer.assetBundleName = bundleName;
            Debug.Log($"[AH64Phase4Builder] Tagged '{folderPath}' → assetBundleName '{bundleName}'");
        }
    }

    private static string BuildModelPrefabs(GameObject fbxRoot)
    {
        GameObject instance = UnityEngine.Object.Instantiate(fbxRoot);
        instance.name = "mdlAH64";

        // FBX roots sometimes wrap content in an extra transform named after the file. Flatten if the
        // only child is a same-named wrapper holding the real parts.
        FlattenSingleWrapper(instance);

        ChildLocator locator = instance.GetComponent<ChildLocator>();
        if (!locator)
            locator = instance.AddComponent<ChildLocator>();

        List<ChildLocator.NameTransformPair> pairs = new List<ChildLocator.NameTransformPair>();
        List<string> missing = new List<string>();

        foreach (string name in RendererNames.Concat(AnchorNames))
        {
            Transform child = FindDeep(instance.transform, name);
            if (!child)
            {
                missing.Add(name);
                continue;
            }
            pairs.Add(new ChildLocator.NameTransformPair { name = name, transform = child });
        }

        if (missing.Count > 0)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            return "Missing ChildLocator targets on FBX: " + string.Join(", ", missing);
        }

        locator.transformPairs = pairs.ToArray();

        EnsureHurtboxCollider(FindDeep(instance.transform, "MainHurtbox"), new Vector3(1.6f, 1.4f, 3.2f));
        EnsureHurtboxCollider(FindDeep(instance.transform, "HeadHurtbox"), new Vector3(0.7f, 0.55f, 0.9f));

        string materialError = AH64MaterialFoundationBuilder.PrepareAndAssign(instance);
        if (materialError != null)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            return "Material foundation failed: " + materialError;
        }

        // Blur discs: transparent FX material, no shadows, and shipped INACTIVE — the lobby display
        // and portrait stay clean, the hopoo conversion pass skips inactive renderers, and
        // AH64FlightVisuals activates + fades them with rotor effort at runtime.
        Material blurMaterial = EnsureRotorBlurMaterial();
        SetupRotorBlurDisc(FindDeep(instance.transform, "RotorBlurMain"), blurMaterial);
        SetupRotorBlurDisc(FindDeep(instance.transform, "RotorBlurTail"), blurMaterial);

        // Undo the FBX exporter's spurious rotation on ChinGatling.
        //
        // In Art/Blender/build_ah64.py the gatling is parented to ChinBarrel with an
        // identity local rotation — verified in the saved .blend. The FBX exporter
        // nonetheless re-applies the Z-up -> Y-up 90 deg X conversion at the SECOND
        // nesting level, so it arrives here at (90,0,0) on top of ChinBarrel's own 90,
        // composing to 180 and pointing the cluster straight down. ChinBarrel is only one
        // level deep and is unaffected, which is what made this look like a modelling
        // error rather than an export one.
        //
        // Corrected here rather than pre-compensated in Blender: a -90 fudge in the build
        // script would make the .blend disagree with itself and break the moment the
        // exporter is fixed. Identity is what the source of truth actually says.
        Transform gatlingPivot = FindDeep(instance.transform, "ChinGatling");
        if (gatlingPivot)
            gatlingPivot.localRotation = Quaternion.identity;

        // Gatling ships hidden — it is the alternate primary, the M230 is default.
        //
        // This only covers prefabs CharacterModel does not drive, such as the
        // character-select display. On a live body it is NOT sufficient:
        // CharacterModel.UpdateMaterials sets renderer.enabled = true on every entry in
        // baseRendererInfos whenever materials go dirty, so this flag is overwritten
        // within a frame. Runtime visibility is owned by AH64GatlingSpin via
        // Renderer.forceRenderingOff, which CharacterModel does not touch.
        //
        // Disable the RENDERER, not the GameObject: ChinGatling is a child of ChinBarrel,
        // so SetActive(false) would take the M230 and the shared Muzzle anchor with it.
        Transform gatling = FindDeep(instance.transform, "ChinGatling");
        if (gatling)
        {
            MeshRenderer gatRenderer = gatling.GetComponent<MeshRenderer>();
            if (gatRenderer)
                gatRenderer.enabled = false;
        }

        // CharacterModel is added at runtime by Prefabs.SetupCharacterModel when missing; leaving it
        // off the authored prefab keeps Route A (customRendererInfos) as the source of truth.

        Directory.CreateDirectory(Path.GetDirectoryName(ModelPrefabPath) ?? BundleFolder);

        GameObject modelPrefab = PrefabUtility.SaveAsPrefabAsset(instance, ModelPrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        if (!modelPrefab)
            return "Failed to write " + ModelPrefabPath;

        // Display prefab: independent copy so lobby posing can't mutate the body model.
        GameObject displayInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        displayInstance.name = "AH64Display";
        PrefabUtility.SaveAsPrefabAsset(displayInstance, DisplayPrefabPath);
        UnityEngine.Object.DestroyImmediate(displayInstance);

        Debug.Log($"[AH64Phase4Builder] Wrote {ModelPrefabPath} and {DisplayPrefabPath} " +
                  $"({pairs.Count} ChildLocator entries).");
        return null;
    }

    private static void FlattenSingleWrapper(GameObject root)
    {
        if (root.transform.childCount != 1)
            return;

        Transform only = root.transform.GetChild(0);
        // If the wrapper itself is one of our named parts, leave it — that is the real hierarchy.
        if (RendererNames.Contains(only.name) || AnchorNames.Contains(only.name))
            return;

        List<Transform> grandchildren = new List<Transform>();
        for (int i = 0; i < only.childCount; i++)
            grandchildren.Add(only.GetChild(i));

        foreach (Transform child in grandchildren)
            child.SetParent(root.transform, true);

        UnityEngine.Object.DestroyImmediate(only.gameObject);
    }

    /// <summary>
    /// The blur material deliberately uses a legacy particle shader, not Standard: the mod's
    /// ConvertDefaultShaderToHopoo only converts shaders named standard*/autodesk*, so this material
    /// survives every conversion pass (display prefab included) with its transparency intact.
    /// </summary>
    private static Material EnsureRotorBlurMaterial()
    {
        return AH64MaterialFoundationBuilder.EnsureRotorBlurMaterial();
    }

    private static void SetupRotorBlurDisc(Transform disc, Material material)
    {
        if (!disc)
            return;

        MeshRenderer renderer = disc.GetComponent<MeshRenderer>();
        if (renderer)
        {
            if (material)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        disc.gameObject.SetActive(false);
    }

    private static void EnsureHurtboxCollider(Transform hurtbox, Vector3 size)
    {
        if (!hurtbox)
            return;

        BoxCollider box = hurtbox.GetComponent<BoxCollider>();
        if (!box)
            box = hurtbox.gameObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = size;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }
}
