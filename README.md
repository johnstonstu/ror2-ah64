# AH-64

A custom Risk of Rain 2 survivor: **AH-64**, an attack helicopter that hovers,
never lands, and carries a full weapons load.

[Store page copy](Build/README.md) · [Changelog](CHANGELOG.md) · [Licence](LICENSE)

---

## Layout

| Path | What it is |
| --- | --- |
| `AH64Mod/` | The C# BepInEx plugin. Builds `AH64.dll`. |
| `AH64UnityProject/` | Unity **2021.3.33f1**, Built-In pipeline. Owns the `ah64` assetbundle. |
| `Art/Blender/` | `AH64.blend` and `build_ah64.py`, the procedural airframe build. |
| `Build/` | Thunderstore package inputs: `manifest.json`, `README.md`, `icon.png`. |
| `tools/pack.ps1` | Builds and validates the release zip. |

Build output (`Build/plugins/`, `AH64UnityProject/AssetBundles/`, `dist/`) is
generated, not tracked. A fresh clone needs Unity opened once before it can
produce a package.

## Building

**1. The assetbundle** — open the Unity project, pinning the path (a bare
`Unity.exe` opens and immediately exits, because the project is not registered
in Unity Hub):

```bash
"C:/Program Files/Unity 2021.3.33f1/Editor/Unity.exe" -projectPath "<repo>/AH64UnityProject"
```

Then run **AH64 → Build AssetBundle** (`Ctrl+Alt+B`). It writes
`AH64UnityProject/AssetBundles/ah64` and installs it to the local r2modman
profile.

> Open this project **only** with 2021.3.33f1. A wrong-version open silently
> rewrites every `.meta` file.

**2. The plugin:**

```bash
dotnet build AH64Mod/AH64.csproj -c Release
```

Dependencies come from NuGet, so no Steam install is required to compile. The
post-build step copies the DLL to `Build/plugins` and, if present, to the local
r2modman profile.

**3. The release zip:**

```bash
pwsh -File tools/pack.ps1
```

This checks that `MODVERSION` matches `manifest.json`, that the icon is exactly
256×256, and that the zip has no enclosing folder — then writes
`dist/AH64-<version>.zip`. It does not upload anything.

## Versioning

`AH64Plugin.MODVERSION` and `Build/manifest.json` must always agree.
`NetworkCompatibility` is `EveryoneNeedSameModVersion`, so a mismatch reaches
players as a lobby rejection. `tools/pack.ps1` refuses to package if they drift.

Use plain `major.minor.patch`. A pre-release suffix like `1.0.0-rc1` is not
parseable as a `System.Version`, and BepInEx skips the plugin silently — which
presents as the game hanging at 99% load.

## Bugs & feedback

### Balance settings

Install [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/)
optionally to tune AH-64 through **Settings → Mod Options → AH-64**. Movement,
each primary weapon, utility, audio, and presentation have their own categories.
The defaults are the intended balance; these controls remain available in release builds.
Base speed, acceleration, primary reload times, and Evasive Roll cooldown require
a game restart. Other controls are read during play; a current action may finish
using its previous values. Gameplay settings are not synchronized across a lobby:
agree on matching values before a multiplayer run. Audio can be set individually.

Add optional notes in **Feedback → Comments**, then use **Share settings → Copy &
open GitHub** from any category. Paste into the feedback form, review, and submit.
The report includes all exposed sliders and toggles, the mod version, changed
defaults, pending restart values, and comments. **Feedback → Copy all settings**
copies without opening your browser. Comments remain saved locally until cleared.
No report is submitted automatically; submitting on GitHub requires an account.

Report bugs on the [issue tracker](https://github.com/johnstonstu/ror2-ah64/issues/new/choose)
— the form asks for the BepInEx log and mod list, which is what makes a report
actionable. Use the in-game sharing action for settings reports; general ideas and questions go in
[GitHub Discussions](https://github.com/johnstonstu/ror2-ah64/discussions) —
Thunderstore packages have no comment section.

## Notes for contributors

`CLAUDE.md` records the hard-won engine constraints — one renderer/one material,
how `ChildLocator` resolves, assetbundle tagging rules, and the FBX export traps.
Read it before touching the model or the bundle.
