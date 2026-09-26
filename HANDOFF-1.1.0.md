# AH-64 1.1.0 — local dev handoff

> **Next task / current status:** Read [HANDOFF-1.1-WEAPON-MODELS.md](HANDOFF-1.1-WEAPON-MODELS.md).
> It identifies the accepted combined build and the existing staging worktree.
> Continue weapon-model and live lobby-preview work there, not in the live checkout.


Updated 2026-09-26 for the next voice playthrough/review chat. Read `AGENTS.md`
first. This file's current-test sections supersede the older `AH64_HANDOFF.md`
release status and the historical notes explicitly labeled below.

## Current combined visual/audio playtest (supersedes audio-only installation below)

The isolated codex/ah64-visuals-1.1 worktree now contains the completed visual,
audio and hover sources. The combined ZIP is installed into demo time new with
unchanged configuration and a complete backup. Read VISUAL-STAGING-1.1.md for
hashes, paths, checks and outstanding gameplay acceptance. The primary checkout
was not changed by this integration; its sources remain independently available.
No push/publication. Do not deploy the older audio-only DLL over this candidate.
## Deferred: laser-designated Hellfire (after 1.1)

Decision 2026-09-26: Stuart shelved this idea for a later release. **Keep the
current Hellfire and Longbow behavior for 1.1.** This is a saved proposal, not
approved implementation work; no code or assets were changed for it.

- Concept: hold special and maintain aim on one enemy for roughly 2–3 seconds,
  then automatically launch one heavy guided Hellfire. Keep Longbow's existing
  hold-to-paint, release-to-launch salvo.
- Proposed prototype: 2.5-second designation; movement, primary and Hydra remain
  available; release early to cancel without spending stock; changing enemies
  resets progress. Allow 0.2 seconds of paused progress for brief aim loss, but
  require a valid visible target at launch. Guidance continues after launch
  without further aiming. Require a fresh press before another designation.
- Untested starting numbers: 2,000% damage, 8-second cooldown starting at launch,
  one stock, 150-unit range, 8-unit blast radius, 1.0 proc coefficient. Initially
  keep designation time independent of attack speed; revisit capped acceleration
  after playtesting. None of these numbers are settled.
- Implementation sketch: dedicated DesignateHellfire state on Weapon2 followed
  by launch; adapt Longbow targeting with tighter aim tolerance and track target
  HealthComponent across hurtboxes. Consume stock only on successful launch;
  handle interruption, dead targets and asset failures. Reuse existing missile
  art/rail effects with a separate guided Hellfire configuration.
- Presentation: designation progress reticle, thin laser or target spot, distinct
  Hellfire icon (currently borrows Hydra). Procedural visuals and existing Wwise
  cues suffice for a prototype; a nose laser anchor can follow with model work.
  Avoid overlap with the passive radar indicator.
- Before release: verify cancellation spends nothing, walls prevent acquisition,
  target changes reset progress, one completed designation fires exactly once,
  gun/Hydra remain usable, and host/client stock, guidance and cleanup agree.
  Compare against Longbow on bosses, elites and moving airborne enemies.

## Current test: candidate C Wwise rotor installed 2026-09-26

Ready for user playtest in **demo time new**. Wwise 2023.1.4.8496 authoring was
installed at `C:\Audiokinetic\Wwise_2023.1.4.8496`. The rotor now uses an embedded
PCM Wwise bank routed to the game's SFX_BUS, with tracked playback ID and bounded
start retries. Local pilot is 2D; other aircraft use 3D panning and distance fade.
Live volume, pitch, load response and tone controls remain; cutoff Hz is now an
approximate tonal target for Wwise's perceptual low-pass scale.

The game was closed before installation. Previous mod folder and config are in
`dist/audio-c-backup-20260926-140602`. Rotor volume reset from 1 to **0.30**;
all other saved controls preserved. No public release, commit or push.

Validation: C# build succeeded (22 existing obsolete-API warnings, zero errors);
Wwise generation succeeded; bank format/ID, embedded media and play/stop events
validated; package includes only AH64Rotor.bnk, never the generated Init.bnk.
Embedded PCM has 131418 samples matching the source within one 16-bit quantization
step. Installed DLL, bank and unchanged Unity bundle match build hashes.

DLL SHA-256: `BB5422C31544A47D710FC94C846A54B17CECFC9BC1586B126BD53CE46DF9F04C`
Bank SHA-256: `DC9C5018207019BA8010190D7024479AAC2047B0F00CAA8FE05FC0A705AEC0C3`

