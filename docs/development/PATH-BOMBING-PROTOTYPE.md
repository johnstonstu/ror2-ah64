# Flight-path bombing prototype

Base: `9e39bd4a6f24925ee339dc20b8bd995fb07588f3`. Feature branch: `feature/1.3-path-bombing`.
This is an unregistered, reversible prototype. No existing source, skill defaults, Longbow,
Hellfire, movement, M230, configuration, profile or runtime resource is changed by this branch.

## Approved provisional tuning

| Parameter | Prototype |
| --- | --- |
| Cast | 1 stock, 10-second recharge beginning at activation |
| Drops | 6, at 0 / 0.3 / 0.6 / 0.9 / 1.2 / 1.5 seconds |
| Payload | 300% per bomb, 1800% summed cast payload coefficient |
| Per target | At most 3 successful damaging hits per HealthComponent per cast; 900% base coefficient |
| Blast | 6 m, no falloff, 0.5 proc, no separate direct damage, no added force |
| Crit | One CharacterBody.RollCrit() per released bomb |
| Pocket I.C.B.M. | No multiplication or recursive bomb launch |
| Ballistics | Horizontal inherited velocity capped at 25 m/s; initial Y = -4 m/s; gravity 30 m/s² |
| Expiry | 6 seconds after release; expiry fizzles without blast |

Comparison verified directly in this checkout's `AH64StaticValues.cs`: Hellfire is 1350%,
8 seconds, 12 m, 1.0 proc. These numbers motivate the prototype, not a claim of balanced
encounters. Crit, native hurtbox/damage modifiers and proc-derived item damage are outside
the base coefficient budgets. 1800% sums released payload coefficients; it does not cap
aggregate damage across every enemy inside an area attack. Attack speed does not alter
this run's count or cadence; native cooldown/stock items remain SkillDef behavior.

Geometry choices for review: 0.2 m swept collision radius, origin 0.5 m vertically below
current body core, 0.02 m clearance before the collision surface. They are centralized
separately from splash tuning. The origin offset is world-down, independent of cosmetic
roll/pitch and weapon aim.

## Behavior and ownership

The server observes the replicated Weapon2 state and samples its current aircraft position
and CharacterMotor velocity at each due opportunity. It never extrapolates an activation
line or writes body movement. Turns and utility altitude changes therefore affect later
release positions. On server scheduling hitches, due opportunities sample the current
position; the code does not invent earlier positions. Native replicated movement is still
subject to network delay and requires remote-client acceptance testing.

Stock is consumed by the integrator's SkillDef at activation. Releasing the special button
does not cancel the finite sequence. Pain-priority or stronger interruption, death,
disable, owner replacement and stage loss stop future drops. `AH64BombingRunOwner` supplies
disable cleanup that EntityState itself lacks. Ending an older cast cannot clear a newer
owner lease. No suppression or interruption refunds/replaces drops. Natural state exit
waits for server confirmation that all six opportunities were consumed. There is no
client-age completion condition, arbitrary grace, or timeout-based natural completion.

The authority allocates a monotonically increasing per-body request number and serializes
it with the native state entry. The server keeps its own release schedule starting at
the observed entry. Its explicit terminal outcome is `Succeeded` after six consumed
opportunities (including suppressed drops), or `Cancelled` when the server cast stops
before that. It sends the request and outcome over the existing reliable UNet channel.
Only the connected server's terminal message for the currently owned body's matching
active BombingRun can finish it. Old-body/old-cast, unrelated-connection and observer
messages are ignored; Running, Exited and unknown wire outcomes are rejected.
The native OnDeserialize-before-OnEnter ordering preserves the received request number.
Observed request numbers also advance the local counter for subsequent authority changes.

The state moves from `Running` to `Succeeded` or `Cancelled`, then to `Exited` on native
OnExit. The first terminal outcome is stable: subsequent cleanup cannot turn a completed
six-opportunity cast into cancellation or revive an exited state. Local disable also
resolves cancellation. Both terminal outcomes release retained host/client Weapon2 states,
even if a stage/run change leaves the body and state alive. Cancellation does not require
a live health component, so a retained dead-body weapon state can exit; pending pain/death
transitions still take precedence over SetNextStateToMain.

Host terminal delivery is local; remote delivery uses
message ID **28066**, distinct from Hellfire's 28064/28065. Registration fails visibly on
collision without replacing another handler. No client-to-server message is added. Pain
and stronger state transitions still call OnExit immediately on arrival, which stops
future server drops; they do not wait for completion. Pending interruptions cannot be
overwritten by natural completion. Body disable cancels the waiting local state as well
as server releases. A delayed acknowledgement adds network transit time to Weapon2 recovery,
but cannot add releases or reset the activation-based cooldown. There is no fallback
timer that assumes successful completion.

