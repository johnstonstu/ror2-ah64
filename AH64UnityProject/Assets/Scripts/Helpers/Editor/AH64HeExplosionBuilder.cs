using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds orange/yellow HE explosion VFX for the ah64 bundle (v3).
/// Visual only — no RoR2 EffectComponent (added at LoadEffect time in mod C#).
///
/// v3 art intent: one readable HE fireball — hard flash, decelerating deformed shell,
/// lopsided embers, sharp sparks, thin warm smoke. Whole event under a second so the
/// M230 doesn't smear into a fog bank at 11 rounds/sec.
///
/// Menu: AH64 -> Create HE Explosion Prefab
/// Menu: AH64 -> Create HE Explosion Prefab + Build Bundle
/// </summary>
public static class AH64HeExplosionBuilder
{
    // ---------------------------------------------------------------------
    // Root scale per variant. These multiply the per-child sizeMul values, so
    // read them together — BuildHydraRoot already authors its children at ~1.7x
    // internally, and a small root scale here silently cancels that out.
    //
    // Playtest 2026-08-03: HydraScale was still 0.55 from when the Hydra was a
    // scaled-down copy of the primary, which made the special render SMALLER than
    // the M230 splash (1.4-1.87 against 1.5-2.0). Raised to 1.4 for roughly 2.5x
    // its old size, and ~2.4x the primary. M230 trimmed 20% — it was landing well
    // but reading slightly hot at close range.
    // ---------------------------------------------------------------------
    private const float HeScale = 0.8f;
    private const float HydraScale = 1.4f;

    // Gatling alternate primary: same composition as the M230 splash, smaller. It fires
    // faster than the M230, so its impact has to be lighter still or sustained fire turns
    // back into the wall of overlapping flashes the v2 playtest showed.
    private const float GatlingScale = 0.5f;

    // Heavy cannon: between the M230 and the Hydra in size, but the only primary impact
    // that gets to LINGER. The M230 and gatling are both held under a second because 11
    // and 18 rounds/sec smear anything longer into a wall; at 2.5 rounds/sec that
    // constraint disappears, so this one keeps its fireball and its smoke.
    private const float CannonScale = 1.15f;

    private const string EffectsFolder = "Assets/AH64/Bundle/AH64Effects";
    private const string PrefabPath = EffectsFolder + "/AH64HeExplosion.prefab";
    private const string HydraPrefabPath = EffectsFolder + "/AH64HydraExplosion.prefab";
    private const string GatlingPrefabPath = EffectsFolder + "/AH64GatlingExplosion.prefab";
    private const string CannonPrefabPath = EffectsFolder + "/AH64CannonExplosion.prefab";
    private const string MaterialFolder = EffectsFolder + "/Materials";
    private const string TextureFolder = EffectsFolder + "/Textures";
    private const string MeshFolder = EffectsFolder + "/Meshes";
    private const string AnimFolder = EffectsFolder + "/Anim";
    private const string AdditiveMatPath = MaterialFolder + "/matAH64ExplosionAdditive.mat";
    private const string SoftMatPath = MaterialFolder + "/matAH64ExplosionSoft.mat";
    private const string SparkMatPath = MaterialFolder + "/matAH64ExplosionSpark.mat";
    private const string GlowTexPath = TextureFolder + "/texAH64ExplosionGlow.png";
    private const string SoftTexPath = TextureFolder + "/texAH64ExplosionSoft.png";
    private const string SparkTexPath = TextureFolder + "/texAH64ExplosionSpark.png";
    private const string BlobMeshPath = MeshFolder + "/meshAH64ExplosionBlob.asset";
    private const string RingMeshPath = MeshFolder + "/meshAH64ExplosionRing.asset";

    private const string RepoBundleDestRelative = @"Build\plugins\AssetBundles";

    [MenuItem("AH64/Create HE Explosion Prefab")]
    public static void CreatePrefabOnly()
    {
        string result = CreatePrefabs();
        if (result != null)
        {
            EditorUtility.DisplayDialog("AH64 HE Explosion", result, "OK");
            return;
        }

        EditorUtility.DisplayDialog(
            "AH64 HE Explosion",
            "Created v3:\n" + PrefabPath + "\n" + HydraPrefabPath +
            "\n\nRun AH64 -> Build AssetBundle when ready to pack.",
            "OK");
    }