**In-game playback is still unverified.** Start stationary, without firing;
compare rotor volume 0 and 0.30. Logs automatically sample playback position
three times. `ah64_audio_status` prints position and mix on demand. Confirm
advancement and looping, volume/pause/focus behavior, death/stage cleanup and
remote aircraft before calling the audio fixed. Inspect Player.log if needed.
Build bank with `tools/build-rotor-bank.ps1`; packager checks freshness and contents.

## Root cause: candidate B rotor playback failed

Read `AUDIO-RESEARCH.md` before any further rotor work. The installed game's
`globalgamemanagers` has **AudioManager.m_DisableAudio = true**. Candidate B logs
`playing=False` with nonzero gain, an active listener and no pause. Unity-based
rotor playback is the wrong backend for this game; further EQ/slider changes
cannot fix it. Migrate the loop to a Wwise bank/emitter. Runtime version verified
as **2023.1.4.8496**, bank format 150. Migration is implemented in candidate C above;
the research report preserves the user's previous tuning for comparison.

## Historical: audio candidate B, installed 2026-09-26

This section supersedes the candidate-A installation/hashes below. Luna's
"AH-64 1.1 Rotor Audio Test" reported faint rotor audio even with Rotor volume,
master and SFX all at maximum; weak spawn audibility; disliked rotor tone; and
over-loud XM301 wind-up/down. Stuart requested a heavier helicopter chop and
temporary live tuning sliders. Once tuned, remove most of those controls for
public release; retain the simple rotor volume control.

**Ready for local listening in `demo time new`.** Game was confirmed closed
before installing. DLL and bundle hashes match the build outputs. Only Rotor
volume was reset in the saved config, from 1.0 to 0.45; other settings preserved.
Nothing committed, pushed or published. Version remains 1.1.0.

### Changes and remaining uncertainty

- The local pilot hears a 2D rotor bed, independent of Unity listener distance.
  Other aircraft retain 3D attenuation. Source begins at configured gain, with
  priority 64. Pause, focus mute, death and re-enable handling remain in place.
- Source recording was already healthy (-12.17 dBFS RMS), so faintness cannot be
  blamed on a quiet file alone. Distance attenuation is removed as a possible
  cause for the pilot, **not confirmed as the original root cause**. Candidate B
  logs actual gain, settings gain, Unity listener volume/pause and listener
  distance once after spawn. Read the Unity Player.log if BepInEx GUI redirects
  detailed output away from LogOutput.log.
- Heavier EQ of the existing aquinn CC0 recording: bass shelf, rumble removal,
  gentler treble. Natural blade rhythm retained, with no synthetic pulse train.
  Original WAV is in `Art/Audio/`; `tools/prepare_rotor.py` reproduces the edit.
- XM301 wind-up/down now use a dedicated Wwise emitter at 0.6 gain (~-4.4 dB).
  Body gunfire is unaffected. The dedicated emitter is stopped on disable and
  weapon swap. Gain errors and failed event posts log warnings.
- Wwise implementation reference: https://www.audiokinetic.com/zh/library/2024.1.4_8780/?id=soundengine_environments.html&source=SDK
  (`SetGameObjectOutputBusVolume`, invalid listener ID applies to all listeners).
- Unity's local validation builder now builds without installing, so verification
  can run while a game is open. Normal menu build retains its install behavior.

### Listen and tune

Risk Of Options > AH-64 > AH-64 Playtest - Presentation:

| Control | Candidate-B default | Purpose |
| --- | --- | --- |
| Rotor volume | 0.45 | Overall bed level, before master/SFX |
| Rotor pitch | 1.0 | Lower gives a slower/heavier chop (0.7-1.2) |
| Rotor high-frequency cutoff Hz | 5000 | Lower removes hiss; 20000 bypasses filter |
| Rotor movement boost dB | 2 | Load loudness (0-6 dB); source caps at gain 1 |
| Rotor movement pitch change | 0.015 | Set to zero for constant rotor RPM |
| Rotor response seconds | 0.6 | Smooth movement response (0.1-2 seconds) |
| Gatling spool volume | 0.6 | Wind-up/down only (0-1); next transition uses value |

Rotor controls apply on update; changes made while paused are heard on resume.
First test idle spawn with no firing, then cruise/climb, then short XM301 bursts.
Compare rotor at 0.45 and 1.0, move far from spawn and rotate camera: local volume
should not fade with position. Confirm the cannon transitions soften without
changing sustained gunfire. Check pause/resume, master/SFX zero, focus mute,
death/respawn and stage transition. Save preferred values before removing controls.
Host/client mixing remains untested. Perceived tone and in-game loudness need
Stuart's ears; offline validation does not establish that these are fixed.

