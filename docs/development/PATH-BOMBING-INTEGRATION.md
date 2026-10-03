# Path bombing integration: source review candidate

The integration copies the final reviewed tree at
`31376ce68100ea33dbef36e7bc93dfe297b6b765`, rather than applying superseded
prototype commits. All 22 worker files retain their reviewed content. The
coordinator reports Astra clearance for source integration; this is not native
acceptance or public release clearance.

`BombingRun` is registered once and appended as the third Special variant after
Longbow and Hellfire. It uses Weapon2, native Skill activation priority, Pain
minimum interruption, one stock and a ten second recharge beginning at use.
Existing Longbow/default order, Hellfire and all primary/secondary/utility
definitions are preserved. Body utilities and Weapon/Weapon3 remain independent.

After Hellfire construction, one helper builds the independent bomb prefab from
the existing ghost and explosion presentation, registers its network prefab and
adds the same object to the projectile catalog. Missing prerequisite presentation
throws a clear startup error. The builder retains server sphere-sweep flight,
disabled client prediction/collision and no inherited direct/grenade damage.
Plugin Awake/OnDestroy initialize and shut down the reviewed terminal transport.
Message 28066 remains distinct from Hellfire 28064/28065.

English name/description tokens use the worker's centralized drop, damage and
per-target values. Translation files are untouched. The existing Special icon,
missile ghost/effect and proportional pylon-stock rendering are private prototype
presentation. Unique icon/bomb art and a six-bomb rack depiction remain separate
release decisions. No config/version/manifest/Unity/bank changes are introduced.

The owned opt-in autopilot subscribes to worker trace events and removes the
subscription at both terminal Finish and OnDestroy. One existing versioned JSONL
event envelope filters to the owned pilot's actual network ID. Its single
event writer retains all cast, owner, drop, scheduling, position/velocity, target,
damage, budget, proc and crit fields in a serializable payload. Ordinary launches
still return before creating a runner. No bombing native scenario or screenshot
is claimed by this trace hookup.

Source checks cover 331 worker API-double assertions and 14 source-copy negative
controls, including entry rejection/exception cancellation, stage/run loss,
authority recipient changes/retries and guaranteed impact destruction. Compiled
checks confirm native enum values, two preserved original Special definitions,
two preserved original Utility definitions, state/network/catalog/plugin/trace
wiring, and 247 unchanged methods across movement, hover, weapons, guidance and
pylon rendering. These checks do not execute Unity prefab construction, catalog
registration or network/damage/item behavior.

Before a native run, freeze the exact combined source and DLL and obtain independent
shared-integration review. Any canonical staging rebuild with different bytes
requires a fresh exact access pin/scan. Retain strict findings rather than granting
unknown members. Preserve the current six Dev files and fresh external profile/save
checkpoint, acquire the token-matched runtime lane, and use only an owned process.
The staged9e39bd4 user candidate remains untouched during this source-only work.

Native acceptance still requires real catalog/stock/reentry, six timed releases
along actual curved/altitude-changing paths with primary/Hydra and each utility,
ICBM non-multiplication, suppression/interruption/death/disable/stage loss, native
world sweeps/expiry and target-facing accepted/rejected damage, per-target cap,
crit and downstream proc attribution. Host/non-host/observer and authority-transfer
terminal delivery, latency and long sessions remain additional release gates.
Only after mechanics pass should owned target-facing recordings and player retests
be prepared. No main merge or public publication is authorized.
