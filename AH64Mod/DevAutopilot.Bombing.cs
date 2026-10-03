using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AH64.Survivors;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using Path = System.IO.Path;

namespace AH64
{
    // Prepared draft. All fixture writes and hooks require the existing two bootstrap
    // gates plus the additional bombing-solo-v1 dispatch. No normal-launch activation.
    internal sealed partial class DevAutopilot
    {
        private const string BombingSuite = "bombing-solo-v1";
        private static readonly string[] BombingNames = { "stationary", "bleed", "curve", "interrupt", "invalid" };
        private static readonly string[] BombingChecks = { "activation", "opportunities", "motion", "payload", "cleanup" };
        private readonly List<Check> bombingChecks = new List<Check>();
        private readonly List<BombingObservation> bombingObservations = new List<BombingObservation>();
        private readonly HashSet<int> bombingProjectileIds = new HashSet<int>();
        private readonly Dictionary<int, int> bombingDropByProjectile = new Dictionary<int, int>();
        private readonly Dictionary<int, ulong> bombingCastByProjectile = new Dictionary<int, ulong>();
        private readonly List<BombingDamage> bombingReports = new List<BombingDamage>();
        private readonly HashSet<DamageInfo> bombingAcceptedNativeInfo = new HashSet<DamageInfo>();
        private bool bombingStarted, bombingComplete, bombingHooks, bombingCaseActive;
        private bool bombingAddedBleed, bombingAddedIcbm;
        private int bombingOnEnemy, bombingOnAll;
        private ulong bombingCastId;
        private CharacterMaster bombingTargetMaster;
        private CharacterBody bombingTarget;
        private uint bombingTargetId;
        private uint bombingTargetMasterId;
        private GameObject bombingObstruction;
        private SkillDef bombingSpecialOverride, bombingUtilityOverride;

        private void InstallBombingProbeHooks()
        {
            if (Environment.GetEnvironmentVariable("AH64_AUTOPILOT_BOMBING_CHECKS") != BombingSuite || bombingHooks) return;
            bombingHooks = true;
            AH64BombingRunTrace.Emitted += ObserveBombingTrace;
            GlobalEventManager.onServerDamageDealt += ObserveBombingDamage;
            On.RoR2.GlobalEventManager.OnHitEnemy += ObserveBombingOnHitEnemy;
            On.RoR2.GlobalEventManager.OnHitAll += ObserveBombingOnHitAll;
        }

        private void CleanupBombingProbe()
        {
            if (bombingHooks) {
                AH64BombingRunTrace.Emitted -= ObserveBombingTrace;
                GlobalEventManager.onServerDamageDealt -= ObserveBombingDamage;
                On.RoR2.GlobalEventManager.OnHitEnemy -= ObserveBombingOnHitEnemy;
                On.RoR2.GlobalEventManager.OnHitAll -= ObserveBombingOnHitAll;
                bombingHooks = false;
            }
            CleanupBombingFixture();
        }

        private void CleanupBombingFixture()
        {
            bombingCaseActive = false;
            brakingCollective = false; prototypeHeld = false; prototypeAim = Vector3.zero; move = Vector3.zero;
            if (pilot) {
                pilot.inputBank.jump.down = pilot.inputBank.skill4.down = false;
                if (bombingSpecialOverride) pilot.skillLocator.special.UnsetSkillOverride(this, bombingSpecialOverride, GenericSkill.SkillOverridePriority.Contextual);
                if (bombingUtilityOverride) pilot.skillLocator.utility.UnsetSkillOverride(this, bombingUtilityOverride, GenericSkill.SkillOverridePriority.Contextual);
                if (pilot.inventory) {
                    if (bombingAddedBleed) pilot.inventory.RemoveItem(RoR2Content.Items.BleedOnHit, 20);
                    if (bombingAddedIcbm) pilot.inventory.RemoveItem(DLC1Content.Items.MoreMissile, 1);
                }
            }
            bombingSpecialOverride = bombingUtilityOverride = null;
            bombingAddedBleed = bombingAddedIcbm = false;
            if (bombingObstruction) { bombingObstruction.SetActive(false); Destroy(bombingObstruction); }
            bombingObstruction = null;
            if (NetworkServer.active) {
                if (bombingTarget) NetworkServer.Destroy(bombingTarget.gameObject);
                if (bombingTargetMaster) NetworkServer.Destroy(bombingTargetMaster.gameObject);
            }
            bombingTarget = null; bombingTargetMaster = null;
            // Any released payloads are allowed their bounded native lifetime; this
            // cleanup never deletes a projectile to manufacture successful drain.
        }

