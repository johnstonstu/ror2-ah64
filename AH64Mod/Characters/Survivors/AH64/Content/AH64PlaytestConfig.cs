using System;
using BepInEx.Configuration;

namespace AH64.Survivors
{
    /// <summary>
    /// Player-facing balance and presentation controls. Keep existing config keys so upgrades
    /// preserve players' values; Risk of Options supplies the friendlier category labels.
    /// </summary>
    internal static class AH64PlaytestConfig
    {
        private const string Movement = "AH-64 Playtest - Movement";
        private const string Chaingun = "AH-64 Playtest - M230";
        private const string Gatling = "AH-64 Playtest - XM301 Gatling";
        private const string Cannon = "AH-64 Playtest - M789 Cannon";
        private const string Utility = "AH-64 Playtest - Utility";
        private const string Presentation = "AH-64 Playtest - Presentation";

        private static ConfigEntry<float> baseMoveSpeed;
        private static ConfigEntry<float> acceleration;
        private static ConfigEntry<float> radarFacingSpeedBonus;
        private static ConfigEntry<float> airtime;
        private static ConfigEntry<float> airtimePerExtraJump;
        private static ConfigEntry<bool> classicAltitude;
        private static ConfigEntry<bool> controllerBDescends;
        private static ConfigEntry<KeyboardShortcut> descendKey;

        private static ConfigEntry<float> chaingunDamage;
        private static ConfigEntry<float> chaingunMaxSpread;
        private static ConfigEntry<float> chaingunBloom;
        private static ConfigEntry<float> chaingunReload;
        private static ConfigEntry<float> chaingunSplashDamage;
        private static ConfigEntry<float> chaingunSplashRadius;
        private static ConfigEntry<float> chaingunSplashVfxScale;

        private static ConfigEntry<float> gatlingDamage;
        private static ConfigEntry<float> gatlingSpooledDuration;
        private static ConfigEntry<float> gatlingMaxSpread;
        private static ConfigEntry<float> gatlingBloom;
        private static ConfigEntry<float> gatlingReload;
        private static ConfigEntry<float> gatlingSplashDamage;
        private static ConfigEntry<float> gatlingSplashRadius;
        private static ConfigEntry<float> gatlingSplashVfxScale;

        private static ConfigEntry<float> cannonDamage;
        private static ConfigEntry<float> cannonDuration;
        private static ConfigEntry<float> cannonReload;
        private static ConfigEntry<float> cannonSplashDamage;
        private static ConfigEntry<float> cannonSplashRadius;
        private static ConfigEntry<float> cannonSplashVfxScale;

        private static ConfigEntry<float> dashCooldown;
        private static ConfigEntry<float> dashPeakSpeed;
        private static ConfigEntry<float> dashClimbHeight;
        private static ConfigEntry<float> dashRampFraction;

        private static ConfigEntry<bool> rotorWashEnabled;
        private static ConfigEntry<float> rotorHoverVolume;
        private static ConfigEntry<float> gatlingSpoolVolume;
        private static ConfigEntry<float> rotorPitch;
        private static ConfigEntry<float> rotorLoadPitch;
        private static ConfigEntry<float> rotorLoadGain;
        private static ConfigEntry<float> rotorResponse;
        private static ConfigEntry<float> rotorToneCutoff;

        internal static float BaseMoveSpeed => baseMoveSpeed.Value;
        internal static float Acceleration => acceleration.Value;
        internal static float RadarFacingSpeedBonus => radarFacingSpeedBonus.Value;
        internal static float Airtime => airtime.Value;
        internal static float AirtimePerExtraJump => airtimePerExtraJump.Value;
        internal static bool ClassicAltitude => classicAltitude.Value;
        internal static bool ControllerBDescends => controllerBDescends.Value;
        internal static KeyboardShortcut DescendKey => descendKey.Value;
        internal static float ChaingunDamage => chaingunDamage.Value;
        internal static float ChaingunMaxSpread => chaingunMaxSpread.Value;
        internal static float ChaingunBloom => chaingunBloom.Value;
        internal static float ChaingunReload => chaingunReload.Value;
        internal static float ChaingunSplashDamage => chaingunSplashDamage.Value;
        internal static float ChaingunSplashRadius => chaingunSplashRadius.Value;
        internal static float ChaingunSplashVfxScale => chaingunSplashVfxScale.Value;
        internal static float GatlingDamage => gatlingDamage.Value;
        internal static float GatlingSpooledDuration => gatlingSpooledDuration.Value;
        internal static float GatlingMaxSpread => gatlingMaxSpread.Value;
        internal static float GatlingBloom => gatlingBloom.Value;
        internal static float GatlingReload => gatlingReload.Value;
        internal static float GatlingSplashDamage => gatlingSplashDamage.Value;
        internal static float GatlingSplashRadius => gatlingSplashRadius.Value;
        internal static float GatlingSplashVfxScale => gatlingSplashVfxScale.Value;
        internal static float CannonDamage => cannonDamage.Value;
        internal static float CannonDuration => cannonDuration.Value;
        internal static float CannonReload => cannonReload.Value;
        internal static float CannonSplashDamage => cannonSplashDamage.Value;
        internal static float CannonSplashRadius => cannonSplashRadius.Value;
        internal static float CannonSplashVfxScale => cannonSplashVfxScale.Value;
        internal static float DashCooldown => dashCooldown.Value;
        internal static float DashPeakSpeed => dashPeakSpeed.Value;
        internal static float DashClimbHeight => dashClimbHeight.Value;
        internal static float DashRampFraction => dashRampFraction.Value;
        internal static bool RotorWashEnabled => rotorWashEnabled.Value;
        internal static float RotorHoverVolume => rotorHoverVolume.Value;
        internal static float GatlingSpoolVolume => gatlingSpoolVolume.Value;
        internal static float RotorPitch => rotorPitch.Value;
        internal static float RotorLoadPitch => rotorLoadPitch.Value;
        internal static float RotorLoadGain => rotorLoadGain.Value;
        internal static float RotorResponse => rotorResponse.Value;
        internal static float RotorToneCutoff => rotorToneCutoff.Value;