The server checks the current effective authority/connection every fixed update until
native state exit. Recipient changes are notified immediately; transfer to host resolves
locally. An unchanged remote recipient receives bounded retries every 0.1 seconds, including
after a failed queue attempt. Queue acceptance never becomes a permanent delivered flag.
This also covers an authority moving away and back between server polls: an old message
may be correctly rejected, but a later retry reaches the returning authority. When no
recipient is available, the delivery lease is cleared; a newly available recipient is
eligible immediately. These are
transport attempts, not additional casts, drops, cooldown grants or arbitrary grace periods.
Native OnExit ends retries. The existing Init/Shutdown wiring is unchanged; peers must use
the same revised terminal-message schema.

A world capsule validates the center-to-origin segment and projectile volume. Nonfinite
motion/damage, blocked origin, missing prefab or missing manager consumes that opportunity
without a payload. Flight uses server sphere sweeps against world and entity-precise
colliders, choosing the nearest permitted collision. Owner/friendly hurtboxes are ignored
according to native splash team rules. World triggers are ignored. Already-inside-world
bombs fizzle. Client copies only consume ProjectileNetworkTransform updates; prediction
and client collision are disabled. No native grenade/direct-damage component is inherited.

Released bombs keep their own bounded lifetime after state interruption. They continue
falling after owner death/disable, but impact damage requires a live active original owner;
invalid-owner impacts are cosmetic only. Stage/run changes despawn them immediately.
External destruction never triggers a replacement bomb or damage.
Impact presentation, damage and item callbacks run inside try/finally. Server destruction
is guaranteed even if a callback throws, while the original exception propagates to Unity
for its full report. A reported positive hit still spends its target budget if a later
native hook throws; a pre-report failure spends none. The finished guard prevents retries.

Native `BlastAttack.FireNoDamage()` supplies hurtbox collection, team filtering and world
line-of-sight. The wrapper additionally deduplicates HealthComponents and reserves a
target slot across callbacks. It calls public `HealthComponent.TakeDamage`, then commits
the slot only after the inflictor receives a matching positive `DamageReport` and the
DamageInfo is not rejected. Explicit rejection, silent rejection and zero/missing reports
do not spend successful-hit budget or invoke item-hit callbacks. Accepted damage invokes
OnHitEnemy and OnHitAll once. These native damage/item interactions require runtime testing.

No native private members, global damage hooks, new networking dependencies or independently
registered gameplay objects are introduced. The terminal-only message above uses the
already-used reliable transport and never launches or damages anything. The builder
returns an inert prefab; the integrator owns both network and projectile-catalog registration.

## Exact integrator wiring

1. In `AH64States.Init`, add:

   ```csharp
   Modules.Content.AddEntityState(typeof(SkillStates.BombingRun));
   ```

2. In `AH64Assets.CreateProjectiles`, after existing Hellfire construction has supplied its
   ghost and explosion, build and register exactly once:

   ```csharp
   GameObject bomb = AH64BombingRunProjectiles.Build(
       hellfireProjectilePrefab.GetComponent<RoR2.Projectile.ProjectileController>().ghostPrefab,
       hellfireExplosionEffect);
   R2API.PrefabAPI.RegisterNetworkPrefab(bomb);
   Modules.Content.AddProjectilePrefab(bomb);
   ```

   Guard missing Hellfire/ghost with the project's startup error handling; the builder
   throws for a missing/invalid ghost. It reuses presentation only, without modifying the
   source ghost, Hellfire prefab or any shared effect. Missile-shaped art is provisional.
   Register before content catalogs freeze. Do not register in the state or at each cast.

