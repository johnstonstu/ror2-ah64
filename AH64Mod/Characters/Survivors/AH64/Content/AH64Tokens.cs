using System.Globalization;
using AH64.Modules;

namespace AH64.Survivors
{
    public static class AH64Tokens
    {
        public static void Init()
        {
            AddTokens();
        }

        public static void AddTokens()
        {
            // Prose lives in AH64.language. These binds only supply numbers and the
            // config-dependent descend phrase, in the order each template's {0}, {1}, ... expects.
            string prefix = AH64Survivor.AH64_PREFIX;

            Language.Bind(prefix + "DESCRIPTION", DescriptionArgs);
            Language.Bind(prefix + "PASSIVE_DESCRIPTION", PassiveArgs);
            Language.Bind(prefix + "PRIMARY_CHAINGUN_DESCRIPTION", ChaingunArgs);
            Language.Bind(prefix + "PRIMARY_GATLING_DESCRIPTION", GatlingArgs);
            Language.Bind(prefix + "PRIMARY_CANNON_DESCRIPTION", CannonArgs);
            Language.Bind(prefix + "SECONDARY_ROCKETPODS_DESCRIPTION", HydraArgs);
            Language.Bind(prefix + "UTILITY_DASH_DESCRIPTION", DashArgs);
            Language.Bind(prefix + "SPECIAL_HELLFIRE_DESCRIPTION", HellfireArgs);
            Language.Bind(prefix + "SPECIAL_LONGBOW_DESCRIPTION", LongbowArgs);
            Language.Bind(prefix + "ALTITUDE_HINT", AltitudeHintArgs);
            Language.Bind(prefix + "DESCEND_KEYBOARD", KeyboardArgs);
            Language.Bind(prefix + "DESCEND_BINDINGS", BindingListArgs);
            Language.Bind(prefix + "DESCEND_WITH_BINDINGS", DescendWithBindingsArgs);
        }

        private static object[] DescriptionArgs(CultureInfo culture)
        {
            return new object[]
            {
                Num(AH64StaticValues.primaryFullRange, "0", culture),
                Num(AH64StaticValues.primaryCloseRangeDamageScale * 100f, "0", culture),
                Num(AH64StaticValues.primaryCloseRange, "0", culture),
                Num(AH64StaticValues.hydraCloseRangeDamageScale * 100f, "0", culture),
                Num(AH64StaticValues.hydraCloseRange, "0", culture),
                Num(AH64StaticValues.hydraFullRange, "0", culture),
                AltitudeHint()
            };
        }

        private static object[] PassiveArgs(CultureInfo culture)
        {
            return new object[]
            {
                Num((AH64StaticValues.radarPaintedDamageMult - 1f) * 100f, "0", culture),
                Num(AH64StaticValues.radarFacingMoveSpeedMult * 100f, "0", culture),
                Num(AH64StaticValues.radarCloseArmor, "0.##", culture)
            };
        }

        private static object[] ChaingunArgs(CultureInfo culture)
        {
            return new object[]
            {
                Percent(AH64StaticValues.chaingunDamageCoefficient, culture),
                Percent(AH64StaticValues.chaingunSplashDamageCoefficient, culture),
                Num(AH64StaticValues.primaryCloseRangeDamageScale * 100f, "0", culture),
                Num(AH64StaticValues.primaryCloseRange, "0", culture),
                Num(AH64StaticValues.primaryFullRange, "0", culture),
                AH64StaticValues.chaingunMagazineSize.ToString(culture),
                Num(AH64StaticValues.chaingunReloadDuration, "0.##", culture)
            };
        }

        private static object[] GatlingArgs(CultureInfo culture)
        {
            return new object[]
            {
                Percent(AH64StaticValues.gatlingDamageCoefficient, culture),
                Percent(AH64StaticValues.gatlingSplashDamageCoefficient, culture),
                Num(AH64StaticValues.primaryCloseRangeDamageScale * 100f, "0", culture),
                Num(AH64StaticValues.primaryCloseRange, "0", culture),
                Num(AH64StaticValues.primaryFullRange, "0", culture),
                AH64StaticValues.gatlingMagazineSize.ToString(culture),
                Num(AH64StaticValues.gatlingReloadDuration, "0.##", culture)
            };
        }

