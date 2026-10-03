using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using AH64.Survivors.Components;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Utility. Forward-diagonal barrel roll — snaps move input to forward / left / right (never back).
    /// Utility. Forward/diagonal barrel roll with bounded analog steering and captured momentum.
    /// velocity then coasts down so it does not slam to a fixed multiplier. I-frames for the first half,
    /// plating armor for the length of the roll, flares through the move. Class name stays <c>ServoDash</c> for EntityState
    /// registration; player-facing name is Evasive Roll.
    /// </summary>
    public class ServoDash : BaseSkillState
    {
        public static float duration = AH64StaticValues.dashDuration;
        public static float initialSpeedCoefficient = AH64StaticValues.dashInitialSpeedCoefficient;
        public static float finalSpeedCoefficient = AH64StaticValues.dashFinalSpeedCoefficient;

        public static string dodgeSoundString = "Play_loader_m2_launch";
        public static float dodgeFOV = global::EntityStates.Commando.DodgeState.dodgeFOV;

        private const float thrusterSmokeScale = 0.35f;
        private const float thrusterSpacing = 0.55f;

        private AH64ManeuverCapture capture;
        private bool hasCapture;
        internal AH64ManeuverCapture EntrySnapshot => capture;
        internal float ManeuverProgress => hasCapture ? Mathf.Clamp01(fixedAge / capture.Duration) : 0f;
        internal bool MotionYielded => motion && motion.IsYielding;
        private AH64ManeuverMotor motion;
        private AH64FlightVisuals flightVisuals;
        private Vector3 forwardDirection;
        private float flareTimer;

        public override void OnEnter()
        {
            base.OnEnter();

            flightVisuals = GetComponent<AH64FlightVisuals>();
            AH64HoverController hover = GetComponent<AH64HoverController>();
            if (isAuthority && !hasCapture)
            {
                Vector3 facing = characterDirection ? characterDirection.forward : transform.forward;
                Vector3 input = inputBank ? inputBank.moveVector : Vector3.zero;
                capture = new AH64ManeuverCapture
                {
                    EntryVelocity = characterMotor ? characterMotor.velocity : Vector3.zero,
                    Facing = AH64ManeuverMath.Horizontal(facing).normalized,
                    Direction = AH64ManeuverMath.Direction(facing, input, false, AH64StaticValues.dashDiagonalBlend),
                    EntryAttitude = flightVisuals ? flightVisuals.CaptureAttitude() : Quaternion.identity,
                    Sign = Vector3.Dot(input, Vector3.Cross(Vector3.up, facing)) < -0.1f ? -1f : 1f,
                    Duration = Mathf.Max(duration, 0.01f),
                    Ramp = Mathf.Clamp(AH64PlaytestConfig.DashRampFraction, 0.05f, 0.9f),
                    Climb = hover ? hover.LimitUtilityClimb(AH64PlaytestConfig.DashClimbHeight) : 0f,
                    StartY = transform.position.y
                };
                AH64ManeuverMath.Speeds(ref capture, moveSpeedStat,
                    AH64StaticValues.dashMinEntrySpeedFraction, AH64PlaytestConfig.DashPeakSpeed, AH64StaticValues.dashExitCarry);
                hasCapture = true;
            }
            // Remote OnDeserialize supplies the complete snapshot before OnEnter.
            if (!hasCapture) return;
            forwardDirection = capture.Direction;
            if (isAuthority && characterMotor)
            {
                motion = GetComponent<AH64ManeuverMotor>();
                if (!motion) motion = gameObject.AddComponent<AH64ManeuverMotor>();
                motion.Begin(this, capture);
                if (hover && !motion.IsYielding) hover.BumpTargetHeight(capture.Climb);
            }
            Util.PlaySound(dodgeSoundString, gameObject);
            PlayThrusterBurst();
            if (flightVisuals) flightVisuals.PlayBarrelRoll(this, capture.Sign, capture.Duration, capture.EntryAttitude);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(AH64Buffs.platingBuff, AH64StaticValues.dashArmorDurationCoefficient * capture.Duration);
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, AH64StaticValues.dashInvincibilityDurationCoefficient * capture.Duration);
            }
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

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasCapture) return;

            flareTimer -= GetDeltaTime();
            if (flareTimer <= 0f)
            {
                flareTimer = AH64StaticValues.dashFlareInterval;
                PopFlare();
            }

            if (flightVisuals) flightVisuals.SetManeuverProgress(this, fixedAge);
            if (isAuthority)
            {
                // Native aim is independent of the roll and trajectory; never rewrite forward.
                if (characterDirection && inputBank) characterDirection.moveVector = inputBank.aimDirection;
                Vector3 input = inputBank ? inputBank.moveVector : Vector3.zero;
                Vector3 target = AH64ManeuverMath.Direction(capture.Facing, input,
                    false, AH64StaticValues.dashDiagonalBlend);
                if (motion) motion.Step(this, target, fixedAge, GetDeltaTime());
            }

            if (isAuthority && fixedAge >= capture.Duration)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        public override void OnExit()
        {
            if (motion) motion.Release(this);
            if (flightVisuals) flightVisuals.EndManeuver(this);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            capture.Write(writer);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            capture = AH64ManeuverCapture.Read(reader);
            hasCapture = true;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
