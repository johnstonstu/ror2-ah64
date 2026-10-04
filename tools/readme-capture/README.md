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
