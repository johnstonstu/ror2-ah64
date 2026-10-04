using System;
using RoR2;
using RoR2.Skills;
using RoR2.SurvivorMannequins;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>Uses the mannequin's owner, including remote users; never the local profile.</summary>
    public sealed class AH64LobbyWeaponPreview : MonoBehaviour
    {
        //Railgunner's weapon swap, verified in the game's SoundbanksInfo.xml: a mechanical clunk as the
        //chin gun changes over, so a loadout change is heard as well as seen.
        private const string SwapSound = "Play_railgunner_R_gun_swap";

        private SurvivorMannequinSlotController slot;
        private NetworkUser owner;
        private AH64PrimaryWeaponVisuals visuals;
        private AH64LoadoutSelection attachments;
        private readonly Loadout loadout = new Loadout();
        //Only a change on the same owner's mannequin is a swap; the first primary shown is not.
        private SkillDef shownPrimary;
        private bool hasShownPrimary;

        private void OnEnable()
        {
            visuals = GetComponent<AH64PrimaryWeaponVisuals>();
            attachments = GetComponent<AH64LoadoutSelection>();
            NetworkUser.onLoadoutChangedGlobal += OnLoadoutChanged;
            RefreshOwner();
        }

        private void Start() => RefreshOwner(true);

        private void OnDisable()
        {
            NetworkUser.onLoadoutChangedGlobal -= OnLoadoutChanged;
            owner = null;
            if (attachments) attachments.Select(null, null);
        }

        private void LateUpdate() => RefreshOwner();

        private void RefreshOwner(bool force = false)
        {
            // SlotController.Swap reparents models before exchanging users. Resolve in
            // LateUpdate as well as creation, after the game's synchronous swap completes.
            var currentSlot = GetComponentInParent<SurvivorMannequinSlotController>();
            NetworkUser currentOwner = currentSlot ? currentSlot.networkUser : null;
            if (!force && slot == currentSlot && owner == currentOwner) return;
            if (owner != currentOwner)
                hasShownPrimary = false;
            slot = currentSlot;
            owner = currentOwner;
            ApplyLoadout();
        }

        private void OnLoadoutChanged(NetworkUser user)
        {
            RefreshOwner();
            if (user == owner) ApplyLoadout();
        }

        private void ApplyLoadout()
        {
            if (!visuals) return;
            if (!owner)
            {
                visuals.Select(null);
                if (attachments) attachments.Select(null, null);
                return;
            }
            // Read fresh network state: the slot's private cached loadout is stale during
            // RebuildMannequinInstance, until its next loadoutDirty pass.
            owner.networkLoadout.CopyLoadout(loadout);
            SurvivorDef survivor = owner.GetSurvivorPreference();
            if (!survivor || !survivor.bodyPrefab) return;
            BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(survivor.bodyPrefab);
            if (bodyIndex == BodyIndex.None) return;
            SkillLocator locator = survivor.bodyPrefab.GetComponent<SkillLocator>();
            GenericSkill[] slots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
            if (attachments) attachments.Select(SelectedSkill(bodyIndex, slots, locator ? locator.special : null),
                SelectedSkill(bodyIndex, slots, locator ? locator.utility : null));
            int primaryIndex = locator ? Array.IndexOf(slots, locator.primary) : -1;
            if (primaryIndex < 0) return;
            var family = locator.primary.skillFamily;
            uint variant = loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, primaryIndex);
            if (!family || variant >= family.variants.Length)
            {
                Log.Error("AH64 lobby primary variant is outside its skill family: " + variant);
                visuals.Select(null);
                return;
            }
            SkillDef primary = family.variants[variant].skillDef;
            if (hasShownPrimary && primary != shownPrimary)
                Util.PlaySound(SwapSound, gameObject);
            shownPrimary = primary;
            hasShownPrimary = true;
            visuals.Select(primary);
        }

        private SkillDef SelectedSkill(BodyIndex bodyIndex, GenericSkill[] slots, GenericSkill skill)
        {
            int index = skill ? Array.IndexOf(slots, skill) : -1;
            if (index < 0 || !skill.skillFamily) return null;
            uint variant = loadout.bodyLoadoutManager.GetSkillVariant(bodyIndex, index);
            return variant < skill.skillFamily.variants.Length ? skill.skillFamily.variants[variant].skillDef : null;
        }
    }
}
