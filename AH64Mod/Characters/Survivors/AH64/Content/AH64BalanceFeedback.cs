using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace AH64.Survivors
{
    internal static class AH64BalanceFeedback
    {
        private const string FeedbackUrl = "https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml";
        // Keep this field id in sync with .github/ISSUE_TEMPLATE/feedback.yml.
        private const string SettingsFieldId = "settings";
        private const int MaxPrefilledFeedbackUrlLength = 6000;
        private static ConfigEntryBase[] entries;
        private static Dictionary<ConfigDefinition, object> startupValues;
        internal static ConfigEntry<string> Comments { get; private set; }

        internal static void Init(ConfigFile config, ConfigEntryBase[] settings)
        {
            entries = settings;
            startupValues = entries.ToDictionary(entry => entry.Definition, entry => entry.BoxedValue);
            Comments = config.Bind("Feedback", "Comments", "",
                "Optional notes included with every copy. Saved locally until you clear them. "
                + "Describe how the setup feels; add run details after pasting into GitHub.");
        }

        internal static void Copy()
        {
            BuildAndCopyReport();
        }

        internal static void CopyAndOpen()
        {
            string report = BuildAndCopyReport();

            string url = FeedbackUrl;
            try
            {
                if (report.Length <= MaxPrefilledFeedbackUrlLength)
                {
                    string prefilledUrl = FeedbackUrl + "&" + SettingsFieldId + "=" + Uri.EscapeDataString(report);
                    if (prefilledUrl.Length <= MaxPrefilledFeedbackUrlLength)
                        url = prefilledUrl;
                }

                if (url == FeedbackUrl)
                    Log.Message("AH-64 report was copied, but is too long to prefill safely; paste it into the GitHub settings field.");
            }
            catch (Exception error)
            {
                Log.Error("AH-64 copied the report but could not prepare a prefilled GitHub link; opening the blank feedback form. " + error);
            }

            try
            {
                Application.OpenURL(url);
            }
            catch (Exception error)
            {
                Log.Error("AH-64 copied settings, but could not open the GitHub feedback form: " + error);
                throw;
            }
        }

        private static string BuildReport() => AH64BalanceReport.Build(
            AH64Plugin.MODVERSION, entries, startupValues, Comments.Value);

        private static string BuildAndCopyReport()
        {
            try
            {
                string report = BuildReport();
                GUIUtility.systemCopyBuffer = report;
                Log.Message("AH-64: all settings and comments copied to the clipboard.");
                return report;
            }
            catch (Exception error)
            {
                Log.Error("AH-64 could not build or copy the settings report: " + error);
                throw;
            }
        }
    }
}
