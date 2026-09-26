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

- Built with R2API and the Risk of Rain 2 modding community's survivor framework.
- Airframe modelled from scratch in Blender for this mod. No third-party or
  ripped aircraft geometry is included or redistributed.
- Rotor audio: "Helicopter Rotor Loop", "Helicopter Loop" and "OH-58 Helicopter
  Loop" by **qubodup** on Freesound, all **CC0 / public domain**. Full provenance,
  source URLs and checksums ship inside the bundle as `LICENSE_SOURCE.txt`.
- All in-game sound effects other than the rotor beds are vanilla Risk of Rain 2
  Wwise events.

## Known issues

- **Jump pads and launch pads** (including the moon pillar pad up to Mithrix and
  False Son's pads) did nothing in 1.0.0: the hover cancelled the launch and
  floated you back down. **Fixed in 1.0.1**, pending playtest confirmation. If a
  pad still won't take you, please report it with the stage name.
- **Solutional Haunt** sent the AH-64 back to the start of the opening shaft, which
  locked the run. **Fixed in 1.0.1**, pending playtest confirmation.
- Behaviour change in 1.0.1: over a drop deeper than the hover's ground sensor
  (about 60 units), the AH-64 now falls under normal gravity like other survivors
  instead of sinking slowly.

## Bugs & feedback

### Optional balance controls

Install [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/)
to adjust AH-64 under **Settings → Mod Options → AH-64**. It is optional. Movement,
primary weapons, utility, audio, and presentation are grouped into categories;
the defaults are the intended balance and the controls remain available in releases.
Restart the game after changing base speed, acceleration, primary reload times,
or Evasive Roll cooldown. Other settings are read during play, though an action
already in progress may use its earlier values. Gameplay settings are not synced:
use matching values in multiplayer. Audio preferences can differ between players.

Want to share a setup? Add optional **Feedback → Comments**, then choose
**Share settings → Copy & open GitHub** in any category. Paste into the form,
review, and submit. Every copy includes all exposed sliders and toggles, the mod
version, changed defaults, pending restart values, and comments. Use **Feedback →
Copy all settings** to copy without opening a browser. Comments stay saved locally
until cleared. Nothing is submitted automatically; GitHub requires an account.

- **Found a bug?** File it on the [issue tracker](https://github.com/johnstonstu/ror2-ah64/issues/new/choose).
  Please include your **BepInEx log** (`BepInEx/LogOutput.log`) and your mod list —
  it turns "it's broken" into something fixable.
- **Balance thoughts, ideas, or a quick question?** Start a thread in
  [GitHub Discussions](https://github.com/johnstonstu/ror2-ah64/discussions).
  Thunderstore has no comments, so messages can't be left on this page.

All players in a lobby need the **same version** of the mod, or the game will
refuse to let you join together.
