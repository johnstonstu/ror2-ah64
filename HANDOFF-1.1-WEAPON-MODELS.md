# AH-64 1.1: primary weapon models and live lobby preview

Updated 2026-09-26. This is the next-session handoff requested by Stuart.
Read AGENTS.md, then this file, before making changes. This document supersedes
older handoff installation/status notes; it does not authorize publishing.

## Scope and user decision

Stuart tested the latest combined build and said: "All the changes appear to be
working." He likes the different primaries and the illustrated icons. Next he
wants the actual chin-mounted weapon to match the selected primary, inspired by
those icons, and to change immediately on the character-select helicopter when
he changes primary. Include this in1.1. The previous turn was planning only;
no weapon-model implementation has started. The user now handed this workload
to a new session to execute. Proceed within the approved phased scope below.

Estimate previously given:2–4hours for art, implementation and checks, then user
playtest. This is an estimate, not an assurance. No need for a new project or rig.
No changes to balance, firing cadence, damage, ammo, sound or skill mechanics are
needed. Laser-designated Hellfire is explicitly deferred beyond1.1.

## Critical: use the combined staging checkout

Opening a new session in the ordinary project folder is fine, but operate with
an explicit working directory pointing to this EXISTING worktree:

    C:\Users\stuwj\.codex\worktrees\ah64-visuals\ror2-ah64
    branch: codex/ah64-visuals-1.1
    latest implementation commit:2be146d

The original/live checkout is:

    C:\Users\stuwj\Documents\Coding\ror2-ah64

Its committed HEAD is6a184d7 and it contains important uncommitted audio/hover/
feedback sources from other chats. Those finished changes have already been
integrated into the staging branch. Do not reset, clean, overwrite, switch its
branch, build, package or export there. Do not copy its entire older tree over
staging. Preserve any subsequently changed work and compare before integrating.
Do not use the retired ror2-chopper checkout. Reuse this existing staging worktree;
it may not appear in the new chat's list_artifacts because attachment is chat-local.

IMPORTANT incident: another session built the original checkout and its automatic
post-build copy replaced the combined DLL in demo time new, leaving new model assets
but old skin code. The staging csproj defaults AH64DeployToProfiles=false, pack.ps1
passes false, and Unity profile writes are opt-in. Preserve those safeguards.
Install only a verified, complete combined package. Never overwrite an open game.
No push, public package upload or live release without Stuart's approval; public
Thunderstore release requires an explicit version-naming go-ahead.

## Current accepted baseline: keep all of it

- Default olive, Desert, Arctic skins; distinct gameplay/lobby materials.
- Permanent inboard missile racks, engine/wing/rotor detail, canopy clearance,
  aligned wheel braces, extended tail and raised tail rotor to avoid clipping.
- Terrain-following wash and cloak/death visibility handling; skin-aware heat.
- Wwise rotor bank, working audio. Latest fix removes the Captain drone climb
  one-shot, applies0.8 rotor-only gain trim, adds smoothed signed directional pitch.
  Saved tuning is preserved. ah64_audio_status lists active emitters/events.
- Hover/external-motion fixes and equipment/lift handling from the other agent.
-35 balance sliders plus one toggle, seven categories, comments, copy/share
  feedback actions, restart-required reporting. Preserve all controls/exports.
- Illustrated primary icons: original image is now gatling; generated M230 and
  heavy-cannon variants match its low-poly olive helicopter/three-quarter gun art.
  User rejected the flat procedural head-on bore diagrams. Do not restore them.
- Lobby skin bug fixed: old display RendererInfos borrowed BODY renderer references,
  causing SkinDef.BakeAsync ancestry errors. Modules/Skins.cs now maps actual display
  children, assigns boosted materials, and Modules/DisplaySkinVerifier.cs logs each
  applied skin. Do not regress this when adding weapon renderers.

Current installed test profile:demo time new. Latest payload SHA256:
DLL B5D8CCD04742629138B5B446801B3858572B995458043B3C8A3F523485EAC9A0
Bundle636048F122D369B65B59A667FFA33BFD836A1D2AAF7E58ECEEC73B79315771EB
Bank DC9C5018207019BA8010190D7024479AAC2047B0F00CAA8FE05FC0A705AEC0C3
ZIP B993A78CA9A7A2CCF49356934ECB37B93150DD40C97E398BE7FE7B1F4A053E04

Staging artifacts:dist/AH64-1.1.0.zip, dist/retest-install.json and backup
 dist/retest-backup-20260926-143749. Config unchanged during installation.
Latest checks: C#0errors/22existing obsolete warnings; feedback tests PASS;
Unity model/anchor/material and primary Sprite audit PASS; package/hash checks PASS.
User has since confirmed these changes appear to work. Multiplayer/late-join missile
stock presentation remains a known follow-up, not silently fixed by rack geometry.

## Plan to execute

1. Blender art: keep common turret mounting and aiming pivots. Refine M230 with
   slender barrel/angular breech; give gatling a clearer barrel cluster and collars,
   keeping stationary housing separate from rotating parts; create a distinct heavy
   cannon with thick shorter barrel/reinforced sleeve/prominent muzzle. Match the
   icons' visual language, not exaggerated proportions at the expense of fit.
   Produce close-up comparison renders early for Stuart to review.
2. Unified selection: choose exactly one primary mesh assembly using equipped
   skill identity. Reuse shared yaw/pitch and existing gatling spool/fire mechanics.
   Separate presentation ownership from gatling audio/spool so two components don't
   fight visibility. Preserve firing/muzzle alignment; if different barrel lengths
   need separate VFX anchors, keep hitscan/aim semantics and avoid gameplay changes.
