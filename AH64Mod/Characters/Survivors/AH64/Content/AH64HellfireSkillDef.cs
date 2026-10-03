using AH64.Survivors.Components;
using RoR2;
using RoR2.Skills;

namespace AH64.Survivors
{
    // Designation observes native input independently. Block only a new activation while
    // its existing live lead (or acknowledgement) owns that press, before stock is spent.
    public sealed class AH64HellfireSkillDef : SkillDef
    {
        private static bool HasGuidanceTarget(GenericSkill slot)
        {
            AH64HellfireOwner owner = slot && slot.characterBody
                ? slot.characterBody.GetComponent<AH64HellfireOwner>() : null;
            return owner && owner.HasLiveOrPendingLead;
        }

        public override bool CanExecute(GenericSkill skillSlot)
        {
            return !HasGuidanceTarget(skillSlot) && base.CanExecute(skillSlot);
        }

        public override void OnExecute(GenericSkill skillSlot)
        {
            if (!HasGuidanceTarget(skillSlot)) base.OnExecute(skillSlot);
        }
    }
}
