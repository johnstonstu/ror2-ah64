# Changelog

All notable changes to AH-64 are recorded here.

The version in `AH64Plugin.MODVERSION` and `Build/manifest.json` must always
match — `NetworkCompatibility` is `EveryoneNeedSameModVersion`, so a mismatch
presents to players as a lobby rejection.

---

## 1.1.0 (unreleased combined playtest)

Combined visual, Wwise audio and hover candidate. In-game playtesting is pending.

- Added permanent two-level inboard missile racks, recessed engine openings,
  tapered cowls/wing roots, and rotor hub detail while preserving attachment points.
- Tidied canopy/intake clearance and aligned both main landing-gear supports to their wheel hubs.
- Extended the tail assembly and raised the tail rotor to clear the main rotor,
  upper fin and stabilator; a build-time mesh sweep guard checks regressions.
- Integrated the Wwise rotor bank with live volume, pitch, tone and response controls;
  playback diagnostics help verify the loop in the game.
- XM301 wind-up/down has its own emitter and volume control.
- Smoke Backflip rotates nose-up first; fall-damage immunity and external vertical
  motion are preserved. Equipment flight and lift volumes remain effective.
- Added selectable Desert and Arctic paint schemes with matching lobby previews.
- Weapon heat follows the selected skin after asynchronous skin application.
- Rotor wash now follows terrain on each client; blur/new wash respect cloak,
  invisibility and death. These behaviors await in-game verification.
- Builds stage locally by default; profile installation is explicitly opt-in.
- Known follow-up: missile depletion still follows local special stock, including
  Longbow reservations before firing; multiplayer presentation needs dedicated work.

---

## 1.0.1

Hotfix for two run-blocking bugs.

### Fixed
- **Jump pads and launch pads work.** This includes the moon pillar pads up to
  Mithrix and False Son's pads. The hover's altitude hold was cancelling the pad's
  launch within a few ticks and floating the chopper back down. The hover now
  hands over to normal character physics for the whole launch and takes over
  again on landing, or when falling back close to the ground. Pads just below the
  hovering chopper now trigger too, since the moon pillar pad's trigger is only
  about 4 m tall.
- **Solutional Haunt's boss-room lifts carry the chopper.** Lifts work through
  gravity, which the hover switches off, so they had no effect.
- **Out-of-bounds recovery after a long fall lands the chopper at rest.** The
  game's teleport keeps the downward speed, which could slam it into the ground.
- **Solutional Haunt no longer sends you back to the start.** The stage opens with
  a shaft deeper than the hover's ground sensor. After 3 seconds with no ground
  below, the hover's failsafe teleported the chopper back to a "safe" spot at the
  top. The chopper now falls through drops like any other survivor, and the
  failsafe only steps in after 12 seconds of fast free fall, which the shaft's
  slow-fall zone never allows.
- **The airframe noses down when flying forward.** It used to tilt backwards.

### Changed
- Over a drop deeper than the ground sensor (about 60 units), the chopper falls
  under normal gravity instead of sinking at a fixed slow rate. The game's own
  out-of-bounds recovery still catches a fall off the map.
- `CHANGELOG.md` now ships in the Thunderstore package, so the Changelog tab is
  populated.
- Feedback links now point to GitHub Issues and Discussions. Thunderstore has no
  package comments, so the old "comment on this page" line pointed nowhere.

---

## 1.0.0

First public release.

### Added
- Real logbook lore for the survivor, replacing the placeholder text.
- Thunderstore packaging: `icon.png`, store `README.md`, this changelog, a
  licence, and `tools/pack.ps1` — a one-command build that mirrors the
  assetbundle, checks version parity between the plugin and the manifest,
  validates the icon, and produces the release zip.

### Changed
- Removed the in-game build stamp overlay. It was a playtest aid and has no
  place in a production build.
- Package author is now `JohnstonStu`, so the Thunderstore package and its
  install folder are `JohnstonStu-AH64`. The post-build deploy now globs every
  r2modman profile rather than hardcoding one, so a new profile no longer needs
  a code edit.
- **The shipped assetbundle is 71% smaller — 5.62 MB down to 1.63 MB.** 141
  unused HenryTutorial template assets were removed: the full humanoid animation
  set, character meshes and textures, sword and fist VFX, and the legacy icon
  set. None of it was reachable from AH-64 code; all of it was being downloaded
  by players.
- Removed all third-party character content inherited from the template
  (Dante, Vergil, Neo, Rebellion meshes and textures), which the mod had no
  right to redistribute.
- Renamed the last template-named runtime assets to match what they actually do:
  the Hellfire rocket ghost, its explosion, its muzzle flash, and the rocket's
  material and texture.
- Restructured the Unity project so every path is AH-64's own —
  `AH64UnityProject/`, with `Assets/AH64/Bundle` as the single bundle-tagged
  folder and `Assets/AH64/Source` holding the Blender FBX outside it.
- The source FBX is no longer tagged into the bundle directly; its meshes ship
  as dependencies of the model prefabs instead.
- Removed a duplicate `matAH64RotorBlur.mat` that shadowed the live one under a
  colliding in-bundle name.
- Dropped dead template guard code and a legacy bundle-cleanup path.

### Fixed
- **The gatling and cannon primary variants were never registered as entity
  states.** They ran locally but had no state index, so they did not replicate —
  in multiplayer other players would not have seen them fire correctly. The log
  showed this as "Sending state that resolves to invalid".
- **Rotor wash and dash dust never rendered.** Both were passing the vanilla
  `GenericFootstepDust` prefab to `EffectManager.SpawnEffect`, which resolves
  through `EffectCatalog`; that prefab is not registered there, so every spawn
  failed silently — 532 error lines in a single run. Now cloned into a
  registered AH-64 effect.
- The dash thruster and Hydra muzzle flash pointed at a legacy resource path
  that no longer exists, so neither ever loaded.
- Skins never baked — `SkinDef.Bake()` requires `skinDefParams`, and only the
  obsolete top-level fields were being set.
- An item display rule referenced a non-existent equipment (`GainAmmo` rather
  than `Recycle`), leaving an invalid key asset in the display rule set.
- Cleared a redundant per-file assetbundle tag on the rotor hover audio that
  duplicated the folder-level tag.

---

## 0.2.1

- Slight damage buff; attack speed now assists the primary reload.

## 0.2.0

- Rotor mix and cannon report reworked following the audio investigation.
- Airframe brightness lifted.

## 0.1.x

- Development series: airframe modelling and art passes, the hover movement
  model, the M230 primary and its gatling/cannon variants, Hydra rocket pods,
  Evasive Roll and the smoke backflip, Hellfire and Longbow specials, the
  Fire Control Radar passive, custom explosion VFX, and the rotor audio layers.