        private static object[] CannonArgs(CultureInfo culture)
        {
            return new object[]
            {
                Percent(AH64StaticValues.cannonDamageCoefficient, culture),
                Percent(AH64StaticValues.cannonSplashDamageCoefficient, culture),
                AH64StaticValues.cannonMagazineSize.ToString(culture),
                Num(AH64StaticValues.cannonReloadDuration, "0.##", culture)
            };
        }

        private static object[] HydraArgs(CultureInfo culture)
        {
            return new object[]
            {
                Percent(AH64StaticValues.hydraDamageCoefficient, culture),
                Num(AH64StaticValues.hydraCloseRangeDamageScale * 100f, "0", culture),
                Num(AH64StaticValues.hydraCloseRange, "0", culture),
                Num(AH64StaticValues.hydraFullRange, "0", culture),
                AH64StaticValues.hydraRocketCount.ToString(culture),
                Num(AH64StaticValues.hydraReloadDuration, "0.##", culture),
                Num(AH64StaticValues.hydraReloadPerExtraRocket, "0.#", culture)
            };
        }

        private static object[] DashArgs(CultureInfo culture)
        {
            return new object[]
            {
                Num(AH64StaticValues.dashArmorBonus, "0.##", culture),
                Num(AH64StaticValues.dashDuration * AH64StaticValues.dashArmorDurationCoefficient, "0.##", culture)
            };
        }

        private static object[] HellfireArgs(CultureInfo culture)
        {
            return new object[] { Percent(AH64StaticValues.hellfireDamageCoefficient, culture) };
        }

        private static object[] LongbowArgs(CultureInfo culture)
        {
            return new object[]
            {
                AH64StaticValues.longbowMaxLocks.ToString(culture),
                Percent(AH64StaticValues.LongbowDamageCoefficient(0), culture),
                Percent(AH64StaticValues.LongbowDamageCoefficient(AH64StaticValues.longbowMaxLocks - 1), culture)
            };
        }

        private static object[] AltitudeHintArgs(CultureInfo culture)
        {
            string bindings = BindingList();
            string descend = bindings.Length > 0
                ? RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "DESCEND_WITH_BINDINGS")
                : RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "DESCEND");
            return new object[] { descend };
        }

        private static object[] KeyboardArgs(CultureInfo culture)
        {
            return new object[] { AH64PlaytestConfig.DescendKey.MainKey.ToString() };
        }

        private static object[] BindingListArgs(CultureInfo culture)
        {
            return new object[]
            {
                RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "DESCEND_CONTROLLER"),
                RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "DESCEND_KEYBOARD")
            };
        }

        private static object[] DescendWithBindingsArgs(CultureInfo culture)
        {
            return new object[] { BindingList() };
        }

        /// <summary>
        /// The character-select blurb's last sentence. Classic controls are a fixed line.
        /// The current controls name the descend binding, which is a config value.
        /// </summary>
        private static string AltitudeHint()
        {
            if (AH64PlaytestConfig.ClassicAltitude)
                return RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "ALTITUDE_HINT_CLASSIC");
            return RoR2.Language.GetString(AH64Survivor.AH64_PREFIX + "ALTITUDE_HINT");
        }

        /// <summary>
        /// "B on controller, C on keyboard", or whichever of those the player still has bound.
        /// Empty when neither is bound. Does not include the word "descend"; the template does.
        /// </summary>
        private static string BindingList()
        {
            string prefix = AH64Survivor.AH64_PREFIX;
            var key = AH64PlaytestConfig.DescendKey.MainKey;
            string keyboard = key == UnityEngine.KeyCode.None
                ? ""
                : RoR2.Language.GetString(prefix + "DESCEND_KEYBOARD");
            string controller = AH64PlaytestConfig.ControllerBDescends
                ? RoR2.Language.GetString(prefix + "DESCEND_CONTROLLER")
                : "";
            if (controller.Length > 0 && keyboard.Length > 0)
                return RoR2.Language.GetString(prefix + "DESCEND_BINDINGS");
            return controller + keyboard;
        }

        // "0.##" so 0.135 * 100 prints as 13.5. The general format used to emit 13.500001
        // for that one coefficient; every other tooltip number was already a short decimal.
        private static string Percent(float coefficient, CultureInfo culture)
        {
            return (coefficient * 100f).ToString("0.##", culture);
        }

        private static string Num(float value, string format, CultureInfo culture)
        {
            return value.ToString(format, culture);
        }
    }
}