        private void BombingCheck(string id, bool passed, string actual)
        {
            bombingChecks.Add(new Check { id = id, passed = passed, actual = actual, scenario = segment });
            Event("bombing-check", id + " " + (passed ? "PASS" : "FAIL") + " " + actual);
        }

        private IEnumerator BombingCases()
        {
            bombingStarted = true;
            Event("bombing-scope", "SOLO actual GenericSkill, native World/HealthComponent/DamageReport and downstream Bleed; scripted inputs and owned disposable fixtures. No physical feel, media, peer or complete terrain matrix acceptance.");
            var slot = pilot.skillLocator.special; var variants = slot.skillFamily.variants;
            var definition = Variant(slot, typeof(BombingRun));
            BombingCheck("catalog.order", variants.Length == 3 && variants[0].skillDef.activationState.stateType == typeof(PaintLongbow)
                && variants[1].skillDef.activationState.stateType == typeof(FireHellfire) && variants[2].skillDef == definition,
                string.Join(",", variants.Select(v => v.skillDef.activationState.stateType.Name)));
            BombingCheck("catalog.definition", definition.activationStateMachineName == "Weapon2" && definition.baseMaxStock == 1
                && definition.stockToConsume == 1 && definition.baseRechargeInterval == 10f && definition.mustKeyPress
                && definition.interruptPriority == InterruptPriority.Skill,
                "machine=" + definition.activationStateMachineName + " priority=" + definition.interruptPriority + " stock=" + definition.baseMaxStock);
            try { foreach (string name in BombingNames) yield return BombingCase(name); bombingComplete = true; }
            finally { CleanupBombingProbe(); }
        }

