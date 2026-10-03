using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using Path = System.IO.Path;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private readonly List<Check> brakingChecks = new List<Check>();
        private bool brakingStarted, brakingComplete, brakingCollective;
        private bool brakingHasRender;
        private bool brakingPreviousVisualActive;
        private Quaternion brakingPreviousRender;
        private int brakingRenderEpoch, brakingRenderViolations;
        private int brakingRenderSamples;
        private static readonly string[] BrakingCaseNames = { "stationary", "small", "fast", "spare", "collective", "interrupt" };
        private static readonly string[] BrakingCaseChecks = { "activation-capture", "midflight", "motion", "exit", "cleanup" };

        private void BrakingCheck(string id, bool passed, string actual)
        {
            brakingChecks.Add(new Check { id = id, passed = passed, actual = actual, scenario = segment });
            Event("braking-check", id + " " + (passed ? "PASS" : "FAIL") + " " + actual);
        }

        private IEnumerator BrakingCases()
        {
            brakingStarted = true;
            Event("braking-scope", "braking-solo-v1: actual SOLO skill/stock and native PreMove; scripted aim, held collective, initial momentum, inventory and overrides. No physical input, peer, collision/item, target-hit or audio acceptance.");
            var utility = pilot.skillLocator.utility;
            var variants = utility.skillFamily.variants;
            var skill = Variant(utility, typeof(BrakingTurn));
            BrakingCheck("catalog.order", variants.Length == 3 && variants[0].skillDef.activationState.stateType == typeof(ServoDash)
                && variants[1].skillDef.activationState.stateType == typeof(SmokeBackflip) && variants[2].skillDef == skill,
                string.Join(",", variants.Select(v => v.skillDef.activationState.stateType.Name)) + "; current default=" + utility.skillDef.skillName);
            BrakingCheck("catalog.definition", skill.activationStateMachineName == "Body" && skill.baseMaxStock == 1
                && skill.baseRechargeInterval == 4f && skill.stockToConsume == 1 && AH64BrakingTurnAcceptance.Priority(skill.interruptPriority)
                && skill.mustKeyPress && skill.icon,
                "machine=" + skill.activationStateMachineName + " stock=" + skill.baseMaxStock + " recharge=" + skill.baseRechargeInterval + " priority=" + skill.interruptPriority + "; placeholder icon=" + (skill.icon ? skill.icon.name : "missing"));
            foreach (string name in BrakingCaseNames) yield return BrakingMotionCase(name);
            yield return BrakingGuidanceCase();
            brakingComplete = true;
        }

        private IEnumerator BrakingMotionCase(string name)
        {
            yield return ResetCase("braking-" + name);
            var slot = pilot.skillLocator.utility;
            var skill = Variant(slot, typeof(BrakingTurn));
            var hover = pilot.GetComponent<AH64HoverController>();
            var visuals = pilot.GetComponent<AH64FlightVisuals>();
            var camera = pilot.GetComponent<CameraTargetParams>();
            bool spare = name == "spare", collective = name == "collective", interruption = name == "interrupt";
            bool removedSafety = false;
            if (spare) pilot.inventory.GiveItem(RoR2Content.Items.UtilitySkillMagazine);
            slot.SetSkillOverride(this, skill, GenericSkill.SkillOverridePriority.Contextual);
            try {
                yield return Until(() => slot.CanExecute() && (!spare || slot.maxStock >= 2), 12f, "braking readiness");
                if (spare && slot.stock < 2) slot.RestockSteplike();
                yield return Until(() => !spare || slot.stock >= 2, 12f, "braking spare stock");
                if (hover.GroundDistance < 0f || hover.GroundDistance > 8f) throw new InvalidOperationException("Braking fixture lacks safe low terrain: " + hover.GroundDistance);
                if (collective) {
                    brakingCollective = true; pilot.inputBank.jump.down = true;
                    yield return FixedSeconds(0.12f);
                }
                float entryFov = camera.fovOverride, entryAirControl = pilot.characterMotor.airControl;
                int beforeStock = slot.stock;
                int renderBefore = brakingRenderViolations;
                float targetBefore = hover.TargetHeight, airtimeBefore = hover.Airtime;
                Vector3 requested = name == "small" ? Quaternion.AngleAxis(30f, Vector3.up) * facing
                    : name == "stationary" ? Quaternion.AngleAxis(90f, Vector3.up) * facing : -facing;
                prototypeAim = requested; move = Vector3.zero; pilot.inputBank.moveVector = move; pilot.inputBank.aimDirection = requested;
                Vector3 incoming = name == "stationary" || collective ? Vector3.zero : facing * pilot.moveSpeed * (name == "fast" ? 2f : 1f);
                pilot.characterMotor.velocity = incoming + Vector3.up * pilot.characterMotor.velocity.y;
                if (spare) { pilot.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility); removedSafety = true; }
                bool accepted = slot.ExecuteIfReady();
                yield return Until(() => Machine("Body").state is BrakingTurn, 2f, "native braking entry");
                var state = (BrakingTurn)Machine("Body").state;
                var capture = state.EntrySnapshot;
                var motor = pilot.GetComponent<AH64BrakingTurnMotor>();
                var presentation = pilot.GetComponent<AH64BrakingTurnPresentation>();
                float capturedSpeed = AH64BrakingTurnMath.Horizontal(capture.EntryVelocity).magnitude;
                BrakingCheck(name + ".activation-capture", accepted && slot.stock == beforeStock - 1 && capturedSpeed >= 0f
                    && Vector3.Angle(capture.RequestedHeading, requested) < 0.1f && capture.Duration == 1f
                    && (name != "stationary" || capturedSpeed < 0.2f) && (name != "small" || Mathf.Abs(capture.SignedCorrection - 30f) < 5f)
                    && (name != "fast" || capturedSpeed > capture.MoveSpeed),
                    "accepted=" + accepted + " stock=" + beforeStock + "->" + slot.stock + " captured=" + JsonUtility.ToJson(capture));
                int spareStock = slot.stock;
                bool early = spare && slot.ExecuteIfReady();
                // Change native aim after capture; requested heading must remain the entry intent.
                prototypeAim = facing;
                yield return FixedSeconds(0.36f);
                AH64BrakingTurnFrame frame;
                bool active = ReferenceEquals(Machine("Body").state, state) && presentation.TryGetFrame(state, out frame);
                bool late = spare && slot.ExecuteIfReady();
                bool middle = active && !state.MotionYielded && motor.HasActiveLease
                    && Vector3.Angle(state.EntrySnapshot.RequestedHeading, requested) < 0.1f;
                bool checkTurn = name == "small" || name == "fast";
                if (checkTurn) middle &= AH64BrakingTurnAcceptance.Turned(capture, motor.LastAppliedVelocity, false);
                if (spare) middle &= spareStock >= 1 && !early && !late && slot.stock == spareStock
                    && !pilot.HasBuff(RoR2Content.Buffs.HiddenInvincibility) && !pilot.HasBuff(RoR2Content.Buffs.Cloak);
                if (collective) middle &= hover.TargetHeight >= targetBefore && hover.IsAscending && hover.Airtime > 0f
                    && hover.Airtime <= airtimeBefore + Time.fixedDeltaTime * 2f;
                BrakingCheck(name + ".midflight", middle, "active=" + active + " yielded=" + state.MotionYielded
                    + " age=" + motor.AppliedAge + " reentry=" + early + "," + late + " target=" + targetBefore + "->" + hover.TargetHeight
                    + " airtime=" + airtimeBefore + "->" + hover.Airtime + " actual directed turn=" + AH64BrakingTurnAcceptance.SignedProgress(capture, motor.LastAppliedVelocity));
                bool interruptAccepted = true;
                if (interruption) interruptAccepted = Machine("Body").SetInterruptState(new AH64Main(), InterruptPriority.Stun);
                yield return Until(() => Machine("Body").state != state, 3f, "braking exit");
                float lastSpeed = AH64BrakingTurnMath.Horizontal(motor.LastAppliedVelocity).magnitude;
                bool motion = motor.AppliedSteps > 0 && Finite(motor.LastAppliedVelocity)
                    && motor.LastAppliedVelocity.y == motor.LastNativeVelocity.y
                    && lastSpeed <= Mathf.Max(capturedSpeed, capture.ExitSpeed) + 0.1f;
                if (name == "stationary") motion &= lastSpeed < 0.2f;
                if (name == "fast") motion &= motor.AppliedAge >= 0.95f && lastSpeed <= capture.MoveSpeed + 0.1f && lastSpeed > 0.1f;
                if (checkTurn) motion &= AH64BrakingTurnAcceptance.Turned(capture, motor.LastAppliedVelocity, true);
                BrakingCheck(name + ".motion", motion, "steps=" + motor.AppliedSteps + " last age=" + motor.AppliedAge
                    + " native=" + motor.LastNativeVelocity + " applied=" + motor.LastAppliedVelocity + " final active speed=" + lastSpeed
                    + " exit ceiling=" + capture.ExitSpeed + " actual directed turn=" + AH64BrakingTurnAcceptance.SignedProgress(capture, motor.LastAppliedVelocity)
                    + " final alignment=" + AH64BrakingTurnAcceptance.Alignment(capture, motor.LastAppliedVelocity) + "; post-Main velocity is not used as exit carry");
                BrakingCheck(name + ".exit", interruptAccepted && Machine("Body").state is AH64Main
                    && !(Machine("Body").state is BrakingTurn) && !motor.HasActiveLease && !presentation.HasActiveOwner
                    && (interruption || motor.AppliedAge >= 0.95f),
                    "actual=" + Machine("Body").state.GetType().Name + " native stronger interruption=" + interruptAccepted + " age=" + motor.AppliedAge);
                brakingCollective = false; pilot.inputBank.jump.down = false; prototypeAim = Vector3.zero;
                yield return FixedSeconds(1.4f);
                BrakingCheck(name + ".cleanup", MovementResourcesReleased(entryFov, entryAirControl)
                    && !motor.HasActiveLease && !presentation.HasActiveOwner && !visuals.HasBrakingOwner && !visuals.IsBrakingRecovering
                    && brakingRenderViolations == renderBefore,
                    "motor=" + motor.HasActiveLease + " presentation=" + presentation.HasActiveOwner + " visual=" + visuals.HasBrakingOwner
                    + " recovery=" + visuals.IsBrakingRecovering + " render violations=" + (brakingRenderViolations - renderBefore));
            } finally {
                brakingCollective = false; prototypeAim = Vector3.zero; move = Vector3.zero;
                slot.UnsetSkillOverride(this, skill, GenericSkill.SkillOverridePriority.Contextual);
                if (spare) pilot.inventory.RemoveItem(RoR2Content.Items.UtilitySkillMagazine);
                if (removedSafety && pilot && pilot.healthComponent.alive) pilot.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            }
        }

        private IEnumerator BrakingGuidanceCase()
        {
            yield return ResetCase("braking-guidance");
            var utility = pilot.skillLocator.utility; var special = pilot.skillLocator.special;
            var brake = Variant(utility, typeof(BrakingTurn)); var hellfire = Variant(special, typeof(FireHellfire));
            utility.SetSkillOverride(this, brake, GenericSkill.SkillOverridePriority.Contextual);
            special.SetSkillOverride(this, hellfire, GenericSkill.SkillOverridePriority.Contextual);
            try {
                yield return Until(() => utility.CanExecute() && special.CanExecute(), 15f, "concurrent guidance readiness");
                prototypeHeld = true; prototypeAim = (facing + Vector3.up * 1.5f).normalized;
                pilot.inputBank.skill4.down = true; pilot.inputBank.aimDirection = prototypeAim;
                bool launch = special.ExecuteIfReady();
                yield return FixedSeconds(0.12f);
                var lead = NativeHellfires().SingleOrDefault(p => p.combo == 0);
                if (!lead) throw new InvalidOperationException("Braking guidance has no unique native lead.");
                var guidance = lead.GetComponent<AH64HellfireGuidance>(); var owner = pilot.GetComponent<AH64HellfireOwner>();
                uint token = guidance.Token; Vector3 heading = lead.transform.forward;
                bool activate = utility.ExecuteIfReady();
                yield return FixedSeconds(0.08f);
                BrakingCheck("guidance.entry", launch && activate && Machine("Body").state is BrakingTurn && token != 0
                    && owner.Policy.CanGuide(token, Time.fixedTime), "braking=" + Machine("Body").state.GetType().Name + " token=" + token);
                prototypeAim = Quaternion.AngleAxis(30f, Vector3.up) * prototypeAim;
                pilot.inputBank.aimDirection = prototypeAim;
                bool primary = pilot.skillLocator.primary.ExecuteIfReady(), hydra = pilot.skillLocator.secondary.ExecuteIfReady();
                yield return FixedSeconds(0.3f);
                BrakingCheck("guidance.turn-weapons", lead && Machine("Body").state is BrakingTurn && primary && hydra
                    && owner.Policy.CanGuide(token, Time.fixedTime) && guidance.LastTurnDegrees > 0f && Vector3.Angle(heading, lead.transform.forward) > 0.1f,
                    "primary=" + primary + " hydra=" + hydra + " active=" + owner.Policy.ActiveToken + " turn=" + (lead ? Vector3.Angle(heading, lead.transform.forward) : -1f));
                prototypeHeld = false; pilot.inputBank.skill4.down = false;
                yield return FixedSeconds(0.12f);
                Vector3 coast = lead ? lead.transform.forward : Vector3.zero;
                prototypeAim = Quaternion.AngleAxis(-90f, Vector3.up) * prototypeAim;
                yield return FixedSeconds(0.2f);
                BrakingCheck("guidance.release", lead && owner.Policy.ActiveToken == 0 && Vector3.Angle(coast, lead.transform.forward) < 0.01f,
                    "active=" + owner.Policy.ActiveToken + "; native release coasts");
                yield return Until(() => NativeHellfires().Length == 0, 12f, "braking Hellfire drain");
                BrakingCheck("guidance.drain", NativeHellfires().Length == 0 && !(Machine("Body").state is BrakingTurn)
                    && !pilot.GetComponent<AH64BrakingTurnMotor>().HasActiveLease, "all native guided missiles drained; no damage/hit claim");
            } finally {
                prototypeHeld = false; prototypeAim = Vector3.zero;
                utility.UnsetSkillOverride(this, brake, GenericSkill.SkillOverridePriority.Contextual);
                special.UnsetSkillOverride(this, hellfire, GenericSkill.SkillOverridePriority.Contextual);
            }
        }

        private void ObserveBrakingPhysics()
        {
            if (!brakingStarted) return;
            var motor = pilot.GetComponent<AH64BrakingTurnMotor>();
            var hover = pilot.GetComponent<AH64HoverController>(); var visuals = pilot.GetComponent<AH64FlightVisuals>();
            var state = Machine("Body").state as BrakingTurn;
            var presentation = pilot.GetComponent<AH64BrakingTurnPresentation>();
            AH64BrakingTurnFrame frame = default(AH64BrakingTurnFrame);
            bool hasPhase = state != null && presentation && presentation.TryGetFrame(state, out frame);
            Write(new BrakingSample { recordType = "braking-sample", runId = Path.GetFileName(output), scenario = segment,
                tick = tick, simulationTime = Time.fixedTime, capture = state != null ? JsonUtility.ToJson(state.EntrySnapshot) : "none",
                phase = hasPhase ? frame.Phase.ToString() : "inactive", progress = state != null ? state.ManeuverProgress : 0f,
                commandedHeading = frame.CommandedHeading, position = pilot.gameObject.transform.position, velocity = pilot.characterMotor.velocity,
                nativeAim = pilot.inputBank.aimDirection, targetHeight = hover.TargetHeight, airtime = hover.Airtime,
                groundDistance = hover.GroundDistance, ascending = hover.IsAscending, appliedAge = motor ? motor.AppliedAge : 0f,
                appliedSteps = motor ? motor.AppliedSteps : 0, nativeVelocity = motor ? motor.LastNativeVelocity : Vector3.zero,
                appliedVelocity = motor ? motor.LastAppliedVelocity : Vector3.zero, lease = motor && motor.HasActiveLease,
                yieldReason = motor ? motor.YieldReason : "none", visualOwner = visuals && visuals.HasBrakingOwner,
                visualRecovery = visuals && visuals.IsBrakingRecovering, displayedBaseWorld = visuals ? visuals.RenderedBaseWorld : Quaternion.identity });
        }

        private void LateUpdate()
        {
            if (!brakingStarted || !scripting || finished || !pilot) return;
            Quaternion world = pilot.modelLocator.modelTransform.rotation;
            var visuals = pilot.GetComponent<AH64FlightVisuals>();
            bool visualActive = visuals && (visuals.HasBrakingOwner || visuals.IsBrakingRecovering);
            float delta = brakingHasRender && brakingRenderEpoch == poseEpoch ? Quaternion.Angle(brakingPreviousRender, world) : 0f;
            // Generous bounded alarm allows native 360deg/s heading plus recoil; this is not a feel score.
            float bound = 5f + 360f * Time.deltaTime;
            if (brakingHasRender && brakingRenderEpoch == poseEpoch && (visualActive || brakingPreviousVisualActive) && delta > bound) brakingRenderViolations++;
            Write(new BrakingRender { recordType = "braking-render", runId = Path.GetFileName(output), scenario = segment,
                renderedTime = Time.time, renderDeltaTime = Time.deltaTime, poseEpoch = poseEpoch, worldAttitude = world,
                observedDelta = delta, alarmBound = bound, visualActive = visualActive, bodyState = Machine("Body").state.GetType().Name });
            brakingPreviousRender = world; brakingHasRender = true; brakingRenderEpoch = poseEpoch; brakingRenderSamples++;
            brakingPreviousVisualActive = visualActive;
        }

        private bool FinishBrakingEvidence()
        {
            if (Environment.GetEnvironmentVariable("AH64_AUTOPILOT_BRAKING_CHECKS") != "braking-solo-v1") return true;
            string[] ids = new[] { "catalog.order", "catalog.definition" }.Concat(BrakingCaseNames.SelectMany(n => BrakingCaseChecks.Select(c => n + "." + c)))
                .Concat(new[] { "guidance.entry", "guidance.turn-weapons", "guidance.release", "guidance.drain" }).ToArray();
            bool passed = brakingStarted && brakingComplete && brakingChecks.Count == ids.Length && brakingChecks.All(c => c.passed)
                && brakingChecks.Select(c => c.id).Distinct().Count() == ids.Length && !ids.Except(brakingChecks.Select(c => c.id)).Any()
                && brakingRenderSamples > 0 && brakingRenderViolations == 0;
            File.WriteAllText(Path.Combine(output, "braking-result.json"), JsonUtility.ToJson(new BrakingResult {
                runId = Path.GetFileName(output), sourceSha = JsonUtility.FromJson<StageIdentity>(File.ReadAllText(Path.Combine(output, "identity.json"))).sourceSha,
                dllSha256 = Hash(typeof(AH64Plugin).Assembly.Location), suite = "braking-solo-v1", status = passed ? "passed" : "failed",
                complete = brakingComplete, expectedChecks = ids.Length, expectedIds = ids, checks = brakingChecks.ToArray(),
                renderSamples = brakingRenderSamples, renderViolations = brakingRenderViolations,
                utilityInterruptPriority = Variant(pilot.skillLocator.utility, typeof(BrakingTurn)).interruptPriority.ToString(),
                limitation = "SOLO scripted input, initial velocity and isolated inventory/overrides. No new braking screenshots. The five baseline images do not show braking. Physical modern descend, native geometry/force/item edge fixtures, targeted hits/audio, controller feel, peers and long sessions remain unverified. Render bound is a coarse alarm. Raw strict runtime failures retain independent blocking/classification." }, true));
            return passed;
        }

        [Serializable] private sealed class BrakingResult { public int schema = 1, expectedChecks, renderSamples, renderViolations;
            public string runId, sourceSha, dllSha256, suite, status, limitation, utilityInterruptPriority; public bool complete; public string[] expectedIds; public Check[] checks; }
        [Serializable] private sealed class BrakingSample : Record { public int tick, appliedSteps; public float simulationTime, progress, appliedAge, targetHeight, airtime, groundDistance;
            public string capture, phase, yieldReason; public Vector3 commandedHeading, position, velocity, nativeAim, nativeVelocity, appliedVelocity;
            public Quaternion displayedBaseWorld; public bool ascending, lease, visualOwner, visualRecovery; }
        [Serializable] private sealed class BrakingRender : Record { public int poseEpoch; public float renderedTime, renderDeltaTime, observedDelta, alarmBound;
            public Quaternion worldAttitude; public string bodyState; public bool visualActive; }
    }
}