    [MenuItem("AH64/Create HE Explosion Prefab + Build Bundle")]
    public static void CreatePrefabAndBuildBundle()
    {
        string createError = CreatePrefabs();
        if (createError != null)
        {
            EditorUtility.DisplayDialog("AH64 HE Explosion", createError, "OK");
            return;
        }

        AH64BundleBuilder.BuildBundles();
        SyncBundleToRepoBuildFolder();
        SyncBundleToProfile();

        EditorUtility.DisplayDialog(
            "AH64 HE Explosion",
            "v3 prefabs created and ah64 bundle rebuilt.\n\n" + PrefabPath + "\n" + HydraPrefabPath,
            "OK");
    }

    /// <summary>Batchmode / MCP entry point.</summary>
    public static void RunFromCommandLine()
    {
        string err = CreatePrefabs();
        if (err != null)
        {
            Debug.LogError("[AH64HeExplosionBuilder] " + err);
            return;
        }

        AH64BundleBuilder.BuildBundles();
        SyncBundleToRepoBuildFolder();
        SyncBundleToProfile();
        Debug.Log("[AH64HeExplosionBuilder] Done — v3 prefabs + bundle.");
    }

    public static string CreatePrefabs()
    {
        EnsureFolder("Assets/AH64");
        EnsureFolder("Assets/AH64/Bundle");
        EnsureFolder(EffectsFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(AnimFolder);

        // Textures and mesh are regenerated every run — this builder is the single source
        // of truth for their content, and rewriting in place keeps the asset GUIDs stable.
        Texture2D glowTex = WriteRadialTexture(GlowTexPath, TextureKind.HotCore);
        Texture2D softTex = WriteRadialTexture(SoftTexPath, TextureKind.SoftPuff);
        Texture2D sparkTex = WriteRadialTexture(SparkTexPath, TextureKind.Streak);
        Mesh blobMesh = WriteBlobMesh(BlobMeshPath);
        Mesh ringMesh = WriteRingMesh(RingMeshPath);

        Material additive = GetOrCreateMaterial(AdditiveMatPath, additive: true, tex: glowTex);
        Material soft = GetOrCreateMaterial(SoftMatPath, additive: false, tex: softTex);
        Material spark = GetOrCreateMaterial(SparkMatPath, additive: true, tex: sparkTex);
        if (!additive || !soft || !spark)
            return "Failed to create particle materials.";

        var he = new Palette(additive, soft, spark, blobMesh, ringMesh);

        GameObject heRoot = BuildExplosionRoot("AH64HeExplosion", he, HeScale);
        PrefabUtility.SaveAsPrefabAsset(heRoot, PrefabPath);
        Object.DestroyImmediate(heRoot);

        // Hydra is NOT a scaled primary any more. A 0.55x copy of the M230 splash read
        // as "smaller primary" in playtest, not as a warhead — so it gets its own
        // composition: bigger core, a flat ground shockwave ring, and fragmentation.
        GameObject hydraRoot = BuildHydraRoot("AH64HydraExplosion", he, HydraScale);
        PrefabUtility.SaveAsPrefabAsset(hydraRoot, HydraPrefabPath);
        Object.DestroyImmediate(hydraRoot);

        GameObject gatlingRoot = BuildExplosionRoot("AH64GatlingExplosion", he, GatlingScale);
        PrefabUtility.SaveAsPrefabAsset(gatlingRoot, GatlingPrefabPath);
        Object.DestroyImmediate(gatlingRoot);

        GameObject cannonRoot = BuildCannonRoot("AH64CannonExplosion", he, CannonScale);
        PrefabUtility.SaveAsPrefabAsset(cannonRoot, CannonPrefabPath);
        Object.DestroyImmediate(cannonRoot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AH64HeExplosionBuilder] Wrote v3 " + PrefabPath + " and " + HydraPrefabPath);
        return null;
    }

    /// <summary>Shared assets handed to every child builder.</summary>
    private struct Palette
    {
        public readonly Material Additive;
        public readonly Material Soft;
        public readonly Material Spark;
        public readonly Mesh Blob;
        public readonly Mesh Ring;

        public Palette(Material additive, Material soft, Material spark, Mesh blob, Mesh ring)
        {
            Additive = additive;
            Soft = soft;
            Spark = spark;
            Blob = blob;
            Ring = ring;
        }
    }

    private static GameObject BuildExplosionRoot(string name, Palette p, float scale)
    {
        var root = new GameObject(name);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * scale;

        // 0) Animated point light — the beat that sells "this is a detonation, not a decal".
        //    Light.range/intensity ignore transform scale, so they're scaled explicitly.
        AddLight(root.transform, "Light", name, scale);

        // 1) Hot white flash — one frame's worth, then gone.
        AddFlash(root.transform, "Flash", p.Additive);

        // 2) Deformed mesh shell — the "sphere that looks like an explosion".
        AddMeshShell(root.transform, "FireballShell", p.Additive, p.Blob);

        // 3) Secondary glowing chunks for irregular silhouette (few, large).
        AddEmberChunks(root.transform, "EmberChunks", p.Additive);

        // 4) Sparse sparks — accents, not a sparkler.
        AddSparks(root.transform, "Sparks", p.Spark);

        // 5) Thin warm smoke linger after the fireball.
        AddSmoke(root.transform, "Smoke", p.Soft, sizeMul: 0.85f, count: 4, alphaMul: 0.8f);

        return root;
    }

    /// <summary>
    /// M789 heavy cannon impact. The M230's composition, but allowed to breathe.
    ///
    /// <para>Every other primary impact in this file is deliberately cut short because
    /// sustained fire would smear them together. At 2.5 rounds/sec that pressure is gone,
    /// so this one keeps a longer fireball and real lingering smoke — the presentation
    /// the other two structurally cannot have. It is the main thing that makes a slow
    /// weapon feel heavy rather than just infrequent.</para>
    /// </summary>
    private static GameObject BuildCannonRoot(string name, Palette p, float scale)
    {
        var root = new GameObject(name);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * scale;

        AddLight(root.transform, "Light", name, scale, peak: 16f, range: 12f, tail: 0.22f);
        AddFlash(root.transform, "Flash", p.Additive, sizeMul: 1.35f, life: 0.09f);

        //Nearly half again the M230's fireball life. This is the deliberate part.
        AddMeshShell(root.transform, "FireballShell", p.Additive, p.Blob,
            sizeMul: 1.4f, life: 0.46f, count: 3);
        AddEmberChunks(root.transform, "EmberChunks", p.Additive, sizeMul: 1.35f, count: 8);
        AddSparks(root.transform, "Sparks", p.Spark);

        //More smoke, and it stays. At this cadence it reads as a battlefield rather than
        //as clutter, and it marks where the last shot landed.
        AddSmoke(root.transform, "Smoke", p.Soft, sizeMul: 1.35f, count: 7, alphaMul: 1.15f);

        return root;
    }

    /// <summary>
    /// Longbow/Hydra warhead. Shares the palette but not the composition — this one
    /// has to out-read the primary at a glance, so it gets a ground shockwave ring and
    /// fragmentation, neither of which the M230 splash has.
    /// </summary>
    private static GameObject BuildHydraRoot(string name, Palette p, float scale)
    {
        var root = new GameObject(name);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * scale;

        // Brighter and longer than the primary's — this is the one that should wash
        // the terrain when a salvo lands. Peak/range are deliberately NOT scaled up
        // in step with the 1.4 root: light intensity that tracks visual size linearly
        // blows the frame out, and a salvo lands six of these.
        AddLight(root.transform, "Light", name, scale, peak: 18f, range: 13f, tail: 0.26f);

        AddFlash(root.transform, "Flash", p.Additive, sizeMul: 1.7f, life: 0.09f);
        AddMeshShell(root.transform, "FireballShell", p.Additive, p.Blob,
            sizeMul: 1.75f, life: 0.42f, count: 3);

        // Flat ring hugging the ground — the concussion read. This is the single
        // clearest tell that a rocket landed rather than a cannon round.
        AddShockRing(root.transform, "ShockRing", p.Additive, p.Ring);

        AddEmberChunks(root.transform, "EmberChunks", p.Additive, sizeMul: 1.6f, count: 9);

        // Fragmentation, in two layers: hot streaks that leave the blast fast, and
        // slower tumbling chunks that arc and fall. One alone doesn't read as shrapnel.
        AddShrapnel(root.transform, "Shrapnel", p.Spark);
        AddFragChunks(root.transform, "FragChunks", p.Additive, p.Blob);

        AddSmoke(root.transform, "Smoke", p.Soft, sizeMul: 1.5f, count: 7, alphaMul: 1f);

        return root;
    }

    /// <summary>
    /// Expanding annulus lying in the ground plane. Very short — a shockwave that
    /// lingers stops reading as a shockwave and starts reading as a decal.
    /// </summary>
    private static void AddShockRing(Transform parent, string childName, Material mat, Mesh ringMesh)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.05f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.26f;
        main.startSpeed = 0f;
        main.startSize = 1.1f;
        main.startColor = new Color(1f, 0.8f, 0.4f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 2;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        // Races out and stops dead — the deceleration is the whole effect.
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 2.4f),
            new Keyframe(1f, 3.1f)));

        SetGradient(ps,
            new[] { new Color(1f, 0.92f, 0.6f), new Color(1f, 0.5f, 0.12f) },
            new[] { 0f, 1f },
            new[] { 0.85f, 0.5f, 0f },
            new[] { 0f, 0.35f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        if (ringMesh)
            renderer.mesh = ringMesh;
        renderer.enableGPUInstancing = false;
        renderer.sortingFudge = -3f;
    }

    /// <summary>Hot fragment streaks — fast, thin, and they outlive the fireball.</summary>
    private static void AddShrapnel(Transform parent, string childName, Material mat)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.05f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(11f, 21f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.1f);
        main.startColor = new Color(1f, 0.94f, 0.6f, 1f);
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 26;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        // Hemisphere, not sphere: fragments that spray downward into the ground are
        // wasted, and an upward bias reads as a warhead detonating on contact.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.14f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.separateAxes = false;
        limit.limit = new ParticleSystem.MinMaxCurve(2.5f);
        limit.dampen = 0.12f;

        SetGradient(ps,
            new[]
            {
                new Color(1f, 0.98f, 0.78f),
                new Color(1f, 0.55f, 0.12f),
                new Color(0.5f, 0.1f, 0.02f),
            },
            new[] { 0f, 0.35f, 1f },
            new[] { 1f, 0.85f, 0f },
            new[] { 0f, 0.6f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.2f;
        renderer.velocityScale = 0.14f;
        renderer.sortingFudge = -2f;
    }

    /// <summary>Tumbling casing fragments — physical debris, cooling as they arc.</summary>
    private static void AddFragChunks(Transform parent, string childName, Material mat, Mesh blobMesh)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.06f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = new Color(1f, 0.6f, 0.2f, 1f);
        main.gravityModifier = 2.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 12;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.12f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.separateAxes = true;
        rotOverLife.x = new ParticleSystem.MinMaxCurve(-7f, 7f);
        rotOverLife.y = new ParticleSystem.MinMaxCurve(-7f, 7f);
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-7f, 7f);

        SetGradient(ps,
            new[]
            {
                new Color(1f, 0.7f, 0.25f),
                new Color(0.8f, 0.25f, 0.05f),
                new Color(0.18f, 0.06f, 0.03f),
            },
            new[] { 0f, 0.4f, 1f },
            new[] { 1f, 0.9f, 0f },
            new[] { 0f, 0.65f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        if (blobMesh)
            renderer.mesh = blobMesh;
        renderer.enableGPUInstancing = false;
    }

    // ------------------------------------------------------------------
    // Light
    // ------------------------------------------------------------------

    /// <summary>
    /// Point light driven by a generated legacy AnimationClip. Legacy Animation is used
    /// rather than RoR2's LightIntensityCurve because that component doesn't exist in this
    /// Unity project — a clip is fully self-contained and survives the bundle intact.
    /// </summary>
    private static void AddLight(
        Transform parent, string childName, string prefabName, float scale,
        float peak = 13f, float range = 10f, float tail = 0.14f)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(parent, false);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.62f, 0.26f);
        light.range = range * scale;
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.Auto;
        light.bounceIntensity = 0f;

        // Spike then decay. Peak has to land within a frame or two of impact or it reads
        // as a lamp switching on rather than a blast.
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.02f, peak * scale),
            new Keyframe(tail * 0.43f, peak * 0.35f * scale),
            new Keyframe(tail, 0f));

        AnimationClip clip = WriteLightClip(AnimFolder + "/anim" + prefabName + "Light", curve);

        var anim = go.AddComponent<Animation>();
        anim.clip = clip;
        anim.AddClip(clip, clip.name);
        anim.playAutomatically = true;
        anim.wrapMode = WrapMode.Once;
        // No Renderer on this GameObject, so default culling would never tick the clip.
        anim.cullingType = AnimationCullingType.AlwaysAnimate;
    }

    private static AnimationClip WriteLightClip(string pathNoExt, AnimationCurve intensity)
    {
        string path = pathNoExt + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        bool isNew = !clip;
        if (isNew)
            clip = new AnimationClip();

        clip.name = Path.GetFileNameWithoutExtension(path);
        clip.legacy = true;
        clip.wrapMode = WrapMode.Once;
        clip.ClearCurves();
        clip.SetCurve("", typeof(Light), "m_Intensity", intensity);

        if (isNew)
            AssetDatabase.CreateAsset(clip, path);
        else
            EditorUtility.SetDirty(clip);

        return clip;
    }

    // ------------------------------------------------------------------
    // Particle children
    // ------------------------------------------------------------------

    private static void AddFlash(
        Transform parent, string childName, Material mat, float sizeMul = 1f, float life = 0.07f)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.04f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.5f * sizeMul, 2.0f * sizeMul);
        main.startColor = new Color(1f, 0.97f, 0.8f, 1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        // Snaps open, then coasts — the opposite of a linear ramp.
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.35f, 1.3f),
            new Keyframe(1f, 1.45f)));

        SetGradient(ps,
            new[] { new Color(1f, 1f, 0.92f), new Color(1f, 0.72f, 0.28f) },
            new[] { 0f, 1f },
            new[] { 1f, 0f },
            new[] { 0f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = -4f;
    }

    private static void AddMeshShell(
        Transform parent, string childName, Material mat, Mesh blobMesh,
        float sizeMul = 1f, float life = 0.32f, int count = 2)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.06f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.81f, life);
        main.startSpeed = 0.1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f * sizeMul, 1.05f * sizeMul);
        main.startColor = new Color(1f, 0.85f, 0.35f, 0.95f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = count + 1;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Random orientation per particle so the two shells never line up, and a slow
        // tumble so the noise ridges crawl. Without this the blob reads as a static ball.
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.flipRotation = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.separateAxes = true;
        rotOverLife.x = new ParticleSystem.MinMaxCurve(-1.1f, 1.1f);
        rotOverLife.y = new ParticleSystem.MinMaxCurve(-1.4f, 1.4f);
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-1.1f, 1.1f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        // Punch out hard, then brake. Real blast fronts decelerate almost immediately —
        // the stop is what reads as pressure rather than inflation.
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.12f, 1.0f),
            new Keyframe(0.4f, 1.16f),
            new Keyframe(1f, 1.24f)));

        SetGradient(ps,
            new[]
            {
                new Color(1f, 0.97f, 0.72f),
                new Color(1f, 0.62f, 0.16f),
                new Color(0.85f, 0.26f, 0.04f),
                new Color(0.25f, 0.05f, 0.01f),
            },
            new[] { 0f, 0.3f, 0.65f, 1f },
            new[] { 0.95f, 0.85f, 0.35f, 0f },
            new[] { 0f, 0.35f, 0.7f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        if (blobMesh)
            renderer.mesh = blobMesh;
        // Legacy particle shaders don't declare instancing; leaving this on only logs warnings.
        renderer.enableGPUInstancing = false;
    }

    private static void AddEmberChunks(
        Transform parent, string childName, Material mat, float sizeMul = 1f, int count = 7)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.08f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f * sizeMul, 0.7f * sizeMul);
        main.startColor = new Color(1f, 0.8f, 0.28f, 0.9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count * 2;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        // Chunks outrun the shell early then stall inside it — that overlap is what makes
        // the silhouette lumpy instead of a clean circle.
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.separateAxes = false;
        limit.limit = new ParticleSystem.MinMaxCurve(0.6f);
        limit.dampen = 0.7f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.55f);
        noise.frequency = 1.4f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.9f);
        noise.damping = true;
        noise.octaveCount = 2;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.55f),
            new Keyframe(0.25f, 1.15f),
            new Keyframe(1f, 0.85f)));

        SetGradient(ps,
            new[]
            {
                new Color(1f, 0.85f, 0.35f),
                new Color(1f, 0.45f, 0.08f),
                new Color(0.3f, 0.06f, 0.02f),
            },
            new[] { 0f, 0.4f, 1f },
            new[] { 0.9f, 0.6f, 0f },
            new[] { 0f, 0.4f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.OldestInFront;
    }

    private static void AddSparks(Transform parent, string childName, Material mat)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.06f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.055f);
        main.startColor = new Color(1f, 0.92f, 0.5f, 1f);
        main.gravityModifier = 1.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 16;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        // Air drag. Without it sparks fly dead-straight to the end of their life, which is
        // the single most "default particle system" tell in the whole effect.
        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.separateAxes = false;
        limit.limit = new ParticleSystem.MinMaxCurve(1.2f);
        limit.dampen = 0.22f;

        SetGradient(ps,
            new[]
            {
                new Color(1f, 0.97f, 0.7f),
                new Color(1f, 0.5f, 0.1f),
                new Color(0.4f, 0.07f, 0.02f),
            },
            new[] { 0f, 0.55f, 1f },
            new[] { 1f, 0.75f, 0f },
            new[] { 0f, 0.5f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 1.6f;
        renderer.velocityScale = 0.11f;
        renderer.sortingFudge = -2f;
    }

    private static void AddSmoke(
        Transform parent, string childName, Material mat,
        float sizeMul = 1f, int count = 4, float alphaMul = 1f)
    {
        GameObject go = CreateParticleChild(parent, childName);
        var ps = go.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.16f;
        main.loop = false;
        main.playOnAwake = true;
        main.startDelay = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * sizeMul, 0.9f * sizeMul);
        main.startColor = new Color(0.35f, 0.22f, 0.11f, 0.3f * alphaMul);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = -0.14f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = count * 2;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.35f);
        noise.frequency = 0.8f;
        noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.4f);
        noise.damping = true;
        noise.octaveCount = 1;
        noise.quality = ParticleSystemNoiseQuality.Low;

        var rotOverLife = ps.rotationOverLifetime;
        rotOverLife.enabled = true;
        rotOverLife.z = new ParticleSystem.MinMaxCurve(-0.7f, 0.7f);

        var sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.55f, 1f, 1.6f));

        // Fades in rather than popping — it should look lit by the fireball, then cool.
        // Values kept well under the terrain's: playtest v3 showed smoke reading as a
        // pale grey balloon on night stages. Explosion smoke has to sit DARKER than its
        // background, or it looks like steam.
        SetGradient(ps,
            new[]
            {
                new Color(0.38f, 0.24f, 0.12f),
                new Color(0.16f, 0.14f, 0.12f),
                new Color(0.08f, 0.075f, 0.07f),
            },
            new[] { 0f, 0.45f, 1f },
            new[] { 0f, 0.30f * alphaMul, 0.14f * alphaMul, 0f },
            new[] { 0f, 0.15f, 0.5f, 1f });

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = 4f;
    }

    private static GameObject CreateParticleChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.AddComponent<ParticleSystem>();
        return go;
    }

    private static void SetGradient(
        ParticleSystem ps, Color[] colors, float[] colorTimes, float[] alphas, float[] alphaTimes)
    {
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;

        var colorKeys = new GradientColorKey[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            colorKeys[i] = new GradientColorKey(colors[i], colorTimes[i]);

        var alphaKeys = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++)
            alphaKeys[i] = new GradientAlphaKey(alphas[i], alphaTimes[i]);

        var grad = new Gradient();
        grad.SetKeys(colorKeys, alphaKeys);
        colorOverLife.color = grad;
    }

    // ------------------------------------------------------------------
    // Generated assets
    // ------------------------------------------------------------------

    private enum TextureKind
    {
        /// <summary>Blown-out core with a fast falloff — flash, shell, embers.</summary>
        HotCore,

        /// <summary>Wide gentle puff — smoke.</summary>
        SoftPuff,

        /// <summary>Horizontal sliver for stretch-billboard sparks.</summary>
        Streak,
    }

    private static Texture2D WriteRadialTexture(string path, TextureKind kind)
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = Path.GetFileNameWithoutExtension(path);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c;
                float dy = (y - c) / c;
                float a;

                switch (kind)
                {
                    case TextureKind.SoftPuff:
                    {
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        a = Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f);
                        break;
                    }

                    case TextureKind.Streak:
                    {
                        // Tight in Y, generous in X — a stretched quad then reads as a sliver
                        // rather than the blurred lozenge a radial disc gives you.
                        float across = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dy)), 6f);
                        float along = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(dx)), 1.1f);
                        a = across * along;
                        break;
                    }

                    default:
                    {
                        float r = Mathf.Sqrt(dx * dx + dy * dy);
                        a = Mathf.Clamp01(1f - r);
                        a = Mathf.SmoothStep(0f, 1f, a * a);
                        if (r < 0.28f)
                            a = 1f;
                        break;
                    }
                }

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        tex.Apply(false, false);
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath, "..", path)), png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>
    /// UV sphere with Perlin displacement along normals — the "deformed explosion sphere".
    /// Rewritten in place so the asset GUID survives and prefabs keep their reference.
    /// </summary>
    private static Mesh WriteBlobMesh(string path)
    {
        Mesh source = BuildBlobSphere(18, 24, 0.28f);
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (!existing)
        {
            source.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(source, path);
            return source;
        }

        existing.Clear();
        existing.vertices = source.vertices;
        existing.normals = source.normals;
        existing.uv = source.uv;
        existing.triangles = source.triangles;
        existing.RecalculateBounds();
        existing.RecalculateTangents();
        Object.DestroyImmediate(source);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    /// <summary>
    /// Flat annulus in the XZ plane for the shockwave. Built lying down rather than
    /// rotated at runtime, so the particle needs no rotation and always hugs the ground.
    /// UVs run around the circumference in U and across the band in V, so the radial
    /// glow texture gives a band that's bright mid-width and soft at both edges.
    /// </summary>
    private static Mesh WriteRingMesh(string path)
    {
        Mesh source = BuildRing(64, 0.55f, 1f);
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

        if (!existing)
        {
            source.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(source, path);
            return source;
        }

        existing.Clear();
        existing.vertices = source.vertices;
        existing.normals = source.normals;
        existing.uv = source.uv;
        existing.triangles = source.triangles;
        existing.RecalculateBounds();
        Object.DestroyImmediate(source);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static Mesh BuildRing(int segments, float innerRadius, float outerRadius)
    {
        var mesh = new Mesh();
        int ringVerts = (segments + 1) * 2;
        var vertices = new Vector3[ringVerts];
        var normals = new Vector3[ringVerts];
        var uvs = new Vector2[ringVerts];

        for (int i = 0; i <= segments; i++)
        {
            float u = (float)i / segments;
            float a = u * Mathf.PI * 2f;
            float cos = Mathf.Cos(a);
            float sin = Mathf.Sin(a);

            vertices[i * 2] = new Vector3(cos * innerRadius, 0f, sin * innerRadius);
            vertices[i * 2 + 1] = new Vector3(cos * outerRadius, 0f, sin * outerRadius);
            normals[i * 2] = Vector3.up;
            normals[i * 2 + 1] = Vector3.up;
            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        var tris = new int[segments * 6];
        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int b = i * 2;
            tris[ti++] = b;
            tris[ti++] = b + 1;
            tris[ti++] = b + 2;

            tris[ti++] = b + 1;
            tris[ti++] = b + 3;
            tris[ti++] = b + 2;
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh BuildBlobSphere(int latSegments, int lonSegments, float noiseAmp)
    {
        var mesh = new Mesh();
        int vCount = (latSegments + 1) * (lonSegments + 1);
        var vertices = new Vector3[vCount];
        var normals = new Vector3[vCount];
        var uvs = new Vector2[vCount];

        int vi = 0;
        for (int lat = 0; lat <= latSegments; lat++)
        {
            float v = (float)lat / latSegments;
            float theta = v * Mathf.PI;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int lon = 0; lon <= lonSegments; lon++)
            {
                float u = (float)lon / lonSegments;
                float phi = u * Mathf.PI * 2f;
                var dir = new Vector3(
                    sinT * Mathf.Cos(phi),
                    cosT,
                    sinT * Mathf.Sin(phi));

                // Multi-octave noise so it reads as a fireball, not a golf ball.
                float n =
                    Mathf.PerlinNoise(dir.x * 1.7f + 2.1f, dir.y * 1.7f + 0.4f) * 0.55f +
                    Mathf.PerlinNoise(dir.y * 3.3f + 5.2f, dir.z * 3.3f + 1.1f) * 0.30f +
                    Mathf.PerlinNoise(dir.z * 5.1f + 0.7f, dir.x * 5.1f + 3.9f) * 0.15f;
                n = (n - 0.5f) * 2f;
                float radius = 1f + n * noiseAmp;

                vertices[vi] = dir * radius;
                normals[vi] = dir.normalized;
                uvs[vi] = new Vector2(u, v);
                vi++;
            }
        }

        var tris = new int[latSegments * lonSegments * 6];
        int ti = 0;
        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int current = lat * (lonSegments + 1) + lon;
                int next = current + lonSegments + 1;

                tris[ti++] = current;
                tris[ti++] = next;
                tris[ti++] = current + 1;

                tris[ti++] = current + 1;
                tris[ti++] = next;
                tris[ti++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>
    /// Legacy particle shaders, deliberately, not Particles/Standard Unlit.
    ///
    /// On the Standard particle shader the blend mode is a GUI-only enum: the real state
    /// lives in _SrcBlend/_DstBlend/_ZWrite/renderQueue plus shader keywords, all written
    /// by StandardParticleShaderGUI. Setting _Mode from script (as v2 did) changes nothing,
    /// so both materials stayed opaque and every colour-over-lifetime gradient was discarded.
    /// The legacy shaders bake their blending into the shader itself, so there is no state
    /// to get wrong — and they're guaranteed present in a Built-In pipeline bundle.
    ///
    /// Trade-off: no soft-particle depth fade, so the fireball has a hard intersection line
    /// against terrain. Acceptable on a burst this short and small; revisit if it reads badly.
    /// </summary>
    private static Material GetOrCreateMaterial(string path, bool additive, Texture2D tex)
    {
        string[] candidates = additive
            ? new[] { "Legacy Shaders/Particles/Additive", "Particles/Additive" }
            : new[] { "Legacy Shaders/Particles/Alpha Blended", "Particles/Alpha Blended" };

        Shader shader = null;
        for (int i = 0; i < candidates.Length && !shader; i++)
            shader = Shader.Find(candidates[i]);

        if (!shader)
        {
            Debug.LogError("[AH64HeExplosionBuilder] No particle shader found for " + path);
            return null;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mat, path);
        }

        // Reassign every run: existing v2 materials on disk are still on the Standard shader.
        mat.shader = shader;

        // Legacy particle shaders compute 2 * vertexColor * _TintColor, so 0.5 grey is
        // neutral and the ParticleSystem's own colours drive the look unmodified.
        if (mat.HasProperty("_TintColor"))
            mat.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
        if (tex && mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", tex);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(assetPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void SyncBundleToRepoBuildFolder()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
        string repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
        string built = Path.Combine(projectRoot, "AssetBundles", "ah64");
        string destDir = Path.Combine(repoRoot, RepoBundleDestRelative);
        string dest = Path.Combine(destDir, "ah64");

        if (!File.Exists(built))
        {
            Debug.LogWarning("[AH64HeExplosionBuilder] Built ah64 bundle not found at " + built);
            return;
        }

        Directory.CreateDirectory(destDir);
        File.Copy(built, dest, true);

        string builtManifest = built + ".manifest";
        if (File.Exists(builtManifest))
            File.Copy(builtManifest, dest + ".manifest", true);

        Debug.Log("[AH64HeExplosionBuilder] Synced ah64 bundle to " + dest);
    }

    private static void SyncBundleToProfile()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? "";
        string built = Path.Combine(projectRoot, "AssetBundles", "ah64");
        string profile = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            @"r2modmanPlus-local\RiskOfRain2\profiles\demo time new\BepInEx\plugins\JohnstonStu-AH64\AssetBundles");

        if (!File.Exists(built) || !Directory.Exists(Path.GetDirectoryName(profile)))
            return;

        Directory.CreateDirectory(profile);
        File.Copy(built, Path.Combine(profile, "ah64"), true);
        string builtManifest = built + ".manifest";
        if (File.Exists(builtManifest))
            File.Copy(builtManifest, Path.Combine(profile, "ah64.manifest"), true);
    }
}
