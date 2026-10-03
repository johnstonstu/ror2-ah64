# Braking-turn private prototype

Base: `9e39bd4a6f24925ee339dc20b8bd995fb07588f3`.
Branch: `feature/1.3-braking-turn`. This slice adds only new braking-owned files.
It is deliberately unregistered until the coordinator performs the wiring below.
No runtime, profile changes, Unity/Wwise work, packaging or publication occurred.

## Behavior and provisional tuning

Body-authority capture contains entry velocity, entry heading (horizontal momentum,
then facing), requested heading (horizontal movement input, then horizontal native
aim, then facing), entry cosmetic attitude, move-speed budget and phase parameters.
Observers deserialize this capture; they never recapture it or acquire motor ownership.

| Phase | Time | Behavior |
| --- | --- | --- |
| Brake | 0.00–0.30 s | Reduce actual horizontal speed toward min(25% of entry speed, 35% of captured move speed). |
| Turn | 0.30–0.80 s | Turn actual horizontal momentum toward captured intent along the shortest signed angle, at most 360 degrees/s. |
| Exit | 0.80–1.00 s | Approach min(entry speed, captured move speed) at at most 3 times captured move speed/s, without further steering. |

Brake deceleration is the captured speed reduction divided by 0.30 s. This is
finite and fixed for a cast; it scales with incoming momentum to reach the brake
target in the approved phase. Braking never accelerates an unexpectedly slower body.
Boundary-crossing steps are split across the three phases. No heading or speed is
snapped at exit. For an unobstructed 8.5 m/s entry the final target is 8.5 m/s, but
the bounded 0.20 s exit achieves 7.225 m/s; reaching cruise is not guaranteed.
This is intentional controlled recovery, not an omitted final carry restoration.

A stationary entry remains stationary horizontally while its presentation heading
can pivot. Low-speed entries cannot gain more horizontal speed than they entered
with. Live input does not retarget the captured maneuver. Zero facing/aim/input has
a deterministic world-forward fallback. Exactly opposite headings use the signed
atan2 result; neither left nor right is forced for smaller requested corrections.

Proposed SkillDef values remain 1 stock, 4 s recharge, activation recharge rather
than end recharge. There is no damage, armor, invulnerability, cloak, countermeasure,
utility climb, added sound, FOV effect, stock refund or airtime replenishment.
All tunables reside in `AH64BrakingTurnStaticValues`, not shared configuration.

Presentation-only first-pass flare/bank is -18 degrees pitch / at most 25 degrees
bank. These small cue amplitudes are implementation proposals for central visual
review, not balance requirements or verified rendered results.

## Native movement ownership

`BrakingTurn : AH64Main` retains the existing GenericCharacterMain input pipeline,
AH64Main collective tap, modern/classic descent, and exactly one existing
ApplyHover call per authoritative movement tick. The braking state does not
implement a parallel altitude servo or reset hover target/anchor/airtime.

The new motor leases the public MMHOOK `On.RoR2.CharacterMotor.PreMove` event.
It snapshots incoming velocity, calls native PreMove unchanged, and replaces only
X/Z using the phase math. The native computed Y survives exactly. It does not
write airControl, isAirControlForced, gravity/flight granters, grounded state,
moveDirection, inputBank, CharacterDirection, camera or model transforms.
Inherited native Main still performs ordinary input/facing/moveDirection behavior.

Both state and effective motor authority, live health, flight/gravity state,
air-control lock, custom gravity and HeadstompersFall are checked before and after
the native call. Nested accepted force or collision callbacks cancel the lease
before the horizontal replacement. Accepted impulses yield; rejected forces do not.
Wall/roof contacts yield; floor contacts retain the horizontal maneuver.
Direct velocity changes during inherited HandleMovements (notably Wax Quail) yield.
Main continues handling jump-pad, void and Headstompers transitions.
A yielded state requests Main on its next authority tick, retaining resolved velocity.
Disable, stage change and state exit remove hook/event subscriptions idempotently.

No new private native fields are referenced. Headstompers detection uses the
existing public `BaseHeadstompersState.FindForBody` API. The strict native scanner
confirms no braking-owned inaccessible sites. The private native PreMove method is
intercepted through MMHOOK's public event, not directly invoked.

