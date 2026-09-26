using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Owns rotary spool, rotation and firing audio. PrimaryWeaponVisuals alone owns
    /// visibility; this component is attached only to gameplay bodies.
    /// The spin axis is derived from the model forward vector after FBX correction.
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
        //The loop is the only one of the three with a paired Stop, so it is tracked by flag.
        private const string SpoolUpSound = "Play_clayBruiser_attack1_windUp";
        private const string FireLoopSound = "Play_clayBruiser_attack1_shootLoop";
        private const string FireLoopStopSound = "Stop_clayBruiser_attack1_shootLoop";
        private const string SpoolDownSound = "Play_clayBruiser_attack1_windDown";

        //Loop comes in once the barrels are actually moving, so the wind-up is audible
        //underneath it first. Below this the gun is spinning up, not firing.
        private const float LoopEnterFraction = 0.45f;

        private Transform gatling;
        private Vector3 spinAxis = Vector3.up;

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

                //The bore points along the aircraft's nose. Expressing that in the
                //cluster's local space gives the roll axis regardless of whatever
                //rotation the FBX pipeline left on the transform.
                Vector3 axis = gatling.InverseTransformDirection(modelLocator.modelTransform.forward);
                spinAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.up;
            }

            RefreshEquippedWeapon();
        }

        // Poll skill identity so swaps/respawns stop a hidden gun's audio and spool.
        private void RefreshEquippedWeapon()
        {
            SkillLocator skillLocator = GetComponent<SkillLocator>();
            bool equipped =
                skillLocator &&
                skillLocator.primary &&
                skillLocator.primary.skillDef == AH64Assets.gatlingSkillDef;

            //Apply once on spawn, then reset audio/spool only on equipment changes.
            if (appliedOnce && equipped == gatlingEquipped)
                return;

            appliedOnce = true;
            gatlingEquipped = equipped;

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
