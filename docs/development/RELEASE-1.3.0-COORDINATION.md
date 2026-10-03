# AH64 1.3.0 coordination

## Baseline and scope

- Integration branch: `release/1.3.0` in a new worktree of the existing repository.
- Reviewed base: `fe3c6ca81958a5210f5ec193f5006dfce21c1c7e` (main, README links only since 1.2.1).
- Bootstrap completed with isolated compilation and existing offline checks. This record now defines feature contracts; it does not implement them.
- Main remains on its existing checkout and branch. Main merge and public release need later explicit approval.
- Translation PR17 belongs to separate work; do not edit or merge it. Incorporation is a separately coordinated integration step.

## Ownership

The local integrator on STUX3DPC is the sole integration owner and runtime resource owner.
The parent coordinator assigns separate feature workers; no runtime reservation is active.
Feature workers must request shared edits and runtime slots through the coordinator.

| Lane | Initial file ownership |
| --- | --- |
| Test foundation | Proposed new `tools/dev-profile/Check-Access.ps1`, `Stage-Build.ps1`, `Run-Autopilot.ps1`, `AH64Mod/DevAutopilot*.cs` and `docs/development/PLAYTEST.md`; integration owner owns any `AH64Plugin.cs` hook or project-file change |
| Movement | `SkillStates/ServoDash.cs`, `SkillStates/SmokeBackflip.cs`, `Components/AH64FlightVisuals.cs`; hover partials only through agreed ownership |
| Guided Hellfire | `SkillStates/FireHellfire.cs` and new guidance components |
| Integration | `AH64Survivor.cs`, content/state/assets registries, static values, config/options/report files, version/manifest, release checks and documentation |
| Translation | Existing translation owner; token/language/README additions coordinated after final behavior is settled |

Paths in the table are relative to `AH64Mod/Characters/Survivors/AH64/` where applicable.
The framework, normal flight and Longbow are baseline behavior and remain unchanged during bootstrap.
No feature worker starts until the coordinator assigns a separate worktree and scope.

## Movement contract

- Preserve Roll's forward/diagonal and Backflip's rearward roles and existing defensive windows.
  Normal flight, aim input, Longbow, airtime, anchored climb ceilings and item behavior remain baseline contracts.
- The body authority captures entry velocity, travel direction/sign, current cosmetic attitude,
  entry/peak/exit speed, allowed climb and maneuver duration once. Serialize the authoritative
  capture for observers; a received capture must not be overwritten by remote `OnEnter` recapture.
  Resolve allowed climb once through the existing hover protection rather than a new flight model.
- Motor motion, facing/aim and model attitude are separate. Only body authority writes maneuver
  motor velocity. Native weapon aim stays usable; do not derive projectile aim from a cosmetic flip.
  `AH64FlightVisuals` remains the single model-transform owner, with kicks layered separately.
- Begin from the captured pose/momentum; use a bounded acceleration/steering curve and a continuous
  attitude recovery rather than an identity reset. Do not turn a high-speed side/reverse entry into
  an instantaneous new velocity. Keep the full intended roll/flip visible; quaternion endpoint
  equality alone does not establish a complete revolution.
- On normal completion hand off bounded achieved momentum to the existing hover/main behavior.
  Respect collision limits and external impulses; do not restore planned carry after a wall stop.
  On stun/death/other interruption release ownership and let the replacing state/force win.
  Recover cosmetic attitude without writing the motor or resetting the hover/airtime anchor.
- Camera/FOV, maneuver visuals and owned travel audio must clean up on interruption, death,
  disable and stage transition. Observers evaluate the same captured maneuver/progress while
  consuming replicated motion; no locally recalculated speed/climb or second transform writer.
- Tunable curves/budgets remain reversible on the feature branch. Workers supply requested
  shared static/config additions to the integrator; do not silently change existing balance defaults.

## Guided Hellfire contract

- Recommended prototype interaction: press special for an immediate rail launch; ordinary native
  aim designates the latest live lead Hellfire while it flies. Releasing special does not cancel
  guidance. Retain press-to-launch stock/cooldown behavior; no charge/confirm state, primary-button
  override or replacement of camera/movement input.
