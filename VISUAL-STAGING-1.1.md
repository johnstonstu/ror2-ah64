# AH-64 1.1 visual staging

Authorized by Stuart on 2026-09-26: isolated visual overhaul, two alternate skins,
sub-agent work, local staging and user playtest. No push or public release yet.

## Isolation

- Branch: `codex/ah64-visuals-1.1`.
- Worktree: `C:\Users\stuwj\.codex\worktrees\ah64-visuals\ror2-ah64`.
- Base: `6a184d77bc1b7a3d406dc43222fd2fac1c3cb85a` (`dev/1.1.0` committed baseline).
- Primary checkout and its uncommitted audio/gameplay work remain untouched.
- Finished audio/hover sources were integrated read-only from the primary checkout on 2026-09-26.
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
- Muzzle anchors and hurtboxes unchanged; cosmetic TailTip anchor follows the approved tail extension.
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

The initial checkpoint above was visual-only. The combined checkpoint below supersedes its package status.

## Visual review follow-up

Stuart approved the overall art direction and requested two small corrections:
engine/intake overlap with the rear canopy and misaligned landing-gear braces.
Both engine assemblies moved outward by 0.09 authoring units; the intake rims now
clear the glass without changing the canopy or any runtime attachment transforms.
Main shock struts and diagonal braces now terminate at the wheel axle centres;
wheel positions and overall bounds are unchanged. The two braces previously ended
0.34 units ahead of the wheels. All 39 FBX transforms and the 25-mesh contract still
pass the round-trip checks. Updated multi-angle Arctic previews are named
`dist/visual-review/inspect-arctic-*.png`; junction-before/after show the canopy fix.
## Combined playtest checkpoint

Finished audio/hover sources and Wwise authoring assets were copied from the primary
checkout without changing it. Packaging validates and includes only AH64Rotor.bnk,
never Init.bnk. Both build systems retain opt-in profile deployment.

Tail assembly extended 0.80 authoring units; tail rotor, axle and gearbox raised
0.10. The cosmetic TailTip anchor moves with the tail; all other attachment/muzzle
anchors and collider dimensions stay fixed. Imported FBX preserves all 39 names,
parents and material slots; 36 object transforms unchanged, three approved tail
transforms moved. No renderer or vertex-count increase from these corrections.

The continuous main/tail mesh and blur-envelope minimum gap is 0.0976 units, above
the 0.05 guard. Main sweep/fin margin passes the continuous test. Mesh intersections
were absent across 72 sampled phases per rotor. Tail/static continuous clearance
is conservative/inconclusive near attached hub hardware; sampled checks pass.
The original model fails the validator, demonstrating the collision regression check.

The user must verify skins/lobby, overlays, rotor/gear clearance in motion, empty
racks, weapons, hover/external lifts, rotor sound and volume 0 vs 0.30 in game.
Multiplayer/late-join missile-depletion presentation remains a known follow-up.
No push or public release authorized.
Combined validation: C# 22 existing warnings / 0 errors; visual Unity bundle audit PASS;
audio bundle PCM/loop seam audit PASS; Wwise bank generation/format/events PASS;
package freshness, version and required entries PASS. Installed the exact ZIP's
three payload files into demo time new, checked against staged hashes. Configuration
was unchanged; previous mod/config backed up under dist/combined-backup-20260926-141247.
ZIP SHA256: 1C424D37A6562D8701788CDCC35B3C7AADA5DB7C5D07E1459270EAF37A150F8F.
DLL SHA256: AEF32494DBD29A08AE599DDC4C125585288B8E9A222766BB6638E59A29DBB9FD.
Bundle SHA256: D11693B149A55DEEB1F0E91D88BF280295F3FDC2C55ECA412BFA24CED8207130.
Bank SHA256: DC9C5018207019BA8010190D7024479AAC2047B0F00CAA8FE05FC0A705AEC0C3.

## Full playtest reinstall: skins, feedback and icons

The previous installed combined DLL was overwritten by a later primary-checkout
balance-feedback build (AE3A7D216F340895BC7935FF0CE265F3E0E430596276CDA9F0B6EDCD2C1DAEF6).
The resulting model/new-DLL mismatch explains absent alternate skins and lobby skin errors.
Integrated its four feedback/config C# files into this branch, preserving all visual code.
Normalized comparison confirms all other primary-checkout C# sources match except the
five intentional visual changes; AH64Skins is additionally present here.
Feedback tests PASS:35 sliders plus toggle, seven categories, precision, restart state,
comments and export isolation. M230 now uses single-bore icon artwork; Unity validates
all three primary Sprite assets. C# build0errors/22existing warnings; bundle and pack PASS.
Installed exact ZIP payload into demo time new; settings unchanged. No publication.
Latest installation record (hashes and backup): dist/full-playtest-install.json.
DLL SHA256: 44C4FAAE28AE1B6492FF28B290A38C1155285A934575B50F4FA59EB6EA9726B0
ZIP SHA256: FB885D961BAD6148C3C54440D12042453D20533E6D0D24F481C9F668901EE102
Only install from this combined staging branch during this playtest; the primary
checkout's normal post-build profile copy can overwrite the candidate again.