### Validation and rollback

- Release `dotnet build --no-restore` passed: 0 errors, 22 existing obsolete-API warnings.
- Unity 2021.3.33f1 batch validation passed on the rebuilt disk bundle: exactly
  one rotor, mono/44100 Hz/131418 frames; decoded peak .8912 and RMS .2837;
  seam check passed; both model prefabs still have 25 non-null mesh filters.
- WAV preparation checks: -1.00 dBFS peak, -10.94 dBFS RMS, no silent 20 ms blocks,
  seam step .001068; low/high-band energy ratio increased 7.29 dB.
- `tools/pack.ps1 -SkipBuild` passed version, freshness and archive checks.
- Candidate DLL: `6516912576E7D45E1FACE601907BE2FDB09E88058910C57097FC5385A5C52C4A`.
- Candidate bundle: `4E51A70178F5FEAD9B4BE9A4E9807503C9099463251A90CA64D2439F6843B25D`.
- Current ZIP: `dist/AH64-1.1.0.zip`; prior ZIP preserved as
  `dist/AH64-1.1.0-before-audio-b.zip`.
- Pre-B installed DLL, bundle and config backup:
  `dist/audio-b-backup-20260926-132457/`.
- Unity verification log: `dist/unity-audio-candidate-b.log`.

## Historical candidate A: previous next-chat instructions

Stuart is opening another chat in this same folder for a voice-guided playthrough
and review. The requested fixes are implemented and installed; start by testing
this build, not rebuilding or implementing the original backlog again.
All work is uncommitted on `dev/1.1.0`, including new untracked source files.
Preserve those files. Nothing was pushed, merged, tagged or published.

1. Launch **Modded** from r2modman's **demo time new** profile. Unity need not be
   running to test. The profile's mod-list label may still show an old version;
   the installed DLL identifies as 1.1.0 in BepInEx.
2. Capture each observation as expected/actual behavior, stage, loadout/items,
   reproduction steps, and relevant log lines. Separate feel preferences from bugs.
3. Prioritize rotor audibility and comfort, then backflip direction, then launch/
   force handling and Chrysalis. Keep a baseline observation before tuning.
4. Do not overwrite the profile while the game is running. No pushes or releases
   are authorized. Public release still needs Stuart's explicit approval.

### Playthrough success criteria

- Rotor is audible at spawn and stays continuous through hover/cruise/climb;
  no obvious loop click, beating or excessive pitch change. Weapons/enemies remain
  easy to hear. Pause/resume, master/SFX sliders and focus mute behave as intended.
- Smoke Backflip raises the nose first, travels backward and finishes upright;
  camera and normal hover recover without a snap.
- Jump pads reach their destinations. Landings cause no fall-damage health loss
  (test without god mode/invulnerability, which would mask a failure).
- Enemy knock-ups/downward pulls visibly displace the body instead of being
  immediately cancelled. Long vertical lifts remain effective beyond 10 seconds.
- Chrysalis can climb beyond the normal ceiling and ground-probe range, hold
  altitude when collective is released, and descend after expiry. Record whether
  lingering vanilla wings/slow-fall feel wrong; do not assume their presence means
  powered flight is still active.
- Death, respawn and stage transitions do not leave stuck or missing rotor audio.
  Host/client multiplayer and a fresh-profile ZIP install are still untested.

### Logs, exact build, rollback

Profile root:
`C:\Users\stuwj\AppData\Roaming\r2modmanPlus-local\RiskOfRain2\profiles\demo time new`

- Runtime log: `BepInEx/LogOutput.log` beneath that profile.
- Config: `BepInEx/config/com.JohnstonStu.AH64.cfg`; new audio key is `Rotor volume`
  under `AH-64 Playtest - Presentation`, default .22. Old three-layer keys no longer
  control the rotor. Read actual saved values before concluding a default is wrong.
- Local package: `dist/AH64-1.1.0.zip`, SHA-256
  `440BA962DDB2F7A3791275EB48FA203E1CF610D7D8AFA493230E3ADB99F8C116`.
- Installed DLL SHA-256:
  `54B10BB601E3FBAF6C901A54864BF6ABF9A6C841682747780612DDCF653AB276`.
- Installed bundle SHA-256:
  `E3B2D0B40E978D5F799075342D7BD1FEB6B2D459363CF8732755954DF4018B62`.
