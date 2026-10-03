# Bombing presentation handoff (source-only)

Base: ceab48ae15df2a94d56b194079f300440525a3b0.
Contract SHA256: 9F06A34306360A860C3D9A4EAD9A5E544ED29A05FCA4E7BB7EB484A5FE912742.
Latest user direction: compile only; user performs game testing. No automated tests, native scan, Unity/editor/game launch, profile changes, staging or deployment performed.

## Result

- New procedural 0.68-unit finned bomb ghost, no rocket smoke, no colliders or network components.
- New two-rail central under-fuselage feed rack with two pivoting latches and a seated bomb. Single material per renderer. Existing airframe hierarchy stays intact.
- Rack pulse occurs once per observed actual existing bombing projectile, using ProjectileController.owner and its ghost. Suppressed opportunities produce no fake drop. No new gameplay message or independent release schedule.
- A nearby release blends only BombVisual from the rack to the existing ghost for 0.10 seconds; flight root, network transform, collision and damage remain unchanged.
- A dedicated impact contains one flash, three fire particles and three smoke particles; maximum particle lifetime 0.65 seconds, effect cleanup 0.85 seconds. No debris, shockwave, point light or camera shake. Uses existing donor materials without changing donor material values or hierarchy. The DebrisSmoke donor supplies a material only, never its emitter or debris behavior.
- Independent visual scale replaces the misleading BlastRadius argument. Inspection correction: the old LoadEffect helper already used applyScale=false, so the authored Hellfire composition was the confirmed source of oversized presentation; the passed 6 m radius was not proof of sixfold scaling.
- Only three existing files changed: owner exposes read-only presentation running status; projectile impact uses visual scale; projectile builder adds the visual observer. Gameplay cast/damage/policy/network/static values/state/terminal files are untouched.

This branch is not a drop-in player candidate until the coordinator wiring below is applied. The old shared assets call still supplies Hellfire presentation; new rack/assets deliberately have no hidden self-registration.

## Coordinator integration: AH64Assets.cs

Add `public static RoR2.Skills.SkillDef bombingSkillDef;` alongside existing exposed SkillDefs.

Inside CreateBombingRunProjectile, retain its existing prerequisite validation, and replace the existing Build call with:

```csharp
AH64BombingRunPresentationAssets.Build(source.ghostPrefab, hellfireExplosionEffect);
Content.CreateAndAddEffectDef(AH64BombingRunPresentationAssets.Impact);
GameObject bomb = AH64BombingRunProjectiles.Build(
    AH64BombingRunPresentationAssets.Ghost,
    AH64BombingRunPresentationAssets.Impact);
```

Keep existing `PrefabAPI.RegisterNetworkPrefab(bomb)` and `Content.AddProjectilePrefab(bomb)` exactly once. Register the impact once before effect catalogs freeze; do not register Ghost or rack as gameplay/network prefabs. Build reads Flash, Fire, DebrisSmoke particle-renderer materials from the existing explosion and a MeshRenderer material from the missile ghost; missing prerequisites throw a specific error. Hellfire, Hydra and Longbow retain original donors.

## Coordinator integration: AH64Survivor.cs

After creating the local bombing SkillDef in AddSpecialSkills, assign:

```csharp
AH64Assets.bombingSkillDef = bombingSkillDef;
```

Call a new coordinator-owned `InstallBombingRackPresentation()` after `InitializeSkills()` and before `InitializeSkins()`. Suggested implementation (add relevant System.Collections.Generic using or retain fully qualified names):

```csharp
private void InstallBombingRackPresentation()
{
    CharacterModel model = prefabCharacterModel;
    ChildLocator locator = model.GetComponent<ChildLocator>();
    Material frameMaterial = null;
    foreach (CharacterModel.RendererInfo info in model.baseRendererInfos)
        if (info.renderer && info.renderer.name == "AirframeDark")
            frameMaterial = info.defaultMaterial;
    if (!frameMaterial || !locator)
        throw new System.InvalidOperationException("Bomb rack requires the converted airframe material and ChildLocator.");

    AH64BombingRunRack rack = AH64BombingRunPresentationAssets.BuildRack(model.transform, frameMaterial);
    rack.BombingSkill = AH64Assets.bombingSkillDef;
    var infos = new System.Collections.Generic.List<CharacterModel.RendererInfo>(model.baseRendererInfos);
    var pairs = new System.Collections.Generic.List<ChildLocator.NameTransformPair>(locator.transformPairs);
    foreach (Renderer renderer in rack.Renderers)
    {
        infos.Add(new CharacterModel.RendererInfo
        {
            renderer = renderer,
            defaultMaterial = renderer.sharedMaterial,
            ignoreOverlays = false,
            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
        });
        pairs.Add(new ChildLocator.NameTransformPair { name = renderer.name, transform = renderer.transform });
    }
    model.baseRendererInfos = infos.ToArray();
    locator.transformPairs = pairs.ToArray();

    // Body skins are mapped by exact renderer name to the display. Without matching rack
    // geometry the existing MapDisplayRenderers throws during InitializeSkins.
    AH64BombingRunRack displayRack = AH64BombingRunPresentationAssets.BuildRack(displayPrefab.transform, frameMaterial);
    displayRack.BombingSkill = AH64Assets.bombingSkillDef;
    displayRack.PresentationReady = false; // no lobby loadout adapter in this prototype
    rack.PresentationReady = true;
}
```

