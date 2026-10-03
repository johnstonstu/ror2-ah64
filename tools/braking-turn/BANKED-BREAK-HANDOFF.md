# Banked Break feedback handoff — build only

Base: ceab48ae15df2a94d56b194079f300440525a3b0.
Worktree/branch: ah64-1.3-feedback-banked-break / feature/1.3-feedback-banked-break.
Contract SHA256: 9F06A34306360A860C3D9A4EAD9A5E544ED29A05FCA4E7BB7EB484A5FE912742.

The user's later instruction superseded the earlier test plan: implement and compile,
with brief necessary source checks only. No test suites, mutations, game launch,
profile writes, installation, staging, capture or runtime acceptance were performed.
The installed ceab48ae candidate and other worktrees remain untouched.

## What should feel different

- From hover, the aircraft now gains useful forward motion and banks into a quarter-turn.
- From cruise, it carries incoming momentum through the maneuver instead of braking to a crawl.
- The arc lasts 1.25 seconds, with smooth 90-degree heading progression and a tangent exit.
- Side is selected once from native lateral movement relative to entry travel; below 0.5 m/s,
  use horizontal aim then body facing. Left input beyond the 0.15 deadzone selects left;
  neutral, ambiguous or forward/back input defaults RIGHT. No remembered side or physical button.
- Speed approaches 1.35 times captured normal movement speed with a 0.25-second response.
  Faster entry eases toward that target; chained utility velocity never seeds another boost.
  Tiny drift acquires the aim-based path through bounded thrust rather than an orientation snap.
- Capture removes CharacterBody's sprint multiplier from the move-speed budget, while retaining
  ordinary movement-stat scaling. Installed CharacterBody source confirms sprint is included in
  moveSpeed; this is not a guess based solely on the publicized reference assembly.
- No new defense, countermeasure, climb, airtime grant, stock or cooldown change.
- Native aim/camera/weapons remain independent. Existing typed Hellfire allowance stays valid
  because the registered BrakingTurn type is retained.
- Visuals use the existing sole FlightVisuals writer: approximately 32-degree bank, mild 5-degree
  forward pitch, smooth bank-in/out, and commanded tangent through exit. Its existing world-space
  recovery resumes ordinary facing afterward. No copied barrel roll or second transform writer.

Internal BrakingTurn names and the Brake phase identifier are retained for registration/shared
interface stability; that phase now means bank-in and does not decelerate the aircraft.
Capture serialization changed: peers must use the same candidate DLL/version.

## Altitude diagnosis: findings, not a claimed runtime root cause

Read-only inspection of OnEnter/FixedUpdate/OnExit, AH64Main, hover target updates, external
motion and teleport reset paths found:

1. The old utility never requested entry altitude. It inherited whatever target ordinary hover
   already had. Its horizontal hook preserves native PreMove Y exactly, so "Y preserved" was
   insufficient evidence that the inherited target was appropriate.
2. A decaying previous utility bump is deliberately separate from the pilot's held altitude.
   Neutral resting hover eases down over lower terrain; classic release and exhausted airtime
   also settle. Missing terrain, external launch/knockback and H3AD-5T have separate handoffs.
   These can produce real descent without any explicit Y write in the braking state.
3. ResetAfterTeleport clears the held-altitude/anchor request and sets velocity to zero.
   The existing scripted fixture calls that reset, so its post-reset hover result cannot establish
   what the pilot felt entering from an ordinary in-flight position.
4. Native PreMove uses one vector acceleration calculation. Preserving its Y means preserving
   that complete native result, not proving independent vertical acceleration or altitude hold.
5. No captured pilot trace identifies which of these explains the reported drop. Do not describe
   the source review, initial compile, or historical solo passes as a reproduced/fixed runtime bug.

The new AH64BankedBreakAltitude component is a constrained request, not a second controller.
It owns no transform/velocity/hover private field and never replenishes resources. A climb or
descent input permanently releases the captured hold for that cast so release cannot drag the
aircraft back to its old entry height. Authority/force/collision/death/disable/stage/teleport
handoffs clear it. Legacy classic controls retain their existing release-to-settle behavior.

## Required coordinator integration before pilot installation

**Altitude consumption is not active in this worker's compiled DLL.**
Apply/review `tools/braking-turn/BankedBreak-Hover.coordinator.patch` in the integration
checkout using git apply --unidiff-zero. This zero-context patch is pinned to the exact base above. This worker did not edit shared AH64HoverController.cs.

The patch:
- consumes the request inside UpdateTargetHeight, after ApplyHover's external-flight,
  force/void/slam early returns and airtime accounting;
- converts captured body world Y into feet altitude and clamps the target through the existing
  rest/terrain/anchored-ceiling calculation;
- lets the existing hover PD servo and native PreMove calculate Y; it never pins height,
  resets velocity, or refills airtime;
- prevents a preceding utility's higher bump target from continuing to climb during the hold;
- cancels the maneuver before ResetAfterTeleport resets native hover state, covering both
  event-driven teleports and direct test/void reset calls.

The patch passed `git apply --check --unidiff-zero` against this exact base; it was not applied or compiled
as part of the worker build. Coordinator must compile the integrated result. Neutral entry
altitude is not fully implemented until this patch is consumed.

Shared language edit (coordinator/translation owner):
- Keep token IDs AH64_UTILITY_BRAKING_TURN_NAME / DESCRIPTION.
- Name: **Banked Break**.
- Suggested description: "Bank through a sweeping 90-degree turn while maintaining flight.
  Choose left or right with movement input; neutral input turns right. Aim and fire throughout."
- Existing stock 1 / cooldown 4 seconds, state registration and family order remain unchanged.
- Existing placeholder icon can remain for the private pilot comparison.
- Update shared README/localization only after pilot acceptance; do not present old braking
  descriptions or clips as this behavior.

No changes are required to the central FlightVisuals.cs interface: the worker-owned
AH64FlightBrakingMath and presentation frame update already feed its existing integration.

## Build and known limits

Release compile passed with 0 errors and 46 warnings. It is deployment-disabled, using the existing isolated Set-Environment.ps1.
CLI/NuGet/temp/output remain local. Commands:

```powershell
. ./tools/braking-turn/Set-Environment.ps1
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config /p:AH64DeployToProfiles=false --disable-parallel
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false --disable-build-servers
```

Logs: dist/braking-turn/restore-feedback.log and build-feedback.log.
No new access scan was run in this build-only wave; no new private native member access is
intended. Brief installed-native source inspection checked the sprint speed interpretation.

Historical tools/braking-turn stop/pivot assertions are superseded, not Banked Break tests.
The legacy braking-solo-v1 opt-in now fails explicitly before starting its scripted cases.
It must not report stationary-pivot or small-requested-angle passes for the redesigned arc.
Do not run the old offline/mutation campaigns as acceptance of this candidate; rebuilding
that test suite was deferred under the user's build-only direction.

The player owns the next in-game assessment after the coordinator integrates and installs
with the game closed. Four short cases matter:
1. Hover: activation visibly carries the helicopter into a useful left/right reposition.
2. Cruise: continuous travel, a readable arc, and tangent exit without a stop.
3. Neutral collective with available airtime: no unexplained sink on level ground; distinguish
   intentional descent, resource expiry, terrain safety and external-force handoffs.
4. Presentation: bank agrees with travel, exit recovers smoothly, aiming/firing remain usable.

Collision/terrain, both collective directions, authority transitions, multiplayer and physical
controller feel remain unverified. The fixed commanded path is not a guarantee of endpoint:
world collisions and external forces win, with no velocity restoration afterward.
