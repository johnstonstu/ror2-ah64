using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Builds and checks the 1.1 local playtest bundle, including its decoded rotor audio.</summary>
public static class AH64LocalTestBuilder
{
    public static void RunFromCommandLine()
    {
        AssetBundle bundle = null;
        try
        {
            string error = AH64AudioFoundationBuilder.PrepareAudioAsset();
            if (error != null)
                throw new InvalidOperationException("Rotor preparation failed: " + error);
            AssetDatabase.SaveAssets();

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string bundlePath = Path.Combine(projectRoot, "AssetBundles", "ah64");
            DateTime previousWrite = File.Exists(bundlePath)
                ? File.GetLastWriteTimeUtc(bundlePath) : DateTime.MinValue;
            DateTime buildStarted = DateTime.UtcNow;
            // Validation must finish before any profile copy; the game may still be open.
            if (AH64BuildSafety.ProfileDeploymentRequested)
                throw new InvalidOperationException("Local verification cannot install to a profile.");
            if (AH64BundleBuilder.BuildBundlesChecked(true) == null)
                throw new InvalidOperationException("Bundle build failed.");

            if (!File.Exists(bundlePath))
                throw new InvalidOperationException("Bundle build produced no file: " + bundlePath);
            DateTime written = File.GetLastWriteTimeUtc(bundlePath);
            // Allow filesystem timestamp precision, but never accept the previous bundle unchanged.
            if (written <= previousWrite || written < buildStarted.AddSeconds(-2))
                throw new InvalidOperationException("Bundle was not freshly written: " + bundlePath);

            // Do not reuse an already-loaded bundle: it could be the previous build's contents.
            // Nor unload unrelated bundles or assets owned by an open editor session.
            if (AssetBundle.GetAllLoadedAssetBundles().Any(loaded => loaded.name == "ah64"))
                throw new InvalidOperationException("The ah64 bundle is already loaded. Unload its " +
                    "owning verification session before verifying the freshly built file.");
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (!bundle)
                throw new InvalidOperationException("Cannot load built bundle: " + bundlePath);

            AudioClip[] rotors = bundle.LoadAllAssets<AudioClip>()
                .Where(clip => clip.name == "sfxAH64RotorHoverGrounded").ToArray();
            if (rotors.Length != 1)
                throw new InvalidOperationException("Expected one grounded rotor AudioClip; found " +
                    rotors.Length + ". Check duplicate bundle asset names.");
            AudioClip rotor = rotors[0];
            if (rotor.channels != 1 || rotor.samples != 131418 || rotor.frequency != 44100)
                throw new InvalidOperationException("Unexpected rotor format: channels=" + rotor.channels +
                    ", samples=" + rotor.samples + ", frequency=" + rotor.frequency +
                    "; expected mono, 131418 samples at 44100 Hz.");
            VerifyAudio(rotor);

            int modelMeshes = VerifyPrefab(bundle, "mdlAH64");
            int displayMeshes = VerifyPrefab(bundle, "AH64Display");
            Debug.Log("[AH64LocalTestBuilder] PASS: fresh ah64 bundle; one mono rotor, " +
                "131418 samples at 44100 Hz; mdlAH64=" + modelMeshes +
                " valid meshes, AH64Display=" + displayMeshes + " valid meshes.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AH64LocalTestBuilder] Local build verification failed: " + exception);
            throw;
        }
        finally
        {
            if (bundle)
                bundle.Unload(true);
        }
    }

    private static int VerifyPrefab(AssetBundle bundle, string name)
    {
        GameObject prefab = bundle.LoadAsset<GameObject>(name);
        if (!prefab)
            throw new InvalidOperationException("Built bundle is missing prefab " + name + ".");
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0)
            throw new InvalidOperationException(name + " has no mesh filters.");
        foreach (MeshFilter filter in filters)
        {
            if (!filter.sharedMesh)
                throw new InvalidOperationException(name + "/" + filter.name + " has a null mesh.");
        }
        return filters.Length;
    }

    private static void VerifyAudio(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        if (!clip.GetData(samples, 0))
            throw new InvalidOperationException("Built rotor PCM cannot be decoded.");
        float peak = 0f;
        double energy = 0;
        foreach (float sample in samples)
        {
            if (float.IsNaN(sample) || float.IsInfinity(sample))
                throw new InvalidOperationException("Built rotor contains non-finite PCM.");
            peak = Mathf.Max(peak, Mathf.Abs(sample));
            energy += sample * sample;
        }
        double rms = Math.Sqrt(energy / samples.Length);
        if (peak > 0.9f || peak < 0.85f || rms < 0.1 ||
            Mathf.Abs(samples[0] - samples[samples.Length - 1]) > 0.01f)
            throw new InvalidOperationException($"Built rotor failed signal check: peak={peak}, RMS={rms}.");
        Debug.Log($"[AH64LocalTestBuilder] Decoded rotor: peak={peak:F4}, RMS={rms:F4}; seam OK.");
    }
}
