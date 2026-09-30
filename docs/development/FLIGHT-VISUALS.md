# Flight visuals and missile presentation

Rotor wash probes terrain on each client at its emission cadence. Dust aligns to
the surface normal; absent ground suppresses emission. Observer-side wash uses
replicated motor velocity instead of authority-only hover-controller probe state.

Blur and new wash emissions stop for dead, cloaked or hidden bodies. Existing dust
expires normally. CharacterModel visibility and cloak state are checked explicitly
because blur renderers are outside its material-management array.

## Attitude layers (1.2)

`AH64FlightVisuals` owns the model transform: yaw from ModelBase, then one of the
crash, backflip, barrel-roll or flight lean, then the weapon/hit kick. The kick is
applied at the model only and never fed back into the lean smoothing.

- Flight lean: velocity and stick lean, collective pitch, surge dip and braking
  flare, sprint lean, idle sway, and a coordinated-turn bank from the smoothed
  yaw rate (ignored across a heading jump of 45 degrees or more, e.g. after a roll).
- Kick: `Kick(bodyObject, pitchUp, roll)` from the firing states. Everything but the
  Longbow calls it outside the authority check, so remote aircraft kick too; the
  Longbow fires from an authority-only method and kicks only for its pilot.
  Heavy hits kick from synced health, on every client.
- Crash: `AH64Death` calls `PlayCrash` on every client. The spin sign comes from
  the body's netId, so all clients agree without syncing anything. The authority
  scripts the fall through `AH64HoverController.ApplyCrash`; the server destroys
  the body on impact, and every client explodes the wreck in `OnExit`.
- Engine smoke (below `damageSmokeHealthFraction`) and the crash trail use the
  Hydra smoke ring. `SmokePuffEffect` is an explosion flash, not smoke.

Rotor sound follows `AH64RotorSpin.Spool`: silent in the drop pod, pitching up
with the spool, and winding down on death. A pod arrival (stage 1, first five
seconds) starts from a stop; every other spawn starts at flight RPM.

## Known missile presentation limitations

- Longbow consumes stock when locks are reserved, before missiles launch. Rack
  meshes currently follow that local stock.
- GenericSkill stock is not serialized by SkillLocator; local stock does not
  guarantee synchronized remote or late-join presentation.
- Eight meshes represent six default charges proportionally. Exact one-shot/one-store
  removal is not possible with that mapping.
- Hellfire's launch-side counter is process-wide, so independent clients may
  disagree on which side supplied a launch.

These are presentation follow-ups, not claimed fixes in 1.1. A later implementation
should synchronize an absolute station mask or count with owner validation and
late-join state, preserve gameplay deductions/refunds, and measure mirrored station
positions. Permanent rails must remain outside the missile visibility list.

Check terrain contact on flat/sloped ground, gaps/high altitude, cloak, death,
stage changes and remote observers. Verify one visible weapon assembly for every
primary/skin combination. Future missile fixes need partial/full salvo, cancellation,
refund, reload, multiple-owner and late-join checks.