3. In `AH64Survivor.AddSpecialSkills`, construct `bombingSkillDef` with:

   ```csharp
   SkillDef bombingSkillDef = Skills.CreateSkillDef(new SkillDefInfo
   {
       skillName = "AH64BombingRun",
       skillNameToken = AH64_PREFIX + "SPECIAL_BOMBING_NAME",
       skillDescriptionToken = AH64_PREFIX + "SPECIAL_BOMBING_DESCRIPTION",
       skillIcon = assetBundle.LoadAsset<Sprite>("texAH64SpecialIcon"), // temporary existing art
       activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.BombingRun)),
       activationStateMachineName = "Weapon2",
       interruptPriority = EntityStates.InterruptPriority.Skill,
       baseMaxStock = 1,
       requiredStock = 1,
       stockToConsume = 1,
       rechargeStock = 1,
       baseRechargeInterval = AH64BombingRunStaticValues.Cooldown,
       beginSkillCooldownOnSkillEnd = false,
       resetCooldownTimerOnUse = false,
       dontAllowPastMaxStocks = false,
       fullRestockOnAssign = true,
       mustKeyPress = true,
       isCombatSkill = true,
       canceledFromSprinting = false,
       cancelSprintingOnActivation = false
   });
   Skills.AddSpecialSkills(bodyPrefab, longbowSkillDef, hellfireSkillDef, bombingSkillDef);
   ```

   Replace only the existing AddSpecialSkills call. Longbow remains first/default and
   Hellfire second. `dontAllowPastMaxStocks=false` retains native added-special-stock
   behavior. One cast consumes one stock; a new cast needs a fresh press after recovery.
   Use a unique final icon later; the temporary icon is present in the existing bundle.
   Existing `AH64PylonMissiles` maps every special's stock proportionally to its eight
   visible rail missiles, so bombing currently empties them at activation. This is
   unchanged placeholder presentation, not a six-bomb rack animation.

4. Add language tokens through the translation owner. Suggested English name: `Bombing Run`.
   Suggested description: `Drop 6 bombs along your flight path for 300% damage each.
   Each enemy can be damaged by at most 3 bombs per run. Interrupted drops are lost.`
   Do not describe bombing as shipping until integrated and accepted.

5. Subscribe the dev telemetry owner to `AH64BombingRunTrace.Emitted` only during an owned
   run and unsubscribe on every cleanup path. Map records into the existing versioned
   JSONL event schema rather than introducing a second writer. Records expose cast ID,
   owner network ID, drop index, scheduled/actual cast-relative time, origin/impact position,
   velocity, crit, payload/delivered damage, target network ID, accepted-hit count,
   remaining per-target base coefficient, proc coefficient and suppression/termination reason.
   `hit` is one accepted native damage report, not proof of a downstream item proc.
   Downstream item damage/proc counts must be separately instrumented by the native scenario.

6. **New requirement after P2 review:** in `AH64Plugin.Awake`, immediately after the existing
   Hellfire network initialization, add:

   ```csharp
   Survivors.Components.AH64BombingRunNetwork.Init();
   ```

   In the existing `AH64Plugin.OnDestroy`, add:

   ```csharp
   Survivors.Components.AH64BombingRunNetwork.Shutdown();
   ```

   Apply this on every peer before gameplay. The remote server completion send throws a
   clear setup error if Init was omitted; host-only tests do not establish correct remote
   wiring. The worker has not edited the shared plugin file.

7. No csproj changes are needed (SDK compile glob). Apart from the explicit completion
   transport hookup, no shared config, hover, flight visuals, Hellfire ownership, version
   or manifest edits are required by these files.
   Tunables live in the uniquely owned static-values class. Coordinator owns any future
   promotion into shared config/options and corresponding shared count-harness updates.

## Validation and remaining gates

Run `tools/path-bombing/Check.ps1`: it compiles the actual state, cast, owner, policy,
projectile motion and damage implementation against small API doubles. It does not test
the prefab builder in a Unity runtime. Checks cover finite scheduling, curved/altitude
sampling, snapshotted damage, host/remote authority, rejected-hit budget, duplicate
hurtboxes, callbacks, lifecycle cancellation, ballistics, expiry and one-shot collision.
The P2 regressions also compile the real completion transport against in-memory network
doubles. Three independent entry/completion/exit-delay scenarios use separate clocks;
the first reproduces 100 ms entry delay versus 20 ms old natural-exit delay. Natural exit
must wait for six consumed server opportunities, while explicit pain/death/disable still
cancels promptly. Additional checks cover stale requests, unrelated connections, observers,
removed bodies, suppressed drops, pending interruptions and registration collision/cleanup.
Full impact-path tests inject effect, pre-report, post-report and item-callback exceptions,
asserting destruction, exact accepted-hit accounting, original exception identity and no
repeated effects/damage. These remain API-double evidence, not native jitter/item acceptance.

For compilation, dot-source `tools/path-bombing/Set-Environment.ps1`, restore with
`--configfile AH64Mod/nuget.config -p:AH64DeployToProfiles=false`, then build Release with
`--no-restore -p:AH64DeployToProfiles=false -p:UseSharedCompilation=false -nodeReuse:false`.
All caches/TEMP/build evidence are worktree-local. The existing project also copies its DLL
to this worktree's ignored Build/plugins; it does not stage a player profile.

