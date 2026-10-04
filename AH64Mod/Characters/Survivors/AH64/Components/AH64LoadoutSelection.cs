using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // One skill-to-attachment mapping for both the live body and its owning mannequin.
    // Selection reads replicated loadouts; it never changes skills or inventory.
    public sealed class AH64LoadoutSelection : MonoBehaviour
    {
        public AH64LoadoutAttachments Attachments;
        public bool IsDisplay;
        private SkillLocator skills;

        private void OnEnable() { RefreshBody(); }
        private void LateUpdate() { RefreshBody(); }

        private void RefreshBody()
        {
            if (IsDisplay) return;
            if (!skills) skills = GetComponentInParent<SkillLocator>();
            if (skills) Select(skills.special ? skills.special.skillDef : null,
                skills.utility ? skills.utility.skillDef : null);
        }

        public void Select(SkillDef special, SkillDef utility)
        {
            if (!Attachments) return;
            Attachments.Select(SpecialIndex(special), UtilityIndex(utility));
        }

        private static int SpecialIndex(SkillDef skill)
        {
            if (!skill) return -1;
            switch (skill.skillName)
            {
                case "AH64Longbow": return 0;
                case "AH64Hellfire": return 1;
                case "AH64BombingRun": return 2;
                default: return -1;
            }
        }

        private static int UtilityIndex(SkillDef skill)
        {
            if (!skill) return -1;
            switch (skill.skillName)
            {
                case "AH64EvasiveJink": return 0;
                case "AH64SmokeBackflip": return 1;
                case "AH64BrakingTurn": return 2;
                default: return -1;
            }
        }
    }
}
