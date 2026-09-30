using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using AH64.Survivors.Components;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Utility loadout variant. Aerobatic backflip: surge rearward + climb, procedural pitch flip,
    /// smoke screen, brief vanilla cloak. Default utility remains <see cref="ServoDash"/> (Evasive Roll).
    /// </summary>
    public class SmokeBackflip : BaseSkillState
    {
        public static float duration = AH64StaticValues.backflipDuration;

        private const string launchSound = "Play_loader_m2_launch";
        private const string travelLoopPlay = "Play_loader_m2_travel_loop";
        private const string travelLoopStop = "Stop_loader_m2_travel_loop";
        private static readonly float dodgeFOV = global::EntityStates.Commando.DodgeState.dodgeFOV;

        private float dashSpeed;
        private float entrySpeed;
        private float peakSpeed;
        private float exitSpeed;
        private Vector3 rearwardDirection;
        private Vector3 previousPosition;
        private float smokeTimer;
        private float climbHeight;

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority && characterDirection)
                ResolveRearwardDirection();

            CaptureEntrySpeed();
            RecalculateDashSpeed();

            AH64HoverController hover = GetComponent<AH64HoverController>();
            climbHeight = hover
                ? hover.LimitUtilityClimb(AH64StaticValues.backflipClimbHeight)
                : AH64StaticValues.backflipClimbHeight;

            if (characterMotor)
            {
                if (characterMotor.isGrounded)
                    characterMotor.Motor.ForceUnground();

                characterMotor.velocity = rearwardDirection * dashSpeed + Vector3.up * ClimbVelocityAt(0f);
            }

            Vector3 startingVelocity = characterMotor ? characterMotor.velocity : Vector3.zero;
            previousPosition = transform.position - startingVelocity;

            Util.PlaySound(launchSound, gameObject);
            Util.PlaySound(travelLoopPlay, gameObject);
            PopSmokeBurst();

            AH64FlightVisuals flightVisuals = GetComponent<AH64FlightVisuals>();
            if (flightVisuals)
                flightVisuals.PlayBackflip(AH64StaticValues.backflipDuration);

            if (hover)
                hover.BumpTargetHeight(climbHeight);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, AH64StaticValues.backflipCloakDuration);
                characterBody.AddTimedBuff(
                    RoR2Content.Buffs.HiddenInvincibility,
                    AH64StaticValues.backflipInvincibilityDurationCoefficient * duration);
            }
        }

        private void ResolveRearwardDirection()
        {
            Vector3 facing = characterDirection.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                facing = transform.forward;
            facing.Normalize();
            rearwardDirection = -facing;
        }

        private void CaptureEntrySpeed()
        {
            float floor = moveSpeedStat * AH64StaticValues.backflipMinEntrySpeedFraction;
            float horizontal = 0f;
            if (characterMotor)
            {
                Vector3 v = characterMotor.velocity;
                v.y = 0f;
                horizontal = v.magnitude;
            }

            entrySpeed = Mathf.Max(horizontal, floor);
            peakSpeed = entrySpeed * AH64StaticValues.backflipPeakSpeedMult;
            exitSpeed = Mathf.Lerp(entrySpeed, peakSpeed, AH64StaticValues.backflipExitCarry);
        }

        private float ClimbVelocityAt(float age)
        {
            float t = Mathf.Clamp01(age / duration);
            return climbHeight
                * (Mathf.PI * 0.5f / duration)
                * Mathf.Cos(Mathf.PI * 0.5f * t);
        }

        private void RecalculateDashSpeed()
        {
            float t = Mathf.Clamp01(fixedAge / duration);
            float ramp = Mathf.Clamp(AH64StaticValues.backflipRampFraction, 0.05f, 0.9f);

            if (t <= ramp)
            {
                float u = SmoothStep(t / ramp);
                dashSpeed = Mathf.Lerp(entrySpeed, peakSpeed, u);
            }
            else
            {
                float u = SmoothStep((t - ramp) / (1f - ramp));
                dashSpeed = Mathf.Lerp(peakSpeed, exitSpeed, u);
            }
        }

        private static float SmoothStep(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private void PopSmokeBurst()
        {
            GameObject smoke = AH64Assets.SmokePuffEffect;
            if (!smoke)
                smoke = AH64Assets.dashDustEffect;
            if (!smoke || rearwardDirection == Vector3.zero)
                return;

            Vector3 right = Vector3.Cross(Vector3.up, rearwardDirection).normalized;
            Vector3 origin = transform.position + Vector3.up * 0.85f - rearwardDirection * 0.35f;

            EffectManager.SpawnEffect(smoke, new EffectData
            {
                origin = origin,
                scale = AH64StaticValues.backflipSmokeScale,
            }, false);
            EffectManager.SpawnEffect(smoke, new EffectData
            {
                origin = origin + right * 0.55f,
                scale = AH64StaticValues.backflipSmokeScale * 0.85f,
            }, false);
            EffectManager.SpawnEffect(smoke, new EffectData
            {
                origin = origin - right * 0.55f,
                scale = AH64StaticValues.backflipSmokeScale * 0.85f,
            }, false);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            RecalculateDashSpeed();

            smokeTimer -= GetDeltaTime();
            if (smokeTimer <= 0f)
            {
                smokeTimer = AH64StaticValues.backflipSmokeInterval;
                PopSmokeBurst();
            }

            //Keep facing forward while the body surges back — the flip is on the model, not the aim.
            if (characterDirection && rearwardDirection.sqrMagnitude > 0.0001f)
                characterDirection.forward = -rearwardDirection;

            if (cameraTargetParams)
                cameraTargetParams.fovOverride = Mathf.Lerp(dodgeFOV, 60f, fixedAge / duration);

            float climbSpeed = ClimbVelocityAt(fixedAge);
            if (characterMotor && rearwardDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 velocity = rearwardDirection * dashSpeed;
                velocity.y = climbSpeed;
                characterMotor.velocity = velocity;
            }

            previousPosition = transform.position;

            if (isAuthority && fixedAge >= duration)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public override void OnExit()
        {
            Util.PlaySound(travelLoopStop, gameObject);
            if (cameraTargetParams)
                cameraTargetParams.fovOverride = -1f;

            if (characterMotor && rearwardDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 carry = rearwardDirection.normalized * exitSpeed;
                carry.y = characterMotor.velocity.y;
                characterMotor.velocity = carry;
            }

            base.OnExit();

            if (characterMotor)
                characterMotor.disableAirControlUntilCollision = false;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(rearwardDirection);
            writer.Write(entrySpeed);
            writer.Write(peakSpeed);
            writer.Write(exitSpeed);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            rearwardDirection = reader.ReadVector3();
            entrySpeed = reader.ReadSingle();
            peakSpeed = reader.ReadSingle();
            exitSpeed = reader.ReadSingle();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
