# ror2-ah64 — AH-64

A custom Risk of Rain 2 survivor: **AH-64**, an attack helicopter that hovers
and never lands.

The display name `AH-64` and the code identifier `AH64` are intentionally
different — see the name mapping below.

> **This repo is production.** `ror2-ah64` is the live tree — all real work,
> releases and Thunderstore packages come from here.
>
> `ror2-chopper` is the retired predecessor, kept as a scratch/test sandbox and
> as the historical record at tag `pre-1.0-cleanup`. **Never package or release
> from it**, and never copy code forward from it without checking against this
> tree first — it still contains the whole HenryTutorial template inheritance.
>
> The survivor was originally developed there, forked from a finished
> `ror2-droid` survivor built on the HenryTutorial template. That history and
> all of the template's leftover assets were left behind in the 1.0.0 cleanup.
> **Nothing in this tree should reference Henry, the droid, or the tutorial
> template.** If you find such a reference, it is a bug, not history.

Global rules in `~/.codex/AGENTS.md` apply. The TypeScript/Node standards do
**not** — this is a C# BepInEx mod. Ignore any instruction to add `tsconfig.json`,
`.env`, `package.json`, or the `src/ tests/` layout here.

---

## Environment facts

Re-deriving these wastes a session each time.

| Thing | Value |
| --- | --- |
| Unity editor | **2021.3.33f1**, **Built-In** pipeline. **Standalone install** at `C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe` — *not* a Unity Hub install |
| Game install | `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2` |
| Deploy target | r2modman profile **`demo time`** (`%AppData%\r2modmanPlus-local\RiskOfRain2\profiles\demo time`) |
| Target framework | `netstandard2.1`, `LangVersion 7.3` |
| Test / scratch sandbox | `C:\Users\stuwj\Documents\Coding\ror2-chopper` (tag `pre-1.0-cleanup`) — retired predecessor. Never release from it |
| Test profile | r2modman **`demo time new`** — clean, only this mod's declared dependencies |
| Legacy profile | r2modman **`demo time`** — 30+ unrelated mods incl. the old droid. Not a valid release test |
| Thunderstore package | **`JohnstonStu-AH64`** — install folder name is `<author>-<name>` |
| Blender | **5.2.0 LTS**, standard blender.org installer at `C:\Program Files\Blender Foundation\Blender 5.2` (not on PATH). Config at `%AppData%\Blender Foundation\Blender\5.2` |

> **Unity lives outside Unity Hub.** Hub's editor list shows only `6000.5.4f1`;
> checking Hub alone gives the false impression that 2021.3.33f1 is missing.
> It is installed and licensed (`C:\ProgramData\Unity\Unity_lic.ulf`).
>
> **Always launch with the project path pinned** — a bare `Unity.exe` opens and
> immediately exits with return code 0, because the project isn't registered in
> Hub. This has bitten twice:
>
> ```powershell
> & "C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe" `
>   -projectPath "C:\Users\stuwj\Documents\Coding\ror2-ah64\AH64UnityProject"
> ```
>
> Never open this project with `6000.5.4f1` — a wrong-version open silently
> rewrites every `.meta` file.

### Reading RoR2's actual source

Do not guess at engine behaviour. `ilspycmd` is installed globally
(`dotnet tool install -g ilspycmd`) and decompiles any game type on demand:

```bash
ilspycmd -t RoR2.CharacterMotor \
  "C:/Program Files (x86)/Steam/steamapps/common/Risk of Rain 2/Risk of Rain 2_Data/Managed/RoR2.dll"
