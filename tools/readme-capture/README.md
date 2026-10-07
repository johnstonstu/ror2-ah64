# Optional README capture helper

This separately compiled BepInEx plugin is never part of AH64.dll or the release package. Build with `./tools/readme-capture/Build-Helper.ps1 -FrozenDll <accepted AH64.dll path>`. Output: `dist/readme-capture-helper/bin/Release/netstandard2.1/AH64.ReadmeCapture.dll`.

The build uses cached GameLibs 1.4.1-r.0, Unity 2021.3.33, BepInEx 5.4.20 and MMHOOK 2025.12.9 without network package sources. It generates its project under ignored dist. It checks the frozen accepted DLL before and after the build; runtime checks the loaded AH64 assembly against SHA256 `DEB8659CB3D1DAE028B8525D4E6C3EADA5C88DE5DD9FA57E1AA98846BD02D266`.

## Runtime contract

Only the runtime owner may stage the helper or launch/record the game. Use the designated disposable profile, preserve saves/settings and hold the shared runtime lease until the game exits and cleanup completes. Opt-in requires both process environment variables:

- `AH64_README_CAPTURE_MODE=showcase-v1`
- `AH64_README_CAPTURE_DIR=<existing absolute fresh output directory>`

Leave every old `AH64_AUTOPILOT_*` switch unset. The helper refuses an old autopilot mode or a reused markers file. It copies the existing proven solo bootstrap: wait for title/local user, set loaded profile `canSave=false` and clear pending save **before** in-memory survivor preference changes, host one local user, wait for NetworkUser.Start and selection acknowledgement, launch seed1301, then enter golemplains. Save protection remains through process shutdown. No preference is written to disk.

Five bounded scenes use native `GenericSkill.ExecuteIfReady`, contextual skill overrides, scripted input, disposable AI-disabled 100000HP native Golems, and invincible player. Scenes: guided Hellfire hold/release/rehold; moving Banked Break; six-drop path bombing over targets; three live-body special/utility attachment pairs; current M230 firing. Actual production skill logic supplies all projectiles, damage, attachments and effects. A native `ICameraStateProvider` frames the actors and footprint; this is a controlled showcase, not a physical-input demonstration. The arena is golemplains, not a custom testing stage.

The global deadline triggers at 418 seconds after helper initialization, with orderly Application.Quit one second later. Script exceptions produce a failed result and orderly quit. Bootstrap/hash/gate failures before initialization must still be handled by the runtime owner's outer timeout. The original five-scene mode uses an external recorder; the new three-batch mode records Unity framebuffer images as described below.

## Recorder outputs

`markers.jsonl` is flushed per event. Fields: `kind`, `scene`, `detail`, `utc` (ISO8601 UTC), `realtime` (Unity seconds), `frame`, `pid`, `sequence`. Scene names are `hellfire`, `banked-break`, `path-bombing`, `loadouts`, `m230`. `clip-start` / `clip-end` also atomically create `<scene>.start.json` / `<scene>.end.json`. Record the owned game window before launch, then segment using UTC boundaries. Markers do not assert that a requested frame equals a captured frame.

`hit` records native server damage against owned target fixtures; `bomb-observed` records each discovered native owned bombing projectile once. `phase`, `activation`, `loadout`, and bootstrap records provide context. `capture-result.json` reports completion or failure and total hit observations, with a frozen DLL hash. Completion means script completion, not visual acceptance. Review footage for visible targets, all six drops, missile reacquisition and impacts; compare markers rather than assuming intent proves success.

Compilation succeeded with only the existing two-argument TeleportBody and legacy assembly security attribute obsolescence warnings. No runtime launch, profile write, capture, or production source/DLL modification was performed during preparation.

## Completed README recording, 2026-10-04

The integration owner subsequently recorded the accepted f9cb6ce DLL. The initial helper attempt failed before capture because an internal reset method was searched with public-only reflection; the helper now explicitly includes nonpublic instance methods. Production gameplay was unchanged.

