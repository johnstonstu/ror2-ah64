# AH-64 — Session Handoff

**State: 1.0.0, release candidate. Not published.**

Read `CLAUDE.md` first — it holds the environment facts and the engine
constraints. This file is the current-state summary and the open-work list.

> The detailed phase-by-phase development narrative (Phases 1–4, hover design,
> weapon kit iteration, the art passes, audio investigation, playtest run sheets)
> lives in the pre-1.0 archive repo `ror2-chopper` at tag `pre-1.0-cleanup`.
> It was deliberately not carried across: most of it described a project state
> that no longer exists, and the parts that still matter were folded into
> `CLAUDE.md`.

---

## What 1.0.0 changed

The mod was functionally complete at 0.2.1. 1.0.0 is a provenance and packaging
release, not a gameplay one.

- **141 unused HenryTutorial template assets deleted.** The shipped assetbundle
  went from **5.62 MB to 1.63 MB**. Every deletion was derived from the live
  dependency closure of the actual bundle keys in `AH64Assets.cs` /
  `AH64Survivor.cs`, not from a hand-written list.
- **All third-party character content removed** (Dante, Vergil, Neo, Rebellion).
  The mod had no right to redistribute it.
- **Unity project restructured**: `AH64UnityProject/`, with `Assets/AH64/Bundle`
  as the single tagged folder and `Assets/AH64/Source` outside it.
- **The last template-named runtime assets renamed** to match their role:
  the Hellfire ghost, explosion, muzzle flash, and the rocket material/texture.
- **Real lore text** replaced the `"sample lore"` placeholder.
- **Release packaging built from nothing**: icon, store README, changelog,
  MIT licence, and `tools/pack.ps1`.

Verified after the change: 25 renderers each with exactly one material, zero null
meshes, 7,496 vertices, all five renamed bundle keys resolving — checked by
loading the built bundle file, not the AssetDatabase.

---

## Open work

**Blocking a Thunderstore publish — nothing.** The package is structurally valid.
What remains is judgement, not plumbing:

1. **In-game smoke test has not been run since the purge.** This is the one
   verification gap. The bundle was verified programmatically but the game has
   not been launched. Test every skill, and **Hellfire specifically** — it is the
   only skill whose bundle keys changed. See the checklist below.
2. **Cold-install test.** Install `dist/AH64-1.0.0.zip` into a *fresh* r2modman
   profile. The `demo time` profile cannot catch a packaging fault, because the
   post-build step already put a DLL and bundle there.
3. **Icon is auto-generated** — the baked character portrait over a dark
   gradient. Functional, but a hand-drawn one would present better on the store.
4. **Skill icons** are AH-64's own bakes but vary in polish between slots.

### Not blocking, worth knowing

- The mastery skin is a single skin with no mesh replacements. `Skins.cs` supports
  `meshReplacements` when a second skin is wanted.
- `AH64ItemDisplays.cs` positions were tuned against the current airframe. Any
  significant model change needs a display pass.
- The build stamp (`AH64BuildStampController`) still renders in-game. Decide
  whether a public release should show it.

---

## Smoke-test checklist

Launch through the `demo time` profile (or better, a fresh profile from the zip).

- [ ] AH-64 appears in character select **with portrait and icon**. A survivor
      that loads invisible means the bundle did not load — check that first.
- [ ] Airframe, rotors, glass, optics, markings all render. No magenta, no
      missing part, no floating geometry.
- [ ] Rotor blur discs appear and spin.
- [ ] **Primary** — M230 chain gun: fires, drum depletes, reloads all at once,
      attack speed shortens the reload. Gatling and cannon variants both fire.
- [ ] **Secondary** — Hydra pods ripple-fire; rockets have a visible ghost in
      flight and explode.
- [ ] **Utility** — Evasive Roll gives i-frames and climbs. Smoke backflip variant.
- [ ] **Special — Hellfire.** *The one to watch.* Its ghost, explosion and muzzle
      flash were all renamed in 1.0.0. A missing rocket trail or a silent
      explosion means a stale bundle key.
- [ ] **Special — Longbow**: radar paints locks while the gun keeps firing;
      release launches.
- [ ] Passive: chin turret tracks the strongest nearby target independently of
      camera aim.
- [ ] Rotor audio at hover, in forward flight, and climbing — three layers.
- [ ] BepInEx console shows **no `ErrorAssetBundle` lines** and no missing-asset
      warnings.

---

## Release procedure

```bash
pwsh -File tools/pack.ps1
```

Rebuild the assetbundle in Unity first (**AH64 → Build AssetBundle**, `Ctrl+Alt+B`)
— `pack.ps1` refuses to package a bundle older than the newest Unity asset.

The script checks version parity between `MODVERSION` and `manifest.json`, that
the icon is exactly 256×256, and that the zip has no enclosing folder. It writes
`dist/AH64-<version>.zip` and **does not upload anything**.

Publishing to Thunderstore is manual and needs an explicit go-ahead naming the
version.
