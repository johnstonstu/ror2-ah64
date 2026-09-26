using UnityEditor;
using UnityEngine;

/// <summary>
/// Import policy for the approved CC0 grounded rotor bed. This stages the clip
/// for the ah64 bundle but never builds or deploys that bundle.
/// </summary>
public static class AH64AudioFoundationBuilder
{
    //Aquinn CC0 mono loop, prepared by Dust Front and EQ'd by tools/prepare_rotor.py.
    //A three-second PCM loop is cheap and needs no lossy re-encoding.
    public const string RotorClipPath =
        "Assets/AH64/Bundle/AH64Audio/sfxAH64RotorHoverGrounded.wav";

    [MenuItem("AH64/Prepare Grounded Rotor Audio (no bundle build)")]
    public static void PrepareFromMenu()
    {
        string error = PrepareAudioAsset();
        if (error != null)
        {
            Debug.LogError("[AH64AudioFoundation] " + error);
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[AH64AudioFoundation] Grounded rotor clip prepared at a conservative " +
                  "runtime volume; no bundle build or deployment was performed.");
    }

    public static string PrepareAudioAsset()
    {
        AssetDatabase.ImportAsset(RotorClipPath, ImportAssetOptions.ForceSynchronousImport);
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RotorClipPath);
        if (!clip)
            return "Approved rotor clip is missing at " + RotorClipPath + ".";

        AudioImporter importer = AssetImporter.GetAtPath(RotorClipPath) as AudioImporter;
        if (!importer)
            return "AudioImporter was not available for " + RotorClipPath + ".";

        importer.forceToMono = false;
        importer.loadInBackground = false;
        importer.preloadAudioData = true;
        importer.ambisonic = false;
        importer.defaultSampleSettings = new AudioImporterSampleSettings
        {
            loadType = AudioClipLoadType.DecompressOnLoad,
            compressionFormat = AudioCompressionFormat.PCM,
            quality = 1f,
            sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
        };
        importer.assetBundleName = "ah64";
        importer.SaveAndReimport();
        return null;
    }
}
