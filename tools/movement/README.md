# Existing manoeuvre prototype

Movement-owned first slice on `feature/1.3-flight-utilities`, based on docs commit
`7955cbef707f2065951c97e6532c6126b8f1bdee` (gameplay `fe3c6ca`).
No braking-turn utility, hover partial, shared tuning/config, registry, plugin,
asset, localization, version, foundation or profile changes.

## Implementation and defaults

- Authority captures world velocity, role facing/direction, sign, base cosmetic
  attitude, duration, speed profile, ramp, allowed climb and start world height.
  Remote `OnEnter` uses the received snapshot and never recaptures or writes motion.
- Motor ownership is a runtime-only component lease. Air control temporarily
  becomes zero so the actual game's `CharacterMotor.PreMove` does not accelerate
  toward stale Main input. Entry does not overwrite velocity. Native aim input
  remains independent; the utility updates the native facing target, not `forward`.
- Lateral stick strength continuously selects a direction within the existing
  roll diagonal cone, keeping forward/rearward roles. Backflip now accepts bounded
  lateral intent too. Neutral/backward stick keeps the role's minimum speed profile.
  Live changes steer through world-velocity acceleration instead of direction snaps.
- Deliberate deviation from the old speed formula: incoming speed is preserved
  at capture but the boosted peak uses at most current `moveSpeedStat` as its seed.
  Chained utility carry therefore cannot multiply itself. Existing floor, peak
  multiplier, ramp and carry settings remain in use; no cooldown or buff defaults change.
- Prototype horizontal acceleration is `2 * peak / (duration * ramp)`; vertical
  acceleration is derived from entry vertical speed and the half-sine climb budget.
  These are mechanical defaults in the narrow helper, pending runtime tuning.
  A hard remaining-height clamp takes precedence over smooth vertical acceleration
  at the ceiling. Existing `LimitUtilityClimb` is called exactly once on authority;
  no hover fallback means zero utility climb. No anchors, airtime or launch flags reset.
- Wall/roof contact yields scripted thrust for the rest of this cast. Floor contact
  remains eligible to climb. Applied forces, existing gravity/launch handoff, custom
  gravity, H3AD-5T slam and authority loss yield motion. Rejected forces retain it.
  Neither completion nor interruption restores planned carry; resolved velocity wins.
- FlightVisuals owns transform, travel-loop audio and FOV. Base attitude blends
  from captured lean into current normal lean; recoil stays a separate layer.
  Spin uses an explicit unwrapped +/-360-degree curve, never endpoint-quaternion
  interpolation. Interruption resumes normal smoothing from the last rendered pose.
  Exit, disable, destruction and crash clean up owned audio/FOV. Repeated/stale
  cleanup cannot reset a replacing camera owner.

## Offline validation

Success criteria: analog intent differs for partial/full input while retaining each
utility role; ordinary reverse/sideways/vertical entries have bounded velocity and
climb; repeated peak budgets stay bounded; snapshot values survive serialization
and remote entry; observers never write motion; collision/force/interruption/disable
preserve achieved or replacing velocity and release subscriptions; cosmetic spin
passes through half/full revolutions with continuous entry/recovery and balanced cleanup.

Run with worktree-local CLI/NuGet/temp directories and disabled reusable servers.
The ignored `dist/bootstrap/Set-BaselineEnvironment.ps1` was adapted from integration;
`DOTNET_GENERATE_ASPNET_CERTIFICATE=false` was set before first SDK invocation.

```powershell
. ./dist/bootstrap/Set-BaselineEnvironment.ps1
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config /p:AH64DeployToProfiles=false --disable-parallel
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false --disable-build-servers
& ./tools/movement/check-movement.ps1
& ./tools/check-feedback.ps1
& ./tools/check-weapon-previews.ps1
```

The focused harness compiles actual state, capture, motor and visual sources against
API doubles. Its binary writer tests field preservation/order, not UNet transport.
Visual quaternion math uses System.Numerics with Unity's Euler composition order;
physics/effects and lifecycle calls are doubles. The Release build checks actual API
signatures. None establish live KCC behavior, Unity rendering/audio, controller feel,
multiplayer transport/timing or item acceptance. Logs are under ignored `dist/bootstrap/`.

## Integrator requests and runtime gate

No shared edit is required to compile this slice. If the prototype acceleration,
lateral cone or speed cap needs new user controls, the integrator owns promotion
into StaticValues/PlaytestConfig and associated counts/reports/localization.
Normal hover's existing airtime accounting remains unchanged (it runs from Main,
not during these Body utility states); this slice does not claim to redesign it.

For foundation telemetry, both states expose internal read-only `EntrySnapshot`,
`ManeuverProgress` (normalized fixed age) and `MotionYielded` (local authority lease).
Sample capture fields once at entry, actual motor velocity/position/native aim and
model quaternion each tick, state enter/exit and whether motion yielded. Observers'
lease-yield flag is not replicated and should not be interpreted as server evidence.
Please coordinate telemetry hooks through the parent; no DevAutopilot files were edited.

Capture serialization layout changes. Keep every runtime peer on the same candidate
DLL/hash; the integrator must coordinate the eventual plugin/manifest version bump.
This branch still declares baseline 1.2.1 and is not a release package.

Before integration/deployment, foundation and unedited-baseline runtime gates must pass.
Use the reserved runtime lane and a complete identified DLL/bundle/bank set.
No game/profile, Unity/Wwise, packaging, publication, push or PR was performed here.

Focused playtest checklist:

1. Neutral, partial/full lateral, reverse and sideways entry at walk/sprint speed:
   predictable forward/rearward escape, steering response, entry/recovery pose,
   complete roll/flip and simultaneous native aiming/fire. Include low/high frame rates.
2. Ascending/descending entry, raised ledges, pits, floor/wall/roof contact and
   multiple utility stocks: no renewed wall carry, utility altitude ratchet or
   growing peak speed; verify existing airtime lock/ceiling and defensive expiry.
3. Quail before entry, jump-pad/knockback during movement, rejected force during
   invincibility, H3AD-5T slam and Fungus after settling: external handoff and
   ordinary hover behavior after completion/interruption.
4. Stun, death, body disable/re-enable and stage transition: no motor subscription,
   loop sound, FOV or model-owner leak. Check actual grounded/ForceUnground ordering
   against PreMove; the offline lease tests do not reproduce KCC tick ordering.
5. Host, non-host authority and observer, with latency: identical received capture,
   no remote recapture/motor writes, native aim independent of cosmetic flip,
   progress/exit timing and visible recovery after collision/interruption.
