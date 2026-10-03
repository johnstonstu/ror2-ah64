using System;
using AH64.Modules;
using AH64.Survivors.Achievements;

namespace AH64.Survivors
{
    public static class AH64Tokens
    {
        public static void Init()
        {
            AddTokens();

            ////uncomment this to spit out a lanuage file with all the above tokens that people can translate
            ////make sure you set Language.usingLanguageFolder and printingEnabled to true
            //Language.PrintOutput("AH64.txt");
            ////refer to guide on how to build and distribute your mod with the proper folders
        }

        /// <summary>
        /// Altitude controls for the character description, from the player's config: the descend
        /// binding is configurable, and classic controls have no airtime limit.
        /// </summary>
        private static string AltitudeHint()
        {
            if (AH64PlaytestConfig.ClassicAltitude)
                return "Hold jump to climb; let go to settle back down, or pull back to settle faster.";

            var key = AH64PlaytestConfig.DescendKey.MainKey;
            string keyboard = key == UnityEngine.KeyCode.None ? "" : key + " on keyboard";
            string controller = AH64PlaytestConfig.ControllerBDescends ? "B on controller" : "";
            string bindings = controller.Length > 0 && keyboard.Length > 0 ? controller + ", " + keyboard
                : controller + keyboard;
            string descend = bindings.Length > 0 ? "descend (" + bindings + ")" : "descend";
            return "Hold jump to climb and " + descend + " to drop; let go of both to hold your height. "
                + "Time above resting height is limited, and extra jumps extend it.";
        }

        private static string PrimaryRangeClause()
        {
            return $"Both deal {Tokens.UtilityText($"{AH64StaticValues.primaryCloseRangeDamageScale * 100f:0}%")} within {Tokens.UtilityText($"{AH64StaticValues.primaryCloseRange:0}m")}, rising to full damage at {Tokens.UtilityText($"{AH64StaticValues.primaryFullRange:0}m")}.";
        }