`Check-Native.ps1 -BaselineScan <baseline access.json> -DependencyDirectories <installed dirs>`
performs a fresh installed-assembly scan and compares inaccessible sites to the reviewed
baseline. This is an API-access delta check, not runtime-policy approval. Strict scanning
still reports the baseline's 35 known inaccessible sites. No feature allowances are added.
An initial scan caught three new accesses to internal `DamageTypeCombo.GenericSpecial`;
all were replaced with equivalent public-field initialization.

Native source inspection used the installed game RoR2.dll, not GameLibs method bodies:
ProjectileManager.FireProjectileImmediateServer is public, initializes synchronously and
returns its spawned instance; ProjectileNetworkTransform defaults to server authority;
BlastAttack.FireNoDamage collects team/LOS-filtered hits without damage; HealthComponent
reports accepted damage through IOnDamageInflictedServerReceiver on the inflictor.
NetworkStateMachine deserializes received state data before OnEnter and directly applies
state transitions, which is why independent client age could not safely schedule normal exit.

Remaining native gates: prefab/catalog/network spawning and observer visuals; host and
remote-client duplicate/latency/cancellation behavior; real scene geometry and high-speed
walls/ceilings; target shields/barrier/armor/immunity and multi-hurtbox enemies; native item
proc chains and Captain deletion; cooldown/extra-stock items; hovering versus curved paths,
utilities and stair/ledge transitions; death/disable/stage cleanup; readability and actual
encounter balance. There has been no game launch, stage, deployment, Unity/Wwise build,
package, merge or publication from this branch.

## Initial offline checkpoint (a5dac93, superseded by P2 corrections)

- Release build: zero errors, 33 warnings (same count as the coordinator baseline).
- API doubles: **63 assertions passed**. No native runtime result is inferred.
- Three negative controls were rejected by assertions: removing the overlap cap,
  replacing current body position with a fixed origin, and enabling client projectile motion.
- Fresh installed-native scan: **14,178 references**, zero unresolved, the same **35**
  inaccessible sites as the reviewed baseline, zero added/removed inaccessible sites.
  Strict scanner exit remains 1 because of those existing sites; no new runtime policy
  was generated or approved.
- Compiled DLL SHA256: `5B86D9DBC6B83150B6FD10655FE4A636DD4A4CD5B2D7F28077AD1D53F8E8AD05`.
- Worktree-local evidence: `dist/path-bombing/build.log`, `checks.log`, `mutations.log`,
  `native.log`, `native-f3b9a21d94a040ddbbcbb6038a1dcb4a/comparison.json`, and
  `mutations-e6e565aa90a84d2d9bd1d9ced55cf1de/`.

## Initial P2 correction checkpoint (070d5ad; terminal lifecycle superseded below)

The follow-up fixes natural completion under unequal entry/exit transit delay and guarantees
impact cleanup when presentation/native damage/item callbacks throw. Balance values,
release cadence, hit budgets, existing skills and all shared files remain unchanged.

- **132 API-double assertions passed**, including separate clocks and independently delayed
  entry/completion/exit deliveries, interruption cancellation, and full projectile-impact
  effect/pre-report/post-report/item exceptions. This is not native multiplayer/item evidence.
- **Six negative controls rejected at their specifically expected assertions**: unbounded
  overlap, fixed origin, client flight simulation, restoring client-clock completion,
  moving impact cleanup outside finally, and swallowing the impact exception.
- Deployment-disabled Release build: **0 errors / 33 warnings**.
- Fresh installed-native scan: **14,331 references**, **0 unresolved**, unchanged **35**
  baseline inaccessible sites, empty inaccessible-site delta. Strict scan still exits 1;
  this creates no new runtime policy or access allowance.
- DLL SHA256: `97E757009B47FA1E0B2D0637FF154EE38B118BEF74D679B81BF3798A6B6FC6FC`.
- Evidence under this worktree's `dist/path-bombing/`: `p2-build-initial.log`,
  `p2-checks.log`, `p2-mutations.log`, `p2-native.log`,
  `native-db97dc3b7af742248431027e9c0f06d8/comparison.json`, and
  `mutations-46b19b916e91481e98efdb93e99c3653/`.

Integrator action added by this correction: initialize/shut down `AH64BombingRunNetwork`
as detailed in step 6 above. All original native acceptance gates remain, now explicitly
including real remote-client jitter, reliable completion delivery and prompt interruptions.
There has still been no runtime/staging/deployment/package action from this worker.

## Terminal-lifecycle correction checkpoint (d7008d8; failed-entry coverage extended below)