- Prior local installation backup: `dist/local-backup-20260926/`. With the game
  closed, restore `AH64.dll` to the profile's `BepInEx/plugins/JohnstonStu-AH64/`
  and `ah64` to its `AssetBundles/` subfolder. Restore the backed-up config only
  if wanted, preserving any subsequent tuning first. This backup is the prior
  local installation, not a claim that it was a freshly installed release ZIP.

### Review map and known gaps

- `AH64FlightAudio.cs` and new `AH64AudioSettings.cs`: one source, lifecycle,
  load response and game-volume bridge. Missing-listener warning exists; neither
  pre-test log showed listener errors, but live camera changes remain unverified.
- `AH64HoverController.cs` and new `.ExternalMotion.cs`: balanced granter
  ownership, ApplyForceImpulse hook, fall immunity and Chrysalis behavior.
  The body flag grants general fall immunity, broader than only jump-pad landings.
- `AH64StaticValues.cs`: audio tuning and negative backflip pitch.
- `AH64PlaytestConfig.cs` / `AH64RiskOfOptions.cs`: single new rotor setting and
  corrected slider range. Obsolete audio clips remain bundled but are not played.
- Unity audio WAV/importer/provenance and new `AH64LocalTestBuilder.cs` (+ meta):
  lossless import and disk-bundle checks. Unity MCP was unavailable in this chat;
  batch Unity 2021.3.33f1 completed successfully and exited. Do not open with Unity 6.
- Plugin/manifest are both 1.1.0. Changelog clearly marks this unreleased.
- Build and asset validation passed; nobody has yet listened to this mix in-game
  or verified gameplay behavior. No credential or environment-file changes.

## Where things stand

### Local test pass, 2026-09-26

Implementation now exists on `dev/1.1.0`; still no commits/pushes/releases from this pass.
Plugin and package version are 1.1.0 for local testing (plain numeric, no suffix).
The earlier backlog/audio description below records the starting point.

- Single aquinn CC0 rotor loop from Dust Front replaces the hover WAV. Original
  prepared loop hash and provenance are in `AH64Audio/LICENSE_SOURCE.txt`.
  Unity imports lossless mono PCM at 44.1 kHz; idle gain .22, pitch .96 to 1.02,
  load gain capped at +3 dB. The two old layers remain bundled but are not played.
- New `Rotor volume` setting prevents inherited old gain from making the new,
  louder recording overwhelming. Risk Of Options exposes the correct 0..1 range.
- Rotor component resumes after re-enable, stops on death, and follows pause,
  master/SFX controls and focus mute. A missing Unity listener logs a diagnostic;
  no global listener is created or modified.
- Backflip uses -360 degrees (nose-up first).
- Hover owns and removes only its own flight/gravity contributions; vertical
  force impulses immediately yield control, lifts no longer time out after 10s,
  and the body ignores fall damage.
- Active Chrysalis bypasses the hover ceiling using existing collective controls.
  On expiry it returns to the normal hover/descent path. Vanilla expired wings
  and slow-fall can remain until actual landing; verify this during playtesting.

Validation: Release C# build passes (existing obsolete-API warnings); Unity
2021.3.33f1 batch build verifies exactly one new rotor clip (mono, 131418 samples,
44100 Hz) and 25 non-null mesh filters in each of mdlAH64 and AH64Display.
Gameplay, listening balance and host/client behavior remain unverified.

Installed to `demo time new/BepInEx/plugins/JohnstonStu-AH64`: DLL and bundle
SHA-256 match the build outputs. The compiled BepInPlugin attribute is 1.1.0.
`dist/AH64-1.1.0.zip` passed pack.ps1 version, freshness and archive checks.
The mod manager's installed-package metadata was left intact, so its list can
still show the older downloaded version; the game loads the verified 1.1.0 DLL.

Build command: `dotnet build AH64Mod/AH64.csproj -c Release /p:AppData=<empty-local-directory>`
avoids the project's automatic all-profile DLL deployment. Copy only to the
selected test profile afterward. Unity batch entry is
`AH64LocalTestBuilder.RunFromCommandLine`; its existing builder installs the
bundle into `demo time new`. The local backup is `dist/local-backup-20260926/`
(DLL, bundle, config). Unity verification log: `dist/unity-local-test.log`.

First playtest: hover/cruise/climb and pause audio; Smoke Backflip; jump-pad landing
with no health loss; enemy vertical displacement; Chrysalis ascent beyond normal
ceiling, release to hold, expiry to descend; stage transition/death audio cleanup.

