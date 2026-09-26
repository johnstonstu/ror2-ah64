using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Lives on the AH64 body prefab and owns the chopper's vertical axis. Holds a fixed height above
    /// terrain; hold jump to climb toward a jump-count-scaled ceiling, release to settle. Vertical
    /// motion uses a PD (stiffness + damping) altitude hold so climbs ease in instead of bouncing or
    /// inching — the same mass-spring-damper pattern used by arcade helo / VTOL altitude stabilizers.
    ///
    /// <para><b>How this works against CharacterMotor.</b> RoR2 already has the machinery — two switches,
    /// both needed:</para>
    /// <list type="bullet">
    /// <item><c>flightParameters</c> sets <c>isFlying</c>, which makes <c>PreMove</c> treat the whole of
    /// <c>moveDirection</c> as a velocity target (<c>moveDirection * walkSpeed</c>) instead of zeroing
    /// its Y and preserving the current vertical velocity. That turns <c>moveDirection.y</c> into a real
    /// vertical throttle.</item>
    /// <item><c>gravityParameters</c> clears <c>useGravity</c>. Flight alone does <i>not</i> do this —
    /// <c>PreMove</c> still adds the gravity term afterwards — so without both the chopper would spend
    /// every frame fighting a fall.</item>
    /// </list>
    ///
    /// <para><b>This deliberately has no FixedUpdate.</b> <see cref="ApplyHover"/> is called from
    /// <c>AH64Main.HandleMovements</c> instead, because <c>GenericCharacterMain.HandleMovements</c>
    /// overwrites <c>moveDirection</c> with the horizontal input vector every single frame.</para>
    /// </summary>
    internal class AH64HoverController : MonoBehaviour
    {
        /// <summary>Altitude the chopper is currently trying to hold, in units above the terrain below it.</summary>
        public float TargetHeight { get; private set; }

        /// <summary>True while jump is held and we are still below the collective ceiling.</summary>
        public bool IsAscending { get; private set; }

        /// <summary>
        /// Smoothed −1…1 pitch cue for flight visuals. Positive = nose up (climb), negative = nose down
        /// (settle). Driven by altitude error + vertical velocity, not a one-shot flag.
        /// </summary>
        public float AscentPitchWeight { get; private set; }

        /// <summary>
        /// Raise the held altitude briefly (Evasive Roll hop). Clamped to the current collective ceiling.
        /// </summary>
        public void BumpTargetHeight(float amount)
        {
            if (amount <= 0f)
                return;

            TargetHeight = Mathf.Min(TargetHeight + amount, GetCeiling());
        }

        /// <summary>
        /// Distance from the feet to the terrain directly below, or <b>-1</b> when the probe found nothing
        /// within <c>hoverProbeDistance</c> — i.e. we are out over a pit or the void.
        /// </summary>
        public float GroundDistance { get; private set; }

        private CharacterMotor motor;
        private CharacterBody body;
        private float timeWithoutGround;
        private float spawnGrace;
        private float lastValidGroundDistance;
        private float probeMissTimer;
        private float pitchWeightVelocity;
        private bool externalLaunchActive;
        private float externalLaunchAge;
        private bool lastDisableAirControl;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
        }

        private void Start()
        {
            //Start, not Awake: CharacterMotor.Awake recomputes useGravity/isFlying from these structs, so
            //writing them in Awake would be a coin flip on component order.
            ConfigureMotor();
            TargetHeight = AH64StaticValues.hoverHeight;
            lastValidGroundDistance = AH64StaticValues.hoverHeight;
            GroundDistance = lastValidGroundDistance;
            spawnGrace = AH64StaticValues.hoverSpawnGrace;
        }

        private void ConfigureMotor()
        {
            if (!motor)
                return;

            motor.flightParameters = new CharacterFlightParameters { channeledFlightGranterCount = 1 };
            motor.gravityParameters = new CharacterGravityParameters { channeledAntiGravityGranterCount = 1 };
            motor.airControl = AH64StaticValues.hoverAirControl;
        }

        /// <summary>
        /// Drops our flight and anti-gravity granters so the motor behaves like any vanilla survivor.
        /// Read-modify-write, so environmental anti-gravity a stage grants while we're in this mode
        /// (e.g. a slow-fall volume) is respected.
        /// </summary>
        private void UseVanillaPhysics(float airControl)
        {
            CharacterFlightParameters flight = motor.flightParameters;
            if (flight.channeledFlightGranterCount != 0)
            {
                flight.channeledFlightGranterCount = 0;
                motor.flightParameters = flight;
            }

            CharacterGravityParameters gravity = motor.gravityParameters;
            if (gravity.channeledAntiGravityGranterCount != 0)
            {
                gravity.channeledAntiGravityGranterCount = 0;
                motor.gravityParameters = gravity;
            }

            motor.airControl = airControl;
        }

        public float GetCeiling()
        {
            return AH64StaticValues.hoverHeight + GetMaxRise();
        }

        public float GetMaxRise()
        {
            int jumps = body ? Mathf.Max(body.maxJumpCount, 1) : 1;
            return AH64StaticValues.collectiveMaxRise
                + (jumps - 1) * AH64StaticValues.collectiveRisePerExtraJump;
        }

        /// <summary>
        /// Runs the altitude controller for one physics step and writes the result into
        /// <c>motor.moveDirection.y</c>.
        /// </summary>
        public void ApplyHover(bool jumpHeld, bool descendHeld, float deltaTime)
        {
            if (!motor || !body)
                return;

            ResolveGroundDistance(deltaTime);

            if (spawnGrace > 0f)
                spawnGrace -= deltaTime;

            if (UpdateExternalLaunch(deltaTime))
            {
                UpdateAscentPitch(deltaTime);
                return;
            }

            if (GroundDistance < 0f && spawnGrace <= 0f)
            {
                //Nothing within probe range: fall under vanilla gravity. See voidRecoveryDelay.
                UseVanillaPhysics(AH64StaticValues.hoverAirControl);
                TargetHeight = AH64StaticValues.hoverHeight;
                IsAscending = false;
                UpdateAscentPitch(deltaTime);

                timeWithoutGround += deltaTime;
                if (timeWithoutGround >= AH64StaticValues.voidRecoveryDelay)
                    RecoverFromVoid();
                return;
            }

            if (!motor.isFlying || motor.useGravity)
                ConfigureMotor();

            UpdateTargetHeight(jumpHeld, descendHeld, deltaTime);
            UpdateAscentPitch(deltaTime);

            float maxClimb = AH64StaticValues.hoverMaxClimbSpeed;
            float maxDescend = AH64StaticValues.hoverMaxDescendSpeed;
            if (descendHeld)
                maxDescend *= AH64StaticValues.collectiveCrouchSettleMult;

            float desiredVerticalSpeed;
            if (GroundDistance < 0f)
            {
                desiredVerticalSpeed = 0f;
                timeWithoutGround = 0f;
            }
            else
            {
                timeWithoutGround = 0f;

                //PD altitude hold: P drives toward the target, D kills vertical speed so we don't
                //overshoot and bob. Arcade helo / VTOL altitude stabilizers use the same shape.
                float error = TargetHeight - GroundDistance;
                float verticalVel = motor.velocity.y;
                desiredVerticalSpeed = error * AH64StaticValues.hoverStiffness
                    - verticalVel * AH64StaticValues.hoverDamping;
                desiredVerticalSpeed = Mathf.Clamp(desiredVerticalSpeed, -maxDescend, maxClimb);
            }

            if (desiredVerticalSpeed > 0f && motor.isGrounded)
                motor.Motor.ForceUnground();

            float walkSpeed = body.moveSpeed;
            if (walkSpeed <= 0f)
                return;

            Vector3 moveDirection = motor.moveDirection;
            moveDirection.y = desiredVerticalSpeed / walkSpeed;
            motor.moveDirection = moveDirection;
        }

        /// <summary>
        /// Detects and rides out an external launch (jump pad, knock-up). Returns true while the hover
        /// must keep its hands off the vertical axis. See <c>AH64StaticValues.externalLaunchMinExcessSpeed</c>.
        /// </summary>
        private bool UpdateExternalLaunch(float deltaTime)
        {
            //Edge-triggered: the flag stays set until the next collision, so a level check would
            //re-enter the launch the tick after we hand back.
            bool airControlFlag = motor.disableAirControlUntilCollision;
            bool flagRaised = airControlFlag && !lastDisableAirControl;
            lastDisableAirControl = airControlFlag;

            if (!externalLaunchActive)
            {
                bool launchedFast = motor.velocity.y
                    > AH64StaticValues.hoverMaxClimbSpeed + AH64StaticValues.externalLaunchMinExcessSpeed;
                if (!flagRaised && !launchedFast)
                    return false;

                externalLaunchActive = true;
                externalLaunchAge = 0f;
                timeWithoutGround = 0f;
                IsAscending = false;
            }

            externalLaunchAge += deltaTime;

            bool pastMinimum = externalLaunchAge >= AH64StaticValues.externalLaunchMinDuration;
            bool landed = motor.isGrounded;
            bool fallingNearGround = motor.velocity.y <= 0f && GroundDistance >= 0f
                && GroundDistance <= AH64StaticValues.externalLaunchHandbackHeight;

            if ((pastMinimum && (landed || fallingNearGround))
                || externalLaunchAge >= AH64StaticValues.externalLaunchMaxDuration)
            {
                externalLaunchActive = false;
                TargetHeight = AH64StaticValues.hoverHeight;
                //Otherwise CharacterMotor keeps zero air acceleration until we touch something and the
                //hover can't brake the fall. ServoDash and SmokeBackflip clear it on exit for the same reason.
                motor.disableAirControlUntilCollision = false;
                lastDisableAirControl = false;
                return false;
            }

            UseVanillaPhysics(AH64StaticValues.externalLaunchAirControl);
            return true;
        }

        private void ResolveGroundDistance(float deltaTime)
        {
            float probe = ProbeGround();
            if (probe >= 0f)
            {
                lastValidGroundDistance = probe;
                probeMissTimer = 0f;
                GroundDistance = probe;
                return;
            }

            probeMissTimer += deltaTime;
            if (probeMissTimer <= AH64StaticValues.hoverProbeMissHold && lastValidGroundDistance >= 0f)
            {
                GroundDistance = lastValidGroundDistance;
                return;
            }

            GroundDistance = -1f;
        }

        private void UpdateTargetHeight(bool jumpHeld, bool descendHeld, float deltaTime)
        {
            float ceiling = GetCeiling();

            if (jumpHeld)
            {
                TargetHeight = Mathf.MoveTowards(TargetHeight, ceiling,
                    AH64StaticValues.collectiveClimbRate * deltaTime);
            }
            else
            {
                float settleRate = AH64StaticValues.collectiveSettleRate;
                if (descendHeld)
                    settleRate *= AH64StaticValues.collectiveCrouchSettleMult;

                TargetHeight = Mathf.MoveTowards(TargetHeight, AH64StaticValues.hoverHeight,
                    settleRate * deltaTime);
            }

            //Lead clamp: keep the target ahead of the feet enough for PD to accelerate, but never
            //bank unreachable altitude under a low ceiling.
            if (GroundDistance >= 0f)
                TargetHeight = Mathf.Min(TargetHeight, GroundDistance + AH64StaticValues.collectiveLead);

            TargetHeight = Mathf.Clamp(TargetHeight, AH64StaticValues.hoverHeight, ceiling);

            IsAscending = jumpHeld && TargetHeight > AH64StaticValues.hoverHeight + 0.25f
                && (GroundDistance < 0f || GroundDistance < ceiling - 0.35f);
        }

        private void UpdateAscentPitch(float deltaTime)
        {
            //Intent from altitude error + live climb rate — continuous, not a binary pop flag.
            float error = 0f;
            if (GroundDistance >= 0f)
                error = TargetHeight - GroundDistance;

            float climbVel = motor ? motor.velocity.y : 0f;
            float intent = Mathf.Clamp(error * 0.35f + climbVel * 0.12f, -1f, 1f);

            AscentPitchWeight = Mathf.SmoothDamp(
                AscentPitchWeight,
                intent,
                ref pitchWeightVelocity,
                AH64StaticValues.collectivePitchSmoothTime,
                20f,
                deltaTime);
        }

        private void RecoverFromVoid()
        {
            timeWithoutGround = 0f;
            probeMissTimer = 0f;
            externalLaunchActive = false;
            lastValidGroundDistance = AH64StaticValues.hoverHeight;
            spawnGrace = AH64StaticValues.hoverSpawnGrace;
            TargetHeight = AH64StaticValues.hoverHeight;
            IsAscending = false;
            AscentPitchWeight = 0f;
            pitchWeightVelocity = 0f;

            if (!Run.instance || !body)
                return;

            Vector3 destination = Run.instance.FindSafeTeleportPosition(body, null, 0f, 0f);
            TeleportHelper.TeleportBody(body, destination, false);

            GameObject teleportEffect = Run.instance.GetTeleportEffectPrefab(gameObject);
            if (teleportEffect)
                EffectManager.SimpleEffect(teleportEffect, destination, Quaternion.identity, true);
        }

        private float ProbeGround()
        {
            Vector3 origin = transform.position;
            origin.y += motor.capsuleYOffset - motor.capsuleHeight * 0.5f;
            origin.y += AH64StaticValues.hoverProbeOriginLift;

            float distance = AH64StaticValues.hoverProbeDistance + AH64StaticValues.hoverProbeOriginLift;

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return -1f;

            return Mathf.Max(hit.distance - AH64StaticValues.hoverProbeOriginLift, 0f);
        }
    }
}