3. Live lobby preview: bind each mannequin to its owning player's current loadout.
   Apply on initial creation and primary changes, retain selected skin and updated
   lighting, and reapply visibility safely after asynchronous skin changes.
   Preview is silent; never attach body combat/audio components to the lobby model.
4. Integrate/test: all9 primary/skin combinations, repeated toggles, character swaps,
   lobby return, spawn, respawn and stage transitions. Verify one gun visible, no
   detached parts, correct muzzle flashes/tracers/aiming, only rotary cluster spins,
   cloak/fire/skin updates don't reveal hidden weapons, and multiplayer observers
   see the equipped primary. Rebuild a combined local package for Stuart's retest.

## Code findings and entry points

Art/Blender/build_ah64.py is procedural source of truth; hand edits to .blend are
lost on regeneration. build_airframe currently creates ChinTurret, ChinBarrel and
ChinGatling. M230 is a simple breech/tube/muzzle; gatling has6 tubes and clamp.
FireCannon explicitly uses the existing M230 barrel; there is NO third cannon mesh.
Hierarchy:ChinTurret(yaw) -> ChinBarrel(pitch) -> ChinGatling(spin).
Shared Muzzle is parented under ChinBarrel. Never deactivate ChinBarrel GameObject
just to hide its mesh: that disables the gatling and Muzzle too.

AH64Mod/Characters/Survivors/AH64/Components/AH64GatlingSpin.cs already switches
M230/gatling during gameplay via SkillLocator.primary.skillDef and owns spool/audio.
Uses Renderer.forceRenderingOff because CharacterModel resets renderer.enabled.
Its spin axis is derived from the model forward vector; preserve that behavior.
AH64Survivor.cs: customRendererInfos, primary SkillDefs, body components, skins.
Content/AH64Assets.cs publishes gatlingSkillDef; extend exact skill identity mapping
for cannon as appropriate. Components/AH64ChinTurret.cs owns aiming.
Modules/Skins.cs and Content/AH64Skins.cs own body/display paints. New weapon meshes
must participate in skin mapping and have their own exact ChildLocator entries.

Actual installed game source was inspected with ilspycmd, not guessed:
RoR2.SurvivorMannequins.SurvivorMannequinSlotController subscribes to
NetworkUser.onLoadoutChangedGlobal, copies that user's networkLoadout, rebuilds the
mannequin when needed, and ApplyLoadoutToMannequinInstance currently applies ONLY
skin index via ModelSkinController.ApplySkinAsync. Use this lifecycle to connect
primary preview; inspect full source to choose a narrow robust hook/component.
Read game install RoR2.dll, NOT stripped NuGet GameLibs for method bodies.

## Asset contracts and build tools

- One renderer/material/submesh; split by material and runtime motion. All renderers
  need exact ChildLocator/customRendererInfos mapping. Current baseline25meshes,
  14anchors,7952Unity vertices. New gun parts may increase counts legitimately:
  update verifier expectations deliberately, don't remove the checks.
- Known FBX second-nesting90-degree X rotation is corrected by AH64Phase4Builder.
  Check source blend AND imported FBX before adding transform fudges. Use
  reparent_keep_world() and remeasure spin axes. Preserve gameplay/collider anchors.
- Keep Art/Blender/validate_rotor_clearance.py called by generator; baseline regression
  fails and current swept rotor checks pass. Existing tail/static margin has noted
  conservative uncertainty around central hardware; don't claim complete proof.
- Icons:AH64UnityProject/Assets/AH64/Bundle/Icons/texAH64PrimaryIcon.png,
  texAH64GatlingIcon.png, texAH64CannonIcon.png. Art/Icons/README.md records provenance.
  AH64GatlingIconBaker now imports/validates approved PNGs only; it no longer draws.
- Blender background executable:C:\Program Files\Blender Foundation\Blender 5.2\blender.exe.
  Use --python-exit-code1 (separate arguments: --python-exit-code 1).
- Unity ONLY2021.3.33f1:C:\Program Files\Unity 2021.3.33f1\Editor\Unity.exe.
  Always -projectPath <STAGING>\AH64UnityProject; never open in Unity6000.
  AH64VisualStageBuilder.RunFromCommandLine refreshes prefabs, validates anchors/
  renderers/icons, force-builds and reloads bundle. AH64LocalTestBuilder checks audio.
- dotnet build AH64Mod/AH64.csproj -c Release /p:AH64DeployToProfiles=false
- tools/check-feedback.ps1 verifies all36controls and export behavior.
- tools/build-rotor-bank.ps1 generates/validates existing Wwise project (only if
  needed). Package includes SoundBanks/AH64Rotor.bnk; NEVER Init.bnk.
- tools/pack.ps1 -SkipBuild validates versions, freshness and ZIP contents.
  No AllowStaleBundle bypass. Install exact ZIP payload after game exit, back up
  previous mod/config, preserve settings, compare installed hashes. Test demo time new.

Full history/evidence:VISUAL-STAGING-1.1.md, HANDOFF-1.1.0.md, AUDIO-RESEARCH.md,
VISUAL-LAUNCH-REVIEW.md. Read current sections before historical ones.
MCP availability varies by session; inspect callable tools before relying on them.
Previous work used background Blender/Unity against staging without touching GUI scenes.

## Completion report expected

Provide renders of all three gun assemblies, note functional checks/limitations,
confirm complete1.1integration and installed profile when ready. Ask Stuart to test
live menu swaps and all three guns. Do not claim gameplay checks run unless observed.
Keep commits local until user approves pushing; never publish as part of this task.