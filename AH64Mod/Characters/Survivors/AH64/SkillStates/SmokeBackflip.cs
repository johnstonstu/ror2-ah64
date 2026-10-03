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

        private AH64ManeuverCapture capture;
        private bool hasCapture;
        internal AH64ManeuverCapture EntrySnapshot => capture;
        internal float ManeuverProgress => hasCapture ? Mathf.Clamp01(fixedAge / capture.Duration) : 0f;
        internal bool MotionYielded => motion && motion.IsYielding;
        private AH64ManeuverMotor motion;
        private AH64FlightVisuals flightVisuals;
        private Vector3 rearwardDirection;
        private float smokeTimer;

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
                    Direction = AH64ManeuverMath.Direction(facing, input, true, AH64StaticValues.dashDiagonalBlend),
                    EntryAttitude = flightVisuals ? flightVisuals.CaptureAttitude() : Quaternion.identity,
                    Sign = Vector3.Dot(input, Vector3.Cross(Vector3.up, facing)) < -0.1f ? -1f : 1f,
                    Duration = Mathf.Max(duration, 0.01f),
                    Ramp = Mathf.Clamp(AH64StaticValues.backflipRampFraction, 0.05f, 0.9f),
                    Climb = hover ? hover.LimitUtilityClimb(AH64StaticValues.backflipClimbHeight) : 0f,
                    StartY = transform.position.y
                };
                AH64ManeuverMath.Speeds(ref capture, moveSpeedStat,
                    AH64StaticValues.backflipMinEntrySpeedFraction, AH64StaticValues.backflipPeakSpeedMult, AH64StaticValues.backflipExitCarry);
                hasCapture = true;
            }
            // Remote OnDeserialize supplies the complete snapshot before OnEnter.
            if (!hasCapture) return;
            rearwardDirection = capture.Direction;
            if (isAuthority && characterMotor)
            {
                motion = GetComponent<AH64ManeuverMotor>();
                if (!motion) motion = gameObject.AddComponent<AH64ManeuverMotor>();
                motion.Begin(this, capture);
                if (hover && !motion.IsYielding) hover.BumpTargetHeight(capture.Climb);
            }
            Util.PlaySound(launchSound, gameObject);
            PopSmokeBurst();
            if (flightVisuals) flightVisuals.PlayBackflip(this, capture.Duration, capture.EntryAttitude);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.Cloak, AH64StaticValues.backflipCloakDuration);
                characterBody.AddTimedBuff(
                    RoR2Content.Buffs.HiddenInvincibility,
                    AH64StaticValues.backflipInvincibilityDurationCoefficient * capture.Duration);
            }
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
            if (!hasCapture) return;

            smokeTimer -= GetDeltaTime();
            if (smokeTimer <= 0f)
            {
                smokeTimer = AH64StaticValues.backflipSmokeInterval;
                PopSmokeBurst();
            }

            if (flightVisuals) flightVisuals.SetManeuverProgress(this, fixedAge);
            if (isAuthority)
            {
                // H3AD-5T's vanilla-physics handoff lives in Main.ApplyHover. Releasing our lease
                // alone would leave hover's flight/anti-gravity grants active during the slam.
                if (motion && motion.NeedsMainHandoff)
                {
                    outer.SetNextStateToMain();
                    return;
                }
                // Native aim is independent of the roll and trajectory; never rewrite forward.
                if (characterDirection && inputBank) characterDirection.moveVector = inputBank.aimDirection;
                Vector3 input = inputBank ? inputBank.moveVector : Vector3.zero;
                Vector3 target = AH64ManeuverMath.Direction(capture.Facing, input,
                    true, AH64StaticValues.dashDiagonalBlend);
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
            // EntityStateMachine accepts equal priority (minimum <= incoming). Pain blocks our
            // PrioritySkill activation from re-entering with extra stocks; stun/death still win.
            return InterruptPriority.Pain;
        }
    }
}
