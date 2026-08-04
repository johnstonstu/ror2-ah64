using System;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace AH64.Survivors
{
    /// <summary>
    /// Optional in-game playtest UI. Reflection keeps Risk Of Options out of the shipped
    /// assembly references, so the AH-64 still loads normally when the client-side helper is absent.
    /// </summary>
    internal static class AH64RiskOfOptions
    {
        private const string PluginGuid = "com.rune580.riskofoptions";

        internal static void Register(
            ConfigEntry<float> baseMoveSpeed, ConfigEntry<float> acceleration, ConfigEntry<float> radarFacingSpeedBonus,
            ConfigEntry<float> chaingunDamage, ConfigEntry<float> chaingunMaxSpread, ConfigEntry<float> chaingunBloom,
            ConfigEntry<float> chaingunReload, ConfigEntry<float> chaingunSplashDamage, ConfigEntry<float> chaingunSplashRadius,
            ConfigEntry<float> chaingunSplashVfxScale,
            ConfigEntry<float> dashCooldown, ConfigEntry<float> dashPeakSpeed, ConfigEntry<float> dashClimbHeight,
            ConfigEntry<float> dashRampFraction, ConfigEntry<bool> rotorWashEnabled, ConfigEntry<float> rotorHoverVolume,
            ConfigEntry<float> rotorInFlightVolume, ConfigEntry<float> rotorClimbVolume)
        {
            if (!Chainloader.PluginInfos.ContainsKey(PluginGuid))
            {
                return;
            }

            try
            {
                RegisterSlider(baseMoveSpeed, 7f, 14f);
                RegisterSlider(acceleration, 60f, 180f, "{0:0}");
                RegisterSlider(radarFacingSpeedBonus, 0f, 0.35f, "{0:0%}");

                RegisterSlider(chaingunDamage, 0.30f, 1.00f);
                RegisterSlider(chaingunMaxSpread, 0.50f, 5f);
                RegisterSlider(chaingunBloom, 0f, 0.25f);
                RegisterSlider(chaingunReload, 0.50f, 4f);
                RegisterSlider(chaingunSplashDamage, 0f, 0.75f);
                RegisterSlider(chaingunSplashRadius, 0f, 12f);
                RegisterSlider(chaingunSplashVfxScale, 0.50f, 3f);

                RegisterSlider(dashCooldown, 1f, 10f);
                RegisterSlider(dashPeakSpeed, 1f, 4f);
                RegisterSlider(dashClimbHeight, 0f, 12f);
                RegisterSlider(dashRampFraction, 0.10f, 0.60f);

                RegisterToggle(rotorWashEnabled);
                RegisterSlider(rotorHoverVolume, 0f, 0.15f);
                RegisterSlider(rotorInFlightVolume, 0f, 0.15f);
                RegisterSlider(rotorClimbVolume, 0f, 0.15f);
            }
            catch (Exception error)
            {
                Log.Warning("AH-64 could not register Risk Of Options playtest controls: " + error.Message);
            }
        }

        private static void RegisterSlider(ConfigEntry<float> entry, float min, float max, string format = "{0:0.00}")
        {
            Type sliderConfigType = Type.GetType("RiskOfOptions.OptionConfigs.SliderConfig, RiskOfOptions");
            Type sliderOptionType = Type.GetType("RiskOfOptions.Options.SliderOption, RiskOfOptions");
            object config = Activator.CreateInstance(sliderConfigType);
            SetField(config, "min", min);
            SetField(config, "max", max);
            SetField(config, "formatString", format);
            AddOption(Activator.CreateInstance(sliderOptionType, entry, config));
        }

        private static void RegisterToggle(ConfigEntry<bool> entry)
        {
            Type checkBoxConfigType = Type.GetType("RiskOfOptions.OptionConfigs.CheckBoxConfig, RiskOfOptions");
            Type checkBoxOptionType = Type.GetType("RiskOfOptions.Options.CheckBoxOption, RiskOfOptions");
            object config = Activator.CreateInstance(checkBoxConfigType);
            AddOption(Activator.CreateInstance(checkBoxOptionType, entry, config));
        }

        private static void AddOption(object option)
        {
            Type managerType = Type.GetType("RiskOfOptions.ModSettingsManager, RiskOfOptions");
            MethodInfo addOption = managerType.GetMethod("AddOption", new[] { option.GetType().BaseType, typeof(string), typeof(string) });
            addOption.Invoke(null, new object[] { option, AH64Plugin.MODUID, AH64Plugin.MODNAME });
        }

        private static void SetField(object instance, string name, object value)
        {
            instance.GetType().GetField(name).SetValue(instance, value);
        }
    }
}
