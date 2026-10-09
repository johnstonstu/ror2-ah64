# RoR2 1.5.0 compatibility — October 8, 2026

Applies to Hallowed Concepts, installed Steam build **25475991**. This is a
shared migration checklist for AH-64 and ROR2 Lightning, not a claim that
Lightning has been inspected or fixed.

## Confirmed API changes

| Old API | Installed 1.5.0 behavior | Required action |
| --- | --- | --- |
| `ProjectileImpactExplosion.explodeOnLifeTimeExpiration` | Field removed. `FixedUpdate` calls `Detonate()` when lifetime expires. | Remove assignments of `true`; preserve explicit lifetime. A previous `false` needs a separate behavior design, not blind deletion. |
| `Inventory.RemoveItem(ItemDef, int)` | Old signature absent. `RemoveItem(ItemIndex, int)` remains, forwarding to permanent-item removal. New code can use `RemoveItemPermanent`. | For code still built against 1.4.1 references, pass `itemDef.itemIndex`. Audit temporary/channeled/rented items separately. |
| Assuming `ItemDisplayRule.followerPrefab` is populated | Loader's donor rules now supply `followerPrefabAddress`; the direct field is null. This is an asset change, not a newly introduced field. | Resolve valid addressable references when the direct prefab is absent, and retain assets while generated rules use them. |
| Attaching `AimAnimator` to a model without an `Animator` | `TryInitializeAnimator()` dereferences `animatorComponent.runtimeAnimatorController`; rigid AH-64 throws each frame. | Skip humanoid aiming setup on models without an Animator. Keep procedural turret aiming. |
| Assigning only `ProjectileController.ghostPrefab` on a vanilla clone | `Awake()` prefers a valid `ghostPrefabAddress` inherited from the donor. | Clear the donor address when assigning a custom ghost. Otherwise the donor visual overrides the custom model and smoke without a missing-asset error. |

The code behavior was verified by decompiling the installed game's `RoR2.dll`,
not the stripped NuGet reference assembly.

## AH-64

The AH64 1.3 Dev launch log showed `Loading [AH64 1.3.0]`, then
`MissingFieldException` for the projectile flag in `AH64Assets.CreateProjectiles`.
Initialization stopped before survivor registration, explaining the missing
character. Null survivor preference followed the failed initialization.
The item-display errors were a second, independent addressable-loading issue.

- Removed the obsolete Longbow projectile flag assignment.
- Updated five automated-test inventory cleanup calls to the retained ItemIndex
  overload. These calls are in the developer runner, not normal gameplay.
- Added addressable item-display fallback. A native run verified every generated
  parented display rule had a prefab and an equipped item's follower instantiated.
- Guarded AimAnimator setup for the rigid aircraft model after the native skill
  test exposed repeated null-reference exceptions.
- Clear inherited ghost addresses for Hydra, Hellfire and Longbow when assigning
  their custom missile visuals. The pinned 1.4.1 reference assembly lacks the
  field, so a cached optional reflection lookup bridges the 1.5 field; the live
  test must verify the actual ghost identity and emitting particles.
- The release candidate reports 1.3.3; the original dev DLL was 1.3.0 and the
  earlier local compatibility builds retained the released 1.3.2 version.
  Version labels alone do not identify a locally patched candidate; record hashes.
- Building against pinned 1.4.1 GameLibs does **not** prove 1.5.0 compatibility.
  Use `tools/dev-profile/Check-Access.ps1` against the real game and the profile's
  dependency DLL directories. Before the fix it found six unresolved call sites:
  one field and five calls. Access-policy findings are distinct from missing APIs.

## Dependency issues to reassess

The post-patch AH64 dev log also reported:

- `FixPluginTypesSerialization` could not download the Unity symbol file because
  its runtime HTTP client reported `TLS Support not available`.
- `RoR2BepInExPack`: `FixNonLethalOneHP TryGotoNext failed`.
- R2API 5.3.0 warned that it targets game 1.4.1, while the game reports 1.5.0.

