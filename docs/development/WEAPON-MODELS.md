# Primary weapon models

- M230: angular breech, slender tube and hollow flash cage.
- XM301: six hollow tubes, two collars and a stationary receiver.
- M789: reinforced short barrel, recoil rails and recessed muzzle.

`Art/Blender/build_ah64.py` invokes `build_primary_weapons.py`; geometry is
reproducible without manual blend edits. Each renderer has one material/submesh.
Keep the source guard and exact ChildLocator names.

## Runtime ownership

`AH64PrimaryWeaponVisuals` owns body/display visibility. It selects exact SkillDef
identities using `forceRenderingOff`, retaining aiming transforms and CharacterModel's
cloak/death handling. `AH64GatlingSpin` owns spool, cluster rotation and gun audio.

`AH64LobbyWeaponPreview` reads the mannequin owner's fresh network loadout, handles
loadout events, slot reuse and re-enable, and resolves the primary slot dynamically.
Skin callbacks reassert visibility without replacing materials.

Cannon flash/tracer starts use the cosmetic `MuzzleCannon` anchor. Gameplay
BulletAttack origin, aim, damage, cadence and spread keep their existing values.

## Import constraints

Unity's nested FBX import added rotation and a local offset `(0, -0.34, 0.06)` to
gun grandchildren. The builder resets position and rotation of ChinGatling,
ChinGatlingHousing and ChinCannon to their source pivots. The original Muzzle empty
is retained for compatibility; checks measure the real bore and tips instead.
MuzzleCannon is constructed at pitch-local `(0, 0.98, 0)`. Runtime spin derives its
axis from the model forward vector.

## Verification tools

- `tools/check-weapon-previews.ps1`: production presentation code with game API
  doubles, covering primary/skin combinations, owner isolation, slot swaps,
  callbacks, cloak flags and lifecycle changes. This is not a live networking test.
- `AH64WeaponGeometryChecks` verifies renderer/material counts, ChildLocator
  targets, muzzle/bore alignment and preserved gameplay anchors in Unity.
- `Art/Blender/validate_rotor_clearance.py` checks continuous rotor clearance.

The historical `Art/Blender/validate_primary_weapons.py` comparison requires the
pre-weapon geometry from commit `a2ee956`. Prepare its ignored inputs from the
repository root before running it with Blender:

```powershell
New-Item -ItemType Directory -Force dist/weapon-review | Out-Null
git archive --format=zip --output=dist/weapon-review/baseline-source.zip a2ee956 Art/Blender/AH64.blend AH64UnityProject/Assets/AH64/Source/FBX/AH64.fbx
Expand-Archive dist/weapon-review/baseline-source.zip dist/weapon-review/baseline-source
Copy-Item dist/weapon-review/baseline-source/Art/Blender/AH64.blend dist/weapon-review/baseline.blend
Copy-Item dist/weapon-review/baseline-source/AH64UnityProject/Assets/AH64/Source/FBX/AH64.fbx dist/weapon-review/baseline.fbx
```

Use a separate Blender process so the comparison does not replace an unsaved
interactive scene. Reports are written under `dist/weapon-review`.