This follow-up replaces completion/cancellation flags with the explicit terminal protocol
described above. Server-side stage/run/death stops now propagate cancellation to retained
states, and queue acceptance no longer suppresses delivery after authority transfer.
It requires no architecture decision or additional shared hook beyond the documented
Init/Shutdown calls. The wire outcome byte requires matching candidate binaries on all peers.

- **179 API-double assertions passed**. Retained host and remote bodies exit Weapon2 after
  stage/run loss without later drops. Successful and cancelled terminal outcomes both
  recover after transfer to another remote authority or to host. Returning authority,
  failed queues, stale later-cast packets, pending stronger transitions and invalid wire
  outcomes are also covered. Previous jitter and full-impact exception regressions pass.
- **10 negative controls rejected by their expected assertions**. New controls ignore
  stopped casts, restore a recipient-blind send-once rule, disable same-recipient retries,
  or encode cancellation as success. The previous six controls remain discriminating;
  the old client-clock completion mutant runs the separate-clock suite specifically.
- Deployment-disabled Release build: **0 errors / 33 warnings**.
- Fresh installed-native scan: **14,365 references**, **0 unresolved**, unchanged **35**
  baseline inaccessible sites, empty inaccessible-site delta. Strict scan still exits 1;
  no new runtime policy or access allowance was generated.
- DLL SHA256: `CBFF1DF19908CFBE494E023BACE973A49C47E70F13B0F634BE3A7CD2F873818C`.
- Evidence under `dist/path-bombing/`: `terminal-build.log`, `terminal-checks.log`,
  `terminal-mutations.log`, `terminal-native.log`,
  `native-3a5aee0f328644d68677b835f431e108/comparison.json`, and
  `mutations-243368164c524217b2e945e984f2c79c/`.

These checks model independent clocks, connections and authority roles with API doubles;
they do not establish native transport/authority-transfer or gameplay acceptance. Native
testing must include stage/run loss with retained bodies, terminal delivery across ownership
changes and back again, interruption priority and cancellation alongside the original gates.
No staging, game control, deployment, packaging, merge or publication occurred.

## Failed-entry correction

Server entry now resolves cancellation whenever it cannot construct a cast: dead or missing
health, missing CharacterBody, an inactive or disabled owner, or a rejected owner component.
It caches the state machine's GameObject before base entry so a missing CharacterBody does
not remove the network address needed to deliver cancellation. Original entry exceptions
still propagate. A failed first release stops its partially constructed cast before rethrowing,
so later fixed updates cannot release the remaining payload. Owner recovery never retries
entry or replenishes the cast. Rejected Begin also stops and releases any previous lease.

Host and remote regressions inject each of those failures, base-entry and constructor
exceptions, and a first-release exception. They assert zero initial and later drops,
cancelled rather than successful outcome, bounded retry, immediate authority rerouting,
former-owner rejection and stale-packet rejection by a later cast. Additional checks cover
missing local address, pending stronger transitions, request-counter exhaustion and
replacement rejection. No new shared hook, registration or wire-schema change is required.

Native acceptance must additionally exercise a body dying before the server receives state
entry and retained state machines after entry rejection. API doubles cannot establish native
exception handling, transport or gameplay behavior.

- **331 API-double assertions passed**, including all prior lifecycle, jitter, impact and
  damage-budget checks.
- **14 negative controls rejected at their expected assertions**. Four new controls restore
  the Running phase after rejected entry, require CharacterBody for cancellation routing,
  leave a failed first release scheduling, or retain the prior cast after Begin rejection.
- Deployment-disabled Release build: **0 errors / 33 warnings**.
- Fresh installed-native scan: **14,388 references**, **0 unresolved**, unchanged **35**
  baseline inaccessible sites and no inaccessible-site delta. Strict scan exits 1 for those
  existing sites; this does not authorize a new runtime policy or access allowance.
- DLL SHA256: `5FC996DEA571C3981407FFC1E4BB15FFC9B1107704D97D33244FFC4A2F680026`.
- Evidence under `dist/path-bombing/`: `entry-build.log`, `entry-checks.log`,
  `entry-mutations.log`, `entry-native.log`,
  `native-2932d3c4486144d5ae9fd54c5915b847/comparison.json`, and
  `mutations-052bd42bbd7c48e2aa5c1ae1753f3ef3/`.

This correction changes only the assigned feature files and their offline checks/documentation.
Shared integration files and all profile/runtime/staging resources remain coordinator-owned.
No staging, game control, deployment, packaging, merge or publication occurred.
