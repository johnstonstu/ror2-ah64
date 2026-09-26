# AH-64 1.1.0 — local dev handoff

Written 2026-09-26, right after 1.0.1 shipped. This file is for whoever picks up 1.1.0 work in this folder (Stuart, ChatGPT, or another agent). Read `AGENTS.md` and `AH64_HANDOFF.md` too; they still apply.

## Where things stand

- `main` = exactly what is live on Thunderstore: **1.0.1**, tagged `v1.0.1` (commit `d836ad0`). Do not commit to `main` directly.
- 1.1.0 work happens on the local branch **`dev/1.1.0`** (branched from `v1.0.1`). **Nothing is pushed to GitHub yet.** Keep it local until Stuart decides to push.
- `ror2-chopper` (and `ror2-chopper-backup`) is the retired pre-1.0 project. Do not develop or release from it.

## 1.1.0 backlog

1. **Rotor audio rework (priority).** The main rotor sound doesn't always play and doesn't sound good. Stuart is sourcing/generating better clips (Higgsfield or elsewhere). See "Audio today" below.
2. **Smoke Backflip direction.** Playtest confirmed it pitches nose-DOWN first (a front flip). `SmokeBackflip` rotates +360° on Euler X; by the convention in `AH64FlightVisuals` (negative X = nose up), it should be −360° (or flip the sign of whatever drives the pitch). Retest the look after.
3. **Hover rework (was on hold until 1.0.1 shipped; now unblocked).** Stuart's design:
   - No fall damage after launches (jump pads etc.).
   - Knock-ups and vertical pulls from enemies/items should actually move the chopper.
   - Milky Chrysalis gives uncapped flight (provisional).

Deferred / not yet tested in 1.0.1: multiplayer (host + client on the same version).

## Audio today

- Code: `AH64Mod/Characters/Survivors/AH64/Components/AH64FlightAudio.cs`.
- Three looping Unity `AudioSource`s added at runtime, all 3D:
  - `rotorHoverLoop` — the constant bed. File: `AH64UnityProject/Assets/AH64/Bundle/AH64Audio/sfxAH64RotorHoverGrounded.wav`
  - `rotorInFlightLoop` — fades up with horizontal speed. File: `.../AH64Audio/sfxAH64RotorInFlight.mp3`
  - `rotorClimbLoop` — fades up while climbing. File: `.../AH64Audio/sfxAH64RotorClimb.mp3`
- Volumes come from `AH64PlaytestConfig` (`RotorHoverVolume`, `RotorInFlightVolume`, `RotorClimbVolume`); pitch, distances, doppler and fade time from `AH64StaticValues` (`rotor*`).
- The collective "chirp" is a vanilla Wwise event (`Play_captain_drone_quick_move`) via `Util.PlaySound`.
- Current clips are CC0 (qubodup pack); licence notes in `AH64Audio/LICENSE_SOURCE.txt`. **Any new clip needs a licence that allows redistribution in a mod** — record the source and licence in that file.
- Gatling audio uses vanilla Wwise events on purpose (custom soundbanks are limited to six events), see `AH64GatlingSpin.cs`.

### Leads on "rotor sound doesn't always play" (unconfirmed — verify in game)

- `OnDisable()` stops all three sources, but there is no `OnEnable()` to start them again. If the body is ever disabled and re-enabled, the rotor stays silent for the rest of that life. Adding an `OnEnable` that calls `Play()` (when a clip exists) is a cheap fix to try.
- These are plain Unity `AudioSource`s, not Wwise. RoR2's own audio runs through Wwise, so the in-game volume sliders and pause don't affect them, and they depend on a Unity `AudioListener` being active on the current camera. If the listener changes (death/spectate camera, some cutscenes), Unity audio can drop out.
- If `CreateLoopSource` can't find a clip in the bundle it logs a warning ("... was not found in the ah64 bundle") and that layer stays silent. Check `BepInEx/LogOutput.log` for it.

### Swapping in new rotor clips

1. Replace (or add alongside) the files in `AH64UnityProject/Assets/AH64/Bundle/AH64Audio/`. Keep them seamless loops. WAV or high-bitrate OGG is safest.
2. If names change, update where `AH64Assets` loads `rotorHoverLoop` / `rotorInFlightLoop` / `rotorClimbLoop`.
3. Rebuild the asset bundle in Unity: **AH64 > Build AssetBundle**. `tools/pack.ps1` refuses a stale bundle.
4. Tune volume/pitch in `AH64PlaytestConfig` / `AH64StaticValues`, not by editing the clip.

## Build and test

- `dotnet build -c Release` in `AH64Mod`. A post-build step copies `AH64.dll` into **every r2modman profile that has `JohnstonStu-AH64` installed** (currently `demo time new`, which also has DebugToolkit).
- The old `demo time` profile has the mod under its old name (`Stu-AH64`) and does NOT receive builds. Don't test there.
- Remember that building on `dev/1.1.0` overwrites the DLL in those profiles. Switch to `main` and rebuild to get back to the released 1.0.1.
- DebugToolkit basics: `~` opens the console. Useful commands: `set_scene <name>` (use `list_scene <text>` for names, e.g. `moon2`, `golemplains`, `solutionalhaunt`), `peace`, `god`, `buddha`, `teleport_on_cursor`, `time_scale`.

## Release (when 1.1.0 is ready)

1. Bump `MODVERSION` in `AH64Mod/AH64Plugin.cs` **and** `version_number` in `Build/manifest.json` to `1.1.0` (BepInEx needs `major.minor.patch`). Add a `CHANGELOG.md` entry.
2. Playtest in `demo time new`, then `pwsh -File tools/pack.ps1` builds `dist/AH64-1.1.0.zip` (no upload). Install the zip in a fresh empty profile to check.
3. Push the branch, open a PR into `main`, merge, tag `v1.1.0` on `main`, re-pack from `main`.
4. Upload the zip at thunderstore.io/package/create/ as team **JohnstonStu**, community Risk of Rain 2. (Thunderstore shows the listing as "rejected" review status; that has been harmless for 1.0.0 and 1.0.1.)

## Standing rules

- No merges, tags, pushes to `main`, Thunderstore uploads, or replies on GitHub issues without Stuart's explicit go-ahead.
- Players' issues are answered only after the fix ships, and Stuart approves each reply.
