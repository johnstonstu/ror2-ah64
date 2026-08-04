using UnityEditor;
using UnityEngine;

/// <summary>
/// Import policy for the approved CC0 grounded rotor bed. This stages the clip
/// for the ah64 bundle but never builds or deploys that bundle.
/// </summary>
public static class AH64AudioFoundationBuilder
{
    // Switched from .mp3 to .wav 2026-08-03: the mp3 had 45ms leading / 15ms trailing encoder
    // padding, causing a silent gap every loop (AUDIO_INVESTIGATION.md). The wav is a trimmed,
    // lossless re-export of the same approved clip with that padding removed.
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
        importer.loadInBackground = true;
        importer.preloadAudioData = true;
        importer.ambisonic = false;
        importer.defaultSampleSettings = new AudioImporterSampleSettings
        {
            loadType = AudioClipLoadType.DecompressOnLoad,
            compressionFormat = AudioCompressionFormat.Vorbis,
            quality = 0.45f,
            sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
        };
        importer.assetBundleName = "ah64";
        importer.SaveAndReimport();
        return null;
    }
}
