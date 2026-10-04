<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/banner.jpg" alt="AH-64, an Apache survivor for Risk of Rain 2" width="100%">
</p>

<p align="center">
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.latest_version&label=thunderstore&prefix=v&color=23fc79&style=for-the-badge" alt="Thunderstore version"></a>
  <a href="https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/"><img src="https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fthunderstore.io%2Fapi%2Fv1%2Fpackage-metrics%2FJohnstonStu%2FAH64%2F&query=%24.downloads&label=downloads&color=ff8a28&style=for-the-badge" alt="Thunderstore downloads"></a>
  <a href="https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE"><img src="https://img.shields.io/badge/licence-MIT-6ee1e1?style=for-the-badge" alt="MIT licence"></a>
</p>

<p align="center"><b>An AH-64 Apache attack helicopter survivor for Risk of Rain 2.</b></p>

<p align="center">Enjoying it? Please <a href="https://thunderstore.io/package/JohnstonStu/AH64/">like AH64 on Thunderstore</a> so other players can find it.</p>

> **Early access:** AH-64 is still being tuned. Bug reports, balance settings and run details help shape the next update.
>
> **[Report a bug](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml)** · **[Share balance feedback](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml)** · **[Discussions](https://github.com/johnstonstu/ror2-ah64/discussions)**

<h3 align="center">The Gunship. A survivor that never lands.</h3>

The **AH-64** hovers above terrain and strafes like a ground character, with temporary altitude from collective input and evasive maneuvers. Its chin turret tracks the radar target while you reposition.

**What's new in 1.3**

- **Guide your Hellfire.** Press Special to launch, hold to accelerate and steer the live lead missile toward your aim, release to slow it, and hold again to resume guiding that same missile without spending another charge. Your gun and Hydras stay available.
- **Banked Break.** A sweeping 90-degree utility turn that keeps you moving. Choose left or right with movement input; neutral input turns right. Aim and fire through the turn.
- **Shape a Bombing Run with your flight path.** Drop six bombs over 1.5 seconds while moving and using your other weapons or utility. Each enemy can take at most three bomb hits per run; interrupted drops are lost.
- **Momentum through maneuvers.** Evasive Roll and Smoke Backflip carry your entry motion into the maneuver and ease back into normal flight.
- **Build a visibly different gunship.** Special and utility choices now have their own attachments in character select and gameplay, with distinct illustrated loadout icons. The M230 now carries a 20-round drum.

**Still aboard from 1.2:** altitude hold and airtime, item compatibility improvements, five paint schemes, and an airframe that banks, recoils, smokes and crashes.

[Thunderstore](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/AH64/) · [Changelog](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) · [Help balance AH-64](https://github.com/johnstonstu/ror2-ah64#help-balance-ah-64)

## Flight controls

Hold **jump** to climb and **descend** (default **B on controller**, **C on keyboard**) to drop. Release both to hold your altitude. Above resting height, airtime drains according to how you fly; returning to resting height refills it. Extra jumps extend airtime and climb height. The small tick below the crosshair shows your remaining airtime.

The optional **Classic altitude controls** setting restores release-jump-to-descend controls without the airtime limit.

## The kit

Skill values below describe the default settings. New 1.3 clips show the current build in controlled in-game scenes with scripted inputs and camera framing. Older clips are labeled as historical; their visuals and tuning may differ from 1.3.

### Passive — Fire Control Radar

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PassiveIcon.png" width="64" alt="Fire Control Radar">

Paints the strongest nearby threat. Deal 12% more damage to the painted target, gain 15% movement speed while facing it, and 30 armor while close. The chin turret tracks independently of where you look.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/fire-control-radar.webp" width="320" alt="Historical pre-1.3 Fire Control Radar gameplay">

*Historical pre-1.3 footage.*

### Primary — M230 Chain Gun

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Chaingun.png" width="64" alt="AH64Chaingun loadout icon">

A 20-round drum that reloads all at once. Rounds and the HE blast deal 75% damage within 10m and full damage from 30m. Tap at range to keep the burst tight. Attack speed helps both firing and reloading.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/m230-chain-gun.webp" width="640" alt="Current 20-round M230 firing at a Golem">

### Primary variant — XM301 Rotary Cannon

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Gatling.png" width="64" alt="AH64Gatling loadout icon">

A six-barrel rotary cannon that spools up to about 18 rounds a second from a 60-round drum. Lighter rounds and blasts than the M230, with the same 75% damage within 10m rising to full at 30m.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/xm301-rotary-cannon.webp" width="320" alt="Historical pre-1.3 XM301 Rotary Cannon gameplay">

*Historical pre-1.3 footage.*

### Primary variant — M789 Heavy Cannon

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Cannon.png" width="64" alt="AH64Cannon loadout icon">

Slow, heavy shells at 2.5 shots a second, each with a large blast. Eight to a magazine, and every shot kicks the airframe.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/m789-heavy-cannon.webp" width="320" alt="Historical pre-1.3 M789 Heavy Cannon gameplay">

*Historical pre-1.3 footage.*

### Secondary — Hydra-70 Pods

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64RocketPods.png" width="64" alt="AH64RocketPods loadout icon">

A ripple salvo spread over most of a second: keep your aim on target through it. Rockets deal 75% damage within 8m of flight and full damage from 25m. Each rocket past the base six adds 0.8s to the reload. The pods stay available while your primary reloads.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/hydra-70-pods.webp" width="320" alt="Historical pre-1.3 Hydra-70 Pods gameplay">

*Historical pre-1.3 footage.*

### Utility — Evasive Roll

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64EvasiveJink.png" width="64" alt="AH64EvasiveJink loadout icon">

A climbing forward-diagonal barrel roll chosen with movement input, with invulnerability through the first half and 200 armor for the roll (about 0.95s). Carries entry momentum through a smooth speed ramp and back into flight.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/evasive-roll.webp" width="320" alt="Historical pre-1.3 Evasive Roll gameplay">

*Historical pre-1.3 footage.*



### Utility variant — Smoke Backflip

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64SmokeBackflip.png" width="64" alt="AH64SmokeBackflip loadout icon">

Surge rearward through a climbing pitch loop, dumping smoke and cloaking briefly. Carries entry momentum through the maneuver and eases back into flight.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/smoke-backflip.webp" width="320" alt="Historical pre-1.3 Smoke Backflip gameplay">

*Historical pre-1.3 footage.*

### Utility variant — Banked Break

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BrakingTurn.png" width="64" alt="AH64BrakingTurn loadout icon">

Bank through a sweeping 90-degree turn while maintaining flight. Choose left or right with movement input; neutral input turns right. Aim and fire throughout. Cooldown: 4 seconds.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/banked-break.webp" width="640" alt="Banked Break moving turn">

### Special — AGM-114L Longbow

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Longbow.png" width="64" alt="AH64Longbow loadout icon">

Hold to paint radar locks while you keep firing your gun and Hydras, then release to launch. The base rack holds six missiles; Lysate Cell adds one per stack. Per-missile damage stops climbing after the sixth lock.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skills/agm-114l-longbow.webp" width="320" alt="Historical pre-1.3 AGM-114L Longbow gameplay">

*Historical pre-1.3 footage.*

### Special variant — AGM-114 Hellfire

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64Hellfire.png" width="64" alt="AH64Hellfire loadout icon">

Press Special to launch a slow lead missile. Hold Special to show the targeting laser and accelerate that missile toward your aim; release to slow it, then hold again to resume guiding the same live missile without spending stock. A fresh missile can launch once the previous lead ends. Primary and secondary stay available. Pocket I.C.B.M. adds two ballistic, unguided fan missiles; only the lead follows your laser.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/guided-hellfire.webp" width="640" alt="Guided Hellfire launch, release and rehold against a Golem">

### Special variant — Bombing Run

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/AH64Mod/Characters/Survivors/AH64/Content/LoadoutIcons/AH64BombingRun.png" width="64" alt="AH64BombingRun loadout icon">

Drop six bombs at 0.3-second intervals along the path you fly. Each deals 300% damage in a 6m blast, with at most three hits per enemy per run. Keep flying, aiming and using your primary, secondary or utility to shape the pattern. Interrupted drops are lost. Cooldown: 10 seconds.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/bombing-run.webp" width="640" alt="Six-bomb run over Golem targets">

## Choose your gunship

Each primary has its own chin-mounted assembly. Special choices add open Longbow rails, enclosed Hellfire launchers or bomb carriers; utilities add their own thrusters, smoke canister or banking fins. Changing the loadout updates the character-select model and the live aircraft. Each active skill has a distinct illustrated icon.

<img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/1.3.0/modular-loadouts.webp" width="640" alt="Three special and utility attachment combinations">

Five paint schemes carry through into gameplay: Olive, Desert Tan, Arctic, Army Green, and Night Stalker, unlocked by the AH-64 Mastery achievement (beat the game or obliterate on Monsoon).

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/skin-lineup.png" alt="The AH-64 1.2 model in its five paint schemes: Olive, Desert Tan, Arctic, Army Green and Night Stalker" width="100%">
</p>

*Historical 1.2 Blender model preview; in-game lighting varies. The title banner is promotional artwork from an earlier airframe revision.*

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

I want AH-64 to feel right, and the quickest way there is seeing what you actually play with. Your settings show me where players agree on balance, and they're the place to ask for sliders that don't exist yet.

The tuning menu needs [Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/). It's optional: AH-64 runs on its defaults without it.

<p align="center">
  <img src="https://raw.githubusercontent.com/johnstonstu/ror2-ah64/main/docs/images/feedback/balance-flow.jpg" alt="How to share AH-64 settings: tune the sliders, press Copy &amp; open GitHub to copy them to your clipboard, paste into the GitHub form if it's empty and add notes, submit" width="100%">
</p>

1. Open **Settings → Mod Options → AH-64** and tune the sliders. The tabs are Movement, M230, XM301 Gatling, M789 Cannon, Utility, Presentation, Audio and Feedback. Base speed, acceleration, primary reload times and Evasive Roll cooldown apply after a restart.
2. Optional: add notes in **Feedback → Comments**.
3. At the end of any tab, press **Share settings → Copy & open GitHub**. Your settings report (every AH-64 setting plus your comments) is now on your clipboard, and the feedback form opens on GitHub.
4. The **AH-64 settings and comments** field is usually prefilled. If it's empty, just paste (**Ctrl+V**) into it. The report is already on your clipboard. Then fill in **What kind of feedback** and **Your idea**, including what felt too strong or too weak.
5. Click **Create**.

**Feedback → Copy all settings** copies the same report without opening a browser.

**Privacy:** nothing is sent automatically. The buttons copy to your clipboard and open the form; a prefilled form carries the report in its link, but nothing is posted until you click **Create**. Submitting needs a GitHub account.

### Bugs, ideas and questions

- **Bug:** [open a bug report](https://github.com/johnstonstu/ror2-ah64/issues/new?template=bug_report.yml). Include the version, reproduction steps, stage, host/client role, and your `BepInEx/LogOutput.log` or profile code.
- **Balance or a concrete feature request:** [open the feedback form](https://github.com/johnstonstu/ror2-ah64/issues/new?template=feedback.yml). Paste a settings report when discussing balance.
- **General discussion:** [ask a question](https://github.com/johnstonstu/ror2-ah64/discussions/categories/q-a), [share how it feels](https://github.com/johnstonstu/ror2-ah64/discussions/categories/general), or [suggest an idea](https://github.com/johnstonstu/ror2-ah64/discussions/categories/ideas).

## Known limitations

- Missile rack depletion reflects local special stock, including Longbow reservations while painting locks. Remote and late-join presentation still needs dedicated verification.
- Gameplay tuning is not synchronized between players.
- Deep drops beyond the ground sensor use normal falling physics. Jump-pad and lift handling has been improved; report any remaining stage-specific problems.

## More mods by JohnstonStu

**[Hollow Saint](https://thunderstore.io/c/riskofrain2/p/JohnstonStu/Hollow_Saint/)**: my other Risk of Rain 2 survivor mod.

## Credits and license

- Built with R2API and the Risk of Rain 2 modding community's survivor framework.
- Airframe and weapon geometry modelled from scratch in Blender for this mod.
- Current rotor loop: **“Helicopter Sounds” by aquinn**, CC0. Full provenance and processing notes are in [Art/Audio](https://github.com/johnstonstu/ror2-ah64/blob/main/Art/Audio/README.md) and the bundled `LICENSE_SOURCE.txt`.
- Legacy rotor recordings by **qubodup** are CC0; their provenance remains documented. Other gameplay sounds use vanilla Risk of Rain 2 Wwise events.

[MIT](https://github.com/johnstonstu/ror2-ah64/blob/main/LICENSE) © 2026 Stu Johnston. Third-party audio retains its CC0 license.

See the [changelog](https://github.com/johnstonstu/ror2-ah64/blob/main/CHANGELOG.md) for version history.
