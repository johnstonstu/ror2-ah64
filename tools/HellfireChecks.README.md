# Guided Hellfire prototype handoff

Base: `7955cbef707f2065951c97e6532c6126b8f1bdee` (gameplay `fe3c6ca`).
Branch: `feature/1.3-guided-hellfire`.

Press immediately launches one lead from the alternating rail, converged on the nearest reachable
native-aim hit or a finite 500 m fallback. Hold special to designate only that latest lead; release
permanently closes its designation and it coasts. The explicit worker assignment overrides the
coordination document's earlier release-keeps-guidance sentence. There is no camera replacement,
target lock, predictive target lead or reacquisition. The short existing Weapon2 recovery remains.
Primary Weapon, Hydra Weapon3, Longbow/loadout order and utilities are unchanged.

The server creates a monotonically increasing lead token per body lifetime. A reliable begin intent
precedes the existing vanilla fire message on channel 0 and pairs the acknowledgement with the
correct local launch request. It does not spawn or damage anything. Aim accepts only the owner's
connection, live body, latest token, newer sequence, bounded update rate, finite near-body origin
and normalized finite direction. The server reconstructs the reachable world point. Release bypasses
rate limiting. Invalid/stale designations coast; a fresh valid ray may resume only while the same
launch has never been released. Newest death never hands guidance back to older missiles.

Prediction is deliberately disabled. ProjectileNetworkTransform is server-controlled at 30 Hz;
the client's ProjectileSimple velocity writer is disabled. The stock server ProjectileSimple timer
and stock warhead own lifetime/damage. Continuous collision detection and a server-only collider-sized
overlap/sweep guard cover rail obstruction and fast tick travel, forwarding through the existing
impact filters/behaviors. That guard has no separate damage path. Impact, timeout, death, stun,
disable and scene changes clear ownership/listeners. Pocket I.C.B.M. is exactly two unguided full
payload extras at +/-25 degrees for any positive effective stack, marked combo 1/2; lead uses 0.
No MissileUtils scaling or recursive launch path is added.

## Required integrator changes (not edited on this branch)

1. Plugin Awake, after Log.Init: `AH64.Survivors.Components.AH64HellfireNetwork.Init();`
2. Plugin OnDestroy: `AH64.Survivors.Components.AH64HellfireNetwork.Shutdown();`
3. End of AH64Assets.CreateHellfireProjectile, before catalog registration:
   `Components.AH64HellfireGuidance.Install(hellfireProjectilePrefab);`

No new state registration or survivor-prefab component is required. UNet IDs 28064/28065 are fixed;
conflicts fail with an explicit exception, without replacing another handler. Check them in the
reserved runtime profile. Existing imports may need the full namespace for the shutdown call.
Source/manifest version and localization remain integrator-owned. The Hellfire text should say
press launches, hold guides the latest missile with native aim, release coasts; I.C.B.M. extras do not guide.

## Requested shared tuning

Current guidance-only prototype constants live in AH64HellfirePrototype.cs and are explicitly
reversible. Request these AH64StaticValues names/defaults; keep protocol guardrails server-enforced:

| Addition | Default | Reason |
| --- | --- | --- |
| hellfireGuidanceRange | 500 m | Finite miss fallback and bounded designation distance |
| hellfireGuidanceTurnRate | 120 degrees/s | Visible corrections without snapping or tight circling |
| hellfireGuidanceTurnAcceleration | 720 degrees/s² | Approximately 0.17 s ramp to full turning |
| hellfireGuidanceTurnBudget | 240 degrees | Whole-life correction budget prevents repeated loops |
| hellfireGuidanceUpdateInterval | 0.10 s | At most 10 designation packets/s while active |
| hellfireGuidanceStaleAfter | 0.35 s | Coast when updates stop; never extrapolate a moving target |
| hellfireGuidanceOriginTolerance | 8 m | Bounded room for body-position/network lag |

The existing 1350% coefficient, 12 m splash, speed 140, proc/crit/source, lifetime and 8 s cooldown
are preserved. The 12 m splash gives limited precision incentive. For a separately approved,
reversible balance experiment, recommend 1800% total on direct impact (1350% direct bonus plus
450% splash), 450% splash at 4 m, speed 140, and the guidance limits above. Direct-hit bonus would
need a separate server-only implementation and deduplication tests; it is not in this prototype.
This rewards hitting the aimed target while lowering incidental crowd damage. No balance change
should be inferred from these recommendations or applied before the current flight is measured.