        //Bumped when a release changes config-backed defaults. Not registered with Risk of Options.
        private const int CurrentBalanceVersion = 1;
        private static ConfigEntry<int> balanceVersion;

        internal static void Init(ConfigFile config)
        {
            balanceVersion = config.Bind(
                "AH-64 Internal",
                "Balance version",
                0,
                new ConfigDescription(
                    "Which balance migration has been applied. The mod writes this; it is not a player setting.",
                    null,
                    new ConfigurationManagerAttributes { Browsable = false }));

            baseMoveSpeed = Bind(config, Movement, "Base speed", 8.5f, 7f, 14f,
                "Horizontal cruise speed. Test this before changing hover mechanics.");
            acceleration = Bind(config, Movement, "Acceleration", 110f, 60f, 180f,
                "How quickly the helicopter starts, stops, and reverses while hovering.");
            radarFacingSpeedBonus = Bind(config, Movement, "Radar facing speed bonus", 0.15f, 0f, 0.35f,
                "Extra movement multiplier while facing the painted target (0.15 = +15%).");
            airtime = Bind(config, Movement, "Airtime seconds", AH64StaticValues.airtimeBase, 1f, 60f,
                "How long you can stay above resting height before settling back. Refills at resting height.");
            airtimePerExtraJump = Bind(config, Movement, "Airtime per extra jump", AH64StaticValues.airtimePerExtraJump, 0f, 10f,
                "Extra airtime seconds for each extra jump from items such as Hopoo Feather.");
            classicAltitude = config.Bind(Movement, "Classic altitude controls", false,
                "The 1.1 controls: hold jump to climb, release to settle back to resting height, pull back to "
                + "settle faster. No airtime limit. Off: jump climbs, descend drops, releasing both holds height.");
            controllerBDescends = config.Bind(Movement, "Controller B descends", true,
                "Hold B (Circle on PlayStation) to descend. Turn off if you have rebound B to something else.");
            //Keyboard-only; not shown in Risk of Options, which is wired for sliders and checkboxes here.
            descendKey = config.Bind(Movement, "Descend key", new KeyboardShortcut(UnityEngine.KeyCode.C),
                "Hold to descend on keyboard. Jump climbs; releasing both holds your height.");

            chaingunDamage = Bind(config, Chaingun, "Direct damage coefficient",
                AH64StaticValues.chaingunDamageCoefficient, 0.30f, 1.00f,
                "Per-round direct damage. Keep the cadence and proc coefficient unchanged while testing.");
            chaingunMaxSpread = Bind(config, Chaingun, "Maximum spread", 2.25f, 0.50f, 5f,
                "Maximum held-fire spread in degrees.");
            chaingunBloom = Bind(config, Chaingun, "Spread bloom", 0.08f, 0f, 0.25f,
                "Spread added per M230 round.");
            chaingunReload = Bind(config, Chaingun, "Drum reload seconds", 1.7f, 0.50f, 4f,
                "Time to refill the 30-round drum after firing stops or it empties.");
            chaingunSplashDamage = Bind(config, Chaingun, "HE splash coefficient",
                AH64StaticValues.chaingunSplashDamageCoefficient, 0f, 0.75f,
                "Damage at the centre of each 30mm HE impact. Splash proc remains hard-locked at zero.");
            chaingunSplashRadius = Bind(config, Chaingun, "HE splash radius", 6f, 0f, 12f,
                "World-space radius of each 30mm HE impact.");
            chaingunSplashVfxScale = Bind(config, Chaingun, "HE impact visual scale", AH64StaticValues.chaingunSplashVfxScale, 0.50f, 5f,
                "Visual-only size of the 30mm impact flash and dust. Does not change damage or radius.");

            gatlingDamage = Bind(config, Gatling, "Direct damage coefficient",
                AH64StaticValues.gatlingDamageCoefficient, 0.10f, 0.60f,
                "Per-round direct damage at any spool speed. Well under the M230's by design — "
                + "the gatling trades weight per round for rate.");
            gatlingSpooledDuration = Bind(config, Gatling, "Seconds per round at full spool",
                AH64StaticValues.gatlingSpooledDuration, 0.030f, 0.100f,
                "Lower is faster. 0.055 is ~18 rounds/sec. The cold rate ramps up to this over the spool.");
            gatlingMaxSpread = Bind(config, Gatling, "Maximum spread",
                AH64StaticValues.gatlingMaxSpread, 0.50f, 6f,
                "Maximum held-fire spread in degrees. Wider than the M230 on purpose.");
            gatlingBloom = Bind(config, Gatling, "Spread bloom",
                AH64StaticValues.gatlingSpreadBloom, 0f, 0.25f,
                "Spread added per round.");
            gatlingReload = Bind(config, Gatling, "Drum reload seconds",
                AH64StaticValues.gatlingReloadDuration, 0.50f, 5f,
                "Time to refill the 60-round drum after firing stops or it empties.");
            gatlingSplashDamage = Bind(config, Gatling, "Splash coefficient",
                AH64StaticValues.gatlingSplashDamageCoefficient, 0f, 0.40f,
                "Damage at the centre of each impact. Splash proc remains hard-locked at zero.");
            gatlingSplashRadius = Bind(config, Gatling, "Splash radius",
                AH64StaticValues.gatlingSplashRadius, 0f, 8f,
                "World-space radius of each impact. Much smaller than the M230's; the cadence is far higher.");
            gatlingSplashVfxScale = Bind(config, Gatling, "Impact visual scale",
                AH64StaticValues.gatlingSplashVfxScale, 0.30f, 4f,
                "Visual-only size of the impact flash. Does not change damage or radius.");

            cannonDamage = Bind(config, Cannon, "Direct damage coefficient",
                AH64StaticValues.cannonDamageCoefficient, 1.00f, 5.00f,
                "Per-round direct damage. Very high — there are only 8 of them and every miss costs.");
            cannonDuration = Bind(config, Cannon, "Seconds per round",
                AH64StaticValues.cannonBaseDuration, 0.20f, 1.00f,
                "Lower is faster. 0.40 is 2.5 rounds/sec. Drop this far and it stops reading as a "
                + "cannon and starts competing with Hydra.");
            cannonReload = Bind(config, Cannon, "Drum reload seconds",
                AH64StaticValues.cannonReloadDuration, 1.00f, 6f,
                "Time to refill the 8-round drum after firing stops or it empties.");
            cannonSplashDamage = Bind(config, Cannon, "Splash coefficient",
                AH64StaticValues.cannonSplashDamageCoefficient, 0f, 2.00f,
                "Damage at the centre of each blast. This is where the cannon beats the other two "
                + "primaries against packs. Splash proc remains hard-locked at zero.");
            cannonSplashRadius = Bind(config, Cannon, "Splash radius",
                AH64StaticValues.cannonSplashRadius, 0f, 16f,
                "World-space radius of each blast. The largest of the three primaries.");
            cannonSplashVfxScale = Bind(config, Cannon, "Impact visual scale",
                AH64StaticValues.cannonSplashVfxScale, 0.50f, 5f,
                "Visual-only size of the impact. Does not change damage or radius.");

            dashCooldown = Bind(config, Utility, "Evasive Roll cooldown", 4f, 1f, 10f,
                "Seconds between Evasive Rolls.");
            dashPeakSpeed = Bind(config, Utility, "Evasive Roll peak speed multiplier", 2.35f, 1f, 4f,
                "Peak horizontal speed relative to the speed at roll entry.");
            dashClimbHeight = Bind(config, Utility, "Evasive Roll climb height", 10f, 0f, 12f,
                "Net altitude gained by the roll. Keep this below the collective ceiling.");
            dashRampFraction = Bind(config, Utility, "Evasive Roll peak ramp", 0.28f, 0.10f, 0.60f,
                "Fraction of the roll spent building to peak speed. Lower values bite sooner.");

            rotorWashEnabled = config.Bind(Presentation, "Low-hover rotor wash", true,
                "Enable restrained rotor wash while near the ground, including stationary hover.");
            //A new key avoids inheriting the much louder gain used with the old quiet recording.
            rotorHoverVolume = Bind(config, Presentation, "Rotor volume", AH64StaticValues.rotorHoverVolume, 0f, 1f,
                "Rotor bed volume before master/SFX and a fixed -1.94 dB mix trim. The local pilot hears it without distance fade. Movement and ability response use the approved mix.");
            // Keep development audio keys bound so existing profiles retain their accepted mix.
            // Only rotor volume is exposed in the release UI and balance report.
            gatlingSpoolVolume = Bind(config, Presentation, "Gatling spool volume", AH64StaticValues.gatlingSpoolVolume, 0f, 1f,
                "XM301 wind-up/down volume only. 0.6 is about 4.4 dB quieter; gunfire volume is unchanged.");
            rotorPitch = Bind(config, Presentation, "Rotor pitch", AH64StaticValues.rotorHoverPitch, 0.7f, 1.2f,
                "Base playback speed/pitch. Lower is a slower, heavier chop. 1 is the recorded speed.");
            rotorLoadPitch = Bind(config, Presentation, "Rotor movement pitch change",
                AH64StaticValues.rotorFullLoadPitch - AH64StaticValues.rotorHoverPitch, 0f, 0.12f,
                "Directional pitch depth (three times this value, capped at 0.12). Forward/climb raise pitch; reverse/descent lower it; strafe adds a smaller lift. Zero keeps pitch constant.");
            rotorLoadGain = Bind(config, Presentation, "Rotor movement boost dB", AH64StaticValues.rotorFullLoadGainDb, 0f, 6f,
                "Extra rotor loudness at full movement or climb. The final Wwise emitter gain is capped at 1.");
            rotorResponse = Bind(config, Presentation, "Rotor response seconds", AH64StaticValues.rotorLayerFadeTime, 0.1f, 2f,
                "How gradually rotor pitch and volume respond to movement. Higher is smoother.");
            rotorToneCutoff = Bind(config, Presentation, "Rotor high-frequency cutoff Hz", AH64StaticValues.rotorToneCutoff, 600f, 20000f,
                "Approximate tonal target mapped to Wwise low-pass. Lower removes hiss; 20000 adds no filtering.");

            MigrateBalance();

            var entries = new ConfigEntryBase[] {
                baseMoveSpeed, acceleration, radarFacingSpeedBonus, classicAltitude, airtime, airtimePerExtraJump, controllerBDescends,
                chaingunDamage, chaingunMaxSpread, chaingunBloom, chaingunReload, chaingunSplashDamage, chaingunSplashRadius, chaingunSplashVfxScale,
                gatlingDamage, gatlingSpooledDuration, gatlingMaxSpread, gatlingBloom, gatlingReload,
                gatlingSplashDamage, gatlingSplashRadius, gatlingSplashVfxScale,
                cannonDamage, cannonDuration, cannonReload, cannonSplashDamage, cannonSplashRadius, cannonSplashVfxScale,
                dashCooldown, dashPeakSpeed, dashClimbHeight, dashRampFraction,
                rotorWashEnabled, rotorHoverVolume };
            AH64BalanceFeedback.Init(config, entries);
            AH64RiskOfOptions.Register(entries);
        }

