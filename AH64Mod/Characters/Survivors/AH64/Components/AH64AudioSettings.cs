using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>Reads the actual game audio controls without changing global Unity audio state.</summary>
    internal static class AH64AudioSettings
    {
        private static float nextReadTime;
        private static float effectsGain = 1f;
        private static bool focusedOnly;
        private static bool warnedInvalidSetting;

        internal static float EffectsGain
        {
            get
            {
                if (Time.unscaledTime >= nextReadTime)
                {
                    nextReadTime = Time.unscaledTime + 0.25f;
                    // AudioManager's volume convars read the Wwise RTPC (0..100).
                    effectsGain = Mathf.Clamp01(ReadSetting("volume_master", 100f) / 100f)
                        * Mathf.Clamp01(ReadSetting("volume_sfx", 100f) / 100f);
                    focusedOnly = ReadSetting("audio_focused_only", 0f) != 0f;
                }
                return focusedOnly && !Application.isFocused ? 0f : effectsGain;
            }
        }

        private static float ReadSetting(string name, float fallback)
        {
            if (!Console.instance)
                return fallback;
            var setting = Console.instance.FindConVar(name);
            if (setting != null && TextSerialization.TryParseInvariant(setting.GetString(), out float value)
                && !float.IsNaN(value) && !float.IsInfinity(value))
                return value;
            if (!warnedInvalidSetting)
            {
                warnedInvalidSetting = true;
                Log.Warning($"AH-64 could not read audio setting {name}; using its default until the setting becomes available.");
            }
            return fallback;
        }
    }
}