## Offline verification

Success requires rejecting foreign/stale/reordered designation, permanent release, no older-shot
reacquisition, bounded per-tick/whole-life turning, rail convergence, collision dispatch once through
stock behavior, clean ownership on lifecycle changes, and exactly two unscaled unguided extras.
The check harness compiles the actual owned production files against explicit game/Unity/UNet
doubles. It exercises request/ack/aim serialization, rapid-stock ordering, invalid wire vectors
before Unity Ray normalization, receiver rate limiting, state recovery/interruption and server-only
collision forwarding. Math uses a System.Numerics-based vector/quaternion double. These are
semantic checks; they do not prove native physics, actual API runtime access or network presentation.

Run in an isolated SDK/cache environment, certificate generation disabled before first SDK use,
compiler/build-server reuse disabled, with profile deployment disabled:

```powershell
. .\dist\bootstrap\Set-BaselineEnvironment.ps1
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config --ignore-failed-sources -p:NuGetAudit=false -p:AH64DeployToProfiles=false --disable-parallel
dotnet build AH64Mod/AH64.csproj -c Release --no-restore --disable-build-servers -p:AH64DeployToProfiles=false -p:UseSharedCompilation=false -nodeReuse:false
& .\tools\check-hellfire.ps1
& .\tools\check-feedback.ps1
& .\tools\check-weapon-previews.ps1
```

Bootstrap environment helper/caches and logs are ignored worktree artifacts, not new tracked
foundation tooling. Compilation stages only Build/plugins; no player profile is touched.
Final offline candidate: Release build passed with zero errors and the 22 existing warnings;
Hellfire checks passed 1,320 assertions; feedback passed 34 controls; weapon previews passed
1,978 assertions. Logs are `dist/bootstrap/hellfire-{restore,build,checks,feedback,previews}.log`.
Foundation telemetry can read `AH64HellfireGuidance.Token`, `LastTurnDegrees`,
`RemainingTurnDegrees`, owner `Policy.ActiveToken`/`CanGuide`/`Point`/`ServerRequest` inside this
assembly. It can identify lead/extra through ProjectileController.combo. Emitting JSONL is owned
by the foundation; this branch adds no independently competing logging system.

## Reserved runtime acceptance cases (all unverified)

1. Host keyboard/mouse: tap special, then hold primary/Hydra while guiding; native camera/aim and
   simultaneous fire stay usable. Move aim away and release; the missile keeps its last heading.
   Re-press without a new stock must not reacquire it. Check hit/miss and close rail convergence.
2. Client plus host observer: repeat with latency/jitter. Record server token/heading/contact and
   both ghosts; there must be one visible projectile/payload and no straight prediction ghost.
   Release before acknowledgement; rapid extra-stock launches must guide only the latest lead.
   Latest destruction must leave older leads and fan extras coasting.
3. Controller: native right-stick aim plus held special, simultaneous primary/Hydra and both
   maneuvers. Verify the configured physical button mappings, no camera takeover, comfortable
   steering and no input capture. Offline automation cannot establish physical-controller acceptance.
4. Geometry: spawn rail near wall/floor/low ceiling, steer toward terrain and an occluded target;
   self capsule/hurtboxes must not detonate the missile. Verify earliest obstruction, contact blast
   centre, no tunneling, and one damage event. Test fast lateral targets crossing a tick's path.
5. Lifecycle: stun/interruption, owner death, projectile timeout/deletion, disconnect, scene change
   and body respawn. Assert token clear, no listener leaks and no stale acknowledgement reacquisition.
6. Items/payload: effective I.C.B.M. stacks 0/1/5 produce 1/3/3 shots, only lead guides, extras keep
   +/-25 fan and full payload. Capture crit/proc/special attribution and direct/blast damage counts.
   Reconfirm unchanged Longbow reserve/refund/maxstock, Lysate and range ramp in integration.

Integration and runtime remain gated on the foundation handoff, unmodified baseline evidence,
the three shared hooks, a complete identified DLL/bundle/bank set and an integrator runtime lease.
No game/profile staging, launch, Unity/Wwise work, installs, packaging or publication was performed.
