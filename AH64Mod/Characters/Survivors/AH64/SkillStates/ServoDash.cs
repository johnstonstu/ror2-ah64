using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using AH64.Survivors.Components;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Utility. Forward-diagonal barrel roll — snaps move input to forward / left / right (never back).
    /// Climbs through the maneuver, full procedural 360° on the model. Speed eases up from the entry
    /// velocity then coasts down so it does not slam to a fixed multiplier. I-frames for the first half,
    /// plating armor after, flares through the move. Class name stays <c>ServoDash</c> for EntityState
    /// registration; player-facing name is Evasive Roll.
    /// </summary>
    public class ServoDash : BaseSkillState
    {
        public static float duration = AH64StaticValues.dashDuration;
        public static float initialSpeedCoefficient = AH64StaticValues.dashInitialSpeedCoefficient;
        public static float finalSpeedCoefficient = AH64StaticValues.dashFinalSpeedCoefficient;

        public static string dodgeSoundString = "Play_loader_m2_launch";
        private const string travelLoopPlay = "Play_loader_m2_travel_loop";
        private const string travelLoopStop = "Stop_loader_m2_travel_loop";
        public static float dodgeFOV = global::EntityStates.Commando.DodgeState.dodgeFOV;

        private const float thrusterSmokeScale = 0.35f;
        private const float thrusterSpacing = 0.55f;

        private float dashSpeed;
        private float entrySpeed;
        private float peakSpeed;
        private float exitSpeed;
        private Vector3 forwardDirection;
        private Vector3 previousPosition;
        private float flareTimer;
        // -1 left, +1 right — always non-zero; drives the procedural barrel roll
        private float rollSign;

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority && characterDirection)
            {
                ResolveRollDirection(out forwardDirection, out rollSign);
            }

            CaptureEntrySpeed();
            RecalculateDashSpeed();

            if (characterMotor)
            {
                //KCC projects upward velocity onto the ground plane while stably grounded
                if (characterMotor.isGrounded)
                    characterMotor.Motor.ForceUnground();

                float climbSpeed = ClimbVelocityAt(0f);
                //start at entry speed (not peak) so the ramp is felt
                characterMotor.velocity = forwardDirection * dashSpeed + Vector3.up * climbSpeed;
            }

            Vector3 startingVelocity = characterMotor ? characterMotor.velocity : Vector3.zero;
            previousPosition = transform.position - startingVelocity;

            Util.PlaySound(dodgeSoundString, gameObject);
            Util.PlaySound(travelLoopPlay, gameObject);
            PlayThrusterBurst();

            //OnDeserialize runs before OnEnter on remotes, so rollSign is already filled there
            AH64FlightVisuals flightVisuals = GetComponent<AH64FlightVisuals>();
            if (flightVisuals)
                flightVisuals.PlayBarrelRoll(rollSign, AH64StaticValues.dashDuration);

            AH64HoverController hover = GetComponent<AH64HoverController>();
            if (hover)
                hover.BumpTargetHeight(AH64PlaytestConfig.DashClimbHeight);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(AH64Buffs.platingBuff, AH64StaticValues.dashArmorDurationCoefficient * duration);
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, AH64StaticValues.dashInvincibilityDurationCoefficient * duration);
            }
        }

        /// <summary>
        /// Snap stick to forward / left / right relative to facing. L/R travel on a forward diagonal.
        /// Backward input becomes forward so the utility never rolls into your own wake. Roll sign is
        /// always ±1 (forward defaults right, or follows a light stick bias).
        /// </summary>
        private void ResolveRollDirection(out Vector3 direction, out float bankSign)
        {
            Vector3 facing = characterDirection.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                facing = transform.forward;
            facing.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;

            Vector3 move = inputBank && inputBank.moveVector != Vector3.zero
                ? inputBank.moveVector
                : facing;
            move.y = 0f;
            if (move.sqrMagnitude < 0.0001f)
                move = facing;
            move.Normalize();

            float forwardDot = Vector3.Dot(move, facing);
            float rightDot = Vector3.Dot(move, right);
            float blend = AH64StaticValues.dashDiagonalBlend;

            //prefer left/right when the stick is more sideways than forward; never pick back
            if (Mathf.Abs(rightDot) > Mathf.Abs(forwardDot) && Mathf.Abs(rightDot) > 0.35f)
            {
                Vector3 side = rightDot >= 0f ? right : -right;
                direction = (facing * blend + side).normalized;
                bankSign = rightDot >= 0f ? 1f : -1f;
            }
            else
            {
                direction = facing;
                //forward roll still needs a flip side — light stick bias, else default right
                bankSign = Mathf.Abs(rightDot) > 0.1f ? Mathf.Sign(rightDot) : 1f;
            }
        }

        private void CaptureEntrySpeed()
        {
            float floor = moveSpeedStat * AH64StaticValues.dashMinEntrySpeedFraction;
            float horizontal = 0f;
            if (characterMotor)
            {
                Vector3 v = characterMotor.velocity;
                v.y = 0f;
                horizontal = v.magnitude;
            }

            entrySpeed = Mathf.Max(horizontal, floor);
            peakSpeed = entrySpeed * AH64PlaytestConfig.DashPeakSpeed;
            exitSpeed = Mathf.Lerp(entrySpeed, peakSpeed, AH64StaticValues.dashExitCarry);
        }

        /// <summary>
        /// Half-sine climb: y = H * sin(π/2 * t) so the hop ends elevated by <c>dashClimbHeight</c>.
        /// </summary>
        private static float ClimbVelocityAt(float age)
        {
            float t = Mathf.Clamp01(age / duration);
            return AH64PlaytestConfig.DashClimbHeight
                * (Mathf.PI * 0.5f / duration)
                * Mathf.Cos(Mathf.PI * 0.5f * t);
        }

        private void PlayThrusterBurst()
        {
            if (forwardDirection == Vector3.zero)
                return;

            Vector3 right = Vector3.Cross(Vector3.up, forwardDirection).normalized;
            Vector3 origin = transform.position + Vector3.up * 0.7f - forwardDirection * 0.55f;

            GameObject thruster = AH64Assets.dashThrusterEffect;
            if (thruster)
            {
                EffectManager.SpawnEffect(thruster, new EffectData
                {
                    origin = origin + right * thrusterSpacing,
                    rotation = Util.QuaternionSafeLookRotation(-forwardDirection),
                    scale = thrusterSmokeScale,
                }, false);
                EffectManager.SpawnEffect(thruster, new EffectData
                {
                    origin = origin - right * thrusterSpacing,
                    rotation = Util.QuaternionSafeLookRotation(-forwardDirection),
                    scale = thrusterSmokeScale,
                }, false);
            }

            GameObject dust = AH64Assets.dashDustEffect;
            if (dust)
            {
                EffectManager.SpawnEffect(dust, new EffectData
                {
                    origin = transform.position,
                    scale = 1.35f,
                }, false);
            }
            else
            {
                GameObject smoke = AH64Assets.SmokePuffEffect;
                if (!smoke) return;
                EffectManager.SpawnEffect(smoke, new EffectData { origin = origin + right * thrusterSpacing, scale = thrusterSmokeScale }, false);
                EffectManager.SpawnEffect(smoke, new EffectData { origin = origin - right * thrusterSpacing, scale = thrusterSmokeScale }, false);
            }
        }

        private void PopFlare()
        {
            GameObject flare = AH64Assets.dashFlareEffect;
            if (!flare || forwardDirection == Vector3.zero)
                return;

            Vector3 origin = transform.position
                + Vector3.up * 0.9f
                - forwardDirection * AH64StaticValues.dashFlareTrailDistance
                + Random.insideUnitSphere * AH64StaticValues.dashFlareScatter;

            EffectManager.SpawnEffect(flare, new EffectData
            {
                origin = origin,
                rotation = Util.QuaternionSafeLookRotation(-forwardDirection),
                scale = AH64StaticValues.dashFlareScale,
            }, false);
        }

        /// <summary>
        /// Ease into peak over <c>dashRampFraction</c>, then ease down toward exit carry. Smoothstep
        /// on both legs so the roll reads as a shove-then-coast rather than a teleport.
        /// </summary>
        private void RecalculateDashSpeed()
        {
            float t = Mathf.Clamp01(fixedAge / duration);
            float ramp = Mathf.Clamp(AH64PlaytestConfig.DashRampFraction, 0.05f, 0.9f);

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

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            RecalculateDashSpeed();

            flareTimer -= GetDeltaTime();
            if (flareTimer <= 0f)
            {
                flareTimer = AH64StaticValues.dashFlareInterval;
                PopFlare();
            }

            if (characterDirection) characterDirection.forward = forwardDirection;
            if (cameraTargetParams) cameraTargetParams.fovOverride = Mathf.Lerp(dodgeFOV, 60f, fixedAge / duration);

            float climbSpeed = ClimbVelocityAt(fixedAge);

            Vector3 travelDirection = (transform.position - previousPosition);
            travelDirection.y = 0f;
            if (characterMotor && characterDirection)
            {
                Vector3 velocity;
                if (travelDirection.sqrMagnitude > 0.0001f)
                {
                    travelDirection.Normalize();
                    velocity = travelDirection * dashSpeed;
                    float forwardSpeed = Mathf.Max(Vector3.Dot(velocity, forwardDirection), 0f);
                    velocity = forwardDirection * forwardSpeed;
                }
                else
                {
                    velocity = forwardDirection * dashSpeed;
                }

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
            if (cameraTargetParams) cameraTargetParams.fovOverride = -1f;

            //Hand hover a soft horizontal carry at exitSpeed so Main does not inherit a hard cut.
            if (characterMotor && forwardDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 carry = forwardDirection.normalized * exitSpeed;
                carry.y = characterMotor.velocity.y;
                characterMotor.velocity = carry;
            }

            base.OnExit();

            if (characterMotor) characterMotor.disableAirControlUntilCollision = false;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(forwardDirection);
            writer.Write(rollSign);
            writer.Write(entrySpeed);
            writer.Write(peakSpeed);
            writer.Write(exitSpeed);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            forwardDirection = reader.ReadVector3();
            rollSign = reader.ReadSingle();
            entrySpeed = reader.ReadSingle();
            peakSpeed = reader.ReadSingle();
            exitSpeed = reader.ReadSingle();
        }
    }
}
