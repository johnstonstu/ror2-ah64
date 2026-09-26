<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/banner.jpg" alt="AH-64 — Apache survivor for Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=23fc79&style=for-the-badge" alt="Thunderstore version"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.downloads&label=downloads&color=ff8a28&style=for-the-badge" alt="Thunderstore downloads"></a>
  <a href="https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT licence"></a>
</p>

> **Early access:** AH-64 is still being tuned. Bug reports, balance settings and run details help shape the next update.
>
> **[Report a bug](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)** · **[Share balance feedback](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)** · **[Discussions](https://github.com/johnstonstu/ror2-ah64/discussions)**

<h3 align="center">The Gunship. A survivor that never lands.</h3>

The **AH-64** hovers above terrain and strafes like a ground character, with temporary altitude from collective input and evasive maneuvers. Its chin turret tracks the radar target while you reposition.

**In 1.1:** distinct models for all three primaries, immediate weapon previews in character select, Olive/Desert/Arctic paint schemes, responsive rotor audio, and optional balance controls with a shareable settings report.

[Thunderstore](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/) · [Changelog](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) · [Help balance AH-64](#help-balance-ah-64)

## The kit

| Slot | Skill | What it does |
| :---: | --- | --- |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="Fire Control Radar"><br>**Passive** | **Fire Control Radar** | Paints the strongest nearby threat. The chin turret tracks independently of where you are looking, so the gun stays on target while you reposition. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PrimaryIcon.png" width="64" alt="M230 Chain Gun"><br>**Primary** | **M230 Chain Gun** | A fixed drum that reloads all at once rather than trickling. Tap at range to keep the burst tight; hose the whole drum up close where the spread stops mattering. The reload runs whether or not you emptied it, so top up before you commit. Attack speed helps the reload, not just the fire rate. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64GatlingIcon.png" width="64" alt="XM301 Rotary Cannon"><br>*Primary variant* | **XM301 Rotary Cannon** | The gatling alternative to the M230: a six-barrel rotary cannon. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64CannonIcon.png" width="64" alt="M789 Heavy Cannon"><br>*Primary variant* | **M789 Heavy Cannon** | A low-rate, high-damage cannon. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SecondaryIcon.png" width="64" alt="Hydra 70 Rocket Pods"><br>**Secondary** | **Hydra 70 Rocket Pods** | A ripple salvo spread over most of a second — hold your aim through it. Runs on its own cooldown and stays available while the primary is reloading. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64UtilityIcon.png" width="64" alt="Evasive Roll"><br>**Utility** | **Evasive Roll** | A climbing forward-diagonal barrel roll with i-frames through the motion. Hold jump for altitude; hold back or down to dump height faster. |
| *Utility variant* | **Smoke Backflip** | A smoke backflip in place of the roll. |
| <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64SpecialIcon.png" width="64" alt="AGM-114L Longbow"><br>**Special** | **AGM-114L Longbow** | Hold to paint radar locks while you keep firing the gun and the Hydras, then release to launch. |
| *Special variant* | **AGM-114 Hellfire** | An aimed, unguided missile fired from the wing rails. Can launch while the primary keeps firing. |

## Choose your gunship

Each primary has its own chin-mounted assembly. Changing your loadout updates the lobby model immediately; Olive, Desert and Arctic skins carry through into gameplay.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/m230-aircraft.png" alt="AH-64 1.1 model with the M230 chain gun" width="100%">
</p>

*Blender model preview; in-game lighting varies. The title banner is promotional artwork from an earlier airframe revision.*

## Install

**Mod manager (recommended):** install with [r2modman](https://thunderstore.io/c/riskofrain2/p/ebkr/r2modman/) or Thunderstore Mod Manager. Required dependencies are installed automatically.

**Manual:** copy the package's `plugins` contents into `BepInEx/plugins/JohnstonStu-AH64/`. Keep this layout:

```text
JohnstonStu-AH64/
  AH64.dll
  AssetBundles/ah64
  SoundBanks/AH64Rotor.bnk
```

Everyone in a lobby needs the **same mod version**. Gameplay tuning is local, so agree on matching settings before a multiplayer run.

### Required dependencies

- `bbepis-BepInExPack-5.4.1905`
- `RiskofThunder-R2API_Core-5.0.3`
- `RiskofThunder-R2API_Prefab-1.0.1`
- `RiskofThunder-R2API_RecalculateStats-1.0.0`
- `RiskofThunder-R2API_Language-1.0.1`
- `RiskofThunder-R2API_Sound-1.0.2`

## Help balance AH-64

Install [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) to open **Settings → Mod Options → AH-64**. It is optional: AH-64 works with its defaults without it.

Movement, each primary weapon, utility, audio and presentation have their own categories. The 30 controls cover gameplay tuning, impact visuals, rotor wash and rotor volume. **Rotor volume is the only Audio slider.** Existing development audio settings are retained for compatibility, but their temporary tuning sliders are hidden.

Base speed, acceleration, primary reload times and Evasive Roll cooldown require a restart. Other controls are read during play; an action already underway may finish using its previous values. Audio preferences can differ between players.

### Share your settings

1. Adjust the controls and try a run.
2. Add optional notes in **Feedback → Comments**.
3. Choose **Share settings → Copy & open GitHub** in any category.
4. Review the prefilled **“AH-64 settings and comments”** field. If it is blank, **paste the copied report** there; long reports use this fallback.
5. Add your difficulty, loadout, stage, other balance mods and what felt too strong or weak. Review the report and submit it.

**Feedback → Copy all settings** copies the report without opening a browser. Reports include every exposed control, the mod version, changed defaults, pending restart values and your comments. Comments remain saved locally until cleared. Opening a prefilled form sends its text to GitHub in the link, but does not create an issue. Use copy-only to review the text first. Submitting requires a GitHub account.

### Bugs, ideas and questions

- **Bug:** [open a bug report](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml). Include the version, reproduction steps, stage, host/client role, and your `BepInEx/LogOutput.log` or profile code.
- **Balance or a concrete feature request:** [open the feedback form](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml). Paste a settings report when discussing balance.
- **General discussion:** [ask a question](https://github.com/johnstonstu/ror2-ah64/discussions/categories/q-a), [share how it feels](https://github.com/johnstonstu/ror2-ah64/discussions/categories/general), or [suggest an idea](https://github.com/johnstonstu/ror2-ah64/discussions/categories/ideas).

## Known limitations

- Missile rack depletion reflects local special stock, including Longbow reservations while painting locks. Remote and late-join presentation still needs dedicated verification.
- Gameplay tuning is not synchronized between players.
- Deep drops beyond the ground sensor use normal falling physics. Jump-pad and lift handling has been improved; report any remaining stage-specific problems.
- Laser-guided Hellfire is deferred. The current Hellfire variant is unguided; Longbow supplies radar-guided fire.

## Credits and license

- Built with R2API and the Risk of Rain 2 modding community's survivor framework.
- Airframe and weapon geometry modelled from scratch in Blender for this mod.
- Current rotor loop: **“Helicopter Sounds” by aquinn**, CC0. Full provenance and processing notes are in [Art/Audio](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Audio/README.md) and the bundled `LICENSE_SOURCE.txt`.
- Legacy rotor recordings by **qubodup** are CC0; their provenance remains documented. Other gameplay sounds use vanilla Risk of Rain 2 Wwise events.

[MIT](https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE) © 2026 Stu Johnston. Third-party audio retains its CC0 license.

See the [changelog](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) for version history.
