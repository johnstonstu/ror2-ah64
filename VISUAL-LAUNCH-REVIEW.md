# 1.1 launch-rack and flight-effects review

## Staged flight-effects changes

`AH64FlightVisuals` now probes the world terrain at rotor-wash emission cadence on
each client. Dust spawns just above the hit surface and aligns its up axis to the
surface normal. Missing ground suppresses that emission; the retry remains
rate-limited. This does not change the flight controller or its ground probe.

The previous implementation emitted at the body origin and read
`AH64HoverController.GroundDistance`, which is refreshed by authority-only
`AH64Main.ApplyHover`. Remote observers therefore did not have a current probe.
Ascending wash effort now uses motor vertical velocity, available to observers.

Blur and new wash emissions are suppressed for dead bodies, cloak buffs, and
hidden/disabled CharacterModels. Existing emitted dust is allowed to expire.
The blur renderers bypass CharacterModel material management, so they need this
explicit gate. Cloaked teammates also lose these ordinary opaque-color FX while
their body uses the game's revealed presentation.

API checks used the actual installed game's `RoR2.dll`, decompiled with ilspycmd:
CharacterModel exposes `invisibilityCount` and `visibility`; its camera update
forces Invisible when the counter is positive. CharacterBody.GetVisibilityLevel
uses `hasCloakBuff` to choose Cloaked/Revealed. The direct buff/counter checks avoid
waiting for the camera-dependent visibility value to update. That value is still
checked for other hidden presentations, so resumption may follow the next camera
update. No new network dependency is introduced.

## Existing missile limitations (not changed in this pass)

- PaintLongbow deducts stock at lock acquisition, before FireLongbow launches.
  AH64PylonMissiles immediately hides meshes according to that stock.
- GenericSkill stock is local. Actual SkillLocator.OnSerialize sends skill
  definition indices, not ammunition. The current missile component's claim
  that local stock guarantees synchronized presentation is incorrect.
- Longbow targeting and launch progression run only on authority. Custom target
  lists and launch progress are not serialized. Adding state OnSerialize alone
  would only capture transitions/initial state, not changing locks or shots.
- The array starts MissileL3/R3, but hiding `i >= shown` removes L0/R0 first.
  The generated geometry is two rows of two missiles with identical signed X
  offsets on both wings. Equal numerical indices are not symmetric outboard
  stations. The current ordering comment is inaccurate.
- Eight meshes represent six default charges proportionally. Some shots remove
  two meshes; exact one-shot/one-store correspondence is not possible with that
  mapping. Permanent rails must never enter the missile visibility list.
- FireHellfire uses a process-wide static counter for launch-side alternation.
  Multiple bodies and independently advanced clients can disagree on the side.

## Recommended follow-up implementation

Preserve gameplay deductions, cooldowns, projectile count, damage and refunds.
On authority, display stock plus reserved-but-unlaunched locks, clamped to maximum
stock. Pending locks are `targets.Count` while painting and
`targets.Count - fireIndex` during firing. Preserve existing refunds on dead
targets, interruption and missing assets.

Synchronize an absolute presentation count or station mask, rather than gameplay
stock or incremental hide/show messages. Render immediately on the owner; send
updates to the server; store and replicate the latest snapshot to observers,
including initial state for late observers. Validate sender ownership and clamp
payloads. A custom NetworkBehaviour with explicit serialization and a verified
owner-to-server message transport is suitable. The current project does not
reference R2API.Networking, so dependency/manifest implications require review.
Do not assume attributes such as Command/SyncVar work without a UNet weaving path.

Measure station positions before choosing a mirrored depletion order. Preserve
the current proportional mapping unless a separate design decision changes the
eight-mesh presentation. For Hellfire, use a per-body launch side serialized with
the launch state. Recheck launch anchors against the improved rack before adding
more ignition effects; Longbow already combines registered ignition and smoke.

## Acceptance checks

- Wash touches flat and sloped terrain, stops over gaps/high altitude, and appears
  correctly for both the owner and remote observers. No per-frame missed-probe
  loop, duplicate networked dust, or effect-catalog warnings.
- Blur and new wash stop on cloak, invisibility and death, then recover after
  visibility returns. Test friendly revealed cloak and enemy/spectator cameras;
  verify first-person hiding and scene transitions in game.
- Flight motion, lean, utilities and hover height remain unchanged.
- For the later missile fix: painting reduces HUD stock without removing reserved
  stores; launches remove stores; canceled/dead locks refund without false launches.
- Host, owner and observer agree after partial/full salvos, interruption, reload,
  ammo resets, respawn and late observation; multiple AH-64s stay independent.
- Empty racks remain connected and visible on every skin. Gameplay projectile
  count, damage, cooldown and refund behavior remain identical.

Source review is complete. Compilation and gameplay/multiplayer verification are
owned by the integration pass; no in-game validation is claimed here.
