using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    internal partial class AH64HoverController
    {
        private bool providingHoverGranters;
        private bool equipmentFlightActive;
        private bool crashing;

        /// <summary>
        /// Hands the vertical axis to the death crash. Authority only, from <c>AH64Death</c>. Flight stays
        /// on so <see cref="ApplyCrash"/> can script the fall instead of dropping the wreck like a stone.
        /// </summary>
        public void BeginCrash()
        {
            crashing = true;
            externalLaunchActive = false;
            IsAscending = false;
            IsDescending = false;
            if (motor)
                motor.disableAirControlUntilCollision = false;
        }

        /// <summary>
        /// One tick of the crash descent: an accelerating fall while the horizontal speed bleeds off.
        /// Falls back to vanilla gravity when there is no move speed to express the fall through.
        /// </summary>
        public void ApplyCrash(float age)
        {
            if (!motor || !body)
                return;

            float walkSpeed = body.moveSpeed;
            if (walkSpeed <= 0.01f)
            {
                UseVanillaPhysics(AH64StaticValues.hoverAirControl);
                return;
            }

            ConfigureMotor();
            float fallSpeed = Mathf.Min(
                AH64StaticValues.crashFallSpeedStart + AH64StaticValues.crashFallAccel * age,
                AH64StaticValues.crashFallSpeedMax);
            Vector3 drift = motor.velocity;
            drift.y = 0f;
            Vector3 direction = drift * (AH64StaticValues.crashHorizontalCarry / walkSpeed);
            direction.y = -fallSpeed / walkSpeed;
            motor.moveDirection = direction;
        }

        //JetpackController and CharacterBody also own counted granters. Never replace their structs
        //or zero the shared counts: remove only the one contribution installed by this component.
        private void SetHoverGranters(bool enabled)
        {
            if (!motor || providingHoverGranters == enabled)
                return;

            int change = enabled ? 1 : -1;
            CharacterFlightParameters flight = motor.flightParameters;
            flight.channeledFlightGranterCount += change;
            motor.flightParameters = flight;
            CharacterGravityParameters gravity = motor.gravityParameters;
            gravity.channeledAntiGravityGranterCount += change;
            motor.gravityParameters = gravity;
            providingHoverGranters = enabled;
        }

        private void OnApplyForceImpulse(On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig,
            CharacterMotor self, ref PhysForceInfo forceInfo)
        {
            float previousVerticalSpeed = self.velocity.y;
            orig(self, ref forceInfo);
            //A crashing aircraft keeps its scripted descent: the killing blow's push still lands on the
            //velocity, but must not hand the wreck to vanilla gravity mid-spin.
            if (self != motor || crashing || !self.hasEffectiveAuthority
                || Mathf.Approximately(previousVerticalSpeed, self.velocity.y))
                return;

            //Observe the applied result, so mass, immunity, capped forces and RPC authority follow
            //the real motor. This catches small knock-ups and downward pulls the speed test misses.
            //Yield immediately: PreMove can otherwise brake the impulse before the next Body tick.
            externalLaunchActive = true;
            externalLaunchAge = 0f;
            timeWithoutGround = 0f;
            IsAscending = false;
            YieldToExternalMotion();
        }

        private void YieldToExternalMotion()
        {
            UseVanillaPhysics(AH64StaticValues.externalLaunchAirControl);
            if (motor.isFlying && body && body.moveSpeed > 0f)
            {
                //Other flight grants survive our yield. Set their vertical target immediately,
                //including inside the force hook, before PreMove can brake the new impulse.
                Vector3 direction = motor.moveDirection;
                direction.y = motor.velocity.y / body.moveSpeed;
                motor.moveDirection = direction;
            }
        }

        private bool ApplyEquipmentFlight(bool jumpHeld, bool descendHeld, float deltaTime)
        {
            JetpackController jetpack = JetpackController.FindJetpackController(gameObject);
            //The wings object survives expiry until landing, which a hovering body may never do.
            //Its live timer, not object existence or equipment inventory, defines powered flight.
            bool active = jetpack && jetpack.stopwatch < jetpack.duration;
            if (!active)
            {
                if (equipmentFlightActive)
                {
                    ResetAltitudeToRest();
                    timeWithoutGround = 0f;
                    motor.disableAirControlUntilCollision = false;
                }
                equipmentFlightActive = false;
                return false;
            }

            equipmentFlightActive = true;
            //Give actual external impulses a short uncontested response even during equipment
            //flight, then let the pilot take control again instead of drifting for the whole buff.
            if (externalLaunchActive)
            {
                if (externalLaunchAge < AH64StaticValues.externalLaunchMinDuration)
                    return false;
                externalLaunchActive = false;
            }
            timeWithoutGround = 0f;
            TargetHeight = GetRestHeight();
            ConfigureMotor();
            motor.disableAirControlUntilCollision = false;
            lastDisableAirControl = false;

            //Keep the helicopter's familiar collective controls, but permit flight beyond both the
            //normal altitude ceiling and the ground probe. Releasing both controls holds altitude.
            float verticalSpeed = descendHeld ? -AH64StaticValues.hoverMaxDescendSpeed
                : jumpHeld ? AH64StaticValues.hoverMaxClimbSpeed : 0f;
            Vector3 direction = motor.moveDirection;
            direction.y = body.moveSpeed > 0f ? verticalSpeed / body.moveSpeed : 0f;
            motor.moveDirection = direction;
            if (verticalSpeed > 0f && motor.isGrounded)
                motor.Motor.ForceUnground();
            IsAscending = verticalSpeed > 0f;
            UpdateAscentPitch(deltaTime);
            return true;
        }
    }
}
