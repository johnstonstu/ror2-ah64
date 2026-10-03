using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using Path = System.IO.Path;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private readonly List<Check> prototypeChecks = new List<Check>();
        private bool prototypeStarted, prototypeComplete;

        private void PrototypeCheck(string id, bool passed, string actual)
        {
            prototypeChecks.Add(new Check { id=id, passed=passed, actual=actual, scenario=segment });
            Event("prototype-check", id+" "+(passed ? "PASS" : "FAIL")+" "+actual);
        }

        private SkillDef Variant(GenericSkill skill, Type state)
        {
            return skill.skillFamily.variants.Single(v => v.skillDef.activationState.stateType == state).skillDef;
        }

        private IEnumerator PrototypeCases()
        {
            prototypeStarted = true;
            Event("prototype-scope", "SOLO actual native GenericSkill.ExecuteIfReady and stock pipeline; scripted input-bank hold/aim and isolated inventory/skill overrides; physical controls and peer networking unverified");
            yield return PrototypeManeuver(false);
            yield return PrototypeManeuver(true);
            yield return PrototypeHellfire(false);
            yield return PrototypeHellfire(true);
            yield return PrototypeLongbow();
            prototypeComplete = true;
        }

        private IEnumerator PrototypeManeuver(bool backflip)
        {
            string label = backflip ? "prototype-backflip" : "prototype-roll";
            yield return ResetCase(label);
            var slot = pilot.skillLocator.utility;
            var skill = Variant(slot, backflip ? typeof(SmokeBackflip) : typeof(ServoDash));
            slot.SetSkillOverride(this, skill, GenericSkill.SkillOverridePriority.Contextual);
            try {
                yield return Until(() => slot.CanExecute(), 15f, "utility readiness");
                Vector3 side = Vector3.Cross(Vector3.up, facing);
                move = side * 0.25f;
                pilot.inputBank.moveVector = move;
                pilot.characterMotor.velocity = facing * 7f + side * 3f;
                int stock = slot.stock;
                bool activated = slot.ExecuteIfReady();
                PrototypeCheck(label+".activation-stock", activated && slot.stock == stock-1,
                    "ExecuteIfReady="+activated+" stock "+stock+" -> "+slot.stock);
                yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
                var state = Machine("Body").state;
                var roll = state as ServoDash;
                var flip = state as SmokeBackflip;
                bool entered = backflip ? flip != null : roll != null;
                PrototypeCheck(label+".native-state", entered, state.GetType().FullName);
                if (!entered) throw new InvalidOperationException("Native utility activation failed.");
                var snapshot = backflip ? flip.EntrySnapshot : roll.EntrySnapshot;
                PrototypeCheck(label+".momentum-capture", snapshot.EntryVelocity.magnitude > 1f &&
                    Mathf.Abs(Vector3.Dot(snapshot.EntryVelocity, side)) > 0.1f,
                    "captured="+snapshot.EntryVelocity+" actual="+pilot.characterMotor.velocity);
                PrototypeCheck(label+".reentry-rejected", !slot.ExecuteIfReady(), "native utility ExecuteIfReady during Pain-priority maneuver");
                Vector3 velocity = pilot.characterMotor.velocity;
                move = -side * 0.5f;
                yield return FixedSeconds(0.15f);
                PrototypeCheck(label+".steering-finite", Finite(pilot.characterMotor.velocity) &&
                    (pilot.characterMotor.velocity-velocity).sqrMagnitude > 0.01f,
                    "before="+velocity+" after="+pilot.characterMotor.velocity);
                yield return Until(() => Machine("Body").state != state, 5f, "utility exit");
                PrototypeCheck(label+".exit-carry", Finite(pilot.characterMotor.velocity) && pilot.characterMotor.velocity.magnitude > 0.1f,
                    pilot.characterMotor.velocity.ToString());
                move = Vector3.zero;
                yield return FixedSeconds(0.5f);
                var camera = pilot.GetComponent<CameraTargetParams>();
                PrototypeCheck(label+".cleanup", camera && camera.fovOverride == -1f &&
                    !pilot.characterMotor.disableAirControlUntilCollision && pilot.characterMotor.airControl > 0f,
                    "FOV="+(camera ? camera.fovOverride.ToString() : "missing")+" airControl="+pilot.characterMotor.airControl);
            } finally { slot.UnsetSkillOverride(this, skill, GenericSkill.SkillOverridePriority.Contextual); move=Vector3.zero; }
        }

        private ProjectileController[] NativeHellfires()
        {
            return FindObjectsOfType<ProjectileController>().Where(p => p && p.owner == pilot.gameObject &&
                p.GetComponent<AH64HellfireGuidance>()).ToArray();
        }

        private IEnumerator PrototypeHellfire(bool icbm)
        {
            string label = icbm ? "prototype-icbm" : "prototype-guidance";
            yield return ResetCase(label);
            var slot = pilot.skillLocator.special;
            var skill = Variant(slot, typeof(FireHellfire));
            slot.SetSkillOverride(this, skill, GenericSkill.SkillOverridePriority.Contextual);
            if (icbm) pilot.inventory.GiveItem(DLC1Content.Items.MoreMissile);
            try {
                yield return Until(() => slot.CanExecute(), 15f, "Hellfire readiness");
                // Open sky keeps the native missile observable; damage/obstruction are separate gates.
                prototypeAim = (facing + Vector3.up * 1.5f).normalized;
                prototypeHeld = true; pilot.inputBank.skill4.down = true; pilot.inputBank.aimDirection = prototypeAim;
                int stock=slot.stock, before=launches;
                bool activated=slot.ExecuteIfReady();
                PrototypeCheck(label+".activation-stock", activated && slot.stock==stock-1,
                    "ExecuteIfReady="+activated+" stock "+stock+" -> "+slot.stock);
                yield return FixedSeconds(0.12f);
                var projectiles=NativeHellfires();
                int expected=icbm ? 3 : 1;
                PrototypeCheck(label+".native-count", projectiles.Length==expected && launches-before==expected,
                    "native="+projectiles.Length+" requests="+(launches-before));
                var lead=projectiles.SingleOrDefault(p => p.combo==0);
                if (!lead) throw new InvalidOperationException("No unique native Hellfire lead.");
                var guidance=lead.GetComponent<AH64HellfireGuidance>();
                var owner=pilot.GetComponent<AH64HellfireOwner>();
                PrototypeCheck(label+".lead-token", guidance.Token!=0 && owner && owner.Policy.CanGuide(guidance.Token,Time.fixedTime),
                    "token="+guidance.Token+" active="+(owner ? owner.Policy.ActiveToken : 0));
                Vector3 initial=lead.transform.forward;
                var extras=projectiles.Where(p => p.combo!=0).ToArray();
                var extraHeadings=extras.Select(p => p.transform.forward).ToArray();
                PrototypeCheck(label+".extras-unguided", extras.All(p => p.GetComponent<AH64HellfireGuidance>().Token==0),
                    "extras="+extras.Length+" tokens="+string.Join(",",extras.Select(p => p.GetComponent<AH64HellfireGuidance>().Token.ToString())));
                prototypeAim=(Quaternion.AngleAxis(30f,Vector3.up)*prototypeAim).normalized;
                pilot.inputBank.aimDirection=prototypeAim;
                bool primary=pilot.skillLocator.primary.ExecuteIfReady();
                bool secondary=pilot.skillLocator.secondary.ExecuteIfReady();
                yield return FixedSeconds(0.3f);
                PrototypeCheck(label+".turn-and-weapons", lead && guidance.LastTurnDegrees>0f &&
                    Vector3.Angle(initial,lead.transform.forward)>0.1f && primary && secondary && owner.Policy.CanGuide(guidance.Token,Time.fixedTime),
                    "turn="+(lead ? Vector3.Angle(initial,lead.transform.forward).ToString() : "destroyed")+" primary="+primary+" Hydra="+secondary);
                PrototypeCheck(label+".extras-ballistic", extras.Select((p,i) => p && Vector3.Angle(p.transform.forward,extraHeadings[i])<0.01f).All(v => v),
                    "surviving fan headings unchanged within 0.01 degrees");
                var utility=pilot.skillLocator.utility;
                var maneuverSkill=Variant(utility,icbm ? typeof(SmokeBackflip) : typeof(ServoDash));
                utility.SetSkillOverride(this,maneuverSkill,GenericSkill.SkillOverridePriority.Contextual);
                try {
                    yield return Until(() => utility.CanExecute(),15f,"concurrent maneuver readiness");
                    bool maneuverActivated=utility.ExecuteIfReady();
                    yield return FixedSeconds(0.06f);
                    bool maneuverEntered=icbm ? Machine("Body").state is SmokeBackflip : Machine("Body").state is ServoDash;
                    PrototypeCheck(label+".maneuver-guidance",maneuverActivated && maneuverEntered && lead && owner.Policy.CanGuide(guidance.Token,Time.fixedTime),
                        "native maneuver="+Machine("Body").state.GetType().Name+" active="+owner.Policy.ActiveToken);
                } finally {utility.UnsetSkillOverride(this,maneuverSkill,GenericSkill.SkillOverridePriority.Contextual);}
                prototypeHeld=false; pilot.inputBank.skill4.down=false;
                yield return FixedSeconds(0.12f);
                Vector3 coast=lead ? lead.transform.forward : Vector3.zero;
                prototypeAim=(Quaternion.AngleAxis(-90f,Vector3.up)*prototypeAim).normalized;
                yield return FixedSeconds(0.25f);
                PrototypeCheck(label+".release-coast", lead && owner.Policy.ActiveToken==0 && Vector3.Angle(coast,lead.transform.forward)<0.01f,
                    "active="+owner.Policy.ActiveToken+" coastAngle="+(lead ? Vector3.Angle(coast,lead.transform.forward).ToString() : "destroyed"));
                yield return Until(() => NativeHellfires().Length==0,12f,"native missile expiry");
                PrototypeCheck(label+".expiry", NativeHellfires().Length==0 && owner.Policy.ActiveToken==0,"native projectiles drained; no damage attribution claim");
            } finally {
                prototypeHeld=false; prototypeAim=Vector3.zero;
                slot.UnsetSkillOverride(this,skill,GenericSkill.SkillOverridePriority.Contextual);
                if(icbm) pilot.inventory.RemoveItem(DLC1Content.Items.MoreMissile);
            }
        }

        private IEnumerator PrototypeLongbow()
        {
            yield return ResetCase("prototype-longbow");
            var slot=pilot.skillLocator.special;
            var skill=Variant(slot,typeof(PaintLongbow));
            slot.SetSkillOverride(this,skill,GenericSkill.SkillOverridePriority.Contextual);
            try {
                yield return Until(() => slot.CanExecute(),15f,"Longbow readiness");
                int stock=slot.stock;
                prototypeHeld=true; pilot.inputBank.skill4.down=true;
                bool activated=slot.ExecuteIfReady();
                yield return FixedSeconds(0.3f);
                PrototypeCheck("prototype-longbow.activation",activated && Machine("Weapon2").state is PaintLongbow,"native no-target painting state");
                prototypeHeld=false; pilot.inputBank.skill4.down=false;
                yield return Until(() => !(Machine("Weapon2").state is PaintLongbow),5f,"Longbow release");
                PrototypeCheck("prototype-longbow.no-target-stock",slot.stock==stock,"stock "+stock+" -> "+slot.stock+"; targeted reservations/refunds and Lysate sixth-shot cap remain unverified");
            } finally {prototypeHeld=false;slot.UnsetSkillOverride(this,skill,GenericSkill.SkillOverridePriority.Contextual);}
        }

        private bool FinishPrototypeEvidence()
        {
            bool requested=Environment.GetEnvironmentVariable("AH64_AUTOPILOT_FEATURE_CHECKS")=="solo-prototype-v1";
            if(!requested) return true;
            bool passed=prototypeStarted && prototypeComplete && prototypeChecks.Count==34 && prototypeChecks.All(c => c.passed);
            File.WriteAllText(Path.Combine(output,"prototype-result.json"),JsonUtility.ToJson(new PrototypeResult {
                runId=Path.GetFileName(output),sourceSha=JsonUtility.FromJson<StageIdentity>(File.ReadAllText(Path.Combine(output,"identity.json"))).sourceSha,
                dllSha256=Hash(typeof(AH64Plugin).Assembly.Location),status=passed ? "passed" : "failed",complete=prototypeComplete,
                expectedChecks=34,checks=prototypeChecks.ToArray(),limitation="SOLO native skill/stock pipeline with scripted aim/hold, initial momentum, skill overrides and isolated ICBM inventory. Physical controller, host/remote/observer, targeted damage, Longbow reservation/refund/Lysate cap, floor/ledge and external knockback remain unverified. Five images belong to the preceding comparable baseline cases."
            },true));
            return passed;
        }

        [Serializable] private sealed class PrototypeResult {
            public int schema=1,expectedChecks; public string runId,sourceSha,dllSha256,status,limitation; public bool complete; public Check[] checks;
        }
    }
}