        private IEnumerator SpawnBombingTarget()
        {
            RaycastHit floor;
            if (!Physics.Raycast(mark + facing * 3f + Vector3.up * 6f, Vector3.down, out floor, 20f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Bombing target fixture lacks native terrain.");
            var prefab = MasterCatalog.FindMasterPrefab("GolemMaster");
            if (!prefab) throw new InvalidOperationException("Native GolemMaster unavailable.");
            bombingTargetMaster = new MasterSummon { masterPrefab = prefab, position = floor.point + Vector3.up,
                rotation = Quaternion.LookRotation(-facing), summonerBodyObject = null,
                teamIndexOverride = TeamIndex.Monster, ignoreTeamMemberLimit = true,
                preSpawnSetupCallback = master => { foreach (var ai in master.GetComponents<BaseAI>()) ai.enabled = false; } }.Perform();
            if (!bombingTargetMaster) throw new InvalidOperationException("Native target summon failed.");
            foreach (var ai in bombingTargetMaster.GetComponents<BaseAI>()) ai.enabled = false;
            yield return Until(() => bombingTargetMaster && bombingTargetMaster.GetBody(), 5f, "damageable native Golem body");
            bombingTarget = bombingTargetMaster.GetBody();
            bombingTargetId = bombingTarget.healthComponent.netId.Value;
            bombingTargetMasterId = bombingTargetMaster.netId.Value;
            yield return FixedSeconds(0.5f);
            if (bombingTarget.healthComponent.godMode || !bombingTarget.healthComponent.alive || bombingTarget.HasBuff(RoR2Content.Buffs.HiddenInvincibility))
                throw new InvalidOperationException("Positive damage fixture is protected or dead.");
            Write(new BombingFixture { recordType = "bombing-fixture", runId = Path.GetFileName(output), scenario = segment,
                targetId = bombingTargetId, masterId = bombingTargetMaster.netId.Value, health = bombingTarget.healthComponent.health,
                fullHealth = bombingTarget.healthComponent.fullHealth, armor = bombingTarget.armor, position = bombingTarget.corePosition,
                godMode = bombingTarget.healthComponent.godMode, invulnerable = bombingTarget.HasBuff(RoR2Content.Buffs.HiddenInvincibility),
                pilotBleedChance = pilot.bleedChance, fixture = "native GolemMaster; AI disabled; no health/stat writes, healing or pinning" });
        }

        private AH64BombingRunProjectile[] NativeBombs()
        {
            return FindObjectsOfType<AH64BombingRunProjectile>().Where(p => p && p.GetComponent<ProjectileController>()
                && p.GetComponent<ProjectileController>().owner == pilot.gameObject).ToArray();
        }

        private IEnumerator BombingCase(string name)
        {
            CleanupBombingFixture();
            pilot.GetComponent<AH64HoverController>().ResetAfterTeleport();
            yield return ResetCase("bombing-" + name);
            yield return Until(() => NativeBombs().Length == 0, 8f, "preceding native bombs drain");
            bombingObservations.Clear(); bombingReports.Clear(); bombingAcceptedNativeInfo.Clear(); bombingProjectileIds.Clear(); bombingDropByProjectile.Clear(); bombingCastByProjectile.Clear();
            bombingCastId = 0; bombingTargetId = bombingTargetMasterId = 0; bombingOnEnemy = bombingOnAll = 0;
            bool targetCase = name == "stationary" || name == "bleed", curve = name == "curve", invalid = name == "invalid";
            var slot = pilot.skillLocator.special;
            var originalSpecial = slot.skillDef; var originalUtility = pilot.skillLocator.utility.skillDef;
            bombingSpecialOverride = Variant(slot, typeof(BombingRun));
            slot.SetSkillOverride(this, bombingSpecialOverride, GenericSkill.SkillOverridePriority.Contextual);
            int bleedBefore = pilot.inventory.GetItemCount(RoR2Content.Items.BleedOnHit), icbmBefore = pilot.inventory.GetItemCount(DLC1Content.Items.MoreMissile);
            float healthBefore = 0f; int stockBefore = 0, stockBeforeRestore = -1; bool interrupted = false, primary = false, hydra = false, brake = false, nativeOverlap = false, noRefund = false;
            try {
                if (name == "bleed") {
                    pilot.inventory.GiveItem(RoR2Content.Items.BleedOnHit, 20); bombingAddedBleed = true;
                    yield return Until(() => pilot.bleedChance >= 200f, 5f, "actual native bleedChance >=200 at proc0.5");
                }
                if (curve) {
                    if (icbmBefore != 0) throw new InvalidOperationException("Curve fixture requires exactly one isolated ICBM.");
                    pilot.inventory.GiveItem(DLC1Content.Items.MoreMissile, 1); bombingAddedIcbm = true;
                    bombingUtilityOverride = Variant(pilot.skillLocator.utility, typeof(BrakingTurn));
                    pilot.skillLocator.utility.SetSkillOverride(this, bombingUtilityOverride, GenericSkill.SkillOverridePriority.Contextual);
                    yield return Until(() => pilot.skillLocator.utility.CanExecute(), 12f, "curve braking readiness");
                }
                if (targetCase) { yield return SpawnBombingTarget(); healthBefore = bombingTarget.healthComponent.health; }
                yield return Until(() => slot.CanExecute() && (!curve || pilot.skillLocator.primary.CanExecute() && pilot.skillLocator.secondary.CanExecute()), 15f, "native bombing readiness");
                if (!AH64BombingRunCast.SafeOrigin(pilot.corePosition, pilot.corePosition + Vector3.down * AH64BombingRunStaticValues.ReleaseOffset))
                    throw new InvalidOperationException("Native origin invalid before owned fixture.");
                if (invalid) {
                    bombingObstruction = new GameObject("AH64OwnedBombingInvalidOriginFixture");
                    bombingObstruction.layer = LayerIndex.world.intVal;
                    bombingObstruction.transform.SetParent(pilot.gameObject.transform, true);
                    bombingObstruction.transform.position = pilot.corePosition + Vector3.down * 0.25f;
                    bombingObstruction.AddComponent<BoxCollider>().size = Vector3.one;
                    Physics.SyncTransforms();
                    if (AH64BombingRunCast.SafeOrigin(pilot.corePosition, pilot.corePosition + Vector3.down * AH64BombingRunStaticValues.ReleaseOffset))
                        throw new InvalidOperationException("Actual native World obstruction did not reject origin.");
                }
                if (curve) { move = facing; prototypeAim = facing; brakingCollective = true; pilot.inputBank.jump.down = true; }
                bombingCaseActive = true; stockBefore = slot.stock;
                bool accepted = slot.ExecuteIfReady();
                yield return Until(() => Machine("Weapon2").state is BombingRun, 2f, "native bombing Weapon2 entry");
                var state = Machine("Weapon2").state;
                BombingCheck(name + ".activation", accepted && stockBefore > 0 && slot.stock == stockBefore - 1 && bombingCastId != 0,
                    "accepted=" + accepted + " stock=" + stockBefore + "->" + slot.stock + " nativeCast=" + bombingCastId);
                if (curve) {
                    yield return FixedSeconds(0.4f);
                    move = Quaternion.AngleAxis(70f, Vector3.up) * facing; prototypeAim = move;
                    pilot.inputBank.moveVector = move; pilot.inputBank.aimDirection = move;
                    brake = pilot.skillLocator.utility.ExecuteIfReady();
                    primary = pilot.skillLocator.primary.ExecuteIfReady(); hydra = pilot.skillLocator.secondary.ExecuteIfReady();
                    yield return new WaitForFixedUpdate();
                    nativeOverlap = ReferenceEquals(Machine("Weapon2").state, state) && Machine("Body").state is BrakingTurn
                        && Machine("Weapon").state is FireChaingun && Machine("Weapon3").state is FireRocketPods;
                    Event("bombing-native-overlap", "primary=" + primary + " hydra=" + hydra + " brake=" + brake
                        + " actual=" + Machine("Body").state.GetType().Name + "/" + Machine("Weapon").state.GetType().Name + "/" + Machine("Weapon3").state.GetType().Name);
                }
                if (name == "interrupt") { yield return FixedSeconds(0.45f); interrupted = Machine("Weapon2").SetInterruptState(new Idle(), InterruptPriority.Stun); }
                yield return Until(() => !ReferenceEquals(Machine("Weapon2").state, state), 3f, "finite native bombing exit");
                int consumedAtExit = Opportunities().Length;
                brakingCollective = false; pilot.inputBank.jump.down = false; move = Vector3.zero; prototypeAim = Vector3.zero;
                yield return Until(() => NativeBombs().Length == 0, 8f, "native bombing lifetime drain");
                if (name == "bleed") yield return FixedSeconds(1f);
                var due = Opportunities(); var releases = bombingObservations.Where(o => o.kind == "release").ToArray();
                var terminals = bombingObservations.Where(o => o.kind == "impact" || o.kind == "despawn").ToArray();
                var hits = bombingObservations.Where(o => o.kind == "hit" && o.targetId == bombingTargetId).ToArray();
                var direct = bombingReports.Where(r => r.bomb && r.targetId == bombingTargetId && r.damageDealt > 0f && !r.rejected).ToArray();
                var end = bombingObservations.Where(o => o.kind == "end").ToArray();
                int expected = name == "interrupt" ? consumedAtExit : 6;
                bool opportunities = due.Length == expected && due.Select(o => o.dropIndex).Distinct().Count() == expected
                    && due.Select(o => o.dropIndex).OrderBy(i => i).SequenceEqual(Enumerable.Range(0, expected))
                    && due.All(o => Mathf.Abs(o.scheduledTime - o.dropIndex * 0.3f) < 0.001f && o.actualTime >= o.scheduledTime - 0.001f && o.actualTime - o.scheduledTime < 0.2f)
                    && bombingProjectileIds.Count == releases.Length && releases.All(o => o.projectileId != 0)
                    && terminals.Length == releases.Length && terminals.Select(o => o.dropIndex).Distinct().Count() == releases.Length
                    && terminals.All(t => releases.Any(r => r.dropIndex == t.dropIndex && t.time >= r.time && t.time - r.time <= 6.2f))
                    && end.Length == 1 && end[0].reason == (name == "interrupt" ? "interrupted" : "complete");
                if (name == "interrupt") opportunities &= interrupted && expected > 0 && expected < 6 && due.Length == consumedAtExit;
                if (invalid) opportunities &= releases.Length == 0 && due.All(o => o.kind == "suppressed" && o.reason == "invalid-or-obstructed-origin");
                else opportunities &= releases.Length == expected;
                BombingCheck(name + ".opportunities", opportunities, "due=" + due.Length + " releases=" + releases.Length + " actualProjectileIds=" + bombingProjectileIds.Count + " end=" + string.Join(",", end.Select(o => o.reason)) + " interrupted=" + interrupted);
                bool motion = releases.All(o => Finite(o.position) && Finite(o.velocity) && (o.position - (o.nativeCore + Vector3.down * 0.5f)).magnitude < 0.001f
                    && (o.velocity - (Vector3.ClampMagnitude(new Vector3(o.nativeVelocity.x, 0f, o.nativeVelocity.z), 25f) + Vector3.down * 4f)).magnitude < 0.001f);
                float bend = 0f;
                if (curve && releases.Length >= 4) {
                    Vector3 early = releases[2].position - releases[0].position, late = releases[releases.Length - 1].position - releases[2].position;
                    early.y = late.y = 0f; bend = early.magnitude > 0.1f && late.magnitude > 0.1f ? Vector3.Angle(early, late) : 0f;
                    motion &= bend > 5f && primary && hydra && brake && nativeOverlap && releases.All(o => AH64BombingRunCast.Finite(o.airtime)
                        && AH64BombingRunCast.Finite(o.targetHeight) && o.airtime >= 0f && o.icbmCount == 1 && o.jumpHeld) && releases.Any(o => o.ascending);
                }
                BombingCheck(name + ".motion", motion && (!curve || releases.Length == 6), "same-time native release origin/velocity agreement; path bend=" + bend + "; simultaneous native states=" + nativeOverlap);
                bool payload = !targetCase ? (invalid ? bombingReports.Count == 0 : true)
                    : hits.Length == 3 && direct.Length == 3 && hits.All(h => h.damage > 0f && Mathf.Abs(h.procCoefficient - 0.5f) < 0.001f)
                      && direct.All(d => Mathf.Abs(d.procCoefficient - 0.5f) < 0.001f && d.castId == bombingCastId)
                      && direct.Select(d => d.dropIndex).Distinct().Count() == 3 && hits.Select(h => h.dropIndex).Distinct().Count() == 3
                      && direct.Select(d => d.dropIndex).OrderBy(i => i).SequenceEqual(hits.Select(h => h.dropIndex).OrderBy(i => i))
                      && bombingOnEnemy == 3 && bombingOnAll == 3 && bombingObservations.Any(o => o.kind == "hit-suppressed" && o.targetId == bombingTargetId && o.reason == "overlap-budget");
                int bleeds = bombingReports.Count(r => r.dot == DotController.DotIndex.Bleed.ToString() && r.targetId == bombingTargetId && r.damageDealt > 0f && !r.rejected);
                if (name == "bleed") payload &= pilot.bleedChance >= 200f && bleeds > 0 && direct.Any() && bombingReports.Any(r => r.dot == DotController.DotIndex.Bleed.ToString() && r.time >= direct[0].time && r.targetId == bombingTargetId && r.damageDealt > 0f);
                payload &= bombingReports.All(r => AH64BombingRunCast.Finite(r.damageDealt) && AH64BombingRunCast.Finite(r.rawDamage) && AH64BombingRunCast.Finite(r.procCoefficient));
                BombingCheck(name + ".payload", payload, "native direct=" + direct.Length + " trace accepted=" + hits.Length + " native Bleed=" + bleeds
                    + " separate OnHitEnemy/All=" + bombingOnEnemy + "/" + bombingOnAll + " bleedChance=" + pilot.bleedChance
                    + " HP=" + healthBefore + "->" + (bombingTarget ? bombingTarget.healthComponent.health : -1f) + "; HP delta includes regeneration and is not raw coefficient proof");
                stockBeforeRestore = slot.stock; noRefund = stockBeforeRestore == stockBefore - 1;
            } finally { CleanupBombingFixture(); }
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            bool targetAlive = bombingTargetId != 0 && FindObjectsOfType<CharacterBody>().Any(b => b && b.healthComponent && b.healthComponent.netId.Value == bombingTargetId);
            bool masterAlive = bombingTargetMasterId != 0 && FindObjectsOfType<CharacterMaster>().Any(m => m && m.netId.Value == bombingTargetMasterId);
            bool overridesReleased = !bombingSpecialOverride && !bombingUtilityOverride && slot.skillDef == originalSpecial && pilot.skillLocator.utility.skillDef == originalUtility;
            Write(new BombingCleanup { recordType = "bombing-cleanup", runId = Path.GetFileName(output), scenario = segment,
                time = Time.fixedTime, castId = bombingCastId, bombs = NativeBombs().Length, targetAlive = targetAlive,
                targetMasterAlive = masterAlive, obstructionAlive = bombingObstruction != null, stockBefore = stockBefore, stockBeforeRestore = stockBeforeRestore,
                bleedBefore = bleedBefore, bleedAfter = pilot.inventory.GetItemCount(RoR2Content.Items.BleedOnHit),
                icbmBefore = icbmBefore, icbmAfter = pilot.inventory.GetItemCount(DLC1Content.Items.MoreMissile), noRefund = noRefund,
                originValid = AH64BombingRunCast.SafeOrigin(pilot.corePosition, pilot.corePosition + Vector3.down * 0.5f),
                weapon2State = Machine("Weapon2").state.GetType().Name, overrideReferencesReleased = overridesReleased });
            BombingCheck(name + ".cleanup", !bombingObstruction && !targetAlive && !masterAlive && overridesReleased && !bombingAddedBleed && !bombingAddedIcbm
                && pilot.inventory.GetItemCount(RoR2Content.Items.BleedOnHit) == bleedBefore && pilot.inventory.GetItemCount(DLC1Content.Items.MoreMissile) == icbmBefore
                && NativeBombs().Length == 0 && !(Machine("Weapon2").state is BombingRun) && noRefund
                && AH64BombingRunCast.SafeOrigin(pilot.corePosition, pilot.corePosition + Vector3.down * 0.5f),
                "owned target/world fixture/overrides/items removed; native payloads drained; no refund; origin valid");
        }

        private BombingObservation[] Opportunities() => bombingObservations.Where(o => o.kind == "release" || o.kind == "suppressed" && o.reason == "invalid-or-obstructed-origin").ToArray();

        private void ObserveBombingTrace(AH64BombingRunTrace.Record value)
        {
            if (!bombingCaseActive || finished || !pilot || value == null || value.ownerId != pilot.netId.Value) return;
            if (value.kind == "begin") { if (bombingCastId != 0) throw new InvalidOperationException("Multiple native casts in one fixture."); bombingCastId = value.castId; }
            if (value.castId != bombingCastId) return;
            int projectile = 0;
            if (value.kind == "release") {
                var fresh = NativeBombs().Where(p => !bombingProjectileIds.Contains(p.gameObject.GetInstanceID())).ToArray();
                if (fresh.Length == 1) {
                    projectile = fresh[0].gameObject.GetInstanceID(); bombingProjectileIds.Add(projectile);
                    bombingDropByProjectile[projectile] = value.dropIndex; bombingCastByProjectile[projectile] = value.castId;
                }
            }
            var hover = pilot.GetComponent<AH64HoverController>();
            var record = new BombingObservation { recordType = "bombing-observation", runId = Path.GetFileName(output), scenario = segment,
                castId = value.castId, ownerId = value.ownerId, targetId = value.targetId, kind = value.kind, reason = value.reason,
                dropIndex = value.dropIndex, scheduledTime = value.scheduledTime, actualTime = value.actualTime, time = Time.fixedTime,
                position = value.position, velocity = value.velocity, nativeCore = pilot.corePosition, nativeVelocity = pilot.characterMotor.velocity,
                projectileId = projectile, damage = value.damage, procCoefficient = value.procCoefficient, acceptedHits = value.acceptedHits,
                remainingBaseCoefficient = value.remainingBaseCoefficient, crit = value.crit, airtime = hover.Airtime, targetHeight = hover.TargetHeight,
                icbmCount = pilot.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile), jumpHeld = pilot.inputBank.jump.down, ascending = hover.IsAscending,
                bodyState = Machine("Body").state.GetType().Name, weapon2State = Machine("Weapon2").state.GetType().Name };
            bombingObservations.Add(record); Write(record);
        }

        private bool OwnedBomb(DamageInfo info) => info != null && pilot && info.attacker == pilot.gameObject && info.inflictor && info.inflictor.GetComponent<AH64BombingRunDamage>();

        private void ObserveBombingDamage(DamageReport report)
        {
            if (!bombingCaseActive || finished || report == null || !pilot || report.attacker != pilot.gameObject) return;
            var info = report.damageInfo; int id = info.inflictor ? info.inflictor.GetInstanceID() : 0;
            int drop; ulong cast;
            bool bomb = OwnedBomb(info);
            if (bomb && report.victim && report.victim.netId.Value == bombingTargetId && report.damageDealt > 0f
                && AH64BombingRunCast.Finite(report.damageDealt) && !info.rejected) bombingAcceptedNativeInfo.Add(info);
            var value = new BombingDamage { recordType = "bombing-damage", runId = Path.GetFileName(output), scenario = segment,
                castId = bombingCastByProjectile.TryGetValue(id, out cast) ? cast : 0,
                dropIndex = bombingDropByProjectile.TryGetValue(id, out drop) ? drop : -1, ownerId = pilot.netId.Value,
                targetId = report.victim ? report.victim.netId.Value : 0, projectileId = id, time = Time.fixedTime,
                bomb = bomb, dot = report.dotType.ToString(), damageDealt = report.damageDealt, rawDamage = info.damage,
                procCoefficient = info.procCoefficient, crit = info.crit, rejected = info.rejected };
            bombingReports.Add(value); Write(value);
        }

        private void ObserveBombingOnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo info, GameObject victim)
        {
            if (bombingCaseActive && !finished && OwnedBomb(info) && bombingAcceptedNativeInfo.Contains(info) && bombingTarget && victim == bombingTarget.gameObject) { bombingOnEnemy++; Event("bombing-native-OnHitEnemy", "projectile=" + info.inflictor.GetInstanceID() + " victim=" + victim.GetInstanceID()); }
            orig(self, info, victim);
        }
        private void ObserveBombingOnHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo info, GameObject victim)
        {
            if (bombingCaseActive && !finished && OwnedBomb(info) && bombingAcceptedNativeInfo.Contains(info) && bombingTarget && victim == bombingTarget.gameObject) { bombingOnAll++; Event("bombing-native-OnHitAll", "projectile=" + info.inflictor.GetInstanceID() + " victim=" + (victim ? victim.GetInstanceID() : 0)); }
            orig(self, info, victim);
        }

