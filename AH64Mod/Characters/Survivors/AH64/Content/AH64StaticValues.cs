namespace AH64.Survivors
{
    /// <summary>
    /// Single tuning table for the AH-64 kit. Both the skill states and the language tokens read
    /// from here, so the numbers printed on the skill tooltips can never drift from the numbers we
    /// actually fire. Tune the character here, not in the individual states.
    /// </summary>
    public static class AH64StaticValues
    {
        #region movement - hover
        //The AH-64 never touches the ground. CharacterMotor already has everything needed for this:
        //AH64HoverController turns on flight (which makes moveDirection.y a real vertical throttle) and
        //anti-gravity, and AH64Main drives moveDirection.y off the altitude error every FixedUpdate.
        //
        //Deliberately NOT free flight. Altitude is a band measured against whatever terrain is underneath
        //you, so there is no way to park above an arena and out-range the encounter.

        //resting altitude, from the chopper's feet to the ground directly below it
        //Raised from 3 in 1.2. Ground items are still collected through AH64PickupReach.
        public const float hoverHeight = 3.75f;

        //How hard an altitude error is corrected, in units/sec of climb per unit of error (P term).
        //Paired with hoverDamping — P alone overshoots or feels sluggish; PD is the standard hover
        //stabilizer (mass-spring-damper / rotorcraft altitude hold).
        public const float hoverStiffness = 6.5f;
        //Damps vertical velocity so we ease into the target instead of bouncing past it.
        //Rule of thumb: ~0.15–0.25 × stiffness for a critically-damped-ish feel on CharacterMotor.
        public const float hoverDamping = 1.15f;

        //caps on that correction, so cresting a cliff edge doesn't fire the chopper upward
        public const float hoverMaxClimbSpeed = 16f;
        public const float hoverMaxDescendSpeed = 14f;

        //how far down we look for terrain. Past this we are over the void - see below.
        public const float hoverProbeDistance = 60f;
        //the probe starts this far above the feet, so sitting on a lip doesn't start the ray inside geometry
        public const float hoverProbeOriginLift = 0.5f;

        //Off the edge of the map, or over any drop deeper than the probe. The chopper hands the motor
        //back to vanilla gravity and falls like any other survivor. Holding altitude meant it never left
        //the MapZone bounds volume (OnTriggerExit), so it was never recovered. The earlier fixed 12 u/s
        //descent broke stages whose intended route is a long drop: Solutional Haunt opens with a shaft
        //far deeper than the probe, slowed only near the bottom, and the old 3 s failsafe below teleported
        //the chopper back to the start every time (Solutional Haunt report, issue 3).
        //
        //Failsafe, so the chopper can never be permanently lost on a stage whose bounds volume doesn't
        //catch it. Only fires after this long with no terrain beneath us - over a thousand units at
        //vanilla gravity, far past any real stage drop, so MapZone always gets first crack.
        public const float voidRecoveryDelay = 12f;
        //...and only while falling faster than this. SlowFallZone (the Solutional Haunt shaft) holds a
        //falling body at its speedLimit of -25, so a guided descent can never trip the failsafe; a real
        //void fall passes 40 u/s about 1.3 s after leaving the ground.
        public const float voidRecoveryMinFallSpeed = 40f;

        //External launches: jump pads (JumpVolume, including the moon pillar pads to Mithrix), False
        //Son's pads, and big knock-ups. JumpVolume.OnTriggerStay sets velocity to a jumpVelocity tuned as
        //a gravity arc and raises disableAirControlUntilCollision, but CharacterMotor.PreMove only honours
        //that flag while gravity is on. With the hover's anti-gravity the PD hold braked the launch at full
        //acceleration and floated the chopper back down (the jump-pad reports, issues 1, 2, 4 and 5).
        //While launched, the motor runs as a plain vanilla character, so the pad's no-air-control arc
        //applies exactly as for any survivor; the hover takes over again on landing or near the ground.
        //
        //Upward speed beyond hoverMaxClimbSpeed that counts as a launch when the pad does not also set
        //disableAirControlUntilCollision. Our own utilities peak around 13-17 u/s, so they never trip it.
        public const float externalLaunchMinExcessSpeed = 6f;
        //Ignore the landing check this long after launch, so the frame we leave the pad doesn't end it.
        public const float externalLaunchMinDuration = 0.3f;
        //Past apex, the hover takes back over once the ground is within the distance it needs to brake
        //the fall at full acceleration, and never later than this. Handing back at the apex instead
        //would bleed off the pad's horizontal velocity and drop the chopper short of the target.
        public const float externalLaunchHandbackHeight = 8f;
        //Safety cap, in case a launch never lands. The moon pillar pads' own flight time is 10 s.
        public const float externalLaunchMaxDuration = 15f;
        //Moon pillar pads are 4.34 u tall boxes centred on their transform, and the hover holds the
        //capsule's bottom hoverHeight above the floor, so depending on where the box sits the chopper can
        //float over the trigger without touching it. JumpVolumes and BounceVolumes this far below the
        //capsule are triggered as if touched.
        public const float launchVolumeReach = hoverHeight + 1f;
        //Vanilla CharacterMotor default. hoverAirControl (1.0) would let neutral stick brake the pad's
        //horizontal velocity at full acceleration and drop the chopper short of the target.
        public const float externalLaunchAirControl = 0.25f;

        //Collective: hold jump to climb toward a jump-count-scaled ceiling, hold descend (B on a gamepad,
        //a config key on keyboard — AH64DescendInput) to come down, release both to hold the current
        //height. Vanilla jump is suppressed — a hovering body never grounds, so jumpCount would never
        //reset; maxJumpCount raises the ceiling and the airtime instead.
        //
        //Hold-to-settle (the 1.1 scheme) kept the right thumb on jump for the whole climb, which on a
        //gamepad means off the aim stick (playtest 2026-09-29). Holding height on release frees it.
        //
        //Base ceiling rise above the resting height (1 jump). Each extra jump (Hopoo Feather, etc.) adds
        //collectiveRisePerExtraJump.
        public const float collectiveMaxRise = 12f;
        public const float collectiveRisePerExtraJump = 3f;

        //Airtime: seconds you may spend above the resting height before the collective settles you back
        //on its own. Replaces "release to settle" as the thing keeping the chopper near the ground, which
        //the encounter balance depends on (see AGENTS: low hover with temporary altitude). Defaults for
        //the player-facing config in AH64PlaytestConfig.
        //Long on purpose: altitude is an "eventually you come down" limit, not a short hop budget.
        public const float airtimeBase = 20f;
        //Kept small: the base tank and kill pauses already carry most of the flight time.
        public const float airtimePerExtraJump = 2.5f;
        //Seconds of airtime regained per second spent at resting height: a full 20 s refills in 2 s,
        //so touching back down is a quick pit stop rather than a wait.
        public const float airtimeRefillRate = 10f;
        //Airtime seconds spent per second above resting height, summed from how the pilot is flying.
        //Holding station (turning and fighting over one spot) costs only the base: 100 s from a full tank.
        //Small repositioning near the start point lands around 0.35-0.5 (40-55 s). Flying off at cruise
        //pays stick + speed at once and the stray term within a couple of seconds: capped at 1.25, 16 s.
        public const float airtimeDrainBase = 0.2f;
        //full stick; input under the deadzone (stick drift) is free
        public const float airtimeDrainStick = 0.25f;
        public const float airtimeStickDeadzone = 0.15f;
        //per base cruise speed of horizontal travel
        public const float airtimeDrainSpeed = 0.35f;
        //full once this far (horizontal) from where the pilot left resting height
        public const float airtimeDrainStray = 0.4f;
        public const float airtimeStationRadius = 20f;
        //while holding the collective to climb
        public const float airtimeDrainClimb = 0.35f;
        public const float airtimeDrainMax = 1.25f;
        //After running dry, climbing unlocks again once this much has refilled, so an empty tank can't
        //stutter up and down a fraction of a unit at a time. 0.2 s of refill at the rate above.
        public const float airtimeMinToClimb = 2f;
        //A kill pauses the drain this long. The pause can re-trigger only once the cooldown (measured
        //from the previous trigger) has passed, so a crowd being mown down halves the drain at most
        //instead of freezing it.
        public const float airtimeKillPause = 0.75f;
        public const float airtimeKillPauseCooldown = 1.5f;
        //How fast the target altitude rises while jump is held / sinks on release.
        public const float collectiveClimbRate = 14f;
        public const float collectiveSettleRate = 7f;
        public const float collectiveCrouchSettleMult = 2.2f;
        //the target is never allowed to run further than this above where you actually are, so holding
        //the collective while pinned under a ceiling can't bank altitude that fires you skyward once clear.
        //Wider than the old 2.5 during climb so PD can actually accelerate; settle uses the same value.
        public const float collectiveLead = 5f;
        //The resting hover holds level and eases down over lower ground at this rate, so it rides
        //over bumps and dips instead of tracing them. Matches a 25% grade at base speed; steeper
        //ground or an edge leaves it high enough to spend airtime.
        public const float restSinkRate = 2.5f;

        //Climbs are capped at the world height of the ground where the climb started plus the ceiling,
        //not just the ground currently below. Otherwise hugging a cliff face kept raising the ceiling
        //(and the utilities' height bump with it), so the chopper could stair-step up anything in mid-air
        //(playtest 2026-09-29). Landing at resting height on a ledge re-anchors, like a vanilla jump.

        //Nose attitude cues (degrees) driven by climb intent — negative = nose up in model lean Euler.
        public const float collectiveAscentNosePitch = -16f;
        public const float collectiveDescentNosePitch = 8f;
        public const float collectivePitchSmoothTime = 0.18f;

        //legacy aliases kept so older comments / parked code still compile-read cleanly
        public const float collectiveAscentLead = collectiveLead;
        public const float collectiveAscentMaxClimbSpeed = hoverMaxClimbSpeed;
        public const float collectiveAscentCompleteSlack = 0.6f;
        public const float collectiveJumpFloatSettleMult = 1f;
        public const float collectiveAscentPitchFadeSpeed = 4f;

        //Ignore empty ground probes briefly after spawn (pod eject / teleport settle).
        public const float hoverSpawnGrace = 0.75f;
        //When the ray misses for a frame over a narrow gap, keep using the last valid ground distance
        //instead of treating it as void. Playtest range: ~0.2–0.5s — longer masks more gap dips, shorter
        //is stricter over pits.
        public const float hoverProbeMissHold = 0.35f;

        //CharacterMotor multiplies acceleration by airControl whenever the body isn't grounded, and a
        //hovering body is never grounded - left at the 0.25 default the chopper handles like a barge.
        //Playtest: 0.75–1.0 if strafing feels skatey; 1.0 is the current weight target.
        public const float hoverAirControl = 1f;

        //Interactor.maxInteractionDistance is both the aim-ray length and the overlap sphere around
        //aimOrigin. Commando's cloned 1u never reaches a chest from hoverHeight — the feet sit at 3.75u
        //and aimOrigin is another ~2.5u above that. 12u covers resting hover and a little collective.
        public const float interactionDistance = 12f;
        //Walk-over pickups need the body collider inside the item's trigger, which a hover never is.
        //Items this far below the feet, plus the resting height, are collected as if touched.
        public const float pickupReachMargin = 0.75f;
        public const float pickupReachInterval = 0.1f;
        #endregion

        #region presentation - flight lean / rotor wash
        //visual only — applied to the model child so CharacterDirection still owns yaw on ModelBase
        public const float leanMaxPitch = 14f;
        public const float leanMaxRoll = 20f;
        //Blend of velocity-based lean vs stick anticipation (0 = only velocity, 1 = only input).
        //Helo tutorials / arcade controllers lean into the stick before speed builds — that reads as weight.
        public const float leanInputBlend = 0.55f;
        //degrees of attitude per unit/sec of velocity in that axis
        public const float leanPitchPerSpeed = 0.5f;
        public const float leanRollPerSpeed = 0.85f;
        public const float leanClimbPitchPerSpeed = 0.4f;
        //degrees of attitude at full stick (moveVector magnitude 1)
        public const float leanPitchPerInput = 12f;
        public const float leanRollPerInput = 16f;
        //higher = snappier; playtest ~5–10. Slightly softer so PD climb + lean don't fight visually.
        public const float leanSmoothing = 7f;
        //Sprint holds this much extra nose-down, blended in over ~0.35 s and out over ~0.5 s.
        public const float sprintExtraPitch = 5f;
        public const float sprintLeanInRate = 2.8f;
        public const float sprintLeanOutRate = 2f;
        //Surge dip / braking flare: degrees per u/s between live and lagged forward speed. A full stop
        //from cruise flares about 7 degrees nose-up for roughly half a second.
        public const float surgeLagSeconds = 0.3f;
        public const float surgePitchPerSpeed = 0.7f;
        public const float surgeMaxPitch = 4f;
        public const float flareMaxPitch = 7f;

        //Dust kicked under the rotor disc when hugging the ground. Hovering rotors still move air, so
        //idle wash runs at a restrained cadence and blends up to the moving cadence with flight effort.
        //Disc tip span grew 2.20 → 3.00 (~1.36×); wash footprint tracks that so dust isn't under a stub disc.
        public const float rotorWashMaxHeight = 8f;
        public const float rotorWashInterval = 0.18f;
        public const float rotorWashIdleInterval = 0.45f;
        public const float rotorWashFullSpeed = 10f;
        public const float rotorWashScale = 1.35f;

        //Pilot bed is 2D; remote aircraft retain distance attenuation. Retune by listening
        //at this baseline after fixing spatial attenuation, not by amplifying to clipping.
        public const float rotorHoverVolume = 0.3825f;
        //About -1.94 dB on the dedicated rotor emitter; saved user volume/boost stay intact.
        public const float rotorMixTrim = 0.8f;
        //Make the former subtle movement slider audible with signed directional response.
        public const float rotorDirectionalPitchScale = 4f;
        //Transient response has its own headroom after normal travel reaches full load.
        public const float rotorManeuverGainDb = 1.5f;
        public const float rotorManeuverToneOpening = 6f;
        public const float rotorResponseAttackScale = 0.65f;
        public const float rotorResponseReleaseScale = 1.6f;
        public const float rotorSpecialResponse = 0.3f;
        public const float rotorAccelerationResponse = 0.2f;
        public const float rotorTurnResponse = 0.25f;
        public const float rotorFullTurnRate = 120f; //degrees/sec, gated by actual travel
        public const float rotorMotionSampleSmoothing = 0.12f;
        //Only the dedicated wind-up/down emitter is reduced (~4.4 dB); shots stay unchanged.
        public const float gatlingSpoolVolume = 0.6f;
        public const float rotorToneCutoff = 5000f;
        //Keep the player's camera (~14 units behind) inside the full-volume radius.
        public const float rotorHoverMinDistance = 20f;
        public const float rotorHoverMaxDistance = 70f;
        //Camera lag must not add Doppler wobble to the deliberately restrained pitch change.
        public const float rotorHoverDoppler = 0f;
        public const float rotorLayerFadeTime = 0.6f;
        public const float rotorHoverPitch = 1f;
        public const float rotorFullLoadPitch = 1.015f;
        public const float rotorFullLoadGainDb = 2f;

        //rotor blur discs (RotorBlurMain/RotorBlurTail on mdlAH64). Effort is horizontal speed over
        //rotorBlurFullSpeed, plus a flat boost while the collective is held; the discs activate above
        //rotorBlurMinEffort and their alpha scales to rotorBlurMaxAlpha at full effort. The discs are
        //deliberately NOT CharacterModel renderers, so the alpha write can never be stomped by
        //UpdateRendererMaterials — see AH64FlightVisuals.
        public const float rotorBlurFullSpeed = 14f;
        public const float rotorBlurCollectiveBoost = 0.55f;
        public const float rotorBlurMinEffort = 0.18f;
        //Alpha Blended tint is multiplied by 2 in the shader, so 0.17 reads as ~0.34
        public const float rotorBlurMaxAlpha = 0.17f;
        public const float rotorBlurSmoothing = 5f;

        //Rotor RPM presentation (AH64RotorSpin). Rotors start stopped inside the drop pod and spool up
        //on exit; a dead aircraft winds down while its body lingers (GenericCharacterDeath keeps it
        //1-4 s). Collective and hard manoeuvres run slightly fast — enough to notice, not to strobe.
        public const float rotorSpoolUpSeconds = 1.7f;
        public const float rotorSpinDownSeconds = 2.6f;
        public const float rotorEffortRpmBoost = 0.12f;
        //Character select idles at ground RPM: readable blade motion without wagon-wheel strobing.
        public const float rotorLobbyIdleFraction = 0.35f;
        //Seconds for a full spool in character select, so reaching idle takes about 1.4 s: an engine
        //start when you pick the AH-64, not a snap.
        public const float rotorLobbySpoolUpSeconds = 4f;
        //The rotor sound follows the blades: silent in the drop pod, pitching up as they spool, and
        //winding down with them on death. Pitch at a standstill, as a multiple of flight pitch.
        public const float rotorAudioSpoolPitchFloor = 0.7f;
        //Character-select idle (AH64LobbyRotorAudio), relative to the flight rotor volume, before the
        //spool scales it. At the lobby idle spool (eased 0.28) this is about 40% of the hover volume.
        public const float rotorLobbyAudioGain = 1.5f;

        //Idle hover drift so a stationary aircraft doesn't look frozen. Fades out above
        //idleSwayMaxSpeed, where flight lean takes over. Periods are incommensurate so it never loops visibly.
        public const float idleSwayRollDegrees = 1.3f;
        public const float idleSwayPitchDegrees = 0.8f;
        public const float idleSwayRollPeriod = 4.3f;
        public const float idleSwayPitchPeriod = 6.1f;
        public const float idleSwayMaxSpeed = 4f;

        //Chin turret procedural aim (see AH64ChinTurret). Yaw limits match Prefabs.SetupAimAnimator
        //(±80°) so the gun cone matches what RoR2's aim clips assume; pitch is the real M230 envelope.
        public const float chinTurretTurnSpeed = 200f;
        public const float chinTurretMaxPitch = 35f;
        public const float chinTurretMinPitch = -30f;
        public const float chinTurretMaxYaw = 80f;
        public const float chinTurretMinYaw = -80f;
        public const float chinTurretKickDistance = 0.06f;
        public const float chinTurretKickRecoverPerSecond = 0.55f;
        #endregion

        #region presentation - crash, damage smoke, recoil kick, turn bank
        //Death is a crash rather than a vanish: the tail lets go, the airframe spins up and noses down,
        //trails smoke and explodes on impact (AH64Death). Flight stays on during the fall so the descent
        //is scripted. A plain gravity drop from resting height took under half a second and never read
        //as a crash. Starting from a standstill, the tail spins up before the drop takes hold: impact
        //comes about a second after dying at resting height (the ground check fires ~2.9 units down).
        public const float crashFallSpeedStart = 0f;
        public const float crashFallAccel = 5.5f;
        public const float crashFallSpeedMax = 26f;
        //share of horizontal speed kept as the target each tick, so the wreck drifts on and slows
        public const float crashHorizontalCarry = 0.85f;
        //no impact before this: a body destroyed sooner can drop its last network messages
        public const float crashMinDuration = 0.5f;
        //explodes in the air after this, e.g. over a pit
        public const float crashMaxDuration = 4f;
        //yaw rate ramps from start to max (degrees per second) over the ramp
        public const float crashSpinStart = 120f;
        public const float crashSpinMax = 620f;
        public const float crashSpinRampSeconds = 0.9f;
        public const float crashNoseDownDegrees = 24f;
        public const float crashWobbleDegrees = 9f;
        public const float crashSmokeInterval = 0.06f;

        //Below this share of health the engines trail smoke, thicker and more often as health falls.
        public const float damageSmokeHealthFraction = 0.35f;
        public const float damageSmokeIntervalMax = 0.45f;
        public const float damageSmokeIntervalMin = 0.14f;

        //Airframe kick when a weapon fires, in degrees: nose-up pitch and roll away from the firing side.
        //Springs back at kickRecovery per second. Presentation only, on every client running the state.
        public const float kickRecovery = 9f;
        public const float kickMax = 6f;
        public const float kickCannonPitch = 2.4f;
        public const float kickChaingunPitch = 0.25f;
        public const float kickGatlingPitch = 0.14f;
        public const float kickHydraPitch = 0.35f;
        public const float kickHydraRoll = 0.6f;
        public const float kickHellfirePitch = 1.4f;
        public const float kickHellfireRoll = 1.6f;
        public const float kickLongbowPitch = 0.8f;
        public const float kickLongbowRoll = 1.1f;
        //Hit jolt: a hit taking at least this share of health plus shield knocks the airframe, reaching
        //the full kickMax at hitJoltFullShare.
        public const float hitJoltMinShare = 0.06f;
        public const float hitJoltFullShare = 0.3f;

        //Coordinated turn: bank into a yaw while moving forward, as a helicopter does, instead of pivoting
        //flat. Degrees of roll per degree-per-second of yaw, scaled up to full at turnBankFullSpeed.
        public const float turnBankPerYawRate = 0.05f;
        public const float turnBankMax = 12f;
        public const float turnBankFullSpeed = 8f;
        public const float turnRateSmoothing = 6f;
        #endregion

        #region passive - Fire Control Radar
        //Paints the single strongest enemy in a wide bubble (highest maxHealth; elite then nearest on
        //ties). The radome brightens briefly on every retarget so it reads as actively searching.
        public const float radarSearchRadius = 90f;
        public const float radarRetargetInterval = 8f;
        //Radome glow on each scan: peak emission power and fade time.
        public const float radarScanFlashPower = 1.5f;
        public const float radarScanFlashDuration = 1.1f;
        //Ping spawned on the painted target when lock is acquired / refreshed.
        public const float radarPaintPingScale = 1.6f;
        //Mast dome spin (deg/sec). Faster while a target is painted.
        public const float radarDomeIdleSpinSpeed = 40f;
        public const float radarDomePaintedSpinSpeed = 140f;
        //Soft emission on the radome — kept low so the bubble reads as a grey dome, not a lamp.
        public const float radarDomeGlowIdle = 0.25f;
        public const float radarDomeGlowPainted = 0.55f;

        //damage multiplier vs the painted target (1.12 = +12%)
        public const float radarPaintedDamageMult = 1.12f;
        //move-speed while facing the paint (dot of aim vs to-target above facingDotMin)
        public const float radarFacingMoveSpeedMult = 0.15f;
        public const float radarFacingDotMin = 0.65f;
        //flat armor while within close range of the paint
        public const float radarCloseArmor = 60f;
        public const float radarCloseRange = 25f;
        #endregion

        //Shared across all three primary variants (set on each SkillDef in AH64Survivor.cs). RoR2's own
        //SkillDef.GetRechargeInterval already supports this natively via attackSpeedBuffsRestockSpeed —
        //no custom component needed. 0.35 is a partial pass-through: fire-rate items like Syringe now
        //also speed the reload a bit, so a drum you empty into an item-boosted attack speed doesn't leave
        //you sitting through the same multi-second wait a build with zero attack speed would. Not 1.0 —
        //full pass-through would make attack speed strictly better than dedicated cooldown reduction for
        //these skills, which isn't the balance intent.
        public const float primaryReloadAttackSpeedMultiplier = 0.35f;
        //Eclipse Lite pays barrier per cooldown. A primary reload only comes round after you stop
        //firing, so at one restock it paid out a fraction of what a cooldown survivor gets. Playtested
        //too weak on the XM301; counting a reload as four cooldowns brings it in line.
        public const int primaryReloadBarrierRestocks = 4;

        #region primary - M230 chain gun
        //HITSCAN, not a projectile. The old wrist blaster fired a real travelling bolt because a laser
        //bolt is supposed to be an object you can watch cross the arena - three rounds of tuning proved
        //a Tracer could never sell that. A 30mm cannon round is the opposite case: it IS instant at these
        //ranges, which is exactly what every vanilla gun (Commando, MUL-T, Captain) models with a
        //BulletAttack plus a tracer. So the tracer here is the correct visual, not a compromise, and the
        //chin turret's off-centre parallax stops mattering because the shot resolves in one frame.

        //~667 rpm at base attack speed. The real M230 runs 625 - close enough that the ear reads it as
        //a chain gun rather than as a fast pistol.
        public const float chaingunBaseDuration = 0.09f;
        //Burst damage is deliberately unchanged from the blaster it replaces:
        //  0.55 / 0.09 = 6.1 coeff/sec, against the blaster's 1.5 / 0.25 = 6.0.
        //Three times the rounds for a third of the damage each. Texture change, not a buff.
        //Small HE splash below is the pack-clearing bump — not a direct coeff raise.
        //+12% across all three primaries/secondary/specials after playtest 2026-08-04 — first
        //encounters were running long relative to the kit's fragility. Slight, not a rework.
        public const float chaingunDamageCoefficient = 0.62f;
        public const float chaingunRange = 220f;
        //per-round knockback. Heavier than the early 60 so each round thumps without pinballing at 11/sec.
        public const float chaingunForce = 100f;
        public const float chaingunBulletRadius = 0.4f;
        //camera kick per round. Was 0.5 and disappeared into the fire rate; 1.0 still tracks.
        public const float chaingunRecoil = 1.0f;

        //per-shot camera shake, carried by our cloned muzzle flash (vanilla Muzzleflash1 is shared
        //with Commando, so the ShakeEmitter lives on a clone). Tiny on purpose: at 11 rounds/sec a
        //full drum is ~2.7s of continuous shake, so anything readable per-shot blurs the screen held.
        public const float chaingunShakeDuration = 0.08f;
        public const float chaingunShakeRadius = 12f;
        public const float chaingunShakeAmplitude = 0.5f;
        public const float chaingunShakeFrequency = 22f;

        //30mm HE tip: BlastAttack at the impact point. A 6u / 0.30 first pass is large enough to read
        //on a pack while Linear falloff keeps the edge gentle. Proc remains ZERO — at 11 rps any splash
        //proc would quietly become the best on-hit platform in the game.
        public const float chaingunSplashDamageCoefficient = 0.34f;
        public const float chaingunSplashRadius = 6f;
        public const float chaingunSplashProcCoefficient = 0f;
        public const float chaingunSplashForce = 40f;
        //EffectData.scale alone is weak on world-space particles. Chaingun splash is a scaled Hellfire
        //warhead clone — particleMult is relative to that full boom (~0.4 ≈ half-Lemurian read).
        public const float chaingunSplashVfxScale = 2.5f;
        public const float chaingunSplashParticleMult = 0.42f;

        //PROC COEFFICIENT IS THE DANGEROUS NUMBER HERE. It applies per hit, and this fires roughly three
        //times as often as the blaster did - left at the blaster's 1.0 the chain gun would quietly be
        //the best on-hit-item platform in the game, with nothing in any log to explain why.
        //  11 shots/sec * 0.35 = 3.9 procs/sec, matching the blaster's 4/sec * 1.0.
        //Any change to chaingunBaseDuration has to be paid for here.
        public const float chaingunProcCoefficient = 0.35f;

        //A chin turret hosing rounds is never pinpoint, so unlike the blaster this starts loose. Bloom
        //per shot is much SMALLER than the blaster's 0.35 for the same reason the damage is - there are
        //three times as many shots feeding it.
        public const float chaingunMinSpread = 0.5f;
        public const float chaingunMaxSpread = 2.25f;
        public const float chaingunSpreadBloom = 0.08f;

        //Ammo drum. Still modelled as skill stocks, so the HUD renders remaining rounds and the reload
        //sweep for free. The SkillDef hands back the WHOLE drum in a single recharge tick and resets its
        //own timer on every shot, so you never dribble rounds back mid-burst - you either have the drum
        //or you are reloading.
        //  burst     = chaingunMagazineSize rounds at 1 / chaingunBaseDuration per second
        //  sustained = chaingunMagazineSize / (chaingunMagazineSize * chaingunBaseDuration + chaingunReloadDuration)
        //            = 30 * 0.55 / (2.7 + 1.7) = 3.75 direct coeff/sec.
        public const int chaingunMagazineSize = 30;
        public const float chaingunReloadDuration = 1.7f;
        #endregion

        #region primary variant - XM301 rotary cannon (gatling)
        //Alternate primary. Trades the M230's per-round weight for volume: roughly 60% more
        //rounds per second at full spool, each hitting for about half as much. Sustained
        //direct output is deliberately close to the M230's rather than above it - this is a
        //different feel, not an upgrade. See AH64GatlingSpin for the spin/audio side.
        //
        //  M230     sustained = 30 * 0.55 / (30 * 0.09 + 1.7)      = 3.75 coeff/sec
        //  Gatling  sustained = 60 * 0.30 / (60 * 0.055 + 2.4)     = 3.10 coeff/sec
        //
        //Lower sustained on purpose: the gatling's advantage is burst density and the spool
        //ramp is a real cost, so parity would make it strictly better.
        //Up 15% from 0.30 after playtest 2026-08-03. Sustained output now sits at
        //60 * 0.345 / (60 * 0.055 + 2.4) = 3.57 coeff/sec against the M230's 3.75 —
        //still under it, which keeps the spool ramp a real cost rather than a formality.
        public const float gatlingDamageCoefficient = 0.39f;
        public const float gatlingRange = 190f;          //shorter than the M230; volume, not reach
        public const float gatlingForce = 55f;
        public const float gatlingBulletRadius = 0.35f;
        public const float gatlingRecoil = 0.45f;        //per round; there are a lot more of them

        //Rate of fire ramps with spool. The gun starts near the M230's cadence and winds up
        //to well past it - that ramp IS the weapon's identity, so it is not a cosmetic detail.
        public const float gatlingSpooledDuration = 0.055f;   //~18 rps at full spool
        public const float gatlingUnspooledDuration = 0.115f; //~8.7 rps from cold

        //Wider than the M230 and blooms faster: accuracy is what you give up for the rate.
        public const float gatlingMinSpread = 0.9f;
        public const float gatlingMaxSpread = 3.4f;
        public const float gatlingSpreadBloom = 0.055f;

        //Scaled down from the M230's 0.35 in proportion to the higher cadence, so the two
        //primaries deliver comparable procs per second rather than the gatling flooding items.
        public const float gatlingProcCoefficient = 0.20f;

        //Splash exists so packs still shred, but it is much lighter than the M230's HE tip -
        //at 18 rps a 6u blast would be a permanent explosion carpet.
        public const float gatlingSplashDamageCoefficient = 0.135f;
        public const float gatlingSplashRadius = 3.2f;
        public const float gatlingSplashProcCoefficient = 0f;
        public const float gatlingSplashForce = 18f;
        //Trimmed from 1.3 after playtest 2026-08-03 — visual only, damage and radius unchanged.
        public const float gatlingSplashVfxScale = 1.0f;

        public const int gatlingMagazineSize = 60;
        public const float gatlingReloadDuration = 2.4f;
        #endregion

        #region primary variant - M789 heavy cannon
        //Alternate primary. The opposite trade from the gatling: very few, very heavy,
        //very accurate rounds with a real blast on each. Fires from the existing M230
        //barrel, so this variant needs no model work.
        //
        //Kept explicitly a CANNON - hitscan, flat, instant. That is the whole answer to
        //"why not just use Hydra", which is the trap a slow primary falls into: drop the
        //rate far enough and it starts competing with the rocket pods for the same
        //fantasy. Travel time and arc are what Hydra owns; instant precision is what this
        //owns.
        //
        //  M230     sustained = 30 * 0.55  / (30 * 0.09  + 1.7) = 3.75 coeff/sec
        //  Gatling  sustained = 60 * 0.345 / (60 * 0.055 + 2.4) = 3.57 coeff/sec
        //  Cannon   sustained =  8 * 2.60  / ( 8 * 0.40  + 2.8) = 3.47 coeff/sec
        //
        //All three sit close on single-target sustained, which is the point - they are
        //different feels, not a power ladder. The cannon's real edge is the splash: at
        //9u it covers a pack, so against three targets it is far ahead. Its cost is that
        //every miss is expensive and the drum is tiny.
        public const float cannonDamageCoefficient = 2.90f;
        public const float cannonRange = 250f;          //longest of the three
        public const float cannonForce = 900f;          //visibly staggers what it hits
        public const float cannonBulletRadius = 0.6f;
        public const float cannonRecoil = 4.0f;         //4x the M230's, per shot

        public const float cannonBaseDuration = 0.40f;  //2.5 rounds/sec

        //Accuracy is this weapon's virtue, so spread is tight and bloom barely matters
        //at 2.5 rps - you cannot hold the trigger long enough to walk it off target.
        public const float cannonMinSpread = 0.2f;
        public const float cannonMaxSpread = 0.8f;
        public const float cannonSpreadBloom = 0.35f;

        //Full proc. At 2.5 rps this is ~2.5 procs/sec against the M230's ~3.85, so a
        //1.0 coefficient is affordable and makes on-hit items feel like they belong on
        //a heavy weapon.
        public const float cannonProcCoefficient = 1.0f;

        //Splash proc stays at zero, matching both other primaries. At this cadence a
        //non-zero value would actually be defensible - it is the one primary where it
        //would not flood the item economy - but that is a balance decision for playtest,
        //not something to slip in with the weapon.
        public const float cannonSplashDamageCoefficient = 1.00f;
        public const float cannonSplashRadius = 9f;
        public const float cannonSplashProcCoefficient = 0f;
        public const float cannonSplashForce = 400f;
        public const float cannonSplashVfxScale = 2.2f;

        //Per-shot camera shake. The other two primaries have none: at 11 and 18 rps it
        //would be a permanent rumble. At 2.5 rps each shot can land.
        public const float cannonShakeDuration = 0.18f;
        public const float cannonShakeRadius = 24f;
        public const float cannonShakeAmplitude = 1.6f;
        public const float cannonShakeFrequency = 14f;

        public const int cannonMagazineSize = 8;
        public const float cannonReloadDuration = 2.8f;
        #endregion

        #region secondary - Hydra-70 rocket pods
        //Ripple fire off the wing pylons, alternating left and right the way a real pod pair does. This
        //replaces Tri-Blast, which fired three hitscan bolts in a single frame - the ripple is the whole
        //point, because a salvo you can watch walk out to the target is what makes the pods read as pods.
        public const int hydraRocketCount = 6;
        //Gap between rockets while holding the trigger. Same cadence the old all-at-once salvo used.
        public const float hydraFireInterval = 0.1f;

        //6 * 1.6 = 9.6 total against Tri-Blast's 3 * 3 = 9, but these are AoE and Tri-Blast was not.
        //Reload (was inter-salvo cooldown) sits a touch under the old 6s so dripping rockets still feels
        //generous while a dumped magazine still pays a real pause.
        public const float hydraDamageCoefficient = 1.8f;
        public const float hydraReloadDuration = 5.5f;
        //legacy alias — keep any stray references compiling during the magazine swap
        public const float hydraCooldown = hydraReloadDuration;
        //small blast. The Hellfire's 12 is the heavy one; these are supposed to shred a crowd, not
        //delete it, and overlapping 12u blasts from six rockets would be a screen-filling mess.
        public const float hydraBlastRadius = 5f;
        public const float hydraSpeed = 120f;
        public const float hydraLifetime = 3f;
        public const float hydraForce = 200f;
        //Visual-only. OmniImpact donor — keep tiny. Damage radius stays hydraBlastRadius.
        public const float hydraExplosionVfxScale = 0.85f;
        public const float hydraExplosionParticleMult = 1.15f;
        //Hellfire ghost (bundle key AH64HellfireGhost).
        //Hydra gets a scaled clone.
        public const float hydraGhostScale = 0.38f;
        //six rockets landing means six proc rolls, so each one is worth well under a single chain gun burst
        public const float hydraProcCoefficient = 0.5f;
        //Unguided rockets scatter, and a perfectly stacked salvo would land as one big hit rather than a
        //walk. Half-angle of the cone, in degrees.
        public const float hydraSpreadAngle = 2.5f;
        #endregion

        #region utility - Evasive Roll
        //Forward-diagonal barrel roll: snap move input to F / L / R (no back). L/R travel on a diagonal,
        //not a pure strafe. Brief hop mid-roll; collective still owns sustained altitude after settle.
        //Class name ServoDash is registration-stable.
        public const float dashDuration = 0.95f;
        public const float dashCooldown = 4f;
        //Speed curve: capture horizontal speed on enter, ease up to peak, ease down toward a soft carry.
        //A shorter ramp makes the evasive input bite promptly without returning to the old hard speed snap.
        public const float dashRampFraction = 0.28f;
        public const float dashPeakSpeedMult = 2.35f;
        //How much of the (peak − entry) boost remains at exit (0 = back to entry speed).
        public const float dashExitCarry = 0.3f;
        //Stationary / slow rolls still need a floor so the maneuver has bite.
        public const float dashMinEntrySpeedFraction = 0.8f;
        //legacy aliases — ServoDash still exposes these as statics for any stray refs
        public const float dashInitialSpeedCoefficient = dashPeakSpeedMult;
        public const float dashFinalSpeedCoefficient = 1f + dashExitCarry * (dashPeakSpeedMult - 1f);
        //full aileron flip on mdlAH64 (procedural; no anim clip)
        public const float dashBarrelRollDegrees = 360f;
        //net altitude gained over the roll (half-sine climb). TargetHeight is bumped by the same amount
        //so hover does not slam the chopper back down the instant Main resumes.
        //Must stay under collectiveMaxRise or BumpTargetHeight clamps it away.
        public const float dashClimbHeight = 10f;
        //Forward weight for L/R paths: travel = normalize(facing * blend + ±right). At 0.7 the jink
        //stays forward-moving but shifts ~55° off the nose, reading more like lateral cyclic.
        public const float dashDiagonalBlend = 0.7f;
        //bonus armor while the plating is braced, granted as a timed buff for 3x the dash duration
        public const float dashArmorBonus = 300f;
        public const float dashArmorDurationCoefficient = 3f;
        public const float dashInvincibilityDurationCoefficient = 0.5f;

        //countermeasure flares popped behind the airframe through the roll (kept restrained — firework
        //scale reads as a party, not IR decoys)
        public const float dashFlareInterval = 0.38f;
        public const float dashFlareScale = 0.12f;
        public const float dashFlareTrailDistance = 1.1f;
        public const float dashFlareScatter = 0.35f;

        #region utility - Smoke Backflip (loadout variant)
        //Aerobatic rearward surge + pitch flip: smoke screen, brief cloak, climb then fade back.
        //Default utility stays Evasive Roll; this is the SkillFamily variant.
        //Longer duration + taller climb so the 360° pitch reads as a smooth loop, not a snap.
        public const float backflipDuration = 1.65f;
        public const float backflipCooldown = 6.5f;
        public const float backflipRampFraction = 0.28f;
        public const float backflipPeakSpeedMult = 2.35f;
        public const float backflipExitCarry = 0.28f;
        public const float backflipMinEntrySpeedFraction = 0.75f;
        public const float backflipClimbHeight = 14f;
        //Negative local X lifts the +Z nose first; positive X produces a front flip.
        public const float backflipPitchDegrees = -360f;
        //cloak linger past the flip so the fade-in reads after the smoke clears
        public const float backflipCloakDuration = 2.0f;
        public const float backflipInvincibilityDurationCoefficient = 0.50f;
        public const float backflipSmokeInterval = 0.16f;
        public const float backflipSmokeScale = 1.55f;
        #endregion
        #endregion

        #region special - AGM-114 Hellfire
        //One heavy missile off an inboard rail. Damage, cooldown and blast radius are carried over from
        //the wrist rocket unchanged - the shot was already the right weight, it was just called the wrong
        //thing and left from the wrong place.
        //
        //Deliberately DUMB-FIRE for now. Real Hellfires are laser-guided and RoR2 models that with
        //ProjectileSteerTowardTarget + ProjectileTargetComponent, but guidance changes how the skill
        //plays enough to deserve its own playtest rather than riding along with the kit swap.
        public const float hellfireDamageCoefficient = 13.5f;
        public const float hellfireCooldown = 8f;
        public const float hellfireBlastRadius = 12f;
        //faster than the wrist rocket's 110 - a rocket motor off a rail, not a lobbed grenade
        public const float hellfireSpeed = 140f;
        public const float hellfireLifetime = 6f;
        //one shot, one proc roll — keep at 1.0 so on-hit items still respect the special's weight.
        //Must be set explicitly on the projectile: CreateFlatFlyingRocket clones Commando's grenade,
        //whose procCoefficient is not 1.0 and would otherwise be inherited silently.
        public const float hellfireProcCoefficient = 1f;
        //Pocket I.C.B.M. adds two missiles, as it does for Engineer's harpoons. Those home, so vanilla
        //throws them out at ±45°; a dumb-fire Hellfire would send them into the scenery. A narrow fan
        //stacked all three blasts on one target and played far too strong, so ±25° spreads them into
        //separate impacts: area coverage, not triple damage on the aim point.
        public const float hellfireIcbmFanAngle = 25f;
        #endregion

        #region item interactions
        //The hover never grounds and AH64Main swallows the vanilla jump, so jump- and landing-based items
        //see a body that is permanently airborne and never jumps. Within this band above hoverHeight the
        //chopper counts as "on the ground" for them: H3AD-5T v2 cannot arm a slam (E at a chest would
        //otherwise start one), a slam detonates on reaching it, and the jump event re-arms.
        public const float restAltitudeBand = 1f;
        //Wax Quail fires on a collective tap while sprinting. A grounded survivor gets one per landing;
        //the hover never lands, so a cooldown stands in (playtest 2026-09-29 preferred this to once per climb).
        public const float waxQuailCooldown = 1.5f;
        //Luminous Shot gains a stack per secondary activation, and every Hydra rocket is one. Rockets
        //closer together than this (or than 2.5 fire intervals at the current attack speed) count as
        //one ripple and one stack. Generous so network jitter on the server can't split a ripple.
        public const float luminousRippleGap = 0.5f;
        #endregion

        #region special - AGM-114L Longbow
        //Custom PaintLongbow → FireLongbow. Hold special to paint, release to fire; Engi harpoon
        //projectile + lock VFX, our damage + pod muzzles. Primary/secondary stay free while painting.
        //Hybrid scale: more locks = more missiles, and later locks in the salvo hit harder.
        //  damage = longbowDamageBase + lockIndex * longbowDamagePerLock  (lockIndex 0..max-1)
        //  → 4.0, 4.55, 5.1, 5.65, 6.2, 6.75 at a full rack of 6 (+12% slight buff, 2026-08-04)
        //Lysate Cell adds a missile per stack on top of this, and the ramp keeps going for them
        //(7.3, 7.85, ...) — deliberate, so the cell pays off in burst the way it does for Hellfire.
        public const int longbowMaxLocks = 6;
        public const float longbowDamageBase = 4.0f;
        public const float longbowDamagePerLock = 0.55f;
        //tooltip / average reference — mid-salvo coeff for language tokens
        public const float longbowDamageCoefficient = longbowDamageBase + 2.5f * longbowDamagePerLock;
        //six locks at Engi's old 3s felt oppressive once damage went up; slight bump
        public const float longbowRechargeInterval = 3.5f;

        public const float longbowLockInterval = 0.25f;
        public const float longbowLockAngle = 20f;
        public const float longbowLockDistance = 150f;
        public const float longbowFireInterval = 0.15f;
        public const float longbowMaxPaintDuration = 12f;
        //Warhead on each guided hit. Between Hydra (5) and Hellfire (12) — a six-lock salvo should
        //read as overlapping AGM impacts, not Engi's quiet seeker pop and not six full Hellfires.
        public const float longbowBlastRadius = 8f;
        #endregion
    }
}