The native AH-64 skill test reproduced `PlayerCharacterMasterController.jumpWasClaimed`
missing. The installed **SeekersPatcher 1.4.1** owns the failing `JumpClaimed` hook
(verified by decompilation). Update both its runtime plugin and preloader patcher
together. The official **1.4.3** package includes the 1.4.2 restoration of
`jumpWasClaimed`/`itemStacks`, plus `useTransformedAimVector` restoration.
See the [publisher's changelog](https://thunderstore.io/c/riskofrain2/p/pseudopulse/SeekersPatcher/).

The separate Hollow Saint Dev log also reported failed R2API.Skins hooks. These
remain investigation leads for other profiles, not confirmed Lightning failures.
Do not modify the game's assemblies or audio settings to conceal dependency failures.

## ROR2 Lightning handoff

1. Capture its first post-patch initialization exception and installed DLL hash.
2. Search for both removed APIs above, including helper libraries and test code.
3. Run a member-reference scan of its compiled DLL against installed 1.5.0 and
   its actual dependencies; compilation alone misses these runtime breaks.
4. Review hooks touching player input, inventory, skins and projectiles against
   the installed source. Do not copy AH-64's fixes unless the same usage exists.
   Audit custom projectile ghosts for inherited addresses and capture Player.log
   through shutdown, including native audio crashes absent from BepInEx's log.
5. Test in an isolated profile: survivor registration, visible model/skins,
   live spawn, each skill's damage and cleanup, item interactions, death, stage
   transition, then host/client behavior.

## Validation status

Initial offline baseline: build, language, feedback, weapon previews and 3,905
movement assertions passed. Hellfire's offline harness lacks rendering stubs;
the movement visual suite fails `visual math exit ends at ordinary world basis`.
Neither failure by itself establishes a game-patch regression.

The initial completed isolated native run passed **415 gameplay assertions** with the patched DLL
and SeekersPatcher 1.4.3:

- Language switching, survivor registration, expected meshes/loadout slots,
  solo lobby, live body, enabled model renderers and stage reconstruction.
- All 52 generated parented item-display rules resolved to prefabs; equipping
  glasses instantiated an item follower.
- Stationary hover, all three primary firing calls, Hydra and Hellfire launches.
- All three utility states entered, travelled and returned to AH64Main.
- Longbow entered its paint state and cancelled cleanly (no target fixture).
- Bombing Run created six bombs through the immediate server API. Owned
  projectiles drained after expiry. No runtime errors occurred during skill checks.

The hidden-window test excluded only Windows' specific cursor-confinement
denial from its skill-error count; the original game logs retain it. The
launcher could not obtain a usable process exit code, so its outer verdict was
inconclusive despite the completed successful native result. Later inspection
found a native Wwise shutdown crash in that run's Player.log; the 415 assertions
establish gameplay checks only, not a clean end-to-end pass. Saved game files
were verified preserved. Screenshot output was absent; renderer assertions do
not substitute for a visual review.

The tested runtime files and SeekersPatcher update are now installed in
**AH64 1.3 Dev**, with replacements backed up under ignored `dist/` and all
existing configuration files preserved. r2modman's import records may still
show old package versions; this is a local development installation, identified
by the installed DLL hash, not a new published package.

After that native run, the user updated dependencies through r2modman. The dev
profile now contains SeekersPatcher **1.4.3** and MiscFixes **1.6.0**, matching
the publishers' package pages. That dependency update preserved the patched AH-64 DLL.
The 415-assertion run used MiscFixes **1.5.9**; the new dependency combination
was subsequently used in the user's two-run gameplay session. Do not attribute
the prior 415-assertion pass to 1.6.0.

## Follow-up playtest findings

The user's post-update playtest registered and launched AH-64 successfully, with
no recurrence of the removed-field/method initialization errors. The user reported
missing rocket smoke, especially Hydra. No missing-effect-asset error was logged:
the inherited ghost address takes precedence silently. A subsequent native test
with the address override fixed confirmed the intended Hydra, Hellfire and
Longbow ghosts, each with an active smoke system emitting 32 particles at the
sample point. Catalog checks confirmed all referenced effects were registered.
An attempted screen-effect sampling fixture was inconclusive; it must not be
counted as proof that explosions appeared correctly on screen.

The user's Player.log also recorded a native crash during
`AkCallbackManager.PostCallbacks()` from `AkSoundEngineInitialization.TerminateSoundEngine`.
The same shutdown crash recurred with the rocket-only fix after 443 gameplay and
asset assertions passed. Do not dismiss a successful native helper result as a
successful session unless the process exits cleanly and Player.log has no crash.

The flight rotor now checks its owned playing ID once per second instead of
registering an end-of-event callback. The final native run with SeekersPatcher
1.4.3 and MiscFixes 1.6.0 passed **445 assertions**, including the intended live
rocket ghosts and emitting smoke, advancing rotor playback across the polling
interval, and absence of a rotor end-callback registration. The process exited
with code **0**, Player.log reported successful sound-engine termination and no
crash, and saved game files were verified preserved. The final DLL is installed
in AH64 1.3 Dev; configuration files were preserved. This confirms this isolated
session's shutdown, not every possible multiplayer or audio interaction.

The final installed-game member scan found **0 unresolved references**; its 38
non-public access findings remain a separate review category. The optional
reflection lookup for the new ghost-address field is validated by the live
ghost-identity checks, not by that static scan. Feedback checks and 1,987 weapon
preview assertions also pass. No Unity assets or soundbanks needed rebuilding.

Effect-pool cleanup errors, one Animator state/layer warning pair and the existing
dependency startup warnings remain recorded. The log gives no object or stack
for the Animator pair, so attribution to AH-64 is unconfirmed. Pool cleanup errors
alone do not establish the cause of a missing trail. Normal play writes BepInEx's
LogOutput.log and Unity's Player.log; it does not generate a separate weapon report.

Additional coverage still outstanding: targeted Longbow acquisition/salvo and damage
checks, held-fire and guided-Hellfire controls, audio, all skin appearances,
knockback/item interactions, death and host/client play. Reassess the startup
dependency warnings above. Repair the two offline harness issues separately.
The user subsequently confirmed the rocket fix appeared resolved in play.
Thunderstore's live package API reports 1.3.2 as the latest published release.
The prepared 1.3.3 ZIP includes the compatibility fixes, updated dependency
minimums, and a shorter Thunderstore README with a linked Hollow Saint icon.
Isolated installations of that exact ZIP with required dependencies alone and
with optional Risk of Options each passed 445 native assertions and exited with
code 0 without a Player.log crash. Original saved files were verified preserved
after both runs. Evidence directories (local and ignored):

- `dist/patch-1.5-required-d3c02598eb1241beb35c0f0b2e723055/`
- `dist/patch-1.5-options-248d310432d445888f16ee78ac85c853/`

Tested candidate ZIP SHA-256: `2897AA1195DAF7C8F5FE3CC2596756101FB7E3458D2B824C5252088D1B024187`.

The 1.3.3 GitHub release is approved. Thunderstore upload is reserved for the
maintainer. The release ZIP is repacked only to finalize its changelog; verify
every other packaged file against the tested candidate. The broader manual
and multiplayer checks above remain outstanding.

Final release ZIP SHA-256: `97379EAE89230987DC12E03CFA7A6AA601537E8FF35B4806CEC169E37BAC3257`.
All nine other entries match the tested candidate byte for byte; only
`CHANGELOG.md` differs, removing the candidate label and pending-approval text.

Official announcement: [October 8 patch notes](https://support.2k.com/hc/en-us/articles/56193476123795-Risk-of-Rain-2-Patch-Notes-October-8-2026).