```

**Point it at the game install, not at the NuGet `RiskOfRain2.GameLibs`
package.** GameLibs is a stripped reference assembly — every method body is
`throw null`, so decompiling it gives signatures and nothing else. The game's
own `Managed/RoR2.dll` has real IL. (GameLibs *is* publicized, though, so
`protected`/`private` members are reachable from mod code even when the
decompiled source shows them as non-public.)

This is how Phase 2's hover was designed. It answered in minutes what the
handoff had listed as questions to settle by playtesting. It also settled
renderer granularity — see below.

**Not every RoR2 type is in the `RoR2` namespace.** `ChildLocator` is in the
*global* namespace, so `-t RoR2.ChildLocator` fails with "Could not find type
definition"; use `-t ChildLocator`. Don't read that error as "the type isn't in
this assembly."

### One renderer = one material

A hard engine constraint, verified in `RoR2.dll`, not a style preference.
`CharacterModel.RendererInfo` holds a **single** `defaultMaterial`, and
`CharacterModel.UpdateRendererMaterials` ends with
`renderer.sharedMaterials = array` where `array` is
`[defaultMaterial, ...up to 6 overlays]` — slot 0 base, slots 1+ overlays (on
fire, elite, cloaked). `UpdateMaterials` applies that to every entry in
`baseRendererInfos` whenever materials go dirty.

So **a mesh with two material slots renders wrong in game** — its extra submeshes
get overlay materials or nothing — and **nothing in the log says so.** It is
equally invisible in Blender and in the Unity inspector, both of which happily
show the multi-material version you intended.

Consequences that bind any future model work:

- Split geometry by material *and* by runtime transform, nothing else. Merging
  further to reduce renderer count is not available.
- `Art/Blender/build_ah64.py` enforces this with `check_single_material()`, which
  raises on every build. Keep that guard.
- Every renderer needs a matching `ChildLocator` entry, because
  `Prefabs.SetupCustomRendererInfos` resolves renderers through it by name.
  `ChildLocator` matching is **exact string equality**, so `Airframe` and
  `AirframeDark` don't collide — the `Contains` footgun is material lookups only.

### Two traps that cost a debugging session each (2026-08-03)

Both surfaced building the gatling. Both are invisible where you'd look for them.

**1. The FBX exporter adds a spurious 90° X rotation at the *second* nesting level.**
`ChinTurret → ChinBarrel` is fine. `ChinTurret → ChinBarrel → ChinGatling` is not: the
grandchild picks up the Z-up→Y-up conversion a second time, composing with its parent's
90° to 180°. The gatling shipped pointing at the floor.

- It is **invisible in Blender** — the saved `.blend` had the child at identity, exactly
  as authored. Only the exported FBX is wrong, so this reads as a modelling error and
  isn't one. Probe the `.blend` *and* the imported FBX before concluding anything.
- `matrix_parent_inverse` does **not** survive export. It is Blender-only; the exporter
  writes `matrix_basis` and drops the inverse. `reparent_keep_world()` in
  `build_ah64.py` bakes the correction into `matrix_basis` instead — use it for any
  new parenting.
- The correction lives in `AH64Phase4Builder` (reset the grandchild's `localRotation`),
  not as a `-90` fudge in Blender. A fudge would make the source of truth disagree with
  itself and break silently if the exporter is ever fixed.
- **Re-measure the spin axis after any orientation change.** Fixing this moved the
  gatling's bore from local +Y to local +Z; the spin component would have wobbled the
  cluster instead of rolling it. Measure against a known-forward anchor
  (`InverseTransformDirection` toward `Muzzle`), never reason it out.

**2. `CharacterModel` forces `renderer.enabled = true` on everything it manages.**
Verified in `RoR2.dll` — `CharacterModel.UpdateMaterials()` walks every entry in
`baseRendererInfos` and sets `renderer.enabled = true` whenever materials go dirty.

So **you cannot hide a part that is in `customRendererInfos` by setting
`renderer.enabled = false`** — the write is undone within a frame. This is what left the
M230 visible *and tracking* next to the gatling. Use `Renderer.forceRenderingOff`, which
is a Unity-level flag CharacterModel never touches. Setting it in the prefab does not
help either: it is runtime-only and not serialized, so a component has to own it.

### A folder-level assetBundleName beats a per-asset one

`Assets/AH64/Bundle` carries the `ah64` bundle tag **on the folder**, and Unity
resolves an asset's bundle by walking up to that folder. So clearing an
individual asset's `assetBundleName` to `""` does **not** remove it from the
bundle — `GetImplicitAssetBundleName` still returns `ah64`.

This has bitten three times:

- Three backup FBXs under the old `Assets/AH64Assets/FBX/` were shipping inside
  the bundle (214 KB of duplicate airframes reaching players).
- Swapping the rotor bed from `.mp3` to `.wav` left **both** in the bundle under
  the same in-bundle name, `sfxAH64RotorHoverGrounded` — a silent name collision.
- A stale duplicate `matAH64RotorBlur.mat` sat in the bundle root alongside the
  live one in `AH64Surface/Materials/` — same in-bundle name, nothing referencing
  the copy. Found in the 1.0 cleanup.

**To exclude an asset you must move it out of the tagged folder.** Either to a
`~`-suffixed directory (Unity ignores those entirely — used for
`Assets/AH64/Source/FBX/FBX_backup~/`) or to a sibling folder outside the bundle
root (used for `Assets/AH64Archive/`). Verify with `GetImplicitAssetBundleName`,
and confirm by loading the built bundle and counting matching assets — the
AssetDatabase and the built bundle can disagree.

**A dependency does not need the tag.** `Assets/AH64/Source/FBX/AH64.fbx` is
deliberately *untagged*: its meshes reach the bundle anyway, because
`mdlAH64.prefab` and `AH64Display.prefab` reference them and Unity packs the
dependency closure. Verified by loading the built bundle — 25 mesh filters,
0 null meshes, 7,496 vertices. So the raw source asset never ships as a
standalone bundle entry. Do not "fix" this by re-tagging the Source folder.

### MCP servers

- **UnityMCP is in `.mcp.json` at project scope**, alongside Blender. Registering
  an MCP server against a single absolute path has bitten before: servers load
  only for the exact path they're registered under, so a session started in a
  differently-named directory comes up with **no Unity tools at all, silently and
  with no error**. Project scope fixes that permanently — the registration
  travels with the directory, including across this repo's rename.
- **UnityMCP runs `uvx --offline`, which makes it hostage to the shared uv
  cache.** `--offline` can only resolve from cached packages, so unrelated `uv`
  or `uvx` work elsewhere on the machine can evict or shift versions and leave
  UnityMCP failing to connect with a `No solution found ... fastmcp` resolver
  error. It is not broken and does not need reinstalling — run the same command
  once *without* `--offline` to re-warm the cache, then `--offline` works again.
  Observed after installing the Blender MCP server.
- **A same-named server in `~/.codex/config.toml` silently shadows the one in `.mcp.json`.**
  Project scope does *not* win. This has now cost time three times in one session:
  a user-scope `blender` on 9876 hid the project entry on 9877, and a local-scope
  `UnityMCP` pointing at `http://127.0.0.1:8080/mcp` hid the project stdio entry
  that dials Unity on 6400. Both failed *plausibly* — ✓ Connected, tools present,
  calls returning "cannot connect" or an empty instance list — never "duplicate
  registration". **When an MCP server misbehaves, check for a duplicate name before
  debugging anything else:**

  ```powershell
  $d = Get-Content "$env:USERPROFILE\.claude.json" -Raw | ConvertFrom-Json
  $d.mcpServers.PSObject.Properties.Name                       # user scope
  $d.projects."C:/Users/stuwj/Documents/Coding/ror2-chopper".mcpServers  # local scope
  ```

  Remove the duplicate with `claude mcp remove <name> -s user` (or `-s local`)
  **with Codex closed** — the app rewrites that file on exit and will
  otherwise clobber the change.
