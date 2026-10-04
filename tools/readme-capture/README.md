# Optional README capture helper

This separately compiled BepInEx plugin is never part of AH64.dll or the release package. Build with `./tools/readme-capture/Build-Helper.ps1`. Output: `dist/readme-capture-helper/bin/Release/netstandard2.1/AH64.ReadmeCapture.dll`.

The build uses cached GameLibs 1.4.1-r.0, Unity 2021.3.33, BepInEx 5.4.20 and MMHOOK 2025.12.9 without network package sources. It generates its project under ignored dist. It checks the frozen accepted DLL before and after the build; runtime checks the loaded AH64 assembly against SHA256 `DEB8659CB3D1DAE028B8525D4E6C3EADA5C88DE5DD9FA57E1AA98846BD02D266`.

## Runtime contract

Only the runtime owner may stage the helper or launch/record the game. This preparation did neither. Opt-in requires both process environment variables:

- `AH64_README_CAPTURE_MODE=showcase-v1`
- `AH64_README_CAPTURE_DIR=<existing absolute fresh output directory>`

Leave every old `AH64_AUTOPILOT_*` switch unset. The helper refuses an old autopilot mode or a reused markers file. It copies the existing proven solo bootstrap: wait for title/local user, set loaded profile `canSave=false` and clear pending save **before** in-memory survivor preference changes, host one local user, wait for NetworkUser.Start and selection acknowledgement, launch seed1301, then enter golemplains. Save protection remains through process shutdown. No preference is written to disk.

Five bounded scenes use native `GenericSkill.ExecuteIfReady`, contextual skill overrides, scripted input, disposable AI-disabled 100000HP native Golems, and invincible player. Scenes: guided Hellfire hold/release/rehold; moving Banked Break; six-drop path bombing over targets; three live-body special/utility attachment pairs; current M230 firing. Actual production skill logic supplies all projectiles, damage, attachments and effects. A native `ICameraStateProvider` frames the actors and footprint; this is a controlled showcase, not a physical-input demonstration. The arena is golemplains, not a custom testing stage.

The global deadline triggers at 238 seconds after helper initialization, with orderly Application.Quit one second later. Script exceptions produce a failed result and orderly quit. Bootstrap/hash/gate failures before initialization must still be handled by the runtime owner's outer timeout. No screen-capture API is used here.

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

The package README uses prospective public media URLs. `dist/readme-publication-manifest.json` lists exact asset paths/hashes; publication remains a separate approval step. Existing historical media paths are preserved.
