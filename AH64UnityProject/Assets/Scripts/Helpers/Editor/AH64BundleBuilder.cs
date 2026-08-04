using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-click assetbundle build for the AH64 mod.
///
/// <para>Deliberately written instead of using the AssetBundle Browser package listed in
/// Packages/manifest.json: that package is v1.7.0, long deprecated and unmaintained against Unity 2021.3.
/// Phase 4 wants a rebuild after every couple of model changes, so the loop needs to be reliable and
/// one keypress - not a GUI that may not even load.</para>
///
/// <para>Menu: <c>AH64 -> Build AssetBundle</c> (Ctrl+Alt+B).</para>
/// </summary>
public static class AH64BundleBuilder
{
    // Everything is written here first, then copied into the profile below.
    // Sits next to the Unity project folder, matching the path the mod csproj's PostBuild step expects.
    private const string OutputFolderName = "AssetBundles";

    // Where the game actually loads from. If this path doesn't exist (different machine, renamed
    // profile) the build still succeeds and just skips the copy with a warning.
    private const string ProfilePluginFolder =
        @"%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\demo time\BepInEx\plugins\Stu-AH64";

    [MenuItem("AH64/Build AssetBundle %&b")]
    public static void BuildBundles()
    {
        // Unity keeps a bundle name registered even after the last asset using it is retagged, so
        // GetAllAssetBundleNames() reports names that have no content. Purge those first.
        AssetDatabase.RemoveUnusedAssetBundleNames();

        // Only names that actually have assets assigned are worth reasoning about.
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames()
            .Where(name => AssetDatabase.GetAssetPathsFromAssetBundle(name).Length > 0)
            .ToArray();

        if (bundleNames.Length == 0)
        {
            EditorUtility.DisplayDialog("No assetbundles tagged",
                "No assets are tagged into an assetbundle.\n\n" +
                "Select the Assets/AH64/Bundle folder and set its AssetBundle name at the " +
                "bottom of the Inspector.",
                "OK");
            return;
        }

        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", OutputFolderName);
        Directory.CreateDirectory(outputPath);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            BuildAssetBundleOptions.ChunkBasedCompression, // LZ4 - the RoR2 convention
            BuildTarget.StandaloneWindows64);              // RoR2 is Windows 64-bit only

        if (manifest == null)
        {
            Debug.LogError("[AH64BundleBuilder] Build failed. Check the Console above for the cause.");
            return;
        }

        foreach (string bundleName in manifest.GetAllAssetBundles())
        {
            string builtFile = Path.Combine(outputPath, bundleName);
            var info = new FileInfo(builtFile);
            Debug.Log($"[AH64BundleBuilder] Built '{bundleName}' - {info.Length / 1024f / 1024f:F2} MB\n{builtFile}");

            CopyToProfile(builtFile, bundleName);
        }

        AssetDatabase.Refresh();
    }

    private static void CopyToProfile(string builtFile, string bundleName)
    {
        string pluginFolder = System.Environment.ExpandEnvironmentVariables(ProfilePluginFolder);

        if (!Directory.Exists(pluginFolder))
        {
            Debug.LogWarning($"[AH64BundleBuilder] Built OK, but the r2modman profile folder wasn't found, " +
                             $"so nothing was installed. Copy '{bundleName}' in by hand, or fix " +
                             $"{nameof(ProfilePluginFolder)} in this script.\nLooked for: {pluginFolder}");
            return;
        }

        string destFolder = Path.Combine(pluginFolder, OutputFolderName);
        Directory.CreateDirectory(destFolder);

        string dest = Path.Combine(destFolder, bundleName);
        File.Copy(builtFile, dest, true);

        Debug.Log($"[AH64BundleBuilder] Installed to profile:\n{dest}");
    }

    /// <summary>Batchmode entry: Unity -batchmode -executeMethod AH64BundleBuilder.RunFromCommandLine</summary>
    public static void RunFromCommandLine()
    {
        BuildBundles();
    }

    /// <summary>
    /// Opens the build output folder. Handy for sanity-checking file size after a build.
    /// </summary>
    [MenuItem("AH64/Reveal Build Output")]
    public static void RevealOutput()
    {
        string outputPath = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", OutputFolderName);

        if (!Directory.Exists(outputPath))
        {
            Debug.LogWarning($"[AH64BundleBuilder] Nothing built yet - {outputPath} doesn't exist.");
            return;
        }

        EditorUtility.RevealInFinder(outputPath);
    }
}
