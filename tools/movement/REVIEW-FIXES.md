# Corrective source review

Follow-up to candidate `56715696c3ff7e8b1bc604da8685b2abc1d7b20f` on the same isolated
`feature/1.3-flight-utilities` branch. Scope remains movement-owned source/tests.
No deployment, integration/production edits, shared configuration, hover partial,
foundation, plugin hook or registry changes.

## Confirmed source defects and corrections

1. **Ground recovery:** descending entry can land after Begin. The next owned
   positive-vertical step now calls ForceUnground while grounded. Inspection also
   showed that KCC calls BeforeCharacterUpdate (and therefore CharacterMotor.PreMove)
   before consuming its unground flag: ForceUnground only queues `_mustUnground` and
   does not immediately clear GroundingStatus. A narrowly scoped PreMove hook forces
   the existing air-control branch for that call while the lease owns motion;
   airControl is zero. It saves/restores the surface-owned `isAirControlForced`
   flag in try/finally and still calls the original method. This closes the grounded
   acceleration gap without replacing KCC grounding reports or global acceleration.
   Yield, interruption, disable and authority loss remove the hook. Walls/roofs
   still release thrust and never re-unground afterward.
2. **FOV ownership:** FlightVisuals tracks the last override it wrote. External
   value changes during the cast stop further utility FOV writes; cleanup restores
   the prior value only if the current value is still owned. Tests change FOV
   before exit/disable/crash and before another active visual frame. This is a
   value guard, not a uniquely identified camera-override API: coincidentally equal
   external writes cannot be distinguished.
3. **Re-entry priority:** installed EntityStateMachine.CanInterruptState evaluates
   `(nextState ?? state).GetMinimumInterruptPriority() <= interruptPriority`.
   Equal PrioritySkill therefore permits re-entry. SkillDef.CanExecute checks
   that gate before OnExecute consumes stock. Both maneuver states now require
   Pain, which is above their PrioritySkill activation and below Stun/Frozen/Death;
   equal Pain remains allowed. Existing SkillDefs, mustKeyPress, defensive timings,
   cooldowns and stocks are unchanged. The source-model regression attempts
   repeated activation with available extra stocks, checks stock/buff preservation
   and verifies permitted replacing priorities; it is not physical-input proof.
4. **H3AD-5T handoff:** the item's Fall state writes motor velocity on its own state
   machine. Hover's `YieldToHeadstompSlam` drops its flight/anti-gravity granters
   only from Main.ApplyHover. Releasing the utility lease alone cannot invoke it.
   Both utility states now request Main when an authoritative live slam is detected,
   preserving item velocity and using the existing hover handoff rather than
   modifying hover partials. The offline test proves the transition request and
   cleanup, not actual item detonation or physics timing.

## Engine evidence

Decompiled from the installed game's actual Managed assemblies with the existing
ilspycmd; no stripped GameLibs method bodies were used. Evidence remains ignored
under `dist/bootstrap/`: `RoR2-EntityStateMachine.cs`, `EntityStates-InterruptPriority.cs`,
`RoR2-Skills-SkillDef.cs`, `CharacterMotor.cs`, `GenericCharacterMain.cs`,
`EntityStates-Headstompers-HeadstompersFall.cs` and `KinematicCharacterMotor.cs`.
The owned source also reads the unchanged AH64Main/hover item handoff.

SHA256 identities inspected:

- RoR2.dll: `0497A902A7AAF3C97FA2F1251A5364723B0C1B422839F7002A4B5F9C02563E4F`
- KinematicCharacterController.dll: `F47D49957C5A65156018E5E2460B3752188FDF05DA631C70AC56FAD010BD1C69`

## Checks and limits

The commands in README.md use the feature-local environment/caches. Corrective
Release build command:

```powershell
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false --disable-build-servers
& ./tools/movement/check-movement.ps1
& ./tools/check-feedback.ps1
& ./tools/check-weapon-previews.ps1
```

- Release: zero errors, **23 obsolete-API warnings**. Two warnings added versus the
  initial candidate's 21 are reads of the already-used obsolete fovOverride API
  needed by the ownership guards. No warnings were suppressed.
- Movement/state/capture regressions: **3,867 assertions passed**.
- Actual visual-source regressions against doubles: **37 assertions passed**.
- Existing feedback: **34 controls passed**; weapon previews: **1,978 assertions passed**.
- Four generated mutants under `dist/bootstrap/review-mutations` each failed at
  its intended assertion: remove step re-unground; remove grounded PreMove guard;
  restore unconditional FOV cleanup; restore Roll's equal utility minimum. The
  priority mutant omits the enum assertion so the multi-stock activation attempt
  is the failing regression. Production files were not mutated for these runs.

