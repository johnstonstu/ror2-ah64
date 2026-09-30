using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>
    /// A drum that reloads whole: every restock fills the magazine to its current <c>maxStock</c>.
    /// Used by all three primaries and the Hydra pods.
    ///
    /// <para>The obvious setup, <c>rechargeStock == baseMaxStock</c>, is wrong in two ways that
    /// were verified in RoR2.dll. <c>RestockSteplike</c> adds a fixed <c>rechargeStock</c>, so once
    /// Backup Magazine grows the pods past six a reload no longer fills them. And it passes
    /// <c>rechargeStock</c> to <c>CharacterBody.OnSkillCooldown</c> as the restock count, which
    /// Eclipse Lite multiplies into its barrier: a 30-round drum paid out as 30 cooldowns.</para>
    ///
    /// <para>Here <c>rechargeStock</c> stays at 1, so the game sees one restock per reload, and
    /// this def tops the drum up afterwards. Any other increase between ticks (Bandolier's ammo
    /// pack adds <c>rechargeStock</c>) is treated as a restock too, so it still refills the drum.</para>
    /// </summary>
    public class AH64MagazineSkillDef : SkillDef
    {
        /// <summary>
        /// How many cooldowns a full reload counts as for Eclipse Lite. One restock paid a drum reload
        /// far less barrier than any cooldown-based survivor gets over the same fight. Scaled by how much
        /// of the drum the reload refilled (rounded up, at least one), because every shot restarts the
        /// reload timer: at a flat rate, tapping one round and waiting farmed the full-drum payout.
        /// </summary>
        public int barrierRestocks = 1;

        private class InstanceData : BaseSkillInstanceData
        {
            public int lastStock;
        }

        public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
        {
            return new InstanceData { lastStock = skillSlot.stock };
        }

        public override void OnFixedUpdate(GenericSkill skillSlot, float deltaTime)
        {
            InstanceData data = skillSlot.skillInstanceData as InstanceData;
            int before = skillSlot.stock;
            bool restockedElsewhere = data != null && before > data.lastStock;

            base.OnFixedUpdate(skillSlot, deltaTime);

            bool reloaded = skillSlot.stock > before;
            if (reloaded && barrierRestocks > 1 && skillSlot.characterBody && skillSlot.maxStock > 0)
            {
                //base.OnFixedUpdate already reported one restock; add the rest of this reload's share
                float refilled = Mathf.Clamp01((skillSlot.maxStock - before) / (float)skillSlot.maxStock);
                int extra = Mathf.CeilToInt(barrierRestocks * refilled) - 1;
                if (extra > 0)
                    skillSlot.characterBody.OnSkillCooldown(skillSlot, extra);
            }

            if ((restockedElsewhere || reloaded) && skillSlot.stock < skillSlot.maxStock)
                skillSlot.stock = skillSlot.maxStock;

            if (data != null)
                data.lastStock = skillSlot.stock;
        }
    }
}