- Keep special on `Weapon2`, primary on `Weapon`, Hydra on `Weapon3`, and utilities on `Body`.
  Longbow stays the default special with unchanged paint/reserve/refund behavior and slot order.
- Use a per-owner, per-body-lifetime monotonically increasing launch token. Only the newest live
  lead missile accepts current designation. A new launch, destruction or owner lifetime change
  invalidates stale token updates. Older missiles coast along their last heading; never reacquire
  a new token or target just because the newest missile disappears.
- Define designation as a reachable world point from the native aim ray, with a finite configured
  range fallback when no surface is hit. Converge from the actual rail position. Server owns
  steering/collision/damage; accept designation only from that body's authority for its active token,
  with finite/range/sequence validation and a bounded update rate. Local observers cannot redirect it.
- Steering has bounded angular rate, acceleration/turn budget and lifetime; no instantaneous snap,
  teleport or steering through terrain. After blocked/stale/invalid designation, coast rather than
  extrapolating arbitrary future targets. Owner death/disable/stage loss clears designation ownership
  and stops guidance; existing missile impact/lifetime behavior governs already released payload.
- Visible flight follows the server's corrected projectile. Verify the actual prefab's prediction
  behavior; do not leave a straight client ghost diverging from collision. No new networking dependency
  or source-wide rewrite without an integration request and evidence that existing APIs are insufficient.
- Pocket I.C.B.M. remains exactly two extra full-payload missiles for any positive effective stack
  count, at the existing +/-25-degree fan; additional stacks add neither shots nor scaling.
  Recommended prototype: only lead missile guides; both extras remain unguided fan shots.
  Extras never register a guidance token, initiate another launch or invoke recursive missile scaling.
- Preserve current damage/proc/crit/special-source attribution and cleanup until measured precision
  balance is reviewed. Guidance changes do not authorize an implicit warhead/cooldown rewrite.

## Minimal test telemetry contract

Use versioned JSONL records (`header`, `sample`, `event`, `assertion`, `summary`) in a unique
run directory; emission is dev-only. Schema version 1 uses these stable field groups:

| Record | Required information |
| --- | --- |
| Header | `schemaVersion`, `runId`, `scenario`, source SHA and dirty-workspace fingerprint, plugin version, DLL/bundle/bank/config hashes, game build, profile, role and requested assertion count |
| Sample | Fixed tick and simulation time, entity/owner identity and authority role, position/velocity, facing and native aim, model attitude quaternion; maneuver capture/progress or guidance token/designation/turn used when relevant |
| Event | Tick/time, entity/owner, state enter/exit/reason; launch token, lead/extra index, projectile identity, collision/despawn or cleanup reason where relevant |
| Assertion | Stable assertion ID, expected/actual values, tolerance, pass/fail and scenario context; pending/skipped checks stay explicitly unverified |
| Summary | Complete/incomplete outcome, expected/executed/passed/failed/skipped counts, capture count, classified warnings/errors and artifact paths |

Keep damage/hit evidence separately identified by attack/projectile token, target, skill source,
damage/proc/crit and direct/blast classification when a scenario asserts payload behavior.
Record semantic assertions independently from heuristic visual-pop flags. A clean process exit
without a complete valid summary is failure; missing prerequisites must not produce a pass.
The full copied game log and snapshots remain evidence, including errors outside scripted sampling.

## Worker start gate

Parent may assign isolated movement and guided-Hellfire prototype coding in parallel with the
test-foundation work. Both workers start from the same recorded baseline and receive only their
owned files; shared edits, including the pending foundation hook, go through the integrator.
Feature integration and runtime deployment/comparison await the runnable foundation handoff
(hook, access gate, artifact protocol and telemetry) and recorded unmodified-baseline runtime
evidence. Parallel coding does not grant a runtime slot or permission to modify profiles.
No further user decision is needed for these reversible prototypes under the defaults above.
The choice of countermeasure alternative and bombing-path/input/refund semantics belongs to the
later utility/bombing design checkpoint and must not expand these two workers' scope.

## Shared resource queue

One serial lane covers RoR2 launches, profile deployment, controller/camera/audio review,
and profile log capture across AH64 and Hollow Saint. Process checks alone are not a lock.
Unity project/import/build state, active MCP instance, Wwise generated banks, packaging
output and capture directories also require named ownership.

