# AH64 controlled SOLO foundation

This foundation is an opt-in diagnostic for the unchanged `fe3c6ca` gameplay baseline.
It is not release acceptance. It does not establish multiplayer, physical controller
behavior, normal input activation, audio, hit attribution, complete revolutions or feel.
Normal launches install no autopilot hooks and write no autopilot evidence.

Offline checkpoint: Release compilation passed with 0 errors and 25 warnings
(22 baseline plus two FOV reads and one legacy teleport call in the harness).
32 evidence/lease checks and 12 synthetic strict/policy access cases passed. The real-game
scan resolves every reference and reports 31 non-public sites, exactly matching the
unmodified baseline DLL with no added sites. Strict mode rejects those sites; the
explicit reviewed runtime policy below can classify them as supported. Neither
result is a runtime pass. The integrator's verified 1.2.1 bundle/bank snapshots are available;
this worker did not copy them into generated packaging paths or run the game.

## Integration hook and ownership

Integrator: add `DevAutopilot.TryStart();` at the end of `AH64Plugin.Awake()`, after
`new Modules.ContentPacks().Initialize();`. The SDK includes `DevAutopilot*.cs`
automatically; no csproj addition is needed. This worker does not edit that shared file.
The harness never changes framework, registries, config entries, tuning or version.

Both environment gates are required: `AH64_AUTOPILOT_MODE=solo-baseline-v1` and an
absolute `AH64_AUTOPILOT_DIR` naming a new prepared execution directory with
`identity.json`. Use the launcher to set these only for its child process.
Do not persist either variable in a player or machine environment.

## Bounded baseline contract

Before hosting, the opt-in harness sets the loaded player's public `canSave` and
`saveRequestPending` flags false for this diagnostic process. The actual game's
`RequestEventualSave` and logout paths respect `canSave`; no player tuning or saved
progress is copied into the disposable mod profile. The flags remain false through
process shutdown so diagnostic progress is not persisted.

The harness hosts `host 0`, selects AH64, fixes seed 1301, enters Titanic Plains,
disables combat directors and grants test-only invincibility. It checks a fixed
terrain mark and chooses the longest unobstructed facing from 24 fixed directions.
More than one local/network user fails the SOLO guard. Every wait has a deadline.

The 12 required assertions are:

| Scenario | Stable assertion IDs | Required observation |
| --- | --- | --- |
| Evasive Roll | `roll.enter`, `.travel`, `.exit`, `.cleanup` | Requested Body state observed; forward displacement >0.1m; original main state resumed; FOV override and air-control ownership released |
| Smoke Backflip | `backflip.enter`, `.travel`, `.exit`, `.cleanup` | Requested Body state observed; rearward displacement >0.1m; original main state resumed; same cleanup |
| Hellfire | `hellfire.enter`, `.launch`, `.exit`, `.cleanup` | Requested Weapon2 state observed; exactly one launch request and one observed owned projectile; original weapon state resumed; observed projectile gone within 30s |

These scenarios inject existing entity states to isolate enter/exit behavior. They
bypass stock, cooldown, alternate-loadout selection and button activation. No I.C.B.M.
is allowed in this baseline. Future tests must separately cover input activation,
side/reverse/high-speed entries, interruption/death/walls, guidance tokens and payload.
Launch-request evidence and observed spawn/despawn evidence are separately labeled.
Disappearance does not establish an impact, damage, proc or crit result.

## Evidence and failure semantics

Each stage and execution gets a GUID directory under ignored `dist/autopilot/`.
`stage.json` records exact source SHA, dirty status/fingerprint, plugin version,
candidate and installed artifact hashes, full game/dependency/config inventories,
reviewed asset provenance, reservation identity and a per-target rollback map.
`dirty.json` contains the tracked binary diff and untracked file hashes.

`telemetry.jsonl` uses schema version 1 records `header`, `sample`, `event`,
`assertion`, `summary`, matching the coordination contract. The header links full
identity and independently hashes loaded DLL/bundle/bank/config/game assembly.
Every physics sample records tick/time, body/owner identity, authority, position,
velocity, facing, native aim and model quaternion. Movement capture/progress and
guidance token are explicitly unverified in this baseline, rather than guessed.
Model attitude is the latest LateUpdate pose read at physics cadence; it is not a
second transform writer or a claim of a freshly evaluated physics pose.

`telemetry.csv` supports before/after comparison. `trace.txt` records assertions
and observed state transitions. `runtime.log` preserves all observed Unity messages,
including loading. The launcher archives the whole profile `LogOutput.log` and the
dedicated Unity `Player.log`, including shutdown. Missing/stale/empty logs fail.
The harness requests five bounded game-engine PNG captures at maneuver entry/cleanup
and Hellfire launch. A missing/empty capture times out the diagnostic. Inspect the
actual images; these frames do not establish full revolutions, feel, audio or physical
controller acceptance. The terminal capture count records files actually observed.

