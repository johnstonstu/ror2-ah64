# Braking-turn source integration

The reviewed isolated prototype from `ffc7f7e9b49efc0e05042e44e484b5abf5192a87`
is integrated on `release/1.3.0`. Braking is appended as the third utility, after
Evasive Roll and Smoke Backflip; default/order, 1-stock/4-second activation recharge,
Pain reentry protection and zero added defense follow the prototype contract.
The existing utility icon is explicitly a private prototype placeholder. English
tokens are added; translation work and dedicated art remain separate release gates.

The typed Hellfire interruption policy permits the three named utility states and
continues to interrupt guidance for other Pain, stun and death states. It does not
exempt an entire priority class or change input/weapon ownership.

AH64FlightVisuals remains the single model transform owner. It reads the lazy
owner-matched braking presentation frame, stores its own last displayed world basis
separately from recoil, and blends braking entry in world space. Converting that
basis back relative to live model-base yaw prevents a local-lean-only yaw snap.
The brake/turn/exit target comes from the captured heading and cosmetic pitch/bank;
exit and interruption recover from the last displayed world basis toward ordinary
flight. Crash takes precedence. No additional body facing, aim, motor, FOV or audio
writer is introduced. Existing roll/backflip paths remain their own visual branches.

New integration checks execute the actual central visual component, presentation,
capture math and typed guidance policy against API doubles. They exercise lazy
component creation, world entry and interrupted recovery when native yaw changes,
recoil separation, sole transform write, no motor/aim/FOV edits, recovery convergence,
stage/death cleanup, typed utility allowances and other Pain/stun/death rejection.
Utility state types in this visual-only harness are priority/type doubles; the
separate braking suite compiles the actual state and motor with unchanged AH64Main.
Two generated source-copy negative controls discard the displayed world basis at
entry or interruption recovery; both must fail their intended continuity assertion.
The native hook/KCC/hover, rendered camera, real input/stock, item and multiplayer
limitations in BRAKING-TURN-PROTOTYPE.md remain applicable.

Check-IntegrationMetadata.ps1 interprets the exact compiled AddUtilitySkills IL with
asset/config/type/skill construction modeled, verifies appended order and definition
values, and verifies state-catalog and typed-owner wiring. It does not instantiate
native Unity objects or claim a game catalog/skill execution test.

Builds use an isolated OutputPath and AH64DeployToProfiles=false. The user's staged
9e39bd4 M230 candidate and frozen private package remain unchanged. Braking has not
been staged, launched, captured or native-tested; bombing is not integrated. A new
source/DLL freeze and independent source review precede any runtime handoff. Runtime
is user-owned until explicit handback.

After handback, native gates include initial displayed world attitude/recovery,
phase/input cadence, grounded/airborne vertical continuity, ceiling/airtime, launch
pads/Quail/Headstompers/forces, multi-stock reentry, interrupts/death/stage cleanup,
simultaneous primary/Hydra/guided Hellfire, and host/non-host/observer transport.

Observed offline integration validation: 3,905 existing movement assertions,
75 central visual/policy assertions (37 existing plus 38 integration assertions),
26,463 actual braking state/motor assertions, four core negative controls, and two
central visual negative controls. Compiled utility registration interpretation passes.
The first entry mutant escaped a zero-render-delta fixture; the corrected test
advances a nonzero delta and checks the displayed recoil-free world basis as well
as normal recoil decay. The earlier attempt/logs remain retained under dist.
No native gameplay or balance acceptance is inferred from these counts.
