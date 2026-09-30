using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>
    /// The primary's reload cue: when the magazine runs dry the barrel coughs one puff of smoke and
    /// plays a vent sound, then a ready click when the fresh drum arrives. Pure client-side presentation
    /// driven by the primary skill's synced stock count, so it stays in lockstep with what the HUD shows.
    ///
    /// <para>No emissive heat glow: a red-lit housing under the canopy read as a rendering fault in
    /// playtesting rather than as barrel heat.</para>
    /// </summary>
    public class AH64HeatVisuals : MonoBehaviour
    {
        //one small puff on entering the reload, not a stream
        private const float smokeScale = 0.18f;

        //vanilla Wwise events (verified against the game's SoundbanksInfo.xml) - Railgunner's reload
        //start, and her reload-complete click for "drum ready".
        private const string ventStartSound = "Play_railgunner_m2_reload_basic";
        private const string ventFinishSound = "Play_railgunner_m2_reload_pass";

        private SkillLocator skillLocator;
        private ChildLocator childLocator;
        private bool wasReloading;

        private void Start()
        {
            skillLocator = GetComponent<SkillLocator>();

            ModelLocator modelLocator = GetComponent<ModelLocator>();
            Transform model = modelLocator ? modelLocator.modelTransform : null;
            if (model)
                childLocator = model.GetComponent<ChildLocator>();
        }

        private void Update()
        {
            GenericSkill primary = skillLocator ? skillLocator.primary : null;
            if (primary == null) return;

            //an empty magazine IS the reload - there is nothing else the primary can be waiting on
            bool reloading = primary.stock <= 0;
            if (reloading == wasReloading) return;

            wasReloading = reloading;
            Util.PlaySound(reloading ? ventStartSound : ventFinishSound, gameObject);
            //The Hydra exhaust smoke ring, blown out along the barrel. SmokePuffEffect was used here
            //before, but that resolves to OmniExplosionVFX (see its note), so the "puff" was a flash.
            GameObject smoke = AH64Assets.hydraMuzzleFlashEffect;
            if (reloading && smoke)
            {
                Transform muzzle = childLocator ? childLocator.FindChild("Muzzle") : null;
                if (muzzle)
                    EffectManager.SpawnEffect(smoke, new EffectData
                    {
                        origin = muzzle.position,
                        rotation = muzzle.rotation,
                        scale = smokeScale,
                    }, false);
            }
        }
    }
}
