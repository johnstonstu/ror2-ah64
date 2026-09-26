using RoR2;
using UnityEngine;
using AH64.Survivors.SkillStates;

namespace AH64.Survivors.Components
{
    public partial class AH64FlightAudio
    {
        private EntityStateMachine[] audioStateMachines;
        private float maneuverResponse;
        private float maneuverVelocity;
        private Vector3 previousTravelVelocity;
        private bool hasTravelSample;
        private float sampledSpeed;
        private float sampledDirection;
        private float sampledAcceleration;
        private float sampledTurn;
        private float sampledClimb;

        private void ResetResponse()
        {
            maneuverResponse = maneuverVelocity = 0f;
            previousTravelVelocity = Vector3.zero;
            sampledSpeed = sampledDirection = sampledAcceleration = sampledTurn = sampledClimb = 0f;
            hasTravelSample = false;
        }

        private float ResponseTime(float current, float target)
        {
            //Keep reversals on the settling curve rather than snapping to the fast attack.
            bool attacking = current * target >= 0f && Mathf.Abs(target) > Mathf.Abs(current);
            return AH64PlaytestConfig.RotorResponse * (attacking
                ? AH64StaticValues.rotorResponseAttackScale : AH64StaticValues.rotorResponseReleaseScale);
        }

        private void FixedUpdate()
        {
            if (PauseManager.isPaused || !motor || !body || body.moveSpeed <= 0.01f) return;
            Vector3 velocity = motor.velocity;
            velocity.y = 0f;
            sampledSpeed = Mathf.Clamp01(velocity.magnitude / body.moveSpeed);
            sampledDirection = GetDirectionalLoad();
            sampledClimb = hoverController ? Mathf.Clamp01(hoverController.AscentPitchWeight) : 0f;
            float acceleration = 0f;
            float turn = 0f;
            if (hasTravelSample)
            {
                //Physics velocity changes at fixed cadence, never divide it by render delta.
                acceleration = Mathf.Clamp01((velocity.magnitude - previousTravelVelocity.magnitude)
                    / (body.moveSpeed * Time.fixedDeltaTime));
                float travelGate = Mathf.Min(velocity.magnitude, previousTravelVelocity.magnitude) / body.moveSpeed;
                if (travelGate > 0.1f)
                    turn = Mathf.Clamp01(Vector3.Angle(previousTravelVelocity, velocity)
                        / (AH64StaticValues.rotorFullTurnRate * Time.fixedDeltaTime)) * Mathf.Clamp01(travelGate);
            }
            //Filter noisy motor/terrain samples before they reach the musical envelope.
            float blend = 1f - Mathf.Exp(-Time.fixedDeltaTime / AH64StaticValues.rotorMotionSampleSmoothing);
            sampledAcceleration = Mathf.Lerp(sampledAcceleration, acceleration, blend);
            sampledTurn = Mathf.Lerp(sampledTurn, turn, blend);
            previousTravelVelocity = velocity;
            hasTravelSample = true;
        }

        private float GetManeuverTarget()
        {
            float target = Mathf.Max(sampledAcceleration * AH64StaticValues.rotorAccelerationResponse,
                sampledTurn * AH64StaticValues.rotorTurnResponse);
            //Replicated skill states contribute to the same bounded envelope. Circling,
            //climbing and abilities cannot add separate pulses on top of each other.
            if (audioStateMachines != null)
                foreach (EntityStateMachine machine in audioStateMachines)
                {
                    if (!machine) continue;
                    if (machine.state is ServoDash || machine.state is SmokeBackflip)
                        target = 1f;
                    else if (machine.state is FireHellfire || machine.state is FireLongbow)
                        target = Mathf.Max(target, AH64StaticValues.rotorSpecialResponse);
                }
            return target;
        }
    }
}