        private static ConfigEntry<float> Bind(ConfigFile config, string section, string name, float value, float min, float max, string description)
        {
            return config.Bind(section, name, value,
                new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
        }

        /// <summary>
        /// BepInEx keeps whatever a player already saved, so a new default would never reach them.
        /// If their value is still the previous default, move it to the new one. A value they chose
        /// stays. Fresh installs already bind the new default, so the compare does not touch them.
        /// </summary>
        private static void MigrateBalance()
        {
            if (balanceVersion.Value >= CurrentBalanceVersion)
                return;

            if (balanceVersion.Value < 1)
            {
                //1.2.0 defaults -> 1.2.1 defaults.
                MigrateOldDefault(baseMoveSpeed, 10f);
                MigrateOldDefault(chaingunDamage, 0.62f);
                MigrateOldDefault(chaingunSplashDamage, 0.34f);
                MigrateOldDefault(gatlingDamage, 0.39f);
            }

            balanceVersion.Value = CurrentBalanceVersion;
        }

        private static void MigrateOldDefault(ConfigEntry<float> entry, float oldDefault)
        {
            float updated = (float)entry.DefaultValue;
            if (Matches(entry.Value, oldDefault) && !Matches(entry.Value, updated))
                entry.Value = updated;
        }

        private static bool Matches(float value, float expected)
        {
            return Math.Abs(value - expected) <= 0.0001f;
        }

        /// <summary>
        /// Duck-typed for BepInEx Configuration Manager, which hides an entry when a tag of this
        /// name has <c>Browsable == false</c>. Risk of Options only shows the list we register.
        /// </summary>
        private sealed class ConfigurationManagerAttributes
        {
            public bool? Browsable { get; set; }
        }
    }
}
