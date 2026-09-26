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
        Require(entries.Length == 36, "Every existing slider/toggle must reach both UI and export.");
        Require(entries.Distinct().Count() == 36, "Duplicate bindings in UI/export.");
        Require(entries.Count(e => e.SettingType == typeof(float)) == 35, "Expected all 35 sliders.");
        Require(entries.Count(AH64BalanceReport.RequiresRestart) == 6, "Expected six prefab/SkillDef settings requiring restart.");
        Require(entries.Select(AH64BalanceReport.Category).Distinct().Count() == 7, "Expected seven settings categories.");
        var startup = entries.ToDictionary(e => e.Definition, e => e.BoxedValue);
        config.Bind("Unrelated", "Private value", "DO_NOT_EXPORT");
        var speed = (ConfigEntry<float>)entries.Single(e => e.Definition.Key == "Base speed");
        speed.Value = 11.25f;
        var cadence = (ConfigEntry<float>)entries.Single(e => e.Definition.Key == "Seconds per round at full spool");
        cadence.Value = 0.055f;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        string report = AH64BalanceReport.Build("test-version", entries, startup, "Feels good!\nTry this setup.");
        Require(report.Contains("Base speed = 11.25 (default: 10) [restart pending; active: 10]"), "Pending restart/default/current values not reported accurately.");
        Require(report.Contains("Seconds per round at full spool = 0.055"), "Culture or precision changed the cadence.");
        Require(report.Contains("[XM301 Gatling]") && report.Contains("[M789 Cannon]") && report.Contains("[Audio]"), "Missing weapon/audio category.");
        Require(report.Contains("Feels good!\nTry this setup.") && report.Contains("test-version"), "Missing comments/version.");
        Require(!report.Contains("DO_NOT_EXPORT"), "Unrelated config leaked into report.");
        Require(report.Split('\n').Count(line => line.Contains(" = ")) == 36, "Report must contain each control exactly once.");
        speed.Value = (float)startup[speed.Definition];
        report = AH64BalanceReport.Build("test", entries, startup, "");
        Require(!report.Contains("[restart pending;") && report.Contains("(none)"), "Reset/startup values or empty comments handled incorrectly.");
        foreach (var entry in entries.Where(e => e.SettingType == typeof(float)))
            Require(entry.Description.AcceptableValues is AcceptableValueRange<float>, "Slider lacks a bounded range: " + entry.Definition);
        Console.WriteLine("PASS: all 36 controls, seven categories, restart state, precision, comments, and export isolation.");
    }
}

namespace AH64.Survivors
{
    internal static class AH64RiskOfOptions
    {
        internal static ConfigEntryBase[] Entries;
        internal static void Register(ConfigEntryBase[] entries) { Entries = entries; }
    }
    internal static class AH64BalanceFeedback
    {
        internal static void Init(ConfigFile config, ConfigEntryBase[] entries) { }
    }
}
