# Changelog

All notable changes to AH-64 are recorded here.

The version in `AH64Plugin.MODVERSION` and `Build/manifest.json` must always
match — `NetworkCompatibility` is `EveryoneNeedSameModVersion`, so a mismatch
presents to players as a lobby rejection.

---

## 1.3.1

README footage refresh and localization patch. Gameplay balance is unchanged.

### Added

- Simplified Chinese (zh-CN), Russian (ru), and Brazilian Portuguese (pt-BR) for the survivor's in-game text: name, subtitle, description, lore, outros, skills, skins, passive, radar chat, and the Mastery achievement. AH-64 follows **Settings → Language** and falls back to English. These translations are machine-translated; corrections are welcome.

### Changed

- Skill tooltips and the character description are filled from `AH64.language`. The numbers still come from the mod, so translated tooltips retain the existing default balance values and dynamic descend bindings. The gatling splash tooltip prints 13.5% instead of the raw float 13.500001%. The Agile keyword on the chin guns and the Hydra is separated from the next word by a space.
- The English and translated READMEs describe the current controls and loadouts in stacked skill sections that fit narrow screens. Seven newly recorded skill clips span Titanic Plains, Distant Roost and Siphoned Forest; five current clips are retained, all twelve share uniform sizing, and the historical skill loops are removed.

### Fixed

- Embedded English text remains available when the loose translation file is missing, malformed, or incomplete. Recovery logs a warning instead of leaving players with raw language tokens.

---

## 1.3.0

Guided Hellfire, Banked Break, flight-path bombing, and modular loadout presentation.

### Added

- AGM-114 Hellfire laser guidance: press Special to launch a slow lead missile, hold to accelerate and steer toward your aim, release to slow it, then hold again to guide the same live lead without spending stock. The laser originates at the nose optics. A new lead can launch after the previous one ends; primary and secondary stay available. Pocket I.C.B.M.'s two fan missiles remain ballistic and unguided.
- Banked Break utility: a sweeping 90-degree turn that maintains flight. Movement input selects left or right; neutral input turns right. Aim and fire throughout. Default cooldown: 4 seconds.
- Bombing Run special: six bombs released every 0.3 seconds along your flight path. Each deals 300% damage in a 6m blast, with at most three hits per enemy per run and a 10-second default cooldown. Other weapons and utilities remain available; interrupted drops are lost. Bomb carriers, release animation, impact footprint and explosion audio make the run visible and audible.
- Separate special and utility attachments in character select and gameplay: Longbow rails, Hellfire launchers, bomb carriers, and utility thrusters, smoke hardware or banking fins.
- Distinct illustrated icons for all ten active loadout skills, embedded in the plugin independently of the Unity bundle. The passive retains its original icon.

### Changed

- M230 magazine reduced to 20 rounds.
- Evasive Roll and Smoke Backflip capture entry momentum and ease through their speed ramp into normal flight.
---

## 1.2.1

Balance patch. Hover height and airtime are unchanged, and so is the Hellfire.

Saved settings that were still on the previous default for base speed, M230 direct damage, M230 HE splash, or XM301 direct damage move to the new default. A value you set yourself is left alone.

### Changed

- M230 chain gun: time per round 0.09s → 0.10s. Direct damage 0.62 → 0.52. HE splash 0.34 → 0.26. Proc coefficient 0.35 → 0.50. The direct hit and the HE blast both deal 75% damage at 10m or closer, rising to full damage at 30m.
- XM301 rotary cannon: direct damage 0.39 → 0.34. Proc coefficient 0.20 → 0.30. Same 10m–30m damage ramp as the M230, on the direct hit and the splash.
- Hydra-70 rockets: time between rockets 0.10s → 0.115s. A rocket deals 75% damage within 8m of flight, rising to full damage at 25m. Each rocket past the base six (Backup Magazine and any other extra stock) adds 0.8s to the magazine reload, so extra magazines add burst rather than a matching rise in sustained damage.
- AGM-114L Longbow: recharge per missile 3.5s → 4.1s. Per-missile damage stops climbing at the sixth missile (6.75), so extra stocks from Lysate Cell no longer hit harder than that.
- Evasive Roll: armor 300 → 200, and the armor lasts about 0.95s (the length of the roll) instead of about 2.85s. The short invulnerability window is unchanged.
- Fire Control Radar: close-range armor +60 → +30.
- Eclipse Lite: a primary reload counts as 2 cooldowns instead of 4.
- Base move speed 10 → 8.5.

---

## 1.2.0

Altitude controls rework with airtime, an item compatibility pass, a crash on death, Army Green and Night Stalker paint, and a model polish pass.

### Fixed