## Required coordinator integration edits

1. **State registry:** in `AH64States.Init`, add
   `Modules.Content.AddEntityState(typeof(BrakingTurn));`.
2. **Utility family:** in `AH64Survivor.AddUtilitySkills`, create and append a
   third SkillDef, preserving existing Roll/Backflip ordering and default selection:
   - skillName `AH64BrakingTurn`;
   - tokens `AH64_UTILITY_BRAKING_TURN_NAME` and
     `AH64_UTILITY_BRAKING_TURN_DESCRIPTION`;
   - `activationState = new SerializableEntityStateType(typeof(SkillStates.BrakingTurn))`;
   - `activationStateMachineName = "Body"`;
   - `interruptPriority = InterruptPriority.PrioritySkill`;
   - `baseRechargeInterval = AH64BrakingTurnStaticValues.Cooldown`;
   - `baseMaxStock = AH64BrakingTurnStaticValues.Stock`;
   - requiredStock / stockToConsume / rechargeStock = 1;
   - mustKeyPress / fullRestockOnAssign = true;
   - resetCooldownTimerOnUse / beginSkillCooldownOnSkillEnd = false;
   - isCombatSkill / canceledFromSprinting / cancelSprintingOnActivation /
     forceSprintDuringState / dontAllowPastMaxStocks = false.
   Existing utility icon may be reused explicitly as a prototype placeholder.
   State minimum priority is Pain, preventing PrioritySkill reentry without
   preventing stronger interruptions. Never refund stock on early termination.
3. **Language:** suggested English name `Braking Turn`; description:
   `Brake your momentum, turn toward your chosen direction, then regain controlled speed.
   Aim and fire while turning.`
   Do not claim defense or an instant reversal. Translation owner handles other locales.
4. **Guided Hellfire:** in `AH64HellfireOwner.Interrupted`, immediately beside
   ServoDash/SmokeBackflip exemptions, add
   `&& !(bodyMachine.state is SkillStates.BrakingTurn)`.
   This is a typed allowance only; do not exempt all Pain states.
   Without this edit, current ownership logic treats braking as interruption and
   ends guidance. This worker cannot claim concurrent guided Hellfire is complete.
5. **Visuals:** integrate the data lease into existing AH64FlightVisuals as below.
6. **Telemetry:** optional coordinator-owned sampling can read state
   `EntrySnapshot`, `ManeuverProgress`, `MotionYielded`; motor
   `AppliedAge`, `YieldReason`, `HasActiveLease`; and presentation frame.
   Motor flags/age are authority-local observations, not replicated server evidence.
   Record actual velocity, native aim, position, fixed age, capture and displayed
   quaternion separately. Never label a commanded heading as measured motion.
7. SDK default compile includes the new source automatically. No project or prefab
   addition is needed for the lazy motor/presentation components. No projectile,
   asset bundle, networking dependency, shared config or plugin hook is required.
   Coordinator owns version/manifest and matching peer DLL identity before runtime.

## Exact presentation API and central evaluation contract

`AH64BrakingTurnPresentation` is an internal component on the body. It exposes:

- `Begin(object state, AH64BrakingTurnCapture snapshot)`
- `Progress(object state, float age)`
- `End(object state)`
- `bool TryGetFrame(object state, out AH64BrakingTurnFrame frame)`

The state supplies Begin/Progress/End; the visual owner only reads. Begin with the
same owner is idempotent. Progress/End/reads from stale owners cannot affect a
replacement. Disable/stage change/death end the lease; reading a dead body also
ends it immediately.

The frame contains EntryAttitude, RequestedHeading, SignedCorrection,
CommandedHeading, normalized overall Progress, Phase (Brake/Turn/Exit),
PhaseProgress, PitchDegrees and BankDegrees. CommandedHeading is the captured
bounded turn plan, including stationary pivots; actual replicated motor motion
remains separate. The braking state drives normalized fixed-age progress, while
the motor applies phase time in actual PreMove steps. Native phase ordering/observer
timing remains a runtime gate, not a claimed exact sub-frame correspondence.

Central wiring:
- Resolve the `"Body"` EntityStateMachine and this component on the same body.
  Since the component is lazy-created, retry a missing component during a braking
  state rather than caching null permanently at FlightVisuals.Start.
