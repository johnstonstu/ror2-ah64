# AH-64 1.1 visual staging

Authorized by Stuart on 2026-09-26: isolated visual overhaul, two alternate skins,
sub-agent work, local staging and user playtest. No push or public release yet.

## Isolation

- Branch: `codex/ah64-visuals-1.1`.
- Worktree: `C:\Users\stuwj\.codex\worktrees\ah64-visuals\ror2-ah64`.
- Base: `6a184d77bc1b7a3d406dc43222fd2fac1c3cb85a` (`dev/1.1.0` committed baseline).
- Primary checkout and its uncommitted audio/gameplay work remain untouched.
- Audio/hover fixes from the other agent are NOT included in this branch yet.
  Integrate that agent's finished checkpoint before calling this a 1.1 release candidate.
- Never run tooling from or export to the retired `ror2-chopper` tree.

## Build safety

C# builds stage into this checkout's Build/plugins only by default. The original
existence-guarded profile installation remains available only with explicit
`AH64DeployToProfiles=true`; do not use it for staging. `pack.ps1` explicitly
sets false. Unity profile writes require the process argument
`-ah64InstallToProfile`; the staging verifier refuses that argument entirely.
Normal menu and batch builds do not install. Use only Unity 2021.3.33f1 with an
explicit worktree project path. Do not reuse another agent's open Unity scene.

Compile: `dotnet build AH64Mod/AH64.csproj -c Release /p:AH64DeployToProfiles=false`

Unity staging entry: `AH64VisualStageBuilder.RunFromCommandLine`, with
`-batchmode -quit -projectPath <this-worktree>/AH64UnityProject -logFile <this-worktree>/dist/visual-unity.log`.
It refreshes the model/display from the FBX, builds, then loads the on-disk bundle.
It deliberately does not regenerate audio or install a profile.

## Scope and ownership

- Model: permanent inboard racks; recessed engine mouths; wing/root and rotor polish.
- Skins: default olive, Desert, Arctic; shared geometry, per-skin lobby previews.
- Presentation: grounded rotor wash and cloak/death blur visibility.
- Missile launch/depletion networking: focused review, separate implementation gate;
  do not change skill stock/refund mechanics as an incidental art fix.
- Integration: safe builders, exported-asset verification, final combined audio/visual build.

## Acceptance before user test

- Source and built bundle have 25 meshes, one material/submesh each, exact ChildLocator mapping.
- Attachment/muzzle anchor transforms and hurtbox sizes unchanged from baseline.
- Gatling correction and forward spin axis intact; persistent racks survive empty ammunition.
- Imported vertices below 15,000 (baseline documented as 7,496).
- All three skins distinguishable, correct in lobby and gameplay, no shared material contamination.
- Terrain wash appears at the terrain surface and rotor blur respects cloak/death.
- All weapons, reloads/cancellations, item displays, fire/cloak overlays, stage transitions,
  host/client and late joins still need in-game verification by Stuart.
- Fresh profile ZIP test only after integration; do not overwrite an active game installation.
- Push after Stuart's test approval. Thunderstore publish needs explicit version approval.
## Completed checkpoint (2026-09-26)

- C# Release compile: PASS, 0 errors; 22 existing obsolete-API warnings.
- Blender: 25 meshes, 14 anchors, 2,548 source vertices; original 39 object
  names/transforms/parents/material assignments unchanged. FBX round-trip transform
  comparison passes within 1e-5; UV and single-material guards pass.
- Unity 2021.3.33f1: source and disk-bundle model/display checks PASS. Each contains
  25 meshes and 7,952 imported vertices (6.1% above 7,496 baseline); anchor matrices,
  collider sizes, gatling forward spin axis and single-submesh/material contracts pass.
- Build API success is checked explicitly. Staging forces a fresh bundle to keep
  packaging timestamps meaningful after prefab regeneration.
- pack.ps1: PASS version parity, bundle freshness, icon, required ZIP entries and
  path separators. Package is dist/AH64-1.1.0.zip; no installation or publication.
- Logs: dist/visual-dotnet.log, dist/visual-unity.log, dist/visual-pack.log.
- Images and JSON comparisons: dist/visual-review/. Colored images are Blender
  geometry/palette previews, not proof of Unity/Hopoo shading or gameplay lighting.
- Missile depletion timing/networking remains intentionally unchanged and is
  documented in VISUAL-LAUNCH-REVIEW.md. Permanent empty rack geometry is implemented.

Next: integrate the sound agent's finished checkpoint, reconcile version/changelog
and builder changes while preserving no-deploy defaults, then build one combined
1.1 candidate for Stuart's fresh-profile playtest. The visual-only ZIP can be used
for isolated art checks, but must not replace the audio agent's active test profile.

Visual-only ZIP SHA-256: 898C9A52010C5B55EA204E0C4C8F739BF69A84248B5A3CFE0827C85DC503ED4D
