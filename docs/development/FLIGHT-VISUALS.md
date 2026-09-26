# Flight visuals and missile presentation

Rotor wash probes terrain on each client at its emission cadence. Dust aligns to
the surface normal; absent ground suppresses emission. Observer-side wash uses
replicated motor velocity instead of authority-only hover-controller probe state.

Blur and new wash emissions stop for dead, cloaked or hidden bodies. Existing dust
expires normally. CharacterModel visibility and cloak state are checked explicitly
because blur renderers are outside its material-management array.

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