        public static void AddTokens()
        {
            string prefix = AH64Survivor.AH64_PREFIX;

            string desc = "The AH-64 is a gunship that never lands, trading footspeed for altitude and a full weapons load.<color=#CCD3E0>" + Environment.NewLine + Environment.NewLine
             + "< ! > The M230 carries a fixed drum and reloads all at once — it hits harder past 30m, and only at 75% within 10m. Tap at range to keep the burst tight. The reload runs whether you emptied it or not, so top up before you commit." + Environment.NewLine + Environment.NewLine
             + "< ! > The rocket pods ripple over most of a second, so hold your aim through the salvo. Rockets deal 75% damage within 8m of flight and full damage from 25m. They run on their own cooldown and stay available mid-reload." + Environment.NewLine + Environment.NewLine
             + "< ! > Evasive Roll is a climbing forward-diagonal barrel roll — stick snaps forward, left, or right with i-frames on the way through and armor for the length of the roll. " + AltitudeHint() + Environment.NewLine + Environment.NewLine
             + "< ! > Fire Control Radar paints the strongest nearby threat. Hold Longbow to paint locks while you keep firing the gun and Hydra; release to launch. Hellfire stays as a loadout variant." + Environment.NewLine + Environment.NewLine;

            string outro = "..and so it left, airframe scorched, directives intact.";
            string outroFailure = "..and so it went down, another airframe lost to the Provinces.";

            //Logbook entry. Framed as a recovered maintenance log so it explains the kit — fixed drum,
            //no landing gear cycle, radar-slaved missiles — without claiming anything about the pilot.
            string lore = "<style=cSub>Recovered airframe log, partial. Timestamps inconsistent.</style>" + Environment.NewLine + Environment.NewLine
             + "CYCLE 0041 — Escort detail, low pass over the wreck line. No contacts. Rotor bed nominal." + Environment.NewLine + Environment.NewLine
             + "CYCLE 0042 — Contact. Contact. Nothing in the threat library matches. Expended the drum, reloaded, expended it again. Radar kept painting new returns faster than the launcher could clear them." + Environment.NewLine + Environment.NewLine
             + "CYCLE 0058 — Requested relief. No response on any band. Requested landing clearance. No response." + Environment.NewLine + Environment.NewLine
             + "CYCLE 0061 — Field expedient: skids removed. Ninety kilos saved and nothing left to land on anyway. Endurance is no longer a fuel question." + Environment.NewLine + Environment.NewLine
             + "CYCLE 0103 — The gun runs. The pods run. The radar still finds the biggest thing on the field and holds onto it. Whatever the mission was, it has been replaced by the part of it that still works." + Environment.NewLine + Environment.NewLine
             + "<style=cSub>Log continues. Entry count exceeds airframe rated service life by a factor of nine.</style>";

            Language.Add(prefix + "NAME", "AH-64");
            Language.Add(prefix + "DESCRIPTION", desc);
            Language.Add(prefix + "SUBTITLE", "The Gunship");
            Language.Add(prefix + "LORE", lore);
            Language.Add(prefix + "OUTRO_FLAVOR", outro);
            Language.Add(prefix + "OUTRO_FAILURE", outroFailure);

            #region Skins
            Language.Add(prefix + "DESERT_SKIN_NAME", "Desert Tan");
            Language.Add(prefix + "ARCTIC_SKIN_NAME", "Arctic");
            Language.Add(prefix + "ARMY_SKIN_NAME", "Army Green");
            Language.Add(prefix + "NIGHT_SKIN_NAME", "Night Stalker");
            #endregion

            #region Passive
            Language.Add(prefix + "PASSIVE_NAME", "Fire Control Radar");
            Language.Add(prefix + "PASSIVE_DESCRIPTION",
                $"The mast radar paints the {Tokens.UtilityText("strongest nearby enemy")}. " +
                $"Deal {Tokens.UtilityText($"{(AH64StaticValues.radarPaintedDamageMult - 1f) * 100f:0}% increased damage")} to the painted target, " +
                $"gain {Tokens.UtilityText($"{AH64StaticValues.radarFacingMoveSpeedMult * 100f:0}% movement speed")} while facing them, " +
                $"and {Tokens.UtilityText($"{AH64StaticValues.radarCloseArmor} armor")} while close.");
            Language.Add(prefix + "RADAR_TARGET_ACQUIRED", "TARGET ACQUIRED");
            Language.Add(prefix + "RADAR_TARGET_ACQUIRED_DESC", "Fire Control Radar");
            #endregion

            #region Primary
            Language.Add(prefix + "PRIMARY_CHAINGUN_NAME", "M230 Chain Gun");
            Language.Add(prefix + "PRIMARY_CHAINGUN_DESCRIPTION", Tokens.agilePrefix + $"Fire the chin turret for {Tokens.DamageValueText(AH64StaticValues.chaingunDamageCoefficient)} per round, with a small HE blast for {Tokens.DamageValueText(AH64StaticValues.chaingunSplashDamageCoefficient)}. {PrimaryRangeClause()} Holds {Tokens.UtilityText($"{AH64StaticValues.chaingunMagazineSize} rounds")}, then reloads over {Tokens.UtilityText($"{AH64StaticValues.chaingunReloadDuration}s")}. Accuracy degrades while held.");
            Language.Add(prefix + "PRIMARY_GATLING_NAME", "XM301 Rotary Cannon");
            Language.Add(prefix + "PRIMARY_GATLING_DESCRIPTION", Tokens.agilePrefix + $"Spin up a six-barrel rotary cannon for {Tokens.DamageValueText(AH64StaticValues.gatlingDamageCoefficient)} per round, with a light blast for {Tokens.DamageValueText(AH64StaticValues.gatlingSplashDamageCoefficient)}. {PrimaryRangeClause()} {Tokens.UtilityText("Rate of fire climbs as the barrels spool up")}. Holds {Tokens.UtilityText($"{AH64StaticValues.gatlingMagazineSize} rounds")}, then reloads over {Tokens.UtilityText($"{AH64StaticValues.gatlingReloadDuration}s")}. Less accurate than the M230.");
            Language.Add(prefix + "PRIMARY_CANNON_NAME", "M789 Heavy Cannon");
            Language.Add(prefix + "PRIMARY_CANNON_DESCRIPTION", Tokens.agilePrefix + $"Fire slow, heavy shells for {Tokens.DamageValueText(AH64StaticValues.cannonDamageCoefficient)} each, with a {Tokens.UtilityText("large")} blast for {Tokens.DamageValueText(AH64StaticValues.cannonSplashDamageCoefficient)}. Holds {Tokens.UtilityText($"{AH64StaticValues.cannonMagazineSize} shells")}, then reloads over {Tokens.UtilityText($"{AH64StaticValues.cannonReloadDuration}s")}. Highly accurate, but every shot counts.");
            #endregion

            #region Secondary
            Language.Add(prefix + "SECONDARY_ROCKETPODS_NAME", "Hydra-70 Pods");
            Language.Add(prefix + "SECONDARY_ROCKETPODS_DESCRIPTION", Tokens.agilePrefix + $"Ripple-fire unguided rockets from the wing pylons for {Tokens.DamageValueText(AH64StaticValues.hydraDamageCoefficient)} each. Damage is {Tokens.UtilityText($"{AH64StaticValues.hydraCloseRangeDamageScale * 100f:0}%")} within {Tokens.UtilityText($"{AH64StaticValues.hydraCloseRange:0}m")} of flight and full at {Tokens.UtilityText($"{AH64StaticValues.hydraFullRange:0}m")}. Holds {Tokens.UtilityText($"{AH64StaticValues.hydraRocketCount} rockets")}, then reloads over {Tokens.UtilityText($"{AH64StaticValues.hydraReloadDuration}s")}. Each {Tokens.UtilityText("Backup Magazine")} adds one rocket and {Tokens.UtilityText($"{AH64StaticValues.hydraReloadPerExtraRocket:0.#}s")} to the reload.");
            #endregion

            #region Utility
            Language.Add(prefix + "UTILITY_DASH_NAME", "Evasive Roll");
            Language.Add(prefix + "UTILITY_DASH_DESCRIPTION",
                $"Barrel-roll {Tokens.UtilityText("forward, left, or right")} on a climbing diagonal from your move input, bracing the plating for {Tokens.UtilityText($"{AH64StaticValues.dashArmorBonus} armor")} for {Tokens.UtilityText($"{AH64StaticValues.dashDuration * AH64StaticValues.dashArmorDurationCoefficient:0.##}s")}. " +
                $"{Tokens.UtilityText("You cannot be hit during the first half of the roll.")} Hold jump afterward for altitude — the roll only hops.");

            Language.Add(prefix + "UTILITY_BACKFLIP_NAME", "Smoke Backflip");
            Language.Add(prefix + "UTILITY_BACKFLIP_DESCRIPTION",
                $"Kick into an aerobatic {Tokens.UtilityText("backflip")} — surge rearward and climb through a pitch loop while dumping smoke. " +
                $"{Tokens.UtilityText("Cloak")} briefly as you fade back into the fight.");
            #endregion

            #region Special
            Language.Add(prefix + "SPECIAL_HELLFIRE_NAME", "AGM-114 Hellfire");
            Language.Add(prefix + "SPECIAL_HELLFIRE_DESCRIPTION", $"Press to launch a missile from a wing rail. Hold Special and aim with your crosshair to guide the latest missile; release to let it coast. Primary and secondary stay available. Detonates on contact for {Tokens.DamageValueText(AH64StaticValues.hellfireDamageCoefficient)}.");

            Language.Add(prefix + "SPECIAL_LONGBOW_NAME", "AGM-114L Longbow");
            Language.Add(prefix + "SPECIAL_LONGBOW_DESCRIPTION",
                $"{Tokens.UtilityText("Hold")} to paint enemies under the reticle, then {Tokens.UtilityText("release to launch")} up to {Tokens.UtilityText($"{AH64StaticValues.longbowMaxLocks} guided missiles")} from alternating inboard rails. " +
                $"Primary and secondary stay available while painting. Later locks hit harder ({Tokens.DamageValueText(AH64StaticValues.LongbowDamageCoefficient(0))}–{Tokens.DamageValueText(AH64StaticValues.LongbowDamageCoefficient(AH64StaticValues.longbowMaxLocks - 1))}), and further missiles stay at the top of that range. " +
                $"Each {Tokens.UtilityText("Lysate Cell")} adds one missile to the rack.");
            #endregion

            #region Achievements
            Language.Add(Tokens.GetAchievementNameToken(AH64MasteryAchievement.identifier), "AH-64: Mastery");
            Language.Add(Tokens.GetAchievementDescriptionToken(AH64MasteryAchievement.identifier), "As the AH-64, beat the game or obliterate on Monsoon.");
            #endregion
        }
    }
}