        private bool FinishBombingEvidence()
        {
            if (Environment.GetEnvironmentVariable("AH64_AUTOPILOT_BOMBING_CHECKS") != BombingSuite) return true;
            string[] ids = new[] { "catalog.order", "catalog.definition" }.Concat(BombingNames.SelectMany(n => BombingChecks.Select(c => n + "." + c))).ToArray();
            bool passed = bombingStarted && bombingComplete && !bombingHooks && !bombingCaseActive && bombingChecks.Count == ids.Length
                && bombingChecks.All(c => c.passed) && bombingChecks.Select(c => c.id).Distinct().Count() == ids.Length && !ids.Except(bombingChecks.Select(c => c.id)).Any();
            File.WriteAllText(Path.Combine(output, "bombing-result.json"), JsonUtility.ToJson(new BombingResult {
                runId = Path.GetFileName(output), sourceSha = JsonUtility.FromJson<StageIdentity>(File.ReadAllText(Path.Combine(output, "identity.json"))).sourceSha,
                dllSha256 = Hash(typeof(AH64Plugin).Assembly.Location), suite = BombingSuite, status = passed ? "passed" : "failed",
                complete = bombingComplete, expectedChecks = ids.Length, expectedIds = ids, checks = bombingChecks.ToArray(),
                hooksReleased = !bombingHooks, limitation = "Bounded scripted SOLO native smoke. Raw reports, cast/owner/target/drop/time retained; OnHit callbacks are distinct from actual downstream Bleed. No physical bindings/feel, peer, long-session, full natural terrain, audio or authentic media acceptance. Raw strict runtime outcomes remain independently blocking/classifiable." }, true));
            return passed;
        }

