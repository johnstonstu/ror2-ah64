using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Owns everything visual and audible about the XM301 alternate primary: the barrel
    /// swap, the spool, and the three-part gun audio.
    ///
    /// <para>All three live together on purpose. The spool loop must never outlive the
    /// gun — on death, on a skill swap mid-burst, on the body despawning — and the only
    /// way to guarantee that is for whatever owns the loop to also own the state that
    /// starts it, with an <see cref="OnDisable"/> that stops it unconditionally.</para>
    ///
    /// <para>The spin axis is <b>derived at runtime, not hardcoded</b>. It has been wrong
    /// twice: once as a genuine bug, and once when the FBX orientation fix moved it and a
    /// hand re-measurement got it backwards, which sent the whole cluster orbiting a
    /// 0.59-unit circle instead of rolling — in game that reads as the entire turret
    /// spinning. Deriving it from the aircraft's own forward vector means an orientation
    /// change can never desynchronise it again, and it costs one transform call at Start.
    /// </para>
    ///
    /// <para>The barrel swap toggles <em>renderers</em>, never <c>SetActive</c>.
    /// <c>ChinGatling</c> is a child of <c>ChinBarrel</c>, so deactivating the M230
    /// GameObject would take the gatling — and the shared <c>Muzzle</c> anchor — down
    /// with it.</para>
    /// </summary>
    public class AH64GatlingSpin : MonoBehaviour
    {
        //Fast enough to blur at third-person distance, slow enough that individual barrels
        //still register during spool-up. Tune from playtest, not from the real GAU figure.
        private const float MaxRpm = 640f;

        //Asymmetric on purpose: it winds up harder than it coasts down, which is how a
        //driven barrel group behaves and reads as motorised rather than free-spinning.
        private const float SpoolUpRpmPerSecond = 1500f;
        private const float SpoolDownRpmPerSecond = 520f;

        //Vanilla Clay Templar minigun kit, verified against the game's SoundbanksInfo.xml.
        //Vanilla events dodge the six-event custom soundbank limit entirely. The loop is the
        //only one of the three with a paired Stop, which is why the loop is tracked by flag.
        private const string SpoolUpSound = "Play_clayBruiser_attack1_windUp";
        private const string FireLoopSound = "Play_clayBruiser_attack1_shootLoop";
        private const string FireLoopStopSound = "Stop_clayBruiser_attack1_shootLoop";
        private const string SpoolDownSound = "Play_clayBruiser_attack1_windDown";

        //Loop comes in once the barrels are actually moving, so the wind-up is audible
        //underneath it first. Below this the gun is spinning up, not firing.
        private const float LoopEnterFraction = 0.45f;

        private Transform gatling;
        private Vector3 spinAxis = Vector3.up;
        private Renderer gatlingRenderer;
        private Renderer barrelRenderer;

        private float currentRpm;
        private bool firing;
        private bool loopPlaying;
        private bool spoolUpPlayed;
        private bool gatlingEquipped;
        private bool appliedOnce;
        private GameObject spoolEmitter;
        private bool warnedSpoolAudio;

        /// <summary>Set by <see cref="SkillStates.FireGatling"/> on every round fired.</summary>
        public void NotifyFiring()
        {
            firing = true;
            lastFireTime = Time.time;
        }

        private float lastFireTime = -99f;

        //A round every ~0.055s at full spool; this has to outlast the gap between rounds or
        //the gun would spool down between shots, but stay short enough that releasing the
        //trigger winds down promptly.
        private const float FiringHoldTime = 0.18f;

        /// <summary>
        /// Normalised spool state, 0 to 1. <see cref="SkillStates.FireGatling"/> reads this
        /// to ramp its rate of fire, so the gun genuinely accelerates rather than just
        /// looking like it does.
        /// </summary>
        public float SpoolFraction => MaxRpm > 0f ? currentRpm / MaxRpm : 0f;

        private void Start()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
                return;

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
                return;

            gatling = childLocator.FindChild("ChinGatling");
            if (gatling)
            {
                gatlingRenderer = gatling.GetComponent<Renderer>();

                //The bore points along the aircraft's nose. Expressing that in the
                //cluster's local space gives the roll axis regardless of whatever
                //rotation the FBX pipeline left on the transform.
                Vector3 axis = gatling.InverseTransformDirection(modelLocator.modelTransform.forward);
                spinAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
            }

            Transform barrel = childLocator.FindChild("ChinBarrel");
            if (barrel)
                barrelRenderer = barrel.GetComponent<Renderer>();

            RefreshEquippedWeapon();
        }

        /// <summary>
        /// Show whichever barrel matches the equipped primary. Polled rather than hooked:
        /// the loadout can change at respawn, from a Command shrine, or between stages, and
        /// there is no single reliable event for all of those.
        ///
        /// <para>Visibility is driven through <see cref="Renderer.forceRenderingOff"/>, NOT
        /// <c>Renderer.enabled</c>. Both barrels are in <c>customRendererInfos</c>, and
        /// <c>CharacterModel.UpdateMaterials</c> walks every entry in
        /// <c>baseRendererInfos</c> and sets <c>renderer.enabled = true</c> whenever
        /// materials go dirty — verified by decompiling RoR2.dll. Writing to
        /// <c>enabled</c> here is therefore silently undone a frame later, which is what
        /// left the M230 visible and tracking alongside the gatling in playtest.
        /// <c>forceRenderingOff</c> is a Unity-level flag CharacterModel never touches.</para>
        /// </summary>
        private void RefreshEquippedWeapon()
        {
            SkillLocator skillLocator = GetComponent<SkillLocator>();
            bool equipped =
                skillLocator &&
                skillLocator.primary &&
                skillLocator.primary.skillDef == AH64Assets.gatlingSkillDef;

            //Always apply once, even if the answer matches the field's default — otherwise
            //spawning with the M230 equipped would never hide the gatling, and CharacterModel
            //will have enabled its renderer for us.
            if (appliedOnce && equipped == gatlingEquipped)
                return;

            appliedOnce = true;
            gatlingEquipped = equipped;

            if (gatlingRenderer)
                gatlingRenderer.forceRenderingOff = !equipped;
            if (barrelRenderer)
                barrelRenderer.forceRenderingOff = equipped;

            //Swapping away mid-spool must not leave the loop running on a hidden gun.
            if (!equipped)
            {
                StopLoop();
                if (spoolEmitter)
                    AkSoundEngine.StopAll(spoolEmitter);
                currentRpm = 0f;
                firing = false;
                spoolUpPlayed = false;
            }
        }

        private void Update()
        {
            RefreshEquippedWeapon();

            if (!gatlingEquipped || !gatling)
                return;

            float dt = Time.deltaTime;

            //The skill state pulses NotifyFiring per round; hold it briefly so the gap
            //between rounds does not read as "released".
            if (firing && Time.time - lastFireTime > FiringHoldTime)
                firing = false;

            if (firing)
            {
                if (!spoolUpPlayed)
                {
                    PlaySpoolSound(SpoolUpSound);
                    spoolUpPlayed = true;
                }
                currentRpm = Mathf.Min(MaxRpm, currentRpm + SpoolUpRpmPerSecond * dt);
            }
            else
            {
                if (spoolUpPlayed && currentRpm > 0f)
                {
                    //Wind-down is played once, at release, not once per frame of decay.
                    PlaySpoolSound(SpoolDownSound);
                    spoolUpPlayed = false;
                }
                currentRpm = Mathf.Max(0f, currentRpm - SpoolDownRpmPerSecond * dt);
            }

            bool wantLoop = firing && SpoolFraction >= LoopEnterFraction;
            if (wantLoop && !loopPlaying)
            {
                Util.PlaySound(FireLoopSound, gameObject);
                loopPlaying = true;
            }
            else if (!wantLoop && loopPlaying)
            {
                StopLoop();
            }

            if (currentRpm <= 0f)
                return;

            //rpm -> degrees/second is x6. spinAxis is derived in Start — see class remarks.
            gatling.Rotate(spinAxis, currentRpm * 6f * dt, Space.Self);
        }

        private void StopLoop()
        {
            if (!loopPlaying)
                return;

            Util.PlaySound(FireLoopStopSound, gameObject);
            loopPlaying = false;
        }

        private void PlaySpoolSound(string sound)
        {
            if (!spoolEmitter)
            {
                // Output-bus gain affects every sound on an emitter. Keep transitions off
                // the body emitter so shots, impacts and the firing loop retain their mix.
                spoolEmitter = new GameObject("AH64GatlingSpoolAudio");
                spoolEmitter.transform.SetParent(transform, false);
                spoolEmitter.AddComponent<AkGameObj>();
            }
            AKRESULT result = AkSoundEngine.SetGameObjectOutputBusVolume(
                AkSoundEngine.GetAkGameObjectID(spoolEmitter), ulong.MaxValue,
                Mathf.Clamp01(AH64PlaytestConfig.GatlingSpoolVolume));
            if (result != AKRESULT.AK_Success)
            {
                if (!warnedSpoolAudio)
                {
                    warnedSpoolAudio = true;
                    Log.Warning($"Cannot set gatling spool gain: {result}; skipping transition sound.");
                }
                return;
            }
            if (Util.PlaySound(sound, spoolEmitter) == 0 && !warnedSpoolAudio)
            {
                warnedSpoolAudio = true;
                Log.Warning($"Cannot post gatling transition sound {sound}.");
            }
        }

        /// <summary>
        /// Unconditional stop. Death, despawn and stage transitions all route through here,
        /// and a Wwise loop that survives its emitter keeps playing for the rest of the run.
        /// </summary>
        private void OnDisable()
        {
            StopLoop();
            if (spoolEmitter)
                AkSoundEngine.StopAll(spoolEmitter);
            currentRpm = 0f;
            firing = false;
            spoolUpPlayed = false;
        }

        private void OnDestroy()
        {
            if (spoolEmitter)
                Destroy(spoolEmitter);
        }
    }
}