The motor double now queues ForceUnground and processes PreMove before consuming
it. A negative control demonstrates why re-unground alone is insufficient. This
models inspected source ordering; stable-floor projection is deliberately simplified.
The harness does not execute game KCC geometry/sweeps, the complete SkillDef/input
pipeline, actual private API access, networking, rendering or item behavior.
Logs: `correction-build.log`, `correction-movement.log`, `correction-feedback.log`,
`correction-weapon-previews.log`, mutant output logs/results.json and the post-commit
`correction-result.json`, all under ignored `dist/bootstrap/`.

## Runtime gates carried forward

- **Access/physics:** foundation must verify the new On.CharacterMotor.PreMove hook
  and publicized private `CharacterMotor.isAirControlForced` field in the actual
  game, plus descending-floor-reclimb at default and sprint/item speeds. Compilation
  validates signatures only. Preserve hook cleanup and slippery-surface state.
- **H3AD-5T:** prove Main executes the existing granter handoff in time, slam
  velocity/detonation works and hover recovers. The Body transition request does
  not prove cross-state-machine tick timing or equipment-flight interactions.
- **Held input/stocks:** physical held Roll and repeated Backflip presses with
  extra stocks must confirm no mid-cast re-entry/defense renewal. After normal
  completion, intended subsequent stock use remains a runtime check.
- **Multiplayer:** server observing a client authority, remote deserialization /
  OnEnter ordering and mid-cast authority gain/loss/lease transfer remain unverified.
- **Visual timing:** existing tests explicitly render half/full progress. They
  do not prove a full revolution when normal exit clears presentation before the
  next LateUpdate at low FPS; retain this as a visual acceptance gate.
- **Identity:** schema changed while plugin version remains integrator-owned 1.2.1.
  Every test peer must have an identical candidate DLL/hash. Coordinate the final
  plugin/manifest version bump before shared/release testing.

Foundation and unedited-baseline runtime gates still precede integration/deployment.
No game, profile, Unity, Wwise, install, push, PR, merge or publication occurred.

## Focused authority-loss lifecycle correction

Follow-up to `e1f86b24001129354ba1a4c45a576c6c17578d9f`, after source rereview
cleared the prior re-unground/PreMove, FOV, priority and H3AD-5T corrections.
The remaining conditional defect was that losing both state and motor authority
skipped the authority-only Step branch which previously performed lease cleanup.

- Both states now call the owned lease's CheckAuthority immediately after base
  FixedUpdate, outside the movement authority branch and before the capture guard.
  Loss of either authority flag yields/releases the lease, restores prior air
  control and removes collision, force and PreMove subscriptions. Velocity and
  visual progress remain untouched by this cleanup; no unrelated state transition
  or recapture is requested.
- PreMove also yields before calling orig when motor authority is lost, providing
  a fallback wherever that hook is invoked before the next state tick.
- The regression loses BOTH flags and ticks the actual ServoDash/SmokeBackflip
  FixedUpdate paths. It asserts restored non-default air control, removal of all
  three subscription types, retained velocity and safe later observer tick/exit.
  It additionally covers temporary disagreement between authority flags and the
  PreMove fallback. No direct Step call stands in for the state-path regression.
- A generated mutant removing Roll's outer CheckAuthority call fails at the
  expected state-tick air-control restoration assertion. The mutant is ignored
  evidence under dist/bootstrap/authority-cleanup-mutation; production files were
  not mutated for this negative-control run.

Affected checks were rerun with the README.md isolated environment and commands:
movement/state checks passed **3,905 assertions**; visual-source checks passed
**37 assertions**; deployment-disabled Release build passed **zero errors and
23 obsolete warnings**, unchanged from the prior corrective candidate. Unchanged
feedback/weapon checks retain their prior verified 34/1,978 results and were not
rerun for this lifecycle-only change.

Evidence under ignored dist/bootstrap: authority-cleanup-movement.log,
authority-cleanup-build.log, authority-cleanup-mutation/output.log/result.json and
post-commit authority-cleanup-result.json. All documented runtime gates above
remain: this source/double regression does not establish actual network authority
notification/tick order, mid-cast authority gain, KCC/item/controller acceptance,
private hook access, low-FPS visuals or multiplayer. Candidate peers still require
identical DLL hashes while version remains 1.2.1. No integration/runtime work.
