using System;
using RoR2;
using RoR2.SurvivorMannequins;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>Uses the mannequin's owner, including remote users; never the local profile.</summary>
    public sealed class AH64LobbyWeaponPreview : MonoBehaviour
    {
        private SurvivorMannequinSlotController slot;
        private NetworkUser owner;
        private AH64PrimaryWeaponVisuals visuals;
        private readonly Loadout loadout = new Loadout();

        private void OnEnable()
        {
            visuals = GetComponent<AH64PrimaryWeaponVisuals>();
            NetworkUser.onLoadoutChangedGlobal += OnLoadoutChanged;
            RefreshOwner();
        }

        private void Start() => RefreshOwner(true);

        private void OnDisable()
        {
            NetworkUser.onLoadoutChangedGlobal -= OnLoadoutChanged;
            owner = null;
        }

        private void LateUpdate() => RefreshOwner();

        private void RefreshOwner(bool force = false)
        {
            // SlotController.Swap reparents models before exchanging users. Resolve in
            // LateUpdate as well as creation, after the game's synchronous swap completes.
            var currentSlot = GetComponentInParent<SurvivorMannequinSlotController>();
            NetworkUser currentOwner = currentSlot ? currentSlot.networkUser : null;
            if (!force && slot == currentSlot && owner == currentOwner) return;
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
            visuals.Select(family.variants[variant].skillDef);
        }
    }
}