For each future runtime slot: record source SHA and artifact/config identity; require
the game closed; back up one disposable profile; stage and launch one candidate;
preserve logs and captures under a unique run name; classify results; release the slot.
Do not stop another user's or agent's process.

Reservation protocol: the integrator maintains the queue and grants one owner/run token. All
local AH64 and Lightning runtime scripts must use the same canonical lock path, supplied explicitly
by the integrator: `<integration-root>/dist/coordination/runtime.lock`. A future reservation uses
atomic exclusive creation (`FileMode.CreateNew`); existing, unreadable or partly written locks mean
occupied. The lease records owner, run token, resource, profile, source SHA, creation UTC and any
known process IDs. No timeout-based takeover. Only the matching owner releases it after capturing
logs and verifying the run stopped; stale leases require integrator review, never process killing.
No lease is acquired by this documentation checkpoint. Unity/Wwise work also needs an explicit
integrator grant and must not overlap the same project/generated outputs.

Safe staging protocol for a future granted slot:

1. Verify the canonical lease, game closed, exact candidate worktree/SHA/dirty fingerprint and
   successful compile/offline/access results. Read current installed files before replacement.
2. Require a complete identified artifact set: DLL, Unity bundle, AH64Rotor bank and its source
   license. Record all hashes and config separately; do not combine unrecorded generated outputs
   from another worktree. Previously verified bundle/bank reuse is allowed only with matching
   input provenance and hashes. No `Init.bnk`, secrets or developer config in the payload.
3. Use only the reserved disposable profile and one resolved plugin destination. Validate paths;
   back up replaced artifacts/config into that run's workspace-local backup and write a manifest.
   Never enable `AH64DeployToProfiles=true`, glob player profiles or overwrite saved tuning.
4. Stage the exact allowlist; verify installed hashes equal the candidate. Profile manager's cached
   listing is not DLL identity. Prevent a launch while replacement is incomplete; on partial failure
   restore the recorded prior artifacts with the game closed and report the failure.
5. Launch only after the owner requests the permitted scenario; preserve trace/summary/captures and
   the whole profile log before another run. A timeout/failed assertion is a failed run; stop only
   a process demonstrably launched by that reservation, never another existing process.
6. Close the owned run, record outcome and rollback/location, then release the matching lease.
   Clean-release testing later installs the exact ZIP rather than using staged files as proof.

Proposed future profiles: `AH64 1.3 Dev` for controlled tests and `AH64 1.3 Clean`
for exact-ZIP/default-config acceptance. Neither is created or modified by bootstrap.
Preserve `demo time`, `demo time new` and Hollow Saint profiles.

## Safe baseline checks

Inspect scripts before running. Keep temporary files and generated output inside this worktree.
Use `AH64DeployToProfiles=false` for every compilation; never enable all-profile deployment.

```powershell
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config /p:AH64DeployToProfiles=false
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false
powershell -NoProfile -ExecutionPolicy Bypass -File tools/check-feedback.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/check-weapon-previews.ps1
```

Run with workspace-local DOTNET_CLI_HOME, NUGET_PACKAGES, NuGet cache directories,
TEMP and TMP, and disabled reusable build/compiler servers. Bootstrap logs and exact
commands belong under ignored `dist/bootstrap/`.
Set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false` before the first SDK invocation.

## Bootstrap checkpoint

- Dependency restore: passed from the repository-configured BepInEx and nuget.org feeds.
- Release build: passed, 22 baseline obsolete-API warnings, zero errors; deployment disabled.
- Existing feedback check: passed, all 34 release controls and migration/report checks.
- Existing weapon-preview check: passed, 1,978 assertions using game API doubles.
- Source/manifest version remains 1.2.1. No feature source or existing tracked file changed.
- New worktree has no generated Unity bundle or Wwise bank; no runnable/package readiness is claimed.
- No profiles created, deployments, launches, packaging, main commits/merges or public releases.
- Detailed command/result/hash/package records are in `dist/bootstrap/`; reusable lessons are
  in `RELEASE-1.3.0-LESSONS.md`.

These offline checks do not establish game loading, actual-game API access, controller
acceptance, gameplay or multiplayer. Unity/Wwise builds, packaging, profile deployment
and game launch are outside the initial bootstrap scope.
