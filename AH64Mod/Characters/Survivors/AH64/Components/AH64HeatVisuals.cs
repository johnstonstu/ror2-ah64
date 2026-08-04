using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>
    /// The M230's reload: the housing glows red as the drum drains, then when it runs dry the barrel
    /// coughs one puff of smoke and cools back down as the fresh drum arrives. Pure client-side
    /// presentation driven by the primary skill's synced stock count, so it stays in lockstep with what
    /// the HUD shows.
    ///
    /// <para>Targets the <c>ChinTurret</c> renderer. Falls back to no glow if that renderer is missing
    /// (e.g. still on a placeholder model).</para>
    /// </summary>
    public class AH64HeatVisuals : MonoBehaviour
    {
        //one small puff on entering the reload, not a stream
        private const float smokeScale = 0.18f;
        private static readonly Color glowColor = new Color(1f, 0.08f, 0.03f);
        private const string heatRendererName = "ChinTurret";

        //vanilla Wwise events (verified against the game's SoundbanksInfo.xml) - Railgunner's reload
        //start, and her reload-complete click for "drum ready".
        private const string ventStartSound = "Play_railgunner_m2_reload_basic";
        private const string ventFinishSound = "Play_railgunner_m2_reload_pass";

        private SkillLocator skillLocator;
        private ChildLocator childLocator;
        private Material wristMaterial;
        private bool wasReloading;

        private void Start()
        {
            skillLocator = GetComponent<SkillLocator>();

            ModelLocator modelLocator = GetComponent<ModelLocator>();
            Transform model = modelLocator ? modelLocator.modelTransform : null;
            if (!model) return;

            childLocator = model.GetComponent<ChildLocator>();

            CharacterModel characterModel = model.GetComponent<CharacterModel>();
            if (characterModel == null || characterModel.baseRendererInfos == null) return;

            for (int i = 0; i < characterModel.baseRendererInfos.Length; i++)
            {
                Renderer renderer = characterModel.baseRendererInfos[i].renderer;
                if (renderer && renderer.name == heatRendererName)
                {
                    wristMaterial = new Material(characterModel.baseRendererInfos[i].defaultMaterial);
                    characterModel.baseRendererInfos[i].defaultMaterial = wristMaterial;
                }
            }
        }

        private void Update()
        {
            GenericSkill primary = skillLocator ? skillLocator.primary : null;
            if (primary == null || wristMaterial == null) return;

            //an empty magazine IS the reload - there is nothing else the primary can be waiting on
            bool reloading = primary.stock <= 0;

            float glow;
            if (reloading)
            {
                //hold the housing at full heat for the first half of the reload, then bleed it off, so
                //the barrel is visibly cool again at the exact moment the magazine comes back
                float remainingFraction = primary.finalRechargeInterval > 0f
                    ? Mathf.Clamp01(primary.cooldownRemaining / primary.finalRechargeInterval)
                    : 1f;
                glow = Mathf.Clamp01(remainingFraction * 2f);
            }
            else
            {
                //linear ramp, hot ceiling: the quadratic version was invisible in playtesting
                glow = primary.maxStock > 0 ? 1f - (float)primary.stock / primary.maxStock : 0f;
            }

            wristMaterial.SetColor("_EmColor", glowColor * glow);
            wristMaterial.SetFloat("_EmPower", 4f * glow);

            if (reloading != wasReloading)
            {
                wasReloading = reloading;
                Util.PlaySound(reloading ? ventStartSound : ventFinishSound, gameObject);

                GameObject smoke = AH64Assets.SmokePuffEffect;
                if (reloading && smoke)
                {
                    Transform muzzle = childLocator ? childLocator.FindChild("Muzzle") : null;
                    if (muzzle)
                        EffectManager.SpawnEffect(smoke, new EffectData { origin = muzzle.position, scale = smokeScale }, false);
                }
            }
        }

        private void OnDestroy()
        {
            if (wristMaterial) Destroy(wristMaterial);
        }
    }
}