The atomic terminal `result.json` and final JSONL summary must agree on a complete
suite, the 12 distinct assertion IDs, all passing results and sample counts. The
launcher also validates artifact identities, continuous CSV ticks and finite values.
Exit 0 alone cannot pass: missing evidence, incomplete runs, timeouts, skipped or
duplicated assertions, errors, warnings and untriaged visual flags all return failure.
Attitude changes >60 degrees per sampled tick are heuristic visual flags, independent
of semantic assertions. They require review; the foundation has no blanket exclusions.

## Offline preparation (no game or profile writes)

Set worktree-local CLI home, NuGet caches, TEMP/TMP and disabled reusable build
servers, following the integration bootstrap helper. Set
`DOTNET_GENERATE_ASPNET_CERTIFICATE=false` before the first SDK invocation.
Always compile with `/p:AH64DeployToProfiles=false`.

```powershell
dotnet build AH64Mod/AH64.csproj -c Release --no-restore /p:AH64DeployToProfiles=false /p:UseSharedCompilation=false /nodeReuse:false
pwsh -NoProfile -File tools/dev-profile/Test-Foundation.ps1
pwsh -NoProfile -File tools/dev-profile/Check-Access.ps1 -DependencyDirectories $runtimeDllDirectories -OutputDirectory $newAccessDirectory
pwsh -NoProfile -File tools/dev-profile/Test-Access.ps1 -ScannerDll $scannerDll -CecilPath $restoredCecil
```

The scanner resolves real game APIs from the installed game's Managed folder first,
never publicized GameLibs. Supply explicit runtime dependency directories (including
loader, hooks, R2API and patchers). Missing directories and every unresolved assembly,
type or member are failures. Output identifies all resolved assembly paths/hashes.
Protected inheritance is recognized; conservative non-public findings require review.
Default mode remains strict. No generic private-access ignore option exists.

### Explicit supported Unity Mono policy