### Starting point

- `main` = exactly what is live on Thunderstore: **1.0.1**, tagged `v1.0.1` (commit `d836ad0`). Do not commit to `main` directly.
- 1.1.0 work happens on the local branch **`dev/1.1.0`** (branched from `v1.0.1`). **Nothing is pushed to GitHub yet.** Keep it local until Stuart decides to push.
- `ror2-chopper` (and `ror2-chopper-backup`) is the retired pre-1.0 project. Do not develop or release from it.

## Historical: original 1.1.0 backlog (implemented, awaiting playtest)

1. **Rotor audio rework (priority).** The main rotor sound doesn't always play and doesn't sound good. Stuart is sourcing/generating better clips (Higgsfield or elsewhere). See "Audio today" below.
2. **Smoke Backflip direction.** Playtest confirmed it pitches nose-DOWN first (a front flip). `SmokeBackflip` rotates +360° on Euler X; by the convention in `AH64FlightVisuals` (negative X = nose up), it should be −360° (or flip the sign of whatever drives the pitch). Retest the look after.
3. **Hover rework (was on hold until 1.0.1 shipped; now unblocked).** Stuart's design:
   - No fall damage after launches (jump pads etc.).
   - Knock-ups and vertical pulls from enemies/items should actually move the chopper.
   - Milky Chrysalis gives uncapped flight (provisional).

Deferred / not yet tested in 1.0.1: multiplayer (host + client on the same version).

## Historical: audio before this test build

- Code: `AH64Mod/Characters/Survivors/AH64/Components/AH64FlightAudio.cs`.
- Three looping Unity `AudioSource`s added at runtime, all 3D:
  - `rotorHoverLoop` — the constant bed. File: `AH64UnityProject/Assets/AH64/Bundle/AH64Audio/sfxAH64RotorHoverGrounded.wav`
  - `rotorInFlightLoop` — fades up with horizontal speed. File: `.../AH64Audio/sfxAH64RotorInFlight.mp3`
  - `rotorClimbLoop` — fades up while climbing. File: `.../AH64Audio/sfxAH64RotorClimb.mp3`
- Volumes come from `AH64PlaytestConfig` (`RotorHoverVolume`, `RotorInFlightVolume`, `RotorClimbVolume`); pitch, distances, doppler and fade time from `AH64StaticValues` (`rotor*`).
- The collective "chirp" is a vanilla Wwise event (`Play_captain_drone_quick_move`) via `Util.PlaySound`.
- Current clips are CC0 (qubodup pack); licence notes in `AH64Audio/LICENSE_SOURCE.txt`. **Any new clip needs a licence that allows redistribution in a mod** — record the source and licence in that file.
- Gatling audio uses vanilla Wwise events on purpose (custom soundbanks are limited to six events), see `AH64GatlingSpin.cs`.

### Historical dropout leads (lifecycle/settings addressed; verify in game)

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
- Remember that a normal build on `dev/1.1.0` overwrites the DLL in those profiles.
  Use the isolated AppData override above for compile-only. Do not switch this
  dirty tree to main for rollback; restore the backup or install a known release ZIP.
- DebugToolkit basics: `~` opens the console. Useful commands: `set_scene <name>` (use `list_scene <text>` for names, e.g. `moon2`, `golemplains`, `solutionalhaunt`), `peace`, `god`, `buddha`, `teleport_on_cursor`, `time_scale`.

## Release (when 1.1.0 is ready)

1. Bump `MODVERSION` in `AH64Mod/AH64Plugin.cs` **and** `version_number` in `Build/manifest.json` to `1.1.0` (BepInEx needs `major.minor.patch`). Add a `CHANGELOG.md` entry.
2. Playtest in `demo time new`, then `pwsh -File tools/pack.ps1` builds `dist/AH64-1.1.0.zip` (no upload). Install the zip in a fresh empty profile to check.
3. Push the branch, open a PR into `main`, merge, tag `v1.1.0` on `main`, re-pack from `main`.
4. Upload the zip at thunderstore.io/package/create/ as team **JohnstonStu**, community Risk of Rain 2. (Thunderstore shows the listing as "rejected" review status; that has been harmless for 1.0.0 and 1.0.1.)

## Standing rules

- No merges, tags, pushes to `main`, Thunderstore uploads, or replies on GitHub issues without Stuart's explicit go-ahead.
- Players' issues are answered only after the fix ships, and Stuart approves each reply.