Both BuildRack calls require a Chest ChildLocator anchor. The display intentionally remains hidden but supplies renderer mapping for existing body/display skins. For consistent locator bookkeeping, append displayRack.Renderers to its ChildLocator as above too. Existing CreateDisplaySkinController supplies its CharacterModel renderer registration from the mapped body skin. Do not append body renderers after InitializeSkins without rebuilding every body/display skin: a subsequent skin application would discard that registration. Do not set PresentationReady until body registration is complete. Each model gets unique renderer objects; geometry/material assets can be shared.

Renderer names: BombRailL, BombRailR, BombBrace, BombLatchLMesh, BombLatchRMesh, BombFeed. Runtime visibility uses forceRenderingOff; do not use enabled or SetActive to hide these managed renderers. CharacterModel remains the material/cloak owner. Rack root is a child of the model and its pose follows flight attitude without reparenting existing meshes.

## Coordinator integration: AH64PylonMissiles.cs

In Update, after obtaining special, treat exact `special.skillDef == AH64Assets.bombingSkillDef` as `shown = 0`. Apply the existing proportional calculation and stock>0 minimum only in the non-bombing branch. Keep the existing lastShown comparison and forceRenderingOff loop after both branches. This hides the unrelated wing Hellfire missiles throughout bombing selection and restores unchanged Longbow/Hellfire behavior when switching. Do not let the stock>0 clamp raise bombing's shown value back to one. No stock writes or changes to other skills.

## Compile evidence

Command: `powershell -File tools/bombing-visuals/Build.ps1` (or invoke from the existing PowerShell session).

The helper creates an isolated project beneath dist/bombing-visuals/compile, reuses production source and package references, pins only its temporary floating references to installed cached versions, and uses no remote package sources. It does not import the production PostBuild staging target. AH64DeployToProfiles=false. Result: Release build succeeded, 0 errors and 46 obsolete API warnings in existing source.

DLL: dist/bombing-visuals/compile/bin/Release/netstandard2.1/AH64.dll
SHA256: 26253CF0323571F572CDA9F84EDF49E029CC9D9070A35B4E6888D020810EE45F
Log: dist/bombing-visuals/compile/build.log

Initial offline restore failed because wildcard versions require a package index; temporary exact-version pins resolved it. The initial helper invocation emitted the .NET first-run development-certificate message; the committed helper now disables generation with DOTNET_GENERATE_ASPNET_CERTIFICATE=false. No game/profile files were touched. This build does not verify runtime prefab construction, effect registration, material appearance, or shared integration not yet applied.

## Known limits / player-owned acceptance

- Rack location is a provisional model-space offset from Chest: 0.65 down, 0.20 aft. Inspect clearance from hull, gear, turret and item displays, plus readability from normal pilot camera during hover/turn/bank/roll. No native view or capture was taken.
- Feed is a stylized single central mechanism, not an authoritative six-bomb inventory depiction. Stock/running status only controls seated-bomb visibility. Release animation itself requires a real projectile.
- Requires an eligible living owner, current model, selected bombing loadout and observed running state. Receipt must resolve within 0.12 seconds locally and be within 1.4 units of outlet. Distant/late or unresolved spawns skip the rack cue and render the ordinary falling bomb. These are cosmetic bounds, never gameplay suppression.
- Existing replication has no launch timestamp or cast/drop ID on observer projectiles. The proximity rule cannot perfectly identify a delayed nearby old projectile or reconstruct exact release timing. No promise of precise six-station sync or late-join playback. Host/remote/observer behavior remains a player acceptance item.
- Latch returns in 0.22 seconds; feed reappears after 0.17 seconds only while the run remains observed or another stock is ready. State exit, interruption/ineligibility and disable reset latches. Existing released bombs continue with their own lifetime. Model/body destruction owns rack cleanup; there are no static body/event references.
- Verify cloak/death/skin/loadout changes, rapid recast, suppression and projectiles destroyed before animation completes. Verify six compact impacts do not obscure aircraft or enemies, and other weapon effects remain unchanged.
- No automated tests or access scan were run under the user's build-only override. No native testing or gameplay-preservation claim based on execution; preservation is limited to scoped source review.
