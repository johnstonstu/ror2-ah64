# Hellfire feedback handoff

Branch: feature/1.3-feedback-hellfire
Base: ceab48ae15df2a94d56b194079f300440525a3b0
Worktree: C:\Users\stuwj\Documents\Codex\2026-10-03\task\ah64-1.3-feedback-hellfire

## Implemented

Lead-only crawl/boost: 6 m/s crawl, 140 m/s designated, 280 m/s² acceleration (~0.48 s), 420 m/s² deceleration (~0.32 s). Dedicated reversible constants are in Content/AH64HellfireFeedbackValues.cs. These make release visually distinct while retaining the previous guided top speed. No damage/cooldown/lifetime changes.

Release pauses guidance and decelerates along the last heading; holding native Special again resumes the newest still-living lead. Token, lifetime and remaining 240-degree turn budget never reset on rehold. Invalid/stale aim also decelerates. Terminal cancellation remains separate for interruption, unequip, death, disable and stage change. Accepted impact permanently stops this speed writer before stock deferred detonation.

Native stocked activation still launches a new lead and spends stock normally. Rehold during cooldown only designates the existing lead. This does not add SkillDef gating or intercept native input; reduced cooldown/extra stock can therefore produce a new launch on a new press. Older leads slow/coast. Exactly two full-payload unguided +/-25-degree I.C.B.M. extras retain baseline 140 m/s at any effective stack count.

Owner-only laser observes configured inputBank.skill4.down independently of missile stock and acknowledgement. Native aim ray and first-hit/fallback world point are shared with designation. Material is cloned from the already-loaded base-game chaingun tracer; no bundle or camera changes. Material/beam are disposed on disable, destroy and scene change.

## Coordinator changes required

In AH64Survivor body component setup, after body/state machines exist and before spawning:
```csharp
Components.AH64HellfireOwner.Install(bodyPrefab);
```
This is necessary for the laser before the first launch, including an empty-stock hold. FireHellfire also installs on first launch as a fallback. Existing network Init/Shutdown and projectile Guidance.Install wiring stay as currently integrated.

Update shared Hellfire description to explain: press launches; hold Special designates and accelerates; release slows; hold again resumes the live missile. Preserve native stocked new-launch behavior.

Aim protocol appends a terminal-cancel boolean. All peers need the same DLL; do not mix this with the previous prototype despite the unchanged development version. Coordinator owns any version/config/localization updates. No new external dependencies.

## Verification: build only

Per latest instruction, no offline regression suite, access harness, runtime/network test or game launch was run in this wave. Source review checked sender authority, token/sequence, pause versus terminal cancellation, staleness/rate validation, collision stop ordering, lead-only speed writes and unchanged vanilla damage authority. No changed Hellfire file emitted a compiler warning. Git diff --check passed.

Commands, after sourcing dist/bootstrap/Set-BaselineEnvironment.ps1 (copied and isolated locally; ASP.NET certificate generation disabled before SDK use):
```powershell
dotnet restore AH64Mod/AH64.csproj --configfile AH64Mod/nuget.config --ignore-failed-sources -p:NuGetAudit=false -p:AH64DeployToProfiles=false --disable-parallel
dotnet build AH64Mod/AH64.csproj -c Release --no-restore --disable-build-servers -p:AH64DeployToProfiles=false -p:UseSharedCompilation=false -nodeReuse:false
```
Restore and Release build passed: 0 errors, 46 warnings from unchanged files. This is the current feedback base, not the earlier 22-warning prototype baseline.
Logs: dist/hellfire-feedback/restore.log and build.log (ignored).
DLL SHA256: 2EDC821D8B450125E4A7B04DA28B319FEC3BB8950D499B1E0A7A59D3958A05DE.
Only isolated build outputs were written. No profile/game file replacement, staging, installation, packaging, Unity/Wwise or publishing.

## User pilot checks after coordinator integration

- Host and remote client, remapped controller Special: tap gives slow lead; hold boosts/turns; release slows; cooldown rehold resumes same missile without another stock spend.
- Laser immediately on eligible hold before launch/ack and when empty; hides on release/stun/death/unequip; absent for observers and non-Hellfire loadouts.
- Hold primary and Hydra throughout; both stay concurrent. With extra stock/reduced cooldown confirm a stocked new press launches and only the newest lead responds.
- Aim at a close wall/ceiling, terrain, moving enemy and empty sky from both rails. Check impacts never pass through cover or detonate twice.
- I.C.B.M. one and multiple effective stacks: exactly two fast unguided fan extras plus one crawl/boost lead, full unchanged payload.
- Release/rehold under latency, stun, owner death, stage transition and missile timeout: no token resurrection or extended lifetime. Server prediction remains deliberately disabled; inspect remote flight smoothness.
- Check beam brightness, material availability/depth and near-camera readability in game. These rendering properties and speed feel are not established by compilation. Six-second missile lifetime remains unchanged, so a long crawl can expire before reaching distant targets.

Historical tools/Hellfire checks expecting permanent release or constant lead speed are not updated or run; they are outside this feedback allowlist.
