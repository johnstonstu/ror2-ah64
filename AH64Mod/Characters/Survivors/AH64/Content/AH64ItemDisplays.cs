using AH64.Modules;
using AH64.Modules.Characters;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>
    /// Curated item displays for the airframe. Most items intentionally have no rule — without one they
    /// simply don't attach. An early dump that parked every item at Chest+(2,2,2) piled a cloud of
    /// props off the starboard side; this list is hand-placed instead.
    ///
    /// Anchors (Blender empties wired into ChildLocator): Chest, Head, NoseTip, WingL, WingR, TailTip.
    /// Positions are local to those empties; keep offsets small so props sit on the hull.
    /// </summary>
    public class AH64ItemDisplays : ItemDisplaysBase
    {
        protected override void SetItemDisplayRules(List<ItemDisplayRuleSet.KeyAssetRuleGroup> itemDisplayRules)
        {
            //--- canopy / cockpit (Head) ---
            Add(itemDisplayRules, "CritGlasses", "DisplayGlasses",
                "Head", new Vector3(0f, 0.05f, 0.12f), new Vector3(0f, 0f, 0f), S(0.28f));
            Add(itemDisplayRules, "CritGlassesVoid", "DisplayGlassesVoid",
                "Head", new Vector3(0f, 0.05f, 0.12f), new Vector3(0f, 0f, 0f), S(0.28f));
            Add(itemDisplayRules, "AttackSpeedOnCrit", "DisplayWolfPelt",
                "Head", new Vector3(0f, 0.18f, -0.05f), new Vector3(-20f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "Crowbar", "DisplayCrowbar",
                "Head", new Vector3(0.2f, 0f, 0.05f), new Vector3(0f, 90f, -20f), S(0.35f));

            //--- nose art (NoseTip) ---
            Add(itemDisplayRules, "AlienHead", "DisplayAlienHead",
                "NoseTip", new Vector3(0f, 0f, 0.05f), new Vector3(-90f, 0f, 0f), S(0.55f));
            Add(itemDisplayRules, "BleedOnHit", "DisplayTriTip",
                "NoseTip", new Vector3(0f, -0.08f, 0f), new Vector3(0f, 0f, 0f), S(0.4f));
            Add(itemDisplayRules, "BleedOnHitVoid", "DisplayTriTipVoid",
                "NoseTip", new Vector3(0f, -0.08f, 0f), new Vector3(0f, 0f, 0f), S(0.4f));
            Add(itemDisplayRules, "NearbyDamageBonus", "DisplayDiamond",
                "NoseTip", new Vector3(0f, 0.06f, 0.02f), new Vector3(0f, 0f, 0f), S(0.2f));

            //--- fuselage (Chest) ---
            Add(itemDisplayRules, "Bear", "DisplayBear",
                "Chest", new Vector3(-0.45f, 0.05f, 0.35f), new Vector3(0f, 90f, 0f), S(0.22f));
            Add(itemDisplayRules, "BearVoid", "DisplayBearVoid",
                "Chest", new Vector3(0.45f, 0.05f, 0.35f), new Vector3(0f, -90f, 0f), S(0.22f));
            Add(itemDisplayRules, "ArmorPlate", "DisplayRepulsionArmorPlate",
                "Chest", new Vector3(-0.55f, 0f, 0f), new Vector3(0f, 90f, 0f), S(0.4f));
            Add(itemDisplayRules, "BarrierOnOverHeal", "DisplayAegis",
                "Chest", new Vector3(0.55f, 0.05f, 0.1f), new Vector3(0f, -90f, 0f), S(0.28f));
            Add(itemDisplayRules, "BarrierOnKill", "DisplayBrooch",
                "Chest", new Vector3(0f, 0.15f, 0.55f), new Vector3(-90f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "PersonalShield", "DisplayShieldGenerator",
                "Chest", new Vector3(0f, -0.15f, 0.1f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "Infusion", "DisplayInfusion",
                "Chest", new Vector3(0.35f, 0.1f, -0.2f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "Medkit", "DisplayMedkit",
                "Chest", new Vector3(-0.35f, 0.05f, -0.25f), new Vector3(0f, 90f, 0f), S(0.35f));
            Add(itemDisplayRules, "Bandolier", "DisplayBandolier",
                "Chest", new Vector3(0f, 0.05f, 0f), new Vector3(-90f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "FlatHealth", "DisplaySteakCurved",
                "Chest", new Vector3(0.4f, -0.05f, 0.25f), new Vector3(0f, 0f, 0f), S(0.18f));
            Add(itemDisplayRules, "Knurl", "DisplayKnurl",
                "Chest", new Vector3(-0.25f, 0.2f, -0.1f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "Pearl", "DisplayPearl",
                "Chest", new Vector3(0.2f, 0.22f, 0.2f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "ShinyPearl", "DisplayShinyPearl",
                "Chest", new Vector3(-0.15f, 0.22f, 0.25f), new Vector3(0f, 0f, 0f), S(0.12f));

            //--- wing pylons: ordnance / weapons ---
            Add(itemDisplayRules, "Behemoth", "DisplayBehemoth",
                "WingR", new Vector3(0f, -0.12f, 0f), new Vector3(0f, 0f, 0f), S(0.18f));
            Add(itemDisplayRules, "Missile", "DisplayMissileLauncher",
                "WingL", new Vector3(0f, -0.1f, 0.05f), new Vector3(0f, 0f, 90f), S(0.12f));
            Add(itemDisplayRules, "MissileVoid", "DisplayMissileLauncherVoid",
                "WingL", new Vector3(0f, -0.1f, -0.1f), new Vector3(0f, 0f, 90f), S(0.12f));
            Add(itemDisplayRules, "MoreMissile", "DisplayICBM",
                "WingR", new Vector3(0.05f, -0.08f, 0.1f), new Vector3(0f, 0f, 0f), S(0.15f));
            Add(itemDisplayRules, "BossDamageBonus", "DisplayAPRound",
                "WingR", new Vector3(0f, -0.05f, -0.15f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "Firework", "DisplayFirework",
                "WingL", new Vector3(0f, -0.05f, 0.15f), new Vector3(0f, 0f, 0f), S(0.25f));
            Add(itemDisplayRules, "ExplodeOnDeath", "DisplayWilloWisp",
                "WingR", new Vector3(0.1f, -0.15f, 0f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "ExplodeOnDeathVoid", "DisplayWillowWispVoid",
                "WingR", new Vector3(0.1f, -0.15f, -0.12f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "ChainLightning", "DisplayUkulele",
                "WingL", new Vector3(0f, 0.05f, 0f), new Vector3(0f, 90f, 40f), S(0.35f));
            Add(itemDisplayRules, "FireballsOnHit", "DisplayFireballsOnHit",
                "WingR", new Vector3(0f, -0.18f, 0.05f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "LightningStrikeOnHit", "DisplayChargedPerforator",
                "WingL", new Vector3(0f, -0.18f, -0.05f), new Vector3(0f, 0f, 0f), S(0.18f));
            Add(itemDisplayRules, "PrimarySkillShuriken", "DisplayShuriken",
                "WingL", new Vector3(0.05f, 0.08f, 0f), new Vector3(0f, 0f, 0f), S(0.3f));
            Add(itemDisplayRules, "LaserTurbine", "DisplayLaserTurbine",
                "WingR", new Vector3(0f, 0.1f, 0f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "GoldGat", "DisplayGoldGat",
                "WingR", new Vector3(0.15f, 0.05f, 0.1f), new Vector3(0f, -90f, 40f), S(0.12f));

            //--- tail boom charms ---
            Add(itemDisplayRules, "Clover", "DisplayClover",
                "TailTip", new Vector3(0f, -0.1f, 0f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "CloverVoid", "DisplayCloverVoid",
                "TailTip", new Vector3(0.08f, -0.1f, 0f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "ExtraLife", "DisplayHippo",
                "TailTip", new Vector3(0f, -0.2f, 0.05f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "ExtraLifeVoid", "DisplayHippoVoid",
                "TailTip", new Vector3(0f, -0.2f, -0.05f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "Feather", "DisplayFeather",
                "TailTip", new Vector3(-0.1f, 0.05f, 0f), new Vector3(0f, 0f, 20f), S(0.2f));
            Add(itemDisplayRules, "Icicle", "DisplayFrostRelic",
                "TailTip", new Vector3(0f, 0.12f, 0f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "BleedOnHitAndExplode", "DisplayBleedOnHitAndExplode",
                "TailTip", new Vector3(0.12f, -0.05f, 0f), new Vector3(0f, 0f, 0f), S(0.12f));

            //--- equipment that reads as airframe kit ---
            Add(itemDisplayRules, "CommandMissile", "DisplayMissileRack",
                "WingL", new Vector3(0f, -0.22f, 0f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "Blackhole", "DisplayGravCube",
                "Chest", new Vector3(0f, -0.35f, -0.15f), new Vector3(0f, 0f, 0f), S(0.35f));
            Add(itemDisplayRules, "BFG", "DisplayBFG",
                "WingR", new Vector3(0.1f, -0.25f, 0f), new Vector3(0f, 0f, 0f), S(0.3f));
            Add(itemDisplayRules, "QuestVolatileBattery", "DisplayBatteryArray",
                "Chest", new Vector3(0f, 0.05f, -0.55f), new Vector3(0f, 0f, 0f), S(0.25f));
            Add(itemDisplayRules, "GainAmmo", "DisplayRecycler",
                "Chest", new Vector3(-0.5f, -0.1f, -0.2f), new Vector3(0f, 90f, 0f), S(0.12f));
            Add(itemDisplayRules, "Cleanse", "DisplayWaterPack",
                "Chest", new Vector3(0.45f, -0.05f, -0.35f), new Vector3(0f, 0f, 0f), S(0.2f));
            Add(itemDisplayRules, "TeamWarCry", "DisplayTeamWarCry",
                "Head", new Vector3(0f, 0.25f, -0.15f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "DeathMark", "DisplayDeathMark",
                "NoseTip", new Vector3(0.12f, 0f, 0f), new Vector3(0f, 0f, 0f), S(0.08f));
            Add(itemDisplayRules, "LifestealOnHit", "DisplayLifestealOnHit",
                "Chest", new Vector3(-0.4f, 0.15f, 0.15f), new Vector3(0f, 0f, 0f), S(0.12f));
            Add(itemDisplayRules, "Jetpack", "DisplayBugWings",
                "Chest", new Vector3(0f, 0.25f, -0.3f), new Vector3(0f, 0f, 0f), S(0.15f));
        }

        private static Vector3 S(float s) => new Vector3(s, s, s);

        private static void Add(
            List<ItemDisplayRuleSet.KeyAssetRuleGroup> rules,
            string keyAssetName,
            string displayPrefabName,
            string childName,
            Vector3 localPos,
            Vector3 localAngles,
            Vector3 localScale)
        {
            rules.Add(ItemDisplays.CreateDisplayRuleGroupWithRules(keyAssetName,
                ItemDisplays.CreateDisplayRule(displayPrefabName, childName, localPos, localAngles, localScale)));
        }
    }
}
