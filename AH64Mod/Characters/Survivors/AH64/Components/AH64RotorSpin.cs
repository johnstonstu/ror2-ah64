using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Spins <c>MainRotor</c> and <c>TailRotor</c> about their local Y axes, with spool-up, a small
    /// RPM rise under effort, and spin-down on death. Also runs on the character-select display,
    /// which has no <see cref="ModelLocator"/> because it is the model itself.
    ///
    /// <para>Local Y, not Z: Blender's Z-up becomes Unity's Y-up on import, so the thin axis of each
    /// rotor disc is local Y in the game. Spinning about local Z wobbles instead of spinning — the same
    /// failure mode as an off-centre origin, and just as invisible until it's in game.</para>
    /// </summary>
    public class AH64RotorSpin : MonoBehaviour
    {
        //real Apache main rotor is ~225 rpm; this is stylistic — readable at a glance without looking like a fan
        private const float MainRotorRpm = 280f;
        private const float TailRotorRpm = 1200f;

        private Transform mainRotor;
        private Transform tailRotor;
        private bool lobby;
        private CharacterBody body;
        private CharacterModel characterModel;
        private CharacterMotor motor;
        private InputBankTest inputBank;
        private AH64HoverController hover;
        private float spool;
        private bool spoolInitialised;

        /// <summary>Eased rotor speed, 0 (stopped) to 1 (flight RPM). AH64FlightAudio follows it.</summary>
        public float Spool => spool * spool * (3f - 2f * spool);
        private float effort;

        private void Start()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            lobby = !modelLocator;
            Transform model = lobby ? transform : modelLocator.modelTransform;
            ChildLocator childLocator = model ? model.GetComponent<ChildLocator>() : null;
            if (childLocator)
            {
                mainRotor = childLocator.FindChild("MainRotor");
                tailRotor = childLocator.FindChild("TailRotor");
            }
            if (lobby || !model)
                return;

            body = GetComponent<CharacterBody>();
            characterModel = model.GetComponent<CharacterModel>();
            motor = GetComponent<CharacterMotor>();
            inputBank = GetComponent<InputBankTest>();
            hover = GetComponent<AH64HoverController>();
        }

        private void Update()
        {
            //The spool runs even without rotor transforms: AH64FlightAudio follows it, and a missing
            //part must not leave the rotor sound silent.
            float dt = Time.deltaTime;
            float target = TargetSpool();
            //Only a pod arrival should start from a stop. Later stages and revives spawn in the open, where
            //spooling from a stop showed stationary blades on an aircraft already flying. Character select
            //also starts from a stop on purpose: picking the AH-64 starts its engines.
            if (!spoolInitialised)
            {
                if (!lobby && !ArrivingByPod())
                    spool = target;
                spoolInitialised = true;
            }
            float seconds = target <= spool ? AH64StaticValues.rotorSpinDownSeconds
                : lobby ? AH64StaticValues.rotorLobbySpoolUpSeconds
                : AH64StaticValues.rotorSpoolUpSeconds;
            spool = Mathf.MoveTowards(spool, target, dt / seconds);
            effort = Mathf.Lerp(effort, TargetEffort(), 1f - Mathf.Exp(-3f * dt));

            //Smoothstep so the blades ease off the stop and settle into speed instead of snapping.
            float rpmScale = spool * spool * (3f - 2f * spool) * (1f + AH64StaticValues.rotorEffortRpmBoost * effort);
            if (mainRotor)
                mainRotor.Rotate(Vector3.up, MainRotorRpm * 6f * rpmScale * dt, Space.Self);
            if (tailRotor)
                tailRotor.Rotate(Vector3.up, TailRotorRpm * 6f * rpmScale * dt, Space.Self);
        }

        /// <summary>
        /// True at the start of a stage entered by drop pod. The pod hides the model through
        /// <c>invisibilityCount</c>, but on a client the seat assignment can land a frame after the body
        /// spawns, so starting at full speed could leave an audible wind-down inside the pod.
        /// </summary>
        private static bool ArrivingByPod()
        {
            Stage stage = Stage.instance;
            return stage && stage.usePod && stage.entryTime.timeSince < 5f;
        }

        private float TargetSpool()
        {
            if (lobby)
                return AH64StaticValues.rotorLobbyIdleFraction;
            if (body && body.healthComponent && !body.healthComponent.alive)
                return 0f;
            //VehicleSeat.hidePassenger raises invisibilityCount while the drop pod carries us.
            if (characterModel && characterModel.invisibilityCount > 0)
                return 0f;
            return 1f;
        }

        private float TargetEffort()
        {
            if (lobby)
                return 0f;
            float value = motor ? Mathf.Clamp01(motor.velocity.magnitude / AH64StaticValues.rotorBlurFullSpeed) * 0.6f : 0f;
            if ((inputBank && inputBank.jump.down) || (hover && hover.IsAscending))
                value += 0.6f;
            return Mathf.Clamp01(value);
        }
    }
}
