using System;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>Silent, sole owner of primary mesh visibility for bodies and mannequins.</summary>
    public sealed class AH64PrimaryWeaponVisuals : MonoBehaviour
    {
        private Renderer m230;
        private Renderer cluster;
        private Renderer housing;
        private Renderer cannon;
        private SkillLocator bodySkills;
        private ModelSkinController skins;
        private SkillDef selected;
        private bool initialized;

        private void Awake()
        {
            ChildLocator locator = GetComponent<ChildLocator>();
            m230 = RequireRenderer(locator, "ChinBarrel");
            cluster = RequireRenderer(locator, "ChinGatling");
            housing = RequireRenderer(locator, "ChinGatlingHousing");
            cannon = RequireRenderer(locator, "ChinCannon");
            // Authored alternatives start disabled. Thereafter CharacterModel owns enabled
            // (including cloak/death); weapon selection owns forceRenderingOff exclusively.
            foreach (Renderer renderer in new[] { m230, cluster, housing, cannon })
                renderer.enabled = true;
            initialized = true;
            ApplyVisibility();
        }

        private static Renderer RequireRenderer(ChildLocator locator, string name)
        {
            Transform child = locator ? locator.FindChild(name) : null;
            Renderer renderer = child ? child.GetComponent<Renderer>() : null;
            if (renderer) return renderer;
            Log.Error("AH64 primary presentation is missing renderer " + name);
            throw new InvalidOperationException("AH64 primary presentation requires " + name);
        }

        private void OnEnable()
        {
            skins = GetComponent<ModelSkinController>();
            if (skins) skins.onSkinApplied += OnSkinApplied;
            bodySkills = GetComponentInParent<SkillLocator>();
            RefreshBody();
        }

        private void OnDisable()
        {
            if (skins) skins.onSkinApplied -= OnSkinApplied;
        }

        private void OnSkinApplied(int index) => ApplyVisibility();

        private void LateUpdate()
        {
            RefreshBody();
            // Skin/material updates can finish asynchronously. Reassert only our flag,
            // never reactivate GameObjects or override CharacterModel's cloak state.
            ApplyVisibility();
        }

        private void RefreshBody()
        {
            if (!bodySkills) bodySkills = GetComponentInParent<SkillLocator>();
            if (bodySkills && bodySkills.primary) Select(bodySkills.primary.skillDef);
        }

        public void Select(SkillDef skill)
        {
            selected = skill;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (!initialized) return;
            bool isGatling = selected && selected == AH64Assets.gatlingSkillDef;
            bool isCannon = selected && selected == AH64Assets.cannonSkillDef;
            m230.forceRenderingOff = isGatling || isCannon;
            cluster.forceRenderingOff = !isGatling;
            housing.forceRenderingOff = !isGatling;
            cannon.forceRenderingOff = !isCannon;
        }
    }
}
