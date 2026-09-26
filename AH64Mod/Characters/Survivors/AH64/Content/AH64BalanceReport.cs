using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BepInEx.Configuration;

namespace AH64.Survivors
{
    /// <summary>Exports only the entries exposed in our UI, never the entire plugin config.</summary>
    internal static class AH64BalanceReport
    {
        internal static string Category(ConfigEntryBase entry)
        {
            if (entry.Definition.Section == "AH-64 Playtest - Presentation")
                return entry.SettingType == typeof(bool) ? "Presentation" : "Audio";
            const string prefix = "AH-64 Playtest - ";
            string section = entry.Definition.Section;
            return section.StartsWith(prefix, StringComparison.Ordinal) ? section.Substring(prefix.Length) : section;
        }

        internal static bool RequiresRestart(ConfigEntryBase entry)
        {
            string key = entry.Definition.Key;
            return key == "Base speed" || key == "Acceleration" || key == "Drum reload seconds"
                || key == "Evasive Roll cooldown";
        }

        internal static string Build(string version, IEnumerable<ConfigEntryBase> entries,
            IDictionary<ConfigDefinition, object> startupValues, string comments)
        {
            var report = new StringBuilder("AH-64 settings feedback\nMod version: ").Append(version).Append('\n');
            report.AppendLine("Local settings; not synchronized with other players. Changed values show their defaults.");
            report.AppendLine("Restart-required settings show the active startup value when a restart is pending.");
            foreach (var group in entries.GroupBy(Category))
            {
                report.Append('\n').Append('[').Append(group.Key).AppendLine("]");
                foreach (var entry in group)
                {
                    object value = entry.BoxedValue;
                    report.Append(entry.Definition.Key).Append(" = ").Append(Format(value));
                    if (!Equals(value, entry.DefaultValue))
                        report.Append(" (default: ").Append(Format(entry.DefaultValue)).Append(')');
                    if (RequiresRestart(entry) && startupValues.TryGetValue(entry.Definition, out object active)
                        && !Equals(value, active))
                        report.Append(" [restart pending; active: ").Append(Format(active)).Append(']');
                    report.Append('\n');
                }
            }
            report.AppendLine("\nComments:").AppendLine(string.IsNullOrWhiteSpace(comments) ? "(none)" : comments);
            report.AppendLine("\nRun context (please fill in):\nDifficulty / artifacts:\nSolo or multiplayer:\nLoadout / stage:\nOther balance mods:\nWhat felt too strong, weak, or good:");
            return report.ToString();
        }

        private static string Format(object value) => value is float number
            ? number.ToString("R", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
