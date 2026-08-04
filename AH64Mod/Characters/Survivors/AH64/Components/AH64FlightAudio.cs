using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Presence and movement SFX for the airframe.
    ///
    /// <para>The continuous bed is the approved CC0 grounded rotor clip from the ah64 asset bundle,
    /// played quietly through a 3D AudioSource. Two more CC0 clips (same qubodup pack, see
    /// <c>AH64Audio/LICENSE_SOURCE.txt</c>) layer on top and fade with movement: a fuller engine+rotor
    /// loop that rises with horizontal speed, and a lighter loop that rises with collective/climb intent.
    /// Collective retains a short vanilla lift cue.</para>
    /// </summary>
    public class AH64FlightAudio : MonoBehaviour
    {
        // Captain's drone reposition chirp: short lift cue when collective bites. Base game.
        private const string collectiveChirp = "Play_captain_drone_quick_move";

        private InputBankTest inputBank;
        private CharacterMotor motor;
        private CharacterBody body;
        private AH64HoverController hoverController;

        private AudioSource rotorSource;
        private AudioSource inFlightSource;
        private AudioSource climbSource;
        private float inFlightVolumeVelocity;
        private float climbVolumeVelocity;
        private bool collectiveWasHeld;

        private void Start()
        {
            inputBank = GetComponent<InputBankTest>();
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            hoverController = GetComponent<AH64HoverController>();

            rotorSource = CreateLoopSource(AH64Assets.rotorHoverLoop, "Approved grounded rotor hover clip");
            inFlightSource = CreateLoopSource(AH64Assets.rotorInFlightLoop, "Rotor in-flight layer clip");
            climbSource = CreateLoopSource(AH64Assets.rotorClimbLoop, "Rotor climb layer clip");

            if (rotorSource)
                rotorSource.volume = AH64PlaytestConfig.RotorHoverVolume;
        }

        private AudioSource CreateLoopSource(AudioClip clip, string missingClipLabel)
        {
            if (!clip)
            {
                Log.Warning($"{missingClipLabel} was not found in the ah64 bundle; that layer will stay silent.");
                return null;
            }

            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = AH64StaticValues.rotorHoverMinDistance;
            source.maxDistance = AH64StaticValues.rotorHoverMaxDistance;
            source.dopplerLevel = AH64StaticValues.rotorHoverDoppler;
            source.volume = 0f;
            source.time = Random.Range(0f, clip.length);
            source.Play();
            return source;
        }

        /// <summary>
        /// Base pitch per layer. The grounded bed sits slightly flat so it reads as a heavy
        /// disc rather than a fan; the climb layer sits slightly sharp because a helicopter
        /// pulling collective audibly bites. The in-flight layer is driven per-frame in
        /// <see cref="UpdateMovementLayers"/> instead, since its whole job is to change.
        /// </summary>
        private void ApplyBasePitches()
        {
            if (rotorSource)
                rotorSource.pitch = AH64StaticValues.rotorHoverPitch;
            if (climbSource)
                climbSource.pitch = AH64StaticValues.rotorClimbPitch;
        }

        private void Update()
        {
            if (rotorSource)
                rotorSource.volume = AH64PlaytestConfig.RotorHoverVolume;

            ApplyBasePitches();
            UpdateMovementLayers();

            bool jumpHeld = inputBank && inputBank.jump.down;
            if (jumpHeld && !collectiveWasHeld)
                Util.PlaySound(collectiveChirp, gameObject);
            collectiveWasHeld = jumpHeld;
        }

        /// <summary>
        /// Forward-speed layer targets horizontal velocity over the current move speed; climb layer
        /// targets <see cref="AH64HoverController.AscentPitchWeight"/> (already 0…1-ish while climbing,
        /// negative while settling — clamped here since settle shouldn't fade the layer back up).
        /// Both ease with SmoothDamp so the mix doesn't snap on every input change.
        /// </summary>
        private void UpdateMovementLayers()
        {
            float deltaTime = Time.deltaTime;

            if (inFlightSource)
            {
                float speedFactor = 0f;
                if (motor && body && body.moveSpeed > 0.01f)
                {
                    Vector3 horizontalVelocity = motor.velocity;
                    horizontalVelocity.y = 0f;
                    speedFactor = Mathf.Clamp01(horizontalVelocity.magnitude / body.moveSpeed);
                }

                float targetVolume = speedFactor * AH64PlaytestConfig.RotorInFlightVolume;
                inFlightSource.volume = Mathf.SmoothDamp(
                    inFlightSource.volume, targetVolume, ref inFlightVolumeVelocity,
                    AH64StaticValues.rotorLayerFadeTime, Mathf.Infinity, deltaTime);

                //P1 fix (AUDIO_INVESTIGATION.md): this used to sweep 0.93->1.07 with speed, which
                //let the chop rate wander independently of the bed and the visual rotor disc — two
                //near-identical chops a fraction of a Hz apart beat against each other. Fixed pitch
                //locks the chop rate instead of tying it to effort.
                inFlightSource.pitch = AH64StaticValues.rotorInFlightPitch;
            }

            if (climbSource)
            {
                float climbFactor = hoverController ? Mathf.Clamp01(hoverController.AscentPitchWeight) : 0f;
                float targetVolume = climbFactor * AH64PlaytestConfig.RotorClimbVolume;
                climbSource.volume = Mathf.SmoothDamp(
                    climbSource.volume, targetVolume, ref climbVolumeVelocity,
                    AH64StaticValues.rotorLayerFadeTime, Mathf.Infinity, deltaTime);
            }
        }

        private void OnDisable()
        {
            StopRotor();
        }

        private void OnDestroy()
        {
            StopRotor();
        }

        private void StopRotor()
        {
            if (rotorSource)
                rotorSource.Stop();
            if (inFlightSource)
                inFlightSource.Stop();
            if (climbSource)
                climbSource.Stop();
        }
    }
}
