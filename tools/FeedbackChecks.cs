// Run with tools/check-feedback.ps1. Tests the real bindings and report formatter without Unity.
using System;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using AH64.Survivors;

internal static class FeedbackChecks
{
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        var config = new ConfigFile(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".cfg"), false);
        config.SaveOnConfigSet = false;
        AH64PlaytestConfig.Init(config);
        var entries = AH64RiskOfOptions.Entries;
        //1.2 added Classic altitude controls, Airtime seconds, Airtime per extra jump and Controller B descends.
        Require(entries.Length == 34, "Every existing slider/toggle must reach both UI and export.");
        Require(entries.Distinct().Count() == 34, "Duplicate bindings in UI/export.");
        Require(entries.Count(e => e.SettingType == typeof(float)) == 31, "Expected all 31 release sliders.");
        Require(entries.Count(AH64BalanceReport.RequiresRestart) == 6, "Expected six prefab/SkillDef settings requiring restart.");
        Require(entries.Select(AH64BalanceReport.Category).Distinct().Count() == 7, "Expected seven settings categories.");
        Require(entries.Where(e => AH64BalanceReport.Category(e) == "Audio")
            .Select(e => e.Definition.Key).SequenceEqual(new[] { "Rotor volume" }),
            "Rotor volume must be the only release Audio control.");
        Require(AH64PlaytestConfig.RotorHoverVolume == AH64StaticValues.rotorHoverVolume,
            "Fresh profiles must retain the approved rotor-volume default.");
        const string presentation = "AH-64 Playtest - Presentation";
        var legacyAudio = new[] {
            new { Key = "Rotor pitch", Value = 0.8849593f, Read = new Func<float>(() => AH64PlaytestConfig.RotorPitch) },
            new { Key = "Rotor movement pitch change", Value = 0.01499999f, Read = new Func<float>(() => AH64PlaytestConfig.RotorLoadPitch) },
            new { Key = "Rotor movement boost dB", Value = 5.144491f, Read = new Func<float>(() => AH64PlaytestConfig.RotorLoadGain) },
            new { Key = "Rotor response seconds", Value = 0.2807811f, Read = new Func<float>(() => AH64PlaytestConfig.RotorResponse) },
            new { Key = "Rotor high-frequency cutoff Hz", Value = 5000f, Read = new Func<float>(() => AH64PlaytestConfig.RotorToneCutoff) },
            new { Key = "Gatling spool volume", Value = 0.42f, Read = new Func<float>(() => AH64PlaytestConfig.GatlingSpoolVolume) }
        };
        foreach (var setting in legacyAudio)
            config.Bind(presentation, setting.Key, 0f).Value = setting.Value;
        config.Bind(presentation, "Rotor volume", 0f).Value = 0.255f;
        config.Save();
        var reloaded = new ConfigFile(config.ConfigFilePath, false);
        reloaded.SaveOnConfigSet = false;
        AH64PlaytestConfig.Init(reloaded);
        entries = AH64RiskOfOptions.Entries;
        foreach (var setting in legacyAudio)
        {
            Require(setting.Read() == setting.Value, "Saved audio tuning changed: " + setting.Key);
            Require(!entries.Any(e => e.Definition.Key == setting.Key), "Development audio control leaked into release UI.");
        }
        Require(AH64PlaytestConfig.RotorHoverVolume == 0.255f, "Saved rotor volume was reset.");
        var startup = entries.ToDictionary(e => e.Definition, e => e.BoxedValue);
        reloaded.Bind("Unrelated", "Private value", "DO_NOT_EXPORT");
        var speed = (ConfigEntry<float>)entries.Single(e => e.Definition.Key == "Base speed");
        speed.Value = 11.25f;
        var cadence = (ConfigEntry<float>)entries.Single(e => e.Definition.Key == "Seconds per round at full spool");
        cadence.Value = 0.055f;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        string report = AH64BalanceReport.Build("test-version", entries, startup, "Feels good!\nTry this setup.");
        Require(report.Contains("Base speed = 11.25 (default: 8.5) [restart pending; active: 8.5]"), "Pending restart/default/current values not reported accurately.");
        Require(report.Contains("Seconds per round at full spool = 0.055"), "Culture or precision changed the cadence.");
        Require(report.Contains("[XM301 Gatling]") && report.Contains("[M789 Cannon]") && report.Contains("[Audio]"), "Missing weapon/audio category.");
        Require(report.Contains("Feels good!\nTry this setup.") && report.Contains("test-version"), "Missing comments/version.");
        Require(!legacyAudio.Any(setting => report.Contains(setting.Key + " = ")), "Hidden audio controls leaked into report.");
        Require(!report.Contains("DO_NOT_EXPORT"), "Unrelated config leaked into report.");
        Require(report.Split('\n').Count(line => line.Contains(" = ")) == 34, "Report must contain each control exactly once.");
        speed.Value = (float)startup[speed.Definition];
        report = AH64BalanceReport.Build("test", entries, startup, "");
        Require(!report.Contains("[restart pending;") && report.Contains("(none)"), "Reset/startup values or empty comments handled incorrectly.");
        foreach (var entry in entries.Where(e => e.SettingType == typeof(float)))
            Require(entry.Description.AcceptableValues is AcceptableValueRange<float>, "Slider lacks a bounded range: " + entry.Definition);
        AH64BalanceFeedback.Comments.Value = "Unicode notes: café & hover + turns\nKeep this detail.";
        AH64BalanceFeedback.Copy();
        string copied = UnityEngine.GUIUtility.systemCopyBuffer;
        Require(UnityEngine.Application.OpenedUrl == null, "Copy-only unexpectedly opened the browser.");
        AH64BalanceFeedback.CopyAndOpen();
        string url = UnityEngine.Application.OpenedUrl;
        Require(url.Contains("&settings="), "A normal report should prefill the settings field.");
        Require(Uri.UnescapeDataString(url.Substring(url.IndexOf("&settings=", StringComparison.Ordinal) + 10)) == copied,
            "Prefill changed or truncated the clipboard report.");
        AH64BalanceFeedback.Comments.Value = new string('x', 7000);
        AH64BalanceFeedback.CopyAndOpen();
        Require(!UnityEngine.Application.OpenedUrl.Contains("&settings="), "Oversized report must use the plain form.");
        Require(UnityEngine.GUIUtility.systemCopyBuffer.Contains(new string('x', 7000)), "Fallback truncated the clipboard report.");
        AH64BalanceFeedback.Comments.Value = new string('漢', 1200);
        AH64BalanceFeedback.CopyAndOpen();
        Require(!UnityEngine.Application.OpenedUrl.Contains("&settings="), "Encoded URL length must also be bounded.");
        UnityEngine.Application.FailOpen = true;
        bool openFailed = false;
        try { AH64BalanceFeedback.CopyAndOpen(); }
        catch (InvalidOperationException) { openFailed = true; }
        Require(openFailed && Log.ErrorCount == 1, "Browser failure must be logged and rethrown.");
        Require(UnityEngine.GUIUtility.systemCopyBuffer.Contains(new string('漢', 1200)), "Browser failure lost the copied report.");
        System.IO.File.Delete(config.ConfigFilePath);
        CheckBalanceMigration();
        Console.WriteLine("PASS: all 34 release controls, legacy audio preservation, seven categories, restart state, precision, comments, export isolation, prefill/clipboard fallback, browser failure handling, and 1.2.1 default migration.");
    }

    private static void CheckBalanceMigration()
    {
        string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".cfg");
        var legacy = new ConfigFile(path, false);
        legacy.SaveOnConfigSet = false;
        legacy.Bind("AH-64 Playtest - Movement", "Base speed", 0f).Value = 10f;
        legacy.Bind("AH-64 Playtest - M230", "Direct damage coefficient", 0f).Value = 0.62f;
        legacy.Bind("AH-64 Playtest - M230", "HE splash coefficient", 0f).Value = 0.40f;
        legacy.Bind("AH-64 Playtest - XM301 Gatling", "Direct damage coefficient", 0f).Value = 0.39f;
        legacy.Save();

        var migrated = new ConfigFile(path, false);
        migrated.SaveOnConfigSet = false;
        AH64PlaytestConfig.Init(migrated);
        Require(AH64PlaytestConfig.BaseMoveSpeed == 8.5f, "Old default speed was not migrated.");
        Require(AH64PlaytestConfig.ChaingunDamage == 0.52f, "Old default M230 damage was not migrated.");
        Require(AH64PlaytestConfig.ChaingunSplashDamage == 0.40f, "Custom M230 splash was overwritten.");
        Require(AH64PlaytestConfig.GatlingDamage == 0.34f, "Old default XM301 damage was not migrated.");
        Require(migrated.Bind("AH-64 Internal", "Balance version", -1).Value == 1, "Balance version was not bumped.");
        Require(!AH64RiskOfOptions.Entries.Any(entry => entry.Definition.Key == "Balance version"),
            "Balance version leaked into the settings menu.");

        migrated.Bind("AH-64 Playtest - Movement", "Base speed", 0f).Value = 10f;
        migrated.Save();
        var retuned = new ConfigFile(path, false);
        retuned.SaveOnConfigSet = false;
        AH64PlaytestConfig.Init(retuned);
        Require(AH64PlaytestConfig.BaseMoveSpeed == 10f, "A speed retuned to the old default was migrated again.");

        string freshPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".cfg");
        var fresh = new ConfigFile(freshPath, false);
        fresh.SaveOnConfigSet = false;
        AH64PlaytestConfig.Init(fresh);
        Require(AH64PlaytestConfig.BaseMoveSpeed == 8.5f, "Fresh profile did not get the new speed.");
        Require(AH64PlaytestConfig.ChaingunDamage == 0.52f, "Fresh profile did not get the new M230 damage.");
        Require(AH64PlaytestConfig.ChaingunSplashDamage == 0.26f, "Fresh profile did not get the new M230 splash.");
        Require(AH64PlaytestConfig.GatlingDamage == 0.34f, "Fresh profile did not get the new XM301 damage.");
        Require(AH64StaticValues.PrimaryRangeDamageScale(0f) == 0.75f, "Primary ramp below 10m.");
        Require(AH64StaticValues.PrimaryRangeDamageScale(10f) == 0.75f, "Primary ramp at 10m.");
        Require(AH64StaticValues.PrimaryRangeDamageScale(30f) == 1f, "Primary ramp at 30m.");
        Require(AH64StaticValues.PrimaryRangeDamageScale(40f) == 1f, "Primary ramp beyond 30m.");
        Require(AH64StaticValues.PrimaryRangeDamageScale(20f) == 0.875f, "Primary ramp midpoint.");
        Require(AH64StaticValues.RangeDamageScale(0f, 8f, 25f, 0.75f) == 0.75f, "Hydra ramp at launch.");
        Require(AH64StaticValues.RangeDamageScale(25f, 8f, 25f, 0.75f) == 1f, "Hydra ramp at 25m.");
        Require(AH64StaticValues.LongbowDamageCoefficient(5) == AH64StaticValues.LongbowDamageCoefficient(8),
            "Longbow ramp must stop at the sixth missile.");
        Require(AH64StaticValues.LongbowDamageCoefficient(5) > AH64StaticValues.LongbowDamageCoefficient(4),
            "Longbow ramp must still climb through the sixth missile.");
        System.IO.File.Delete(path);
        System.IO.File.Delete(freshPath);
    }
}

namespace AH64.Survivors
{
    internal static class AH64RiskOfOptions
    {
        internal static ConfigEntryBase[] Entries;
        internal static void Register(ConfigEntryBase[] entries) { Entries = entries; }
    }
    internal static class AH64Plugin { internal const string MODVERSION = "test-version"; }
    internal static class Log
    {
        internal static int ErrorCount;
        internal static void Message(string message) { }
        internal static void Error(string message) { ErrorCount++; }
    }
}

namespace UnityEngine
{
    internal static class GUIUtility { internal static string systemCopyBuffer; }
    internal static class Application
    {
        internal static string OpenedUrl;
        internal static bool FailOpen;
        internal static void OpenURL(string url)
        {
            if (FailOpen) throw new InvalidOperationException("Simulated browser failure");
            OpenedUrl = url;
        }
    }
}