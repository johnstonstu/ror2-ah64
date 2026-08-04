# Changelog

All notable changes to AH-64 are recorded here.

The version in `AH64Plugin.MODVERSION` and `Build/manifest.json` must always
match — `NetworkCompatibility` is `EveryoneNeedSameModVersion`, so a mismatch
presents to players as a lobby rejection.

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