The existing plugin declares assembly `SecurityPermissionAttribute(RequestMinimum,
SkipVerification=true)` and module `UnverifiableCodeAttribute`. Unity Mono's
[assembly decoder](https://github.com/Unity-Technologies/mono/blob/unity-2021.3-mbe/mono/metadata/assembly.c#L5245),
[eligibility check](https://github.com/Unity-Technologies/mono/blob/unity-2021.3-mbe/mono/mini/mini.c#L896) and
[method/field checks](https://github.com/Unity-Technologies/mono/blob/unity-2021.3-mbe/mono/mini/method-to-ir.c#L5853)
explain why this existing declaration supports the reviewed member accesses on the
identified Unity Mono runtime. The scanner models that behavior only when supplied
an explicit integrator-reviewed manifest. It changes no assembly, game, OS security
setting or access grant, and verifies metadata without executing the candidate.

Keep the manifest in ignored run evidence. Generate it after the final candidate
build using the reviewed triage pins, never by automatically approving fresh findings:

```powershell
$triage = Get-Content -LiteralPath $triagePath -Raw | ConvertFrom-Json
$policy = @{
    schema = 1
    policy = 'unity-mono-requestminimum-skipverification-v1'
    candidateSha256 = (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash
    reviewEvidence = @{ path = (Resolve-Path $triagePath).Path; sha256 = (Get-FileHash -LiteralPath $triagePath).Hash }
    runtime = @{
        unityPlayer = @{ path = Join-Path $gameDirectory 'UnityPlayer.dll'; sha256 = $triage.UnityPlayerSHA256 }
        mono = @{ path = Join-Path $gameDirectory 'MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll'; sha256 = $triage.MonoSHA256 }
    }
    reviewedSites = @($triage.UnmodeledNonPublicSites)
}
$policy | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $policyPath -Encoding UTF8
pwsh -NoProfile -File tools/dev-profile/Check-Access.ps1 -Dll $dll -DependencyDirectories $runtimeDllDirectories -OutputDirectory $newAccessDirectory -RuntimePolicy $policyPath
```

The policy binds the exact candidate bytes, existing permission metadata, review
evidence hash and installed UnityPlayer/Mono paths and hashes. The entire non-public
site set (member, caller and IL offset) must match the distinct reviewed set. Changed,
new, removed or unreviewed sites, absent/false declarations, runtime/hash mismatch,
non-member/type visibility findings and unresolved references fail. A changed DLL
requires a new candidate hash; altered sites or runtime require integrator re-review.
Raw `inaccessible` findings remain in `access.json`, alongside `supported`,
`unsupported`, `candidatePermission`, policy identities and `policyErrors`.

Staging recompiles the candidate, rebuilds the owned scanner and performs a fresh
scan against the installed game and actual profile dependency directories. Earlier
or edited access JSON cannot authorize staging. Supply the same `-RuntimePolicy`
manifest to the scan and stager. Stage archives the fresh proof, policy and review
evidence; launch preflight also verifies the native runtime hashes. This establishes
the modeled API-access gate, not actual game loading or gameplay acceptance.

## Reserved serial trial (integrator only; not performed by this task)

1. Integrator grants owner/run token and the canonical absolute lock path:
   `<integration-root>/dist/coordination/runtime.lock`. Prepare its parent directory.
   Atomic CreateNew acquisition fails if occupied, unreadable or partially written.
   No stale timeout takeover; only matching owner/token can release, with game closed.
   Stage retains the lease through launch and full-log capture. Failure restores the
   targeted allowlist; a blocked rollback retains the lease for integrator review.
2. Prepare only `AH64 1.3 Dev`, install the mod and its declared/transitive dependencies,
   and place `.ah64-disposable-test-profile` containing exactly `AH64 1.3 Dev` in its
   root. Create no player profiles through these scripts. Close the game normally.
3. Supply a reviewed dependency-lock JSON: `packages` contains each exact manifest
   dependency string once and `directories` contains existing profile-relative DLL
   directories, including the BepInEx core directory for its loader package. Empty or
   missing directories fail. All core/plugins/patchers DLLs are separately inventoried;
   package labels are reviewer-provided claims, not manager listing proof. Config and
   support additions/deletions or changed bytes after staging fail launch preflight.
4. Supply explicit DLL, bundle, `AH64Rotor.bnk`, AH64 config, successful `access.json`,
   and integrator `provenance.json`. Baseline snapshot reuse accepts the schema from
   `dist/bootstrap/runtime-assets/baseline-1.2.1`: matching source trees, unchanged asset
   inputs, input-manifest hash, exact artifact hashes and audio-license hash. Snapshots
   remain outside canonical packaging paths. No Unity/Wwise rebuild is implied.
   The DLL must be this worktree's Release output. Staging recompiles with profile
   deployment disabled and compares SHA/dirty/diff/untracked identities before and
   after compilation, preventing an arbitrary old binary from claiming current source.
   The supplied access scan must still match the resulting exact DLL bytes.
5. Invoke staging only after access review, then launch the retained lease:

```powershell
pwsh -NoProfile -File tools/dev-profile/Stage-Build.ps1 -Dll $dll -Bundle $bundle -Bank $bank -Config $config -DependencyLock $dependencyLock -AccessResult $accessJson -AssetProvenance $provenance -RuntimePolicy $policyPath -RuntimeOwner $owner -RuntimeReservation $token -RuntimeLockPath $canonicalLock
pwsh -NoProfile -File tools/dev-profile/Run-Autopilot.ps1 -StageRecord $stageJson -RuntimeOwner $owner -RuntimeReservation $token -RuntimeLockPath $canonicalLock -ValidateOnly
pwsh -NoProfile -File tools/dev-profile/Run-Autopilot.ps1 -StageRecord $stageJson -RuntimeOwner $owner -RuntimeReservation $token -RuntimeLockPath $canonicalLock
```

`-AllowDirty` is explicit diagnostic use, not clean acceptance. Backups are completed
before any replacement. Only DLL, AssetBundles/ah64, the rotor bank/source license,
mod manifest and AH64 config are replaced. Partial staging failure restores prior
bytes/removes newly created target files, preserving backups and failure records.
Rollback restores only paths listed by that stage, with game closed. A launcher
timeout may stop only its returned PID; failed/missing results never become passes.

Start with one baseline run. Review all warnings/access findings/visual flags and
terrain selection before expanding scenarios or comparing feature branches. Then
repeat the same suite/artifact/config identities for a before/after trace. Normal
controller play, multiplayer owner/observer tests and exact-ZIP clean acceptance
remain separate reserved trials. No main merge or publication is authorized.

## Bootstrap lifecycle and first-trial diagnosis

The first trial at integration SHA `c9e5302` failed before scenarios: 0/12
assertions, zero physics samples/captures, and a generic body prerequisite error.
Its archived execution is
`dist/autopilot/20261003-034518-28e98ac65375452182d7ff794876a8cf/execution-cf98ee6162a14ef498867c83a7a46577`.
The null `survivorDef` exception comes from installed `RoR2.NetworkUser.Start`,
which links the local user before reading `userProfile.GetSurvivorPreference()`.
It interrupts startup before `onPostNetworkUserStart`. Merely seeing a linked
network user was therefore insufficient readiness proof. The later generic body
failure did not identify which prerequisite was missing; its precise cause remains
unproven by that trace.

The corrected bootstrap waits for onLoad/title/local user and the canonical AH64
catalog entry. It disables saving first, sets the loaded profile's AH64 preference
in memory **before hosting**, waits for the actual local network user's completed
startup event, and requires the server-observed body preference before launch.
Bootstrap phase events include scene, user counts, client/server status, expected
and actual body index/name, authority, and specific missing component/control-chain
or state-machine prerequisites. Body initialization waits remain bounded and strict.
A stage change also requires a different cached body before using the fixed arena.
No saved survivor preference or gameplay defaults are changed on disk.

Run `tools/dev-profile/Test-Bootstrap.ps1` alongside the existing offline protocol
and access checks. It compiles the real bootstrap coroutine against local doubles
and covers null/stale preferences, delayed/missing catalog, linked but unfinished
network startup, delayed/missing selection acknowledgment, each body prerequisite,
and stale versus freshly rebuilt stage bodies. It never loads or executes RoR2.

Log classification remains separate from fixing bootstrap. The installed
`NetworkManagerSystem.ClientAddPlayer` explicitly logs "already added, aborting"
and returns when a valid player controller already exists; those lines alone do
not prove a second player. `ClientSetPlayers` also emits "For iteration" at Unity
error severity in normal enumeration. BepInEx debug lines naming exception-hook
types are handler-registration text, which the launcher's word-based exception
detector currently overcounts. These are source-backed classification findings,
**not exclusions**: the failed run is still failed and all original evidence is
preserved. Actual null-survivor exceptions, cursor confinement errors, shader/
addressable failures, startup warnings and visual flags remain blocking and need
individual review. A successful bootstrap does not imply a clean suite result.

The next trial at `527db92` completed AH64 bootstrap, including a fresh fixed-arena
body, but failed before assertions while disabling combat directors. Installed
`CombatDirector.OnDisable` removes the disabled component from `instancesList`,
invalidating a live list enumerator. The harness now disables a snapshot; the
offline bootstrap checks include two directors that remove themselves synchronously.
All 12 scenario assertions and the no-combat setup remain required. The failed trial,
full logs and their blocking classifications remain preserved under `dist`.

The director-corrected trial at `bfeea5f` passed `roll.enter`, then capture failed:
`Player.log:844` reported a 2560x1440 request against an active 512x512 target. The
target's owner was not established by that log. Capture now waits for end-of-frame
as before, explicitly binds the window backbuffer (`RenderTexture.active=null`),
and synchronously reads exactly `Screen.width` by `Screen.height` pixels. A nested
`finally` restores the prior target before PNG encoding, and the temporary texture
is destroyed on success or exception. No camera render, resize or display setting
change occurs. Capture events record source, dimensions, frame/time, prior target
and camera target names; the actual PNGs still require visual inspection.
The existing five capture points and ten-second file checks remain required.
`tools/dev-profile/Test-Capture.ps1` covers mismatched targets and exception cleanup.
API references: [Unity 2021.3 ReadPixels](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Texture2D.ReadPixels.html),
[active render target](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/RenderTexture-active.html),
and [end-of-frame timing](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/WaitForEndOfFrame.html).

The `b861b22` backbuffer trial passed all 12 ability checks, but its five PNGs were
identical black frames. Each read logged `ReadPixels ... while not inside drawing
frame`; the frames are rejected as visual evidence. Diagnostics identified the
prior target as `crtexVoidSunCaustics`, not a portrait target. Capture now requires
exactly one `SceneCamera` whose rig viewer is the local user and target is the AH64
body. It renders that existing gameplay camera into a temporary target at the
camera's original pixel dimensions, then reads that explicit target. It preserves
camera pose/FOV/settings, restores its target and global active target in `finally`,
and releases temporary resources. Unity errors during capture and flat RGB images
fail before PNG acceptance. Captures show the gameplay scene without the UI camera;
this supported camera render is additional diagnostic render work, not a new pose.
[Unity Camera.Render](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Camera.Render.html)
describes the existing camera settings, image filters and render callbacks used.

An explicitly reserved comparison may pass `Run-Autopilot.ps1 -VisibleWindow`.
It changes only that launch's window style and enables a run/capture/frame marker;
no OS, display, resolution, privacy or security setting is changed. The existing
render algorithm remains unchanged for comparison. Before render/read, diagnostics
record camera pose/culling and the postprocess layer's public enabled/final-blit
state, plus command-buffer names/sizes. Private cached destinations are not read.
An external capture must bind to the exact returned PID/window and comparison
request; unrelated desktop/app capture is excluded. A flat result from a window
capture API alone does not prove the visible game viewport itself is black.

## Owned-window baseline capture contract

The reserved visible comparison at `b2064a2` showed real AH64, Titanic Plains and
HUD pixels in the exact owned game window, while the camera-to-texture output
remained flat. Its public postprocess flag was `finalBlitToCameraTarget=False`;
cached command-buffer destinations were not observed and are not a confirmed cause.
The replacement baseline path reads the original owned game client on screen using
installed Pillow `ImageGrab.grab(bbox=verified_bounds, all_screens=True)`. It performs no extra camera render, target
swap, postprocess mutation, game input, display change or simulation pause.

Supply `Run-Autopilot.ps1 -VisibleWindow -WindowCapturePython <installed-python>`
alongside the existing stage/owner/reservation/lease arguments. That Python must
already have Pillow and pywin32. The launcher owns the
helper PID, starts it hidden, captures its full stdout/stderr, and restores the
child-only `AH64_AUTOPILOT_WINDOW_CAPTURE` environment gate. The previous
`AH64_AUTOPILOT_WINDOW_COMPARE`/OnGUI diagnostic marker is retired.

Each of the five unchanged checkpoints publishes a fresh GUID token, run/body,
Body and Weapon2 state, scenario phase, request frame/fixed/realtime/UTC, candidate,
PID/executable and canonical lease identity. The helper verifies stage/request/
lease and the unique visible owned HWND before and after reading screen pixels.
The owned game must remain foreground and unobstructed: visible, uncloaked windows
above its client reject capture before pixels are read, including layered windows
whose transparency is not established. Nine interior hit tests must resolve to the
game. Physical client-to-screen bounds, monitor offset, virtual screen bounds and
a thread-only DPI context are recorded; the prior thread context is restored.
It records actual capture start/end UTC, original client dimensions, RGB extrema
and PNG hash, then atomically publishes the matching acknowledgement. Game-side
frame/physics observations must retain the requested body/states/phase through
acknowledgement; Hellfire also requires a surviving owner projectile. A changed
phase, stale token/process, flat pixels, wrong hash/dimensions or ten-second
deadline rejects the capture. No later image is relabelled as an earlier phase.
The request frame is not claimed as the precise captured rendered frame.

All 12 assertions, continuous physics samples, five PNGs and complete raw logs
remain required. `window-captures` preserves request/ack/phase records; review
the five actual PNGs separately from automated nonflat/provenance checks. The
offline `Test-Capture.ps1`, `Test-WindowIdentity.py` and `Test-Foundation.ps1`
checks exercise rejection paths using labelled fixtures, not gameplay images.
Raw warning/error counts are preserved; classification and acceptance of exact
pre-existing limitations remain a separate review from the strict raw verdict.

The first full window-bridge trial (`3ef9ccb`) completed 12/12 assertions and 1,058
continuous samples, with successful body/state/phase acknowledgements. Visual
inspection nevertheless rejected all five checkpoint images: every original PNG
was byte-identical, including an unchanged HUD clock, across eleven seconds of
moving simulation and different states. Nonflat pixels and correct HWND/process
identity do not establish fresh presentation. The precise cache/render cause is
unproven; no postprocess flag, security or display change is justified by this.
The bridge now rejects duplicate PNG bytes across these distinct moving baseline
phases, preserving any rejected pixels under their token rather than accepting a
later phase label. Its helper also writes an explicit terminal record, avoiding
reliance on a missing Process-wrapper exit code; an observed nonzero exit remains
blocking. The first launcher reported helper failure with empty stderr. This refinement
has offline checks; it is not evidence of a subsequent successful game trial.

A subsequent direct-screen diagnostic on the unchanged `d1f1fd7` control, after
the user explicitly approved temporarily disabling Discord's overlay, produced
two distinct native 2560x1440 images within one roll. Visual review confirmed
an advancing HUD clock (00:04.30 to 00:04.68), changing airframe pose and world view,
with foreground/physical bounds and unobstructed checks passing. Discord's original
ON setting was restored through its normal UI in finally. This diagnostic deliberately
sent no phase acknowledgement and therefore did not complete the 12-assertion suite.
The integrated screen source now rejects the earlier cached HWND provenance and
requires owned foreground/physical-client metadata. A full new run is still needed
to qualify five checkpoints; this diagnostic alone is not that baseline verdict.
