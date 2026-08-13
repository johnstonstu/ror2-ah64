# AH-64

**The Gunship.** A survivor that never lands.

The AH-64 permanently hovers a fixed height above the terrain and strafes like a
ground character, trading footspeed and vertical freedom for a full weapons load
and a gun that is always on target.

---

## The kit

**Passive — Fire Control Radar**
Paints the strongest nearby threat. The chin turret tracks independently of where
you are looking, so the gun stays on target while you reposition.

**Primary — M230 Chain Gun**
A fixed drum that reloads all at once rather than trickling. Tap at range to keep
the burst tight; hose the whole drum up close where the spread stops mattering.
The reload runs whether or not you emptied it, so top up before you commit.
Attack speed helps the reload, not just the fire rate.

*Variants:* an M230 gatling, and a low-rate high-damage cannon.

**Secondary — Hydra 70 Rocket Pods**
A ripple salvo spread over most of a second — hold your aim through it. Runs on
its own cooldown and stays available while the primary is reloading.

**Utility — Evasive Roll**
A climbing forward-diagonal barrel roll with i-frames through the motion. Hold
jump for altitude; hold back or down to dump height faster.

*Variant:* a smoke backflip.

**Special — AGM-114 Hellfire / Longbow**
Hold to paint radar locks while you keep firing the gun and the Hydras, then
release to launch. Hellfire is available as a loadout variant.

---

## Install

Install with a mod manager (r2modman or Thunderstore Mod Manager) — it will pull
the dependencies for you.

Manual install: drop `AH64.dll` and the `AssetBundles` folder into
`BepInEx/plugins/`, keeping `AssetBundles` beside the DLL.

**All players in a lobby need the same version.** The mod declares
`EveryoneNeedSameModVersion`, so a version mismatch is a lobby rejection rather
than a desync.

---

## Credits

- Built on the Risk of Rain 2 survivor mod template (`HenryTutorial` lineage) and
  R2API.
- Airframe modelled from scratch in Blender for this mod. No third-party or
  ripped aircraft geometry is included or redistributed.
- Rotor audio: "Helicopter Rotor Loop", "Helicopter Loop" and "OH-58 Helicopter
  Loop" by **qubodup** on Freesound, all **CC0 / public domain**. Full provenance,
  source URLs and checksums ship inside the bundle as `LICENSE_SOURCE.txt`.
- All in-game sound effects other than the rotor beds are vanilla Risk of Rain 2
  Wwise events.

## Bugs & feedback

- **Found a bug?** File it on the [issue tracker](https://github.com/johnstonstu/ror2-ah64/issues/new/choose).
  Please include your **BepInEx log** (`BepInEx/LogOutput.log`) and your mod list —
  it turns "it's broken" into something fixable.
- **Balance thoughts, ideas, or a quick question?** Drop a comment right here on
  this Thunderstore page.

All players in a lobby need the **same version** of the mod, or the game will
refuse to let you join together.
