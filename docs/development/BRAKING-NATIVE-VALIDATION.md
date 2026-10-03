# Braking native validation contract

`braking-solo-v1` is an optional extension of the existing owned SOLO autopilot.
It requires both existing bootstrap opt-ins plus the exact child-scoped
`AH64_AUTOPILOT_BRAKING_CHECKS=braking-solo-v1` flag. Ordinary launches create no
runner, hooks, writers, inventory changes or input overrides.

The separate `braking-result.json` requires 36 distinct explicit IDs: two native
catalog checks; five checks each for stationary, small-angle, fast opposite,
spare-stock, collective and native stronger-priority interruption cases; four
checks for guided Hellfire plus simultaneous primary/Hydra, release and drain.
Missing cases, duplicate IDs, incomplete execution and failed assertions fail
the extension. Existing baseline 12, prototype 36 and M230 9 contracts retain
their counts and evidence identities.

The auditor excludes inactive preparation rows from steering and cleanup. A
single contiguous capture window identifies the current activation; its last
applied motor step can first appear in the initial post-exit sample. Cleanup
must be observed after that window. Twenty-nine synthetic acceptance assertions
include stale-prefix steering/cleanup and disabled-turn controls.

Native utility priority is `PrioritySkill` (enum value 2), distinct from `Skill`
(value 1). Small and opposite-motion cases require signed progress from actual
horizontal motor velocity at mid-turn and final alignment within 3 degrees.
Commanded presentation heading cannot satisfy these assertions. Isolated
source-copy mutants selecting the wrong priority and removing horizontal turn
must fail the actual diagnostic acceptance checks. The evidence auditor repeats
the measured-vector checks and includes six explicit-reason data controls.

Actual capture and last active motor step are observed, rather than treating
assigned setup velocity or resumed Main acceleration as braking carry. Native
PreMove output and applied velocity, phase, age, lease, yield reason, hover
target/airtime, native aim and visual owner/recovery are exported separately.
Three internal read-only component observers expose their own bookkeeping;
they do not access native private/protected members. A render observer executes
after the central presentation writer without writing transforms. Its generous
5 degrees plus 360 degrees/second bound is an alarm during braking/recovery,
not proof of polished feel or exact choreography. Intentional harness teleports
retain their pose epoch and raw evidence.

Braking presentation and its recovery cap the displayed world basis at the
existing 360 degrees/second turn rate. This also bounds the short Exit blend
back to native facing. Recovery joins ordinary hover/sway smoothing within one
degree; exact convergence to a moving target is not required. These cosmetics
do not write native direction, motor velocity, inputs or the camera.

The six motion cases use the existing terrain mark after safe low terrain is
confirmed. They do not establish wall/roof/ledge geometry coverage. Initial
velocity, aim, collective, skill overrides and an isolated utility-stock item
are scripted. Inventory and overrides are unwound in the case's finally block;
the arena safety buff is distinguished from skill-granted invulnerability.
Stronger interruption uses actual public `SetInterruptState` arbitration.
The inherited modern descent path still reads physical input and is untested.
Braking fixture teleports explicitly reuse the body's existing teleport reset
to discard altitude held by the previous collective case. This is scripted
setup, not evidence that the ordinary two-argument TeleportHelper raises a
MapZone event or that braking resets held altitude during actual play.

No new braking image is claimed by this suite. The five comparable baseline
screenshots remain Roll/Backflip/Hellfire checkpoints. Target hits, damage/procs,
audio, physical bindings/controller feel, accepted/rejected external forces,
floor/roof/wall/item edges, stage/death lifecycle, multiplayer and long sessions
require additional fixtures and review. Raw strict runtime failures are retained
and classified separately; passing feature checks does not erase them.

Before staging, commit and canonically build the exact source, pin actual DLL
bytes, scan installed game/dependencies and repin only unchanged reviewed sites.
Require the canonical token-matched runtime lease and a fresh checkpoint of
external profiles/saves after user handback. Stage only six Dev allowlisted
targets and preserve current configuration. One owned process has a 300 second
launcher deadline; preserve evidence and stop only owned processes. Freeze the
exact candidate and native evidence for independent review before user handoff.
This is private development and provides no public release clearance.

## First native attempt and corrective validation

The immutable `eb730da` run at
`dist/autopilot/20261003-134222-095895785965451cbc9d0296c8d950b0/execution-b25b47267ac94caaa353b89b2100f6de`
completed baseline 12/12, prototype 36/36 and M230 9/9. Braking passed 23 of 27
observed checks, then stopped before interruption/guidance because the previous
collective fixture's altitude was retained. Small/fast measured steering passed;
stationary/collective recovery remained active, and 24 render alarms exposed
the opposite-heading Exit return. The corrected auditor rejected that run and
all six specific-reason controls. Raw strict results retained 9 errors, 55
warnings and zero baseline pose flags. Neither those logs nor feature failures
are waived by offline corrections.

The focused rate-limit/recovery regressions exercise the central writer at
30/60/144 fps with changing hover targets. Both reverting the angular bound and
requiring exact moving-target convergence must fail their respective assertions.
The fixes require a new frozen candidate, exact installed-native access scan
and controlled native revalidation before readiness is claimed. All other
285 profile files and 7 saves were unchanged after the first run and reconnect;
the owned process exited and the runtime lease was released.