- Run `704a40cc8207442c8d48c4c33bc6602d`: five scenes, 700 verified owned-client frames at 2560x1440; one Hellfire hit, 16 bombing damage events, 40 M230 damage events and six observed bombing projectiles. Retain its Hellfire, bombing and M230 clips.
- Run `548632b95fbe4a2dab1c1037e6123a9d`: tighter Banked Break and attachment framing. Set `AH64_README_CAPTURE_SCENES=framing-retake` only for this two-scene mode. Retain its two clips.
- Both successful runs exited normally, removed the temporary helper and released the runtime lease. Preservation reports contain no mismatches or restored files. Evidence is under `dist/readme-capture/<run>/`.
- Final silent WebP loops are under `docs/images/1.3.0/`; each is 800x450 and retains recorded timing. MP4 versions and source recordings remain in ignored `dist`. These controlled showcase clips are not physical-controller or multiplayer acceptance.
- Contact sheets and full-size attachment samples were reviewed. Target damage and bombing impacts are in view; the closer attachment view clearly shows utility changes. No claim of separately validated special-attachment geometry is made by this recording.

The package README uses prospective public media URLs. `dist/readme-publication-manifest.json` lists exact asset paths/hashes; publication remains a separate approval step. The 2026-10-06 refresh replaces the historical skill media; the older banner and skin preview remain explicitly labeled.

## Varied-stage recording, 2026-10-06

The new scene batches keep the same accepted production DLL and use native skills, scripted inputs, invincibility, a controlled camera and disposable high-health targets. They are presentation footage, not physical-input or multiplayer acceptance. Native enemy attack states can continue after their AI components are disabled.

| `AH64_README_CAPTURE_SCENES` | `AH64_README_CAPTURE_ARENA` | Shots |
| --- | --- | --- |
| `coastal` | `blackbeach` | Radar tracking between a Beetle and Golem, Hydra ripple against a group, Longbow painting/release |
| `guns` | `golemplains` | XM301 sustained firing/reload, M789 heavy shells/recoil |
| `maneuvers` | `snowyforest` | Evasive Roll and Smoke Backflip with a following camera |

Arena names are allowlisted. Each batch finds terrain-backed firing lanes in the actual scene rather than reusing another stage's coordinates. The helper temporarily sets a 1280x720 window, disables only HUD tutorial popups in memory, and leaves the native HUD and combat effects visible.

`Capture.Frames.cs` samples Unity's rendered framebuffer after `WaitForEndOfFrame`, targeting 16 fps. It writes numbered high-quality JPEGs and `frames.jsonl` with capture UTC timestamps into the opted-in output directory. `recorder.json` identifies the source and dimensions; the first successful image creates `recording.ready`. Segment the images by actual UTC markers and retain their elapsed timing when encoding silent 800x450 WebP loops. No OS window backing surface is used: a title-based GDI recorder produced stale frames during review and was rejected, while the screen recorder correctly refused an overlapping Discord window.

Before replacing media, review samples throughout every clip, verify that the airframe and action remain visible, check native activation/damage records for weapon shots, and reject static frames or large capture gaps. Retain the five useful 2026-10-04 clips. Replace the seven historical embedded clips and remove the two unused historical skill loops after checking repository references. Both player READMEs display all twelve loops at width 640.

Accepted clean takes:

- `coastal-659079b12936468ab93b34fb2d36e7e0`: radar 128 source frames; Hydra 79 frames / six native damage events; Longbow 106 frames / two damage events.
- `guns-d9bcb63b1f434c38afd53f1845f62726`: XM301 146 frames / 210 damage events; M789 146 frames / 52 damage events.
- `maneuvers-e08f584d9e58457abc03803666687db8`: Evasive Roll 66 frames; Smoke Backflip 77 frames. The following camera retains the airframe before and after the native cloak.

All seven clips were reviewed across their timelines; maximum source capture gap is below 0.1 seconds. Exports retain elapsed timing at 16 fps. Earlier static-window recordings, tutorial-prompt takes and the fixed-camera backflip were rejected. Raw images, timestamps, MP4 copies, review sheets and preservation reports are in ignored `dist/readme-retakes/<run>/`. `dist/readme-refresh-validation.json` contains the current asset hashes and check results.

Each accepted run exited normally, removed the temporary helper and released the runtime lease. The game settings file was restored after the temporary window-size change. Final checks confirm that all original saves, profile settings and mod configuration match the initial session backup; Steam may refresh its `remotecache.vdf` metadata between runs. The final run's thirteen protected files all match its backup. Production AH64.dll retained its accepted hash. Nothing was published.
