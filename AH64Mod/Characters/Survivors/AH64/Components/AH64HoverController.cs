using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Lives on the AH64 body prefab and owns the chopper's vertical axis. Hovers level at least a fixed
    /// height above terrain, easing down over lower ground; hold jump to climb toward a jump-count-scaled ceiling and descend to drop. Releasing both
    /// holds the chosen altitude until airtime runs out (classic controls settle on release). Vertical
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
    internal partial class AH64HoverController : MonoBehaviour
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

        /// <summary>True while the pilot is holding the descend input (see <see cref="AH64DescendInput"/>).</summary>
        public bool IsDescending { get; private set; }

        /// <summary>Seconds of airtime left above the resting height. See <c>AH64StaticValues.airtimeBase</c>.</summary>
        public float Airtime { get; private set; }

        /// <summary>True after airtime ran dry, until enough has refilled to climb again.</summary>
        public bool IsAirtimeLocked => climbLocked;

        /// <summary>
        /// Raise the held altitude briefly (Evasive Roll hop). Unlike a collective climb this settles back
        /// on its own, so a utility still reads as a hop. Clamped to the anchored ceiling.
        /// </summary>
        public void BumpTargetHeight(float amount)
        {
            if (amount <= 0f)
                return;

            bumpHeight = Mathf.Min(Mathf.Max(bumpHeight, TargetHeight) + amount, GetAnchoredCeiling());
            TargetHeight = Mathf.Max(TargetHeight, bumpHeight);
        }

        /// <summary>
        /// Distance from the feet to the terrain directly below, or <b>-1</b> when the probe found nothing
        /// within <c>hoverProbeDistance</c> — i.e. we are out over a pit or the void.
        /// </summary>
        public float GroundDistance { get; private set; }

        private CharacterMotor motor;
        private CharacterBody body;
        private InputBankTest inputBank;
        //where the pilot last left resting height; straying from it spends airtime faster
        private Vector3 hoverStart;
        private float timeWithoutGround;
        private float spawnGrace;
        private float lastValidGroundDistance;
        private float probeMissTimer;
        private float pitchWeightVelocity;
        private bool externalLaunchActive;
        private float externalLaunchAge;
        private bool lastDisableAirControl;
        private CapsuleCollider capsule;
        private Collider proxiedLaunchVolume;
        private readonly Collider[] launchVolumeHits = new Collider[16];
        //height the pilot chose with the collective; holds when both inputs are released
        private float pilotHeight;
        //The flown height as a world altitude, so terrain can't erode it. Terrain-relative storage
        //ratcheted down every time a clamp caught a rise in the ground. holdingAltitude marks a
        //collective climb, which holds level; the resting hover sinks gently over lower ground.
        private float pilotAltitude;
        private bool pilotAltitudeValid;
        private bool holdingAltitude;
        //utility hop on top of it, decaying back at the settle rate
        private float bumpHeight;
        private bool climbLocked;
        private bool hasAnchor;
        private float anchorGroundY;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            inputBank = GetComponent<InputBankTest>();
            capsule = GetComponent<CapsuleCollider>();
            //GlobalEventManager checks this on impact, including after jump-pad launches.
            if (body)
                body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
        }

        private void OnEnable()
        {
            MapZone.onBodyTeleportGlobal += OnMapZoneTeleport;
            On.RoR2.CharacterMotor.ApplyForceImpulse += OnApplyForceImpulse;
        }

        private void OnDisable()
        {
            MapZone.onBodyTeleportGlobal -= OnMapZoneTeleport;
            On.RoR2.CharacterMotor.ApplyForceImpulse -= OnApplyForceImpulse;
            SetHoverGranters(false);
        }

        //TeleportHelper keeps the downward part of the velocity, so a body teleported mid free fall
        //lands on the destination node at full speed and takes fall damage.
        private void OnMapZoneTeleport(CharacterBody teleported)
        {
            if (teleported != body || !motor || !motor.hasEffectiveAuthority)
                return;

            ResetAfterTeleport();
        }

        internal void ResetAfterTeleport()
        {
            timeWithoutGround = 0f;
            probeMissTimer = 0f;
            externalLaunchActive = false;
            lastValidGroundDistance = AH64StaticValues.hoverHeight;
            spawnGrace = AH64StaticValues.hoverSpawnGrace;
            hasAnchor = false;
            ResetAltitudeToRest();
            motor.velocity = Vector3.zero;
            ConfigureMotor();
        }

        /// <summary>Drops every altitude request back to the resting height.</summary>
        private void ResetAltitudeToRest()
        {
            float rest = GetRestHeight();
            TargetHeight = rest;
            pilotHeight = rest;
            holdingAltitude = false;
            pilotAltitudeValid = false;
            bumpHeight = rest;
            IsAscending = false;
        }

        private void Start()
        {
            //Start, not Awake: CharacterMotor.Awake recomputes useGravity/isFlying from these structs, so
            //writing them in Awake would be a coin flip on component order.
            ConfigureMotor();
            ResetAltitudeToRest();
            Airtime = GetAirtimeBudget();
            lastValidGroundDistance = AH64StaticValues.hoverHeight;
            GroundDistance = lastValidGroundDistance;
            spawnGrace = AH64StaticValues.hoverSpawnGrace;
        }

        private void ConfigureMotor()
        {
            if (!motor)
                return;

            SetHoverGranters(true);
            motor.airControl = AH64StaticValues.hoverAirControl;
        }

        /// <summary>
        /// Drops our flight and anti-gravity granters so the motor behaves like any vanilla survivor.
        /// Read-modify-write, so environmental anti-gravity a stage grants while we're in this mode
        /// (e.g. a slow-fall volume) is respected.
        /// </summary>
        private void UseVanillaPhysics(float airControl)
        {
            SetHoverGranters(false);
            motor.airControl = airControl;
        }

        private int ExtraJumps()
        {
            return body ? Mathf.Max(body.maxJumpCount, 1) - 1 : 0;
        }

        /// <summary>
        /// Resting altitude above terrain. Fixed regardless of extra jumps: with long airtime most time is
        /// spent above it anyway, and any lift put chests and ground items out of reach.
        /// </summary>
        public float GetRestHeight()
        {
            return AH64StaticValues.hoverHeight;
        }

        public float GetAirtimeBudget()
        {
            return AH64PlaytestConfig.Airtime + ExtraJumps() * AH64PlaytestConfig.AirtimePerExtraJump;
        }

        public float GetCeiling()
        {
            return GetRestHeight() + GetMaxRise();
        }

        public float GetMaxRise()
        {
            return AH64StaticValues.collectiveMaxRise
                + ExtraJumps() * AH64StaticValues.collectiveRisePerExtraJump;
        }

        private float FeetY()
        {
            return transform.position.y + motor.capsuleYOffset - motor.capsuleHeight * 0.5f;
        }

        /// <summary>
        /// The ceiling as a distance above the ground currently below, capped at the world height of the
        /// ground where the climb started plus the normal ceiling. Never below the resting height, so the
        /// hover can always hold rest over a ledge higher than the anchor.
        /// </summary>
        private float GetAnchoredCeiling()
        {
            float rest = GetRestHeight();
            float ceiling = GetCeiling();
            if (!hasAnchor || GroundDistance < 0f)
                return ceiling;

            float groundY = FeetY() - GroundDistance;
            return Mathf.Max(Mathf.Min(ceiling, anchorGroundY + ceiling - groundY), rest);
        }

        /// <summary>
        /// How much of a utility's scripted climb to allow: the full amount from rest, less whatever
        /// height has already been gained above the anchored rest altitude. Stops climb-then-utility
        /// (or utility chains) stacking past the anchored ceiling onto ledges.
        /// </summary>
        public float LimitUtilityClimb(float amount)
        {
            if (!hasAnchor || !motor)
                return amount;

            float gained = FeetY() - anchorGroundY - GetRestHeight();
            return Mathf.Clamp(amount - Mathf.Max(gained, 0f), 0f, amount);
        }

        private void UpdateAnchor()
        {
            if (!IsAtRestAltitude)
                return;

            anchorGroundY = FeetY() - GroundDistance;
            hasAnchor = true;
        }

        private void UpdateAirtime(bool jumpHeld, float deltaTime)
        {
            float budget = GetAirtimeBudget();
            if (AH64PlaytestConfig.ClassicAltitude)
            {
                Airtime = budget;
                climbLocked = false;
                return;
            }

            if (IsAtRestAltitude)
            {
                Airtime = Mathf.Min(budget, Airtime + AH64StaticValues.airtimeRefillRate * deltaTime);
                hoverStart = transform.position;
            }
            else if (!body || !AH64Buffs.killAirtimeBuff || !body.HasBuff(AH64Buffs.killAirtimeBuff))
            {
                Airtime = Mathf.Max(0f, Airtime - GetAirtimeDrainRate(jumpHeld) * deltaTime);
            }

            if (Airtime <= 0f)
                climbLocked = true;
            else if (climbLocked && Airtime >= Mathf.Min(AH64StaticValues.airtimeMinToClimb, budget))
                climbLocked = false;
        }

        /// <summary>
        /// Airtime spent per second above resting height. Holding station over one spot, turning and
        /// fighting, is cheap; pushing the stick, flying fast, straying from where the climb started
        /// and climbing each add to it. See <c>AH64StaticValues.airtimeDrainBase</c>.
        /// </summary>
        private float GetAirtimeDrainRate(bool jumpHeld)
        {
            float stick = 0f;
            if (inputBank)
            {
                Vector3 move = inputBank.moveVector;
                move.y = 0f;
                stick = Mathf.InverseLerp(AH64StaticValues.airtimeStickDeadzone, 1f, move.magnitude);
            }

            Vector3 velocity = motor.velocity;
            velocity.y = 0f;
            float speed = velocity.magnitude / Mathf.Max(AH64PlaytestConfig.BaseMoveSpeed, 1f);

            Vector3 offset = transform.position - hoverStart;
            offset.y = 0f;
            float stray = Mathf.Clamp01(offset.magnitude / AH64StaticValues.airtimeStationRadius);

            float rate = AH64StaticValues.airtimeDrainBase
                + AH64StaticValues.airtimeDrainStick * stick
                + AH64StaticValues.airtimeDrainSpeed * speed
                + AH64StaticValues.airtimeDrainStray * stray
                + (jumpHeld && !climbLocked ? AH64StaticValues.airtimeDrainClimb : 0f);
            return Mathf.Min(rate, AH64StaticValues.airtimeDrainMax);
        }

        /// <summary>
        /// Runs the altitude controller for one physics step and writes the result into
        /// <c>motor.moveDirection.y</c>.
        /// </summary>
        public void ApplyHover(bool jumpHeld, bool descendHeld, float deltaTime)
        {
            if (!motor || !body)
                return;

            IsDescending = descendHeld;
            ResolveGroundDistance(deltaTime);
            UpdateAnchor();
            UpdateCollectiveTapArm(jumpHeld);

            if (spawnGrace > 0f)
                spawnGrace -= deltaTime;

            if (ApplyEquipmentFlight(jumpHeld, descendHeld, deltaTime))
                return;

            if (YieldToHeadstompSlam())
            {
                UpdateAscentPitch(deltaTime);
                return;
            }

            if (!externalLaunchActive)
                TriggerLaunchVolumesBelow();

            if (UpdateExternalLaunch(deltaTime))
            {
                UpdateAscentPitch(deltaTime);
                return;
            }

            if (GroundDistance < 0f && spawnGrace <= 0f)
            {
                //Nothing within probe range: fall under vanilla gravity. See voidRecoveryDelay.
                UseVanillaPhysics(AH64StaticValues.hoverAirControl);
                ResetAltitudeToRest();
                UpdateAscentPitch(deltaTime);

                timeWithoutGround += deltaTime;
                if (timeWithoutGround >= AH64StaticValues.voidRecoveryDelay
                    && motor.velocity.y < -AH64StaticValues.voidRecoveryMinFallSpeed)
                    RecoverFromVoid();
                return;
            }

            ConfigureMotor();

            UpdateAirtime(jumpHeld, deltaTime);
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
        /// Detects and rides out an external launch (jump pad, knock-up or pull). Returns true while the hover
        /// must keep its hands off the vertical axis. See <c>AH64StaticValues.externalLaunchMinExcessSpeed</c>.
        /// </summary>
        private bool UpdateExternalLaunch(float deltaTime)
        {
            //Edge-triggered: the flag stays set until the next collision, so a level check would
            //re-enter the launch the tick after we hand back.
            bool airControlFlag = motor.disableAirControlUntilCollision;
            bool flagRaised = airControlFlag && !lastDisableAirControl;
            lastDisableAirControl = airControlFlag;

            //VerticalLift clears useCustomGravity on exit. Keep yielding for the full lift,
            //including slow lifts that outlast the launch safety timeout.
            bool lifted = motor.useCustomGravity && motor.CustomGravity > 0f;

            if (!externalLaunchActive)
            {
                bool launchedFast = motor.velocity.y
                    > AH64StaticValues.hoverMaxClimbSpeed + AH64StaticValues.externalLaunchMinExcessSpeed;
                if (!flagRaised && !launchedFast && !lifted)
                    return false;

                externalLaunchActive = true;
                externalLaunchAge = 0f;
                timeWithoutGround = 0f;
                IsAscending = false;
            }

            externalLaunchAge = lifted ? 0f : externalLaunchAge + deltaTime;

            bool pastMinimum = externalLaunchAge >= AH64StaticValues.externalLaunchMinDuration;
            bool landed = motor.isGrounded;
            float fallSpeed = Mathf.Max(-motor.velocity.y, 0f);
            float brakeDistance = fallSpeed * fallSpeed
                / (2f * Mathf.Max(motor.acceleration * AH64StaticValues.hoverAirControl, 1f));
            bool fallingNearGround = motor.velocity.y <= 0f && GroundDistance >= 0f
                && GroundDistance <= Mathf.Max(brakeDistance, AH64StaticValues.externalLaunchHandbackHeight);

            if (!lifted && ((pastMinimum && (landed || fallingNearGround))
                || externalLaunchAge >= AH64StaticValues.externalLaunchMaxDuration))
            {
                externalLaunchActive = false;
                ResetAltitudeToRest();
                //Otherwise CharacterMotor keeps zero air acceleration until we touch something and the
                //hover can't brake the fall. ServoDash and SmokeBackflip clear it on exit for the same reason.
                motor.disableAirControlUntilCollision = false;
                lastDisableAirControl = false;
                return false;
            }

            YieldToExternalMotion();
            return true;
        }

        /// <summary>
        /// Runs the pad's own trigger callbacks for a JumpVolume or BounceVolume just below the capsule,
        /// so the pad applies its launch exactly as it would to a survivor standing in it. See
        /// <c>AH64StaticValues.launchVolumeReach</c>.
        /// </summary>
        private void TriggerLaunchVolumesBelow()
        {
            if (!capsule)
                return;

            float radius = motor.capsuleRadius;
            Vector3 feet = transform.position
                + Vector3.up * (motor.capsuleYOffset - motor.capsuleHeight * 0.5f);
            Vector3 top = feet + Vector3.up * radius;
            //the reach is measured from the default rest height; extra jumps lift the chopper above it
            float reach = AH64StaticValues.launchVolumeReach + GetRestHeight() - AH64StaticValues.hoverHeight;
            Vector3 bottom = feet + Vector3.up * (radius - reach);

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, launchVolumeHits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            Collider found = null;
            for (int i = 0; i < count; i++)
            {
                Collider hit = launchVolumeHits[i];
                if (!hit || !hit.isTrigger)
                    continue;

                JumpVolume jumpVolume = hit.GetComponent<JumpVolume>();
                BounceVolume bounceVolume = jumpVolume ? null : hit.GetComponent<BounceVolume>();
                if (!jumpVolume && !bounceVolume)
                    continue;

                //Already touching it for real: Unity is calling the trigger callbacks itself.
                if (Physics.ComputePenetration(capsule, capsule.transform.position, capsule.transform.rotation,
                        hit, hit.transform.position, hit.transform.rotation, out _, out _))
                    continue;

                found = hit;
                if (jumpVolume)
                {
                    if (proxiedLaunchVolume != hit)
                        jumpVolume.OnTriggerEnter(capsule);
                    jumpVolume.OnTriggerStay(capsule);
                }
                else
                {
                    bounceVolume.OnTriggerStay(capsule);
                }
                break;
            }

            proxiedLaunchVolume = found;
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
            float rest = GetRestHeight();
            float settleRate = AH64StaticValues.collectiveSettleRate;
            bool classic = AH64PlaytestConfig.ClassicAltitude;
            bool climbing = jumpHeld && (classic || !climbLocked);
            bool settling = !climbing && !descendHeld && (classic || climbLocked);
            bool hasGround = GroundDistance >= 0f;
            bool worldHold = !classic && hasGround;
            float groundY = hasGround ? FeetY() - GroundDistance : 0f;
            //A held altitude is capped only at the world height above the anchor. The terrain-relative
            //cap pulled a held climb down over every dip in the ground; airtime bounds the rest.
            float ceiling = worldHold && hasAnchor
                ? Mathf.Max(anchorGroundY + GetCeiling() - groundY, rest)
                : GetAnchoredCeiling();
            float lead = GroundDistance + AH64StaticValues.collectiveLead;

            if (worldHold)
            {
                if (!pilotAltitudeValid)
                {
                    pilotAltitude = groundY + Mathf.Max(pilotHeight, rest);
                    pilotAltitudeValid = true;
                }
                pilotHeight = pilotAltitude - groundY;
            }

            //Inputs act on the height actually being flown, not on a held value the clamps are
            //currently overriding (a hill pushing up from below, a drop beyond the ceiling).
            if (climbing || descendHeld || settling)
                pilotHeight = Mathf.Clamp(pilotHeight, rest, ceiling);

            if (climbing)
            {
                pilotHeight = Mathf.MoveTowards(pilotHeight, ceiling,
                    AH64StaticValues.collectiveClimbRate * deltaTime);
                //Lead clamp: keep the target ahead of the feet enough for PD to accelerate, but
                //never bank unreachable altitude under a low ceiling.
                if (hasGround)
                    pilotHeight = Mathf.Min(pilotHeight, lead);
            }
            else if (descendHeld)
            {
                pilotHeight = Mathf.MoveTowards(pilotHeight, rest,
                    settleRate * AH64StaticValues.collectiveCrouchSettleMult * deltaTime);
            }
            else if (settling)
            {
                //classic release-to-settle, or out of airtime
                pilotHeight = Mathf.MoveTowards(pilotHeight, rest, settleRate * deltaTime);
            }
            else if (worldHold && !holdingAltitude)
            {
                //Resting hover holds its altitude too: rising ground lifts it at once, falling ground
                //lets it ease down. Off an edge that is a slow sink until airtime runs out.
                pilotHeight = pilotHeight < rest
                    ? rest
                    : Mathf.MoveTowards(pilotHeight, rest, AH64StaticValues.restSinkRate * deltaTime);
            }

            if (worldHold)
            {
                if (climbing || descendHeld || settling)
                    holdingAltitude = pilotHeight > rest + 0.01f;
                pilotAltitude = groundY + pilotHeight;
            }

            bumpHeight = Mathf.MoveTowards(bumpHeight, rest, settleRate * deltaTime);
            if (hasGround)
                bumpHeight = Mathf.Min(bumpHeight, lead);
            bumpHeight = Mathf.Clamp(bumpHeight, rest, ceiling);
            TargetHeight = Mathf.Max(Mathf.Clamp(pilotHeight, rest, ceiling), bumpHeight);

            IsAscending = climbing && TargetHeight > rest + 0.25f
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
            AscentPitchWeight = 0f;
            pitchWeightVelocity = 0f;

            if (!Run.instance || !body)
                return;

            //idealMaxDistance 0 skips Approximate placement: this is the Ground-graph node nearest to
            //where the chopper is right now, or the current position if no node could be placed.
            Vector3 destination = Run.instance.FindSafeTeleportPosition(body, null, 0f, 0f);
            TeleportHelper.TeleportBody(body, destination, false);
            ResetAfterTeleport();

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