- A held altitude no longer sinks while flying forward. Your chosen height and the altitude ceiling were both measured from the ground below, so rising terrain lowered the height for good and falling terrain dragged you down with it. Climb, release, and the aircraft now holds that altitude until you descend or airtime runs out.
- Lysate Cell adds a missile to the Longbow rack per stack. It previously only shortened the recharge, so the Hellfire, which does gain a charge, pulled ahead once you had a couple of cells ([#13](https://github.com/johnstonstu/ror2-ah64/issues/13)). Extra missiles continue the Longbow damage ramp: 7.3, 7.85 and so on.
- Hydra pods reload to their full size with Backup Magazine. A reload used to add back only six rockets, so a larger pod needed several reloads to fill.
- Eclipse Lite no longer overpays on reloads. It counted every round in the drum as a separate cooldown, so a single M230 reload was worth about half your maximum health in barrier. A full primary reload now counts as four cooldowns (a partial top-up counts for its share, at least one), and a Hydra reload as one.
- Longbow missiles count as special-skill damage, like the Hellfire, for items that care which skill dealt the hit.
- Wax Quail works. Tapping the collective while sprinting gives the forward boost, with a 1.5 second cooldown.
- Bustling Fungus works. A hovering body never stood perfectly still, because the altitude hold bobs it slightly, so the healing zone never appeared. Only horizontal movement counts now.
- H3AD-5T v2 works. Hold interact while above resting height to slam; the slam detonates when you reach resting height, with damage scaling from how high you started. Interacting at resting height, such as opening a chest, no longer starts a slam.
- Pressing the collective counts as a jump for items and mods that react to jumping, once per climb.
- Luminous Shot gains one stack per Hydra ripple instead of one per rocket, so a single ripple no longer fills it.

### Changed

- Altitude controls reworked for controllers. Hold jump to climb and hold descend to drop: B on a controller, which the game leaves unbound, or C on keyboard. Releasing both holds your height. Pulling the stick back no longer loses altitude, so you can strafe backwards at height.
- The resting hover holds level instead of tracing the ground. Rising ground lifts it; over dips and downhill it eases down gently, so small bumps no longer make it bob. Flying off an edge gives a slow sink while airtime lasts, then it drops to hover height. Classic altitude controls keep the 1.1 ground-following hover.
- The aircraft hovers slightly higher: resting height 3.75 above the ground (was 3), and the collective climbs up to 12 above that (was 10).
- Items on the ground are collected when you hover over them at resting height, as if you touched them. Previously the pickup only registered while the hover dipped close enough.
- A **Classic altitude controls** option in the mod settings restores the 1.1 controls: release jump to settle back to resting height, pull back to settle faster, and no airtime limit.
- Time above resting height is limited to 20 seconds of airtime, which refills in about 2 seconds once you are back at resting height. Each extra jump (Hopoo Feather and similar) adds 2.5 seconds of airtime and 3 units of climb height, and no longer raises the resting height, so chests and ground items stay in reach however many you carry. Airtime and airtime per extra jump are configurable. How fast airtime runs down depends on how you fly: holding station over one spot, turning and fighting, costs very little (about 100 seconds from a full tank), while pushing the stick, flying fast, straying from where you climbed and climbing each spend it faster (about 16 seconds flat out). Each kill pauses the drain for 0.75 seconds, at most once every 1.5 seconds, so clearing a crowd stretches your airtime without freezing it.
- The altitude ceiling is measured from the ground where you last rested, and utilities no longer add height on top of a climb. Climbing onto ledges far above the arena by chaining the collective with Evasive Roll or Smoke Backflip no longer works.
- Painting Longbow targets no longer replaces the crosshair.
- Pocket I.C.B.M. affects the Hellfire: two extra missiles fanned out at 25° either side of the aim point. Further stacks add nothing, since three Hellfires already triple the payload.
- Desert is now Desert Tan, a realistic Army desert CARC with a matte finish and black low-visibility markings. It keeps its place in the skin list, so saved loadouts are unaffected.
- Desert Tan and Arctic keep the airframe's panel lines, rivets, vents and weathering. Both used to render as flat, featureless paint.
- Skin icons are vanilla-style colour swatches of each paint scheme (body, mechanical, markings and canopy glass). The Mastery icon remains a rendered portrait.
- The gun housing under the cockpit no longer glows red as the magazine empties. The reload sounds are unchanged.
- Model polish:
  - Scissor tail rotor.
  - Swept, bevelled main rotor blade tips.
  - Faceted canopy glass whose sides lean in to a narrower roof, as on the Apache.
  - Rounded Hellfire seeker noses.
  - A visible TADS/PNVS sensor turret at the nose tip. The old sensor sat behind the chin gun.
  - The chin gun sits further back and lower, under the gunner's station, with a gap behind the nose sensor.
  - Countermeasure pods on the stub-wing tips.
- The aircraft dips its nose as it speeds up and flares nose-up when it brakes, and leans further forward while sprinting, easing in and out of the sprint.
- Army Green is lighter, with mechanical parts at the default skin's brightness, and the matte finishes keep enough sheen to show the airframe's shape under stage lighting.
- Dark mechanical parts on every skin catch slightly more highlight.
- The rotor sound follows the blades: silent inside the drop pod, rising in pitch as they spool up, and winding down with them on death instead of cutting off.
- The chain gun's reload puffs smoke from the barrel. It used to spawn a small explosion flash.
- New character portrait, rendered from the 1.2 airframe and framed on the cockpit and chin gun so it reads in the small select-screen and scoreboard tiles.
- The Mastery achievement icon shows the Night Stalker paint it now unlocks.

### Added

- A small white airtime tick just under the crosshair. It appears when you climb above resting height, shrinks toward the centre as airtime drains, warms to amber when low and red when empty, and fades out once it has refilled.
- Army Green paint scheme, in US Army CARC aircraft green with a matte finish and black low-visibility markings.
- Rotors spool up as the aircraft leaves the drop pod, run slightly faster under the collective and hard manoeuvres, and wind down when it dies.
- Rotors start up and turn slowly in character select when you pick the AH-64, over a quiet engine idle.
- A subtle drift in pitch and roll while hovering in place, so a stationary aircraft no longer looks frozen.
- The aircraft crashes when it dies. The tail lets go and it spins, noses down, trails smoke and explodes on impact. It used to hang in the air and vanish.
- The engines trail smoke below 35% health, more often as health falls.
- Weapons kick the airframe: a nose-up jolt from each M789 shell, a light shudder from the chain guns, and a lift on the firing side for rockets and missiles.
- The aircraft banks into turns when flying forward, and heavy hits jolt the airframe.
- Night Stalker paint scheme, semi-gloss black with dull red markings, unlocked by the AH-64 Mastery achievement (beat the game or obliterate on Monsoon). The achievement previously unlocked nothing.
- Changing primary in character select plays a weapon-swap clunk as the chin gun changes over.

---

## 1.1.2

Missile launch audio fix, primary muzzle-flash fix, a quieter radar scan, and listing updates.

### Fixed

- Firing Hydra rockets or Longbow no longer leaves a missile fly-by loop running on the aircraft. The launch sound starts a loop that normally ends with the missile, so every volley stacked another whine under the rotor for the rest of the run.
- The M230 no longer flashes purple under the aircraft. The extra effect was Bandit's muzzle flash, whose sprite and light are magenta; the barrel flash is Commando's warm muzzle flash. The XM301 and M789 used the same effect.

### Added

- `ah64_audio_scan` console command. It lists every sound currently playing in the scene, with its name and distance, for audio bug reports.

### Changed

- Fire Control Radar's scan is much quieter visually. The red shockwave ring around the mast every eight seconds is gone; the radar dome on the mast now glows soft teal for about a second on each scan. The target marker and acquire ping are unchanged.
- New Thunderstore icon.
- Updated Thunderstore description.
- The README opens with a short intro line and a link to like the mod on Thunderstore.
- New README clips for the M230, XM301 and M789 showing the corrected muzzle flash.

---

## 1.1.1

Documentation only. No gameplay changes.

### Changed

- The README now shows a short looping in-game clip for every skill.

---

## 1.1.0

### Added

- Three distinct primary weapon models, with immediate loadout previews in character select.
- Desert and Arctic paint schemes alongside Olive, with matching lobby previews and skin-aware weapon heat.
- More airframe detail: inboard missile racks, recessed engine openings, tapered cowls and wing roots, and rotor hub detail.
- Optional Risk of Options controls for movement, primary weapons, utility, impact visuals, rotor wash and rotor volume.
- Settings reports with comments, changed defaults and pending restart values. Copy a report and open a prefilled GitHub form, with clipboard fallback for long reports; review before submitting.

### Changed

- Custom Wwise rotor audio responds to movement and abilities. The local pilot hears a consistent rotor bed; remote aircraft use distance fading.
- Rotor volume is the only exposed Audio slider. Existing development audio tuning remains compatible with saved profiles.
- Refreshed primary icons and documentation, with direct bug-report, balance-feedback and discussion links.
- Builds stage locally by default; profile deployment is opt-in.
- Removed automatic rotor playback diagnostics and routine console chatter; explicit audio diagnostics remain available.

### Fixed

- Primary weapon visibility and lobby skin bindings, including skin changes after asynchronous material updates.
- Gameplay materials retain their authored colors; lobby readability adjustments use separate materials.
- Rotor clearance, canopy/intake clearance and landing-gear alignment.
- Smoke Backflip starts nose-up. Hover preserves external vertical forces, equipment flight and lift volumes; AH-64 is immune to fall damage.
- Rotor wash follows terrain, and rotor blur/wash respect cloak, invisibility and death.
- The rotor sound restarts when the chopper is re-enabled, such as after a respawn or stage change.
- The smoke-ring effect used by the Hydra and Longbow launches and the dash thruster loads from its verified Addressables catalog address. The old lookup used an internal asset ID, which raised `InvalidKeyException`.

### Known limitations

- Gameplay tuning is not synchronized between players.
- Missile rack depletion follows local special stock, including Longbow lock reservations; remote and late-join presentation needs dedicated verification.
- Laser-guided Hellfire was deferred in 1.1; guidance is added in 1.3.0.

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