        [Serializable] private sealed class BombingResult { public int schema = 1, expectedChecks; public string runId, sourceSha, dllSha256, suite, status, limitation; public bool complete, hooksReleased; public string[] expectedIds; public Check[] checks; }
        [Serializable] private sealed class BombingObservation : Record { public ulong castId; public uint ownerId, targetId; public int dropIndex, projectileId, acceptedHits, icbmCount;
            public string kind, reason, bodyState, weapon2State; public float scheduledTime, actualTime, time, damage, procCoefficient, remainingBaseCoefficient, airtime, targetHeight;
            public Vector3 position, velocity, nativeCore, nativeVelocity; public bool crit, jumpHeld, ascending; }
        [Serializable] private sealed class BombingDamage : Record { public ulong castId; public uint ownerId, targetId; public int projectileId, dropIndex;
            public float time, damageDealt, rawDamage, procCoefficient; public bool bomb, crit, rejected; public string dot; }
        [Serializable] private sealed class BombingFixture : Record { public uint targetId, masterId; public float health, fullHealth, armor, pilotBleedChance; public Vector3 position; public bool godMode, invulnerable; public string fixture; }
        [Serializable] private sealed class BombingCleanup : Record { public ulong castId; public float time; public int bombs, bleedBefore, bleedAfter, icbmBefore, icbmAfter, stockBefore, stockBeforeRestore;
            public bool targetAlive, targetMasterAlive, obstructionAlive, noRefund, originValid, overrideReferencesReleased; public string weapon2State; }
    }
}