- In existing LateUpdate, retain crash precedence, then evaluate a braking frame
  only when the current Body state is BrakingTurn and
  `presentation.TryGetFrame(bodyMachine.state, out frame)` succeeds.
  Existing Roll/Backflip and ordinary flight remain the fallback branches.
- Convert the frame's world CommandedHeading into a cosmetic yaw relative to the
  existing modelBase yaw; do not modify body facing or native aim. Compose that
  local yaw with the frame's pitch/bank in the single existing leanLocal layer.
- At brake entry, blend from frame.EntryAttitude (weight 0 at entry), not identity.
  During turn, evaluate the bounded commanded heading; no barrel-roll revolution.
  During exit, blend toward existing ordinary flight lean. Use the existing
  smoothing/recovery path from the last displayed leanLocal on early termination.
  Continue the existing separate weapon/hit kick layer and the existing sole
  `model.SetPositionAndRotation` call.
- Do not call PlayBarrelRoll/PlayBackflip/BeginManeuver: those start their own
  revolution, loop sound and FOV resources, which this utility does not request.

The worker implements no second transform writer and no call to a nonexistent
visual method. The rendered pivot/flare is incomplete until this central wiring
is implemented and visually checked.

## Validation and limits

Success criteria: phase-limited bounded steering, no forced reversal, stationary
and low-speed no-boost behavior, bounded exit, serialization preservation,
observer non-ownership, native Y preservation on grounded/airborne branches,
continuing inherited hover/input paths, and no lease/velocity restoration after
collision, force, direct item handoff, authority loss, death, disable or stage exit.

Run from this worktree in PowerShell:

```powershell
. ./tools/braking-turn/Set-Environment.ps1
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config /p:AH64DeployToProfiles=false --disable-parallel
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false --disable-build-servers
& ./tools/braking-turn/Check-Braking.ps1
& ./tools/braking-turn/Test-Mutations.ps1
```

CLI home, NuGet packages/http/plugin/scratch, TEMP/TMP and output are worktree-local.
Compiler/build-server reuse and ASP.NET certificate generation are disabled.
The repository build's ordinary local Build/plugins DLL copy is not profile staging.

Observed:
- Release compile: 0 errors, 33 warnings (same baseline count).
- 26,463 braking assertions passed against API doubles, compiling the actual
  braking state/capture/math/motor/presentation and unchanged AH64Main.
- Four isolated generated mutants failed at their intended assertion:
  discard native Y, omit post-native authority guard, recapture on observers,
  and omit inherited HandleMovements. Production source was never mutated.
- Fresh installed-game API scan: 14,145 references, 0 unresolved, 35 inaccessible.
  The inaccessible list exactly matches baseline 9e39bd4; none belongs to braking.
  Strict scan therefore exits 1 as expected; no policy exception was created.
  Dependency resolution used this worktree's restored dependency DLLs and installed
  game Managed DLLs, not publicized GameLibs method bodies or profile DLLs.
- Independent read-only source review found no actionable defects.

Evidence is under ignored `dist/braking-turn/`: restore/build/checks/mutations
logs, mutant result records, and `access-strict/access.json`.
Historical engine source inspected in the prior movement worktree matches the
game RoR2 DLL SHA256 recorded by this scan:
`0497A902A7AAF3C97FA2F1251A5364723B0C1B422839F7002A4B5F9C02563E4F`.

These checks do not prove actual hook execution, KCC sweeps/collision order, native
input/stock behavior, complete hover ceiling/item behavior, rendering, controller
feel or multiplayer transport. The doubles model only the required call ordering;
their hover stub is not a reimplementation or verification of the native servo.
The utility is not selectable until registration, and central presentation/Hellfire
wiring is explicitly outstanding. No runtime readiness or balance approval claimed.

Required future native scenarios: moving/stationary and small/opposite turns;
collective and both descent modes at ceilings/airtime exhaustion; floor/wall/roof;
jump-pad, Quail and H3AD-5T handoffs; stun/death/disable/stage exit; multi-stock
reentry; simultaneous primary/Hydra/guided Hellfire; host, non-host and observer
capture/phase/exit behavior under latency. Runtime remains coordinator-owned.
