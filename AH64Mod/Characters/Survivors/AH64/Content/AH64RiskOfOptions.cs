using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine.Events;

namespace AH64.Survivors
{
    /// <summary>Optional UI: no assembly reference to Risk of Options is required at runtime.</summary>
    internal static class AH64RiskOfOptions
    {
        private const string PluginGuid = "com.rune580.riskofoptions";

        internal static void Register(IEnumerable<ConfigEntryBase> entries)
        {
            if (!Chainloader.PluginInfos.ContainsKey(PluginGuid)) return;
            try
            {
                foreach (var group in entries.GroupBy(AH64BalanceReport.Category))
                {
                    foreach (var entry in group) RegisterEntry(entry, group.Key);
                    RegisterShare(group.Key);
                }
                object inputConfig = Create("OptionConfigs.InputFieldConfig");
                SetField(inputConfig, "richText", false);
                AddOption(Create("Options.StringInputFieldOption", AH64BalanceFeedback.Comments, inputConfig));
                RegisterShare("Feedback");
                AddOption(Create("Options.GenericButtonOption", "Copy all settings", "Feedback",
                    "Copies every AH-64 slider, toggle, mod version and your comments. Paste wherever you prefer. Nothing is submitted automatically.",
                    "Copy", new UnityAction(AH64BalanceFeedback.Copy)));
                TypeOf("ModSettingsManager").GetMethod("SetModDescription", new[] { typeof(string), typeof(string), typeof(string) })
                    .Invoke(null, new object[] { "Tune movement, weapons and audio. Defaults are the intended balance. "
                        + "Use Share settings in any category to copy the whole setup and open GitHub. "
                        + "Restart after changing settings marked as requiring it. Gameplay settings are local and are not synchronized between players.",
                        AH64Plugin.MODUID, "AH-64" });
            }
            catch (Exception error)
            {
                Log.Error("AH-64 could not register Risk of Options controls: " + error);
                throw;
            }
        }

        private static void RegisterEntry(ConfigEntryBase entry, string category)
        {
            bool isSlider = entry is ConfigEntry<float>;
            object config = Create(isSlider ? "OptionConfigs.SliderConfig" : "OptionConfigs.CheckBoxConfig");
            SetField(config, "category", category);
            SetField(config, "restartRequired", AH64BalanceReport.RequiresRestart(entry));
            if (isSlider)
            {
                var range = (AcceptableValueRange<float>)entry.Description.AcceptableValues;
                SetField(config, "min", range.MinValue);
                SetField(config, "max", range.MaxValue);
                // Three decimals keep the gatling cadence and small pitch changes readable.
                SetField(config, "formatString", "{0:0.###}");
            }
            AddOption(Create(isSlider ? "Options.SliderOption" : "Options.CheckBoxOption", entry, config));
        }

        private static void RegisterShare(string category)
        {
            AddOption(Create("Options.GenericButtonOption", "Share settings", category,
                "Copies ALL AH-64 settings and Feedback comments, then opens GitHub. "
                + "Paste into the settings field, review, and submit there. Requires a GitHub account. "
                + "No report is sent automatically. Use the Feedback category for copy-only.",
                "Copy & open GitHub", new UnityAction(AH64BalanceFeedback.CopyAndOpen)));
        }

        private static Type TypeOf(string name) => Type.GetType("RiskOfOptions." + name + ", RiskOfOptions", true);
        private static object Create(string name, params object[] args) => Activator.CreateInstance(TypeOf(name), args);

        private static void AddOption(object option)
        {
            MethodInfo addOption = TypeOf("ModSettingsManager").GetMethod("AddOption",
                new[] { TypeOf("Options.BaseOption"), typeof(string), typeof(string) });
            addOption.Invoke(null, new object[] { option, AH64Plugin.MODUID, "AH-64" });
        }

        private static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name).SetValue(instance, value);
        }
    }
}