- **MCP servers load at session start.** Adding one mid-session does nothing
  until Codex is restarted in this directory — the tools simply won't
  exist. Verify on startup that both `mcp__UnityMCP__*` and `mcp__blender__*`
  are present before planning any model work around them.
- **Never hand-edit `~/.codex/config.toml` while Codex is running** — the app
  owns that file and rewrites it on exit, so edits get clobbered. Project-scope
  `.mcp.json` has no such problem.
- **Blender MCP is the official Blender Lab one**, not the popular third-party
  `ahujasid/blender-mcp`. Source: [projects.blender.org/lab/blender_mcp](https://projects.blender.org/lab/blender_mcp),
  docs at [blender.org/lab/mcp-server](https://www.blender.org/lab/mcp-server/).
  **The third-party one was tried first and removed** — it dispatches through
  `bpy.app.timers`, which stalls whenever Blender's main loop isn't pumping
  events, so commands hang or the socket closes with no reply. Don't reinstall
  it. Both bind port 9876, so having both installed is a race, not a fallback.
  Three separate pieces, all required:
  - **Add-on** (in Blender): extension `mcp` from the `lab_blender_org`
    repository, at `%AppData%\Blender Foundation\Blender\5.2\extensions\lab_blender_org\mcp`.
    Needs Blender 5.1+. It is only a *bridge* — it opens a TCP socket and
    executes what arrives; it speaks no MCP itself. **This install listens on
    `localhost:9877`, not the 9876 default.**
  - **MCP server** (the middle piece): cloned to
    `C:\Users\stuwj\Documents\Coding\blender_mcp` — deliberately outside this
    repo so it is never committed. Entry point `blmcp:main`, run via
    `uv --directory <repo>\mcp run blender-mcp`. Registered at **project scope
    in `.mcp.json`**, so it travels with the directory and cannot silently go
    missing the way UnityMCP did. Note this is `uv`, not `uvx` — it runs from a
    checkout, not a published package. It reaches the add-on via
    `BLENDER_MCP_PORT`, set to `9877` in `.mcp.json` to match the add-on. **The
    two must agree** — a mismatch fails at call time with "Cannot connect to
    Blender at localhost:NNNN", not at startup.
  - **Blender itself must be open with a GUI**, with the add-on's bridge server
    started (it can autostart; see the add-on preferences).
- **`claude mcp list` reporting ✓ Connected proves only that the stdio server
  process starts** — it says nothing about whether Blender is listening on 9877.
  The real check is calling a tool and seeing live scene data come back, e.g.
  `get_blendfile_summary_path_info` returning the current file's save state.
- **The Blender MCP executes LLM-generated Python in-process with no sandbox.**
  Blender's own docs recommend a VM. `execute_blender_code` has full filesystem
  access as your user — it can reach the game install, the r2modman profile, and
  this repo. Treat it as you would `shell`, not as a modelling tool.

Dependencies come from NuGet (`RiskOfRain2.GameLibs`, `MMHOOK.RoR2`,
`RoR2BepInExPack`, `R2API.*`) — the build does not reference DLLs out of the game
folder, so it works without Steam being installed.

The `demo time` profile already has R2API, DebugToolkit, and MMHOOK loaded.

---

## Design decisions already made

- **Movement: low hover with temporary altitude.** Permanently hovers a fixed
  height above terrain and strafes like a ground character. Utility skills grant
  brief real altitude. Deliberately *not* free flight — that breaks RoR2 encounter
  balance and forces anti-camping hacks (see Ms Isle, which penalizes being away
  from terrain).
- **Model: built from scratch in Blender.** A helicopter is rigid hard-surface
  geometry with spinning rotors and a yawing chin turret — no humanoid rig, no walk
  cycle. This sidesteps the single largest cost in custom survivor work. It also
  avoids the licensing risk in free "Apache" models, many of which are ripped from
  commercial games.
- **First milestone: chaingun + rocket + hover only.** One skill per slot,
  placeholder art, reused stock RoR2 VFX. Loadout variants come after the core
  feels right.

### Prior art worth consulting

- **ESF** (`Thrayonlosa/ESF` on Thunderstore) — playable VTOL aircraft, ~72k
  downloads. Closest existing thing; its Velocity Bomb overlaps the planned
  dive-bomb special, so that ability needs rethinking.
- **Ms Isle** (`Big-Crab/Ms_Isle`) — playable flying missile drone. Deprecated but
  **source is public** at `bitbucket.org/AJames99/missiledronesurvivormod`. The
  reference implementation for flight code.

---

## Gotchas that cost real playtest cycles

Each of these was found the expensive way. They are the highest-value paragraphs
in this file.

- **Hold-to-fire states must override `GetMinimumInterruptPriority()`.** Without
  it, the skill re-enters itself every physics tick — the predecessor project's
  blaster fired ~60 bolts/sec. The chain gun primary is exactly this shape.
- **Tracers can't be made to read as travelling projectiles.** Three rounds of
  tuning were wasted before switching to a real projectile. The Hydras, the
  Hellfire and any visible-flight ordnance are real projectiles for this reason.
- **Every good-looking laser/blaster effect in the game ships with DLC** —
  depending on one breaks the mod for players without it. Verified against the
  addressables catalog.
- The vanilla Wwise event list is in the game install at
  `Risk of Rain 2_Data/StreamingAssets/Audio/GeneratedSoundBanks/Windows/SoundbanksInfo.xml`.
  Vanilla events dodge the six-event soundbank limit. **This mod ships no custom
  soundbank** — only the three CC0 rotor clips in the assetbundle.
- **`MODVERSION` and `Build/manifest.json` must stay in step** —
  `NetworkCompatibility` is `EveryoneNeedSameModVersion`, so a mismatch is a
  lobby rejection. `tools/pack.ps1` enforces this; don't bypass it.
- **A version with a pre-release suffix is not `System.Version`-parseable.**
  BepInEx skips the plugin silently, which presents as the game hanging at 99%.
  Use plain `major.minor.patch`.
- `Materials.CreateHopooMaterialFromBundle` matches with **`Contains`**, so
  requesting the bare `matAH64` would match `matAH64Body`, `matAH64Markings` and
  the rest indiscriminately. Live footgun for any new `LoadMaterial` call — always
  request the full distinguishing name.
- **A character with a renamed prefab and no matching bundle loads invisible**,
  with one easily-missed log line. If the survivor is missing from character
  select or shows up as nothing, suspect the bundle before the code.

## The framework under `Modules/`

`Modules/` (`SurvivorBase`, `ItemDisplaysBase`, `Skills`, `Skins`, `Prefabs`,
`ContentPacks`) is the survivor-template framework, proven against the current
game build. **Do not rebuild it from scratch.**

`AH64ItemDisplays.cs` is bulk coordinate data — exempt from the 300-line
guideline. Adjust positions; never regenerate it wholesale.

The post-build copy in `AH64.csproj` uses `$(AppData)` and is existence-guarded.
The original template gated it on the template author's Windows username and
silently did nothing. Keep the guarded version.

---

## Name mapping

| Context | Value |
| --- | --- |
| Display name (in-game, Thunderstore) | `AH-64` |
| Code identifier / namespace | `AH64` |
| AssetBundle name (lowercase by convention) | `ah64` |
| Language token prefix | `AH64_` |

The display name lives in the language token value — `AH64_NAME` → `AH-64`.

---

## Conventions

- Follow the survivor template structure — `Modules/`, `Characters/Survivors/<Name>/`
  with `Content/`, `Components/`, `SkillStates/`. Do not impose a generic
  `src/ tests/` layout.
- Reuse stock RoR2 assets (projectiles, VFX, sounds) wherever possible.
- Balance numbers live in `<Name>StaticValues.cs` and config in `<Name>Config.cs`,
  not scattered through skill states.
- Comment the *why* for non-obvious values.

---

## Repo layout and the release path

| Path | Owner |
| --- | --- |
| `AH64Mod/` | C# plugin. `dotnet build` produces `AH64.dll`. |
| `AH64UnityProject/Assets/AH64/Bundle/` | **The only bundle-tagged folder.** Unity owns the `ah64` bundle. |
| `AH64UnityProject/Assets/AH64/Source/FBX/` | Blender export. Untagged on purpose (see above). |
| `AH64UnityProject/Assets/AH64Archive/` | Audio provenance originals. Outside the bundle root and must stay there. |
| `Art/Blender/` | `AH64.blend` + `build_ah64.py`, the procedural airframe build. |
| `Build/` | Thunderstore inputs: `manifest.json`, `README.md`, `icon.png`. |
| `tools/pack.ps1` | Builds and validates `dist/AH64-<version>.zip`. |

**Build output is not tracked.** `Build/plugins/`, `AH64UnityProject/AssetBundles/`
and `dist/` are all gitignored and regenerable. Nothing copies Unity's bundle
into `Build/plugins/` except `pack.ps1` — so **the bundle in a release only
updates if you rebuild it in Unity first.** `pack.ps1` refuses to package a
bundle older than the newest Unity asset.

Releasing is manual and deliberate: run `pack.ps1`, install the resulting zip
into a **fresh** r2modman profile, and test there. Testing in `demo time` proves
nothing about the zip — the post-build step already put a DLL and bundle in that
profile, so it passes even when the package is broken.

**Never publish to Thunderstore without an explicit go-ahead naming the version.**
