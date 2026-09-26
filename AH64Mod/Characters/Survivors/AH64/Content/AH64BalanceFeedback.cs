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
            try
            {
                GUIUtility.systemCopyBuffer = AH64BalanceReport.Build(AH64Plugin.MODVERSION, entries, startupValues, Comments.Value);
                Log.Message("AH-64: all settings and comments copied. Paste into your feedback report.");
            }
            catch (Exception error)
            {
                Log.Error("AH-64 could not copy settings to the clipboard: " + error);
                throw;
            }
        }

        internal static void CopyAndOpen()
        {
            Copy();
            try
            {
                // Keep settings out of the URL: reports can exceed browser URL limits.
                Application.OpenURL(FeedbackUrl);
            }
            catch (Exception error)
            {
                Log.Error("AH-64 copied settings, but could not open " + FeedbackUrl + ": " + error);
                throw;
            }
        }
    }
}
