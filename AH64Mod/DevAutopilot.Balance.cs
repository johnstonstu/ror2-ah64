using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AH64.Survivors;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using Path = System.IO.Path;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private readonly List<Check> balanceChecks = new List<Check>();
        private bool balanceStarted, balanceComplete;

        private void BalanceCheck(string id, bool passed, string actual)
        {
            balanceChecks.Add(new Check { id=id, passed=passed, actual=actual, scenario=segment });
            Event("balance-check", id+" "+(passed ? "PASS" : "FAIL")+" "+actual);
        }

        private IEnumerator M230BalanceCase()
        {
            balanceStarted=true;
            yield return ResetCase("m230-balance");
            Event("balance-scope", "SOLO native GenericSkill.ExecuteIfReady requests at fixed cadence with explicit M230 override; no physical hold, target-hit/damage or item-scaling acceptance");
            var slot=pilot.skillLocator.primary;
            var original=slot.skillDef;
            var skill=Variant(slot,typeof(FireChaingun));
            slot.SetSkillOverride(this,skill,GenericSkill.SkillOverridePriority.Contextual);
            try {
                yield return Until(() => slot.stock==slot.maxStock && slot.CanExecute(),10f,"M230 full native drum");
                float attackSpeed=pilot.attackSpeed;
                BalanceCheck("m230.defaults",AH64StaticValues.chaingunMagazineSize==20 && FireChaingun.baseDuration==0.11f && Mathf.Abs(attackSpeed-1f)<0.001f,
                    "nominal interval="+FireChaingun.baseDuration+" attackSpeed="+attackSpeed+" damage="+AH64PlaytestConfig.ChaingunDamage+" HE="+AH64PlaytestConfig.ChaingunSplashDamage+" proc="+AH64StaticValues.chaingunProcCoefficient);
                BalanceCheck("m230.native-magazine",skill.baseMaxStock==20 && slot.maxStock==20 && slot.stock==20,
                    "SkillDef="+skill.baseMaxStock+" native max="+slot.maxStock+" stock="+slot.stock);
                var nativeEntries=new List<float>();
                EntityState last=null;
                Action observe=() => {
                    var state=Machine("Weapon").state;
                    if(!ReferenceEquals(last,state)) {
                        last=state;
                        if(state is FireChaingun) {
                            nativeEntries.Add(Time.fixedTime);
                            Event("m230-native-fire-entry","entry="+nativeEntries.Count+" stock="+slot.stock+" fixedTime="+Time.fixedTime.ToString("R",Invariant));
                        }
                    }
                };
                int accepted=0;bool singleStock=true;float lastRequest=0f;
                float deadline=Time.realtimeSinceStartup+15f;
                while(slot.stock>0 && Time.realtimeSinceStartup<deadline) {
                    observe();int before=slot.stock;
                    if(slot.ExecuteIfReady()) {
                        accepted++;lastRequest=Time.fixedTime;singleStock &= slot.stock==before-1;
                        Event("m230-accepted-request","request="+accepted+" stock="+before+"->"+slot.stock+" fixedTime="+lastRequest.ToString("R",Invariant));
                    }
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForFixedUpdate();observe();
                yield return new WaitForFixedUpdate();observe();
                BalanceCheck("m230.stock-cost",accepted==20 && singleStock && slot.stock==0,"accepted="+accepted+" one-stock-each="+singleStock+" stock="+slot.stock);
                BalanceCheck("m230.native-fire-entries",nativeEntries.Count==20,"actual FireChaingun state entries="+nativeEntries.Count);
                var intervals=nativeEntries.Skip(1).Select((time,index)=>time-nativeEntries[index]).ToArray();
                float nominal=FireChaingun.baseDuration/attackSpeed;
                bool cadence=intervals.Length==23 && intervals.All(dt=>dt>=nominal-0.001f && dt<=nominal+Time.fixedDeltaTime*3f+0.001f);
                BalanceCheck("m230.native-cadence",cadence,"nominal="+nominal+" observed min="+(intervals.Length>0 ? intervals.Min().ToString("R",Invariant) : "none")+" max="+(intervals.Length>0 ? intervals.Max().ToString("R",Invariant) : "none")+" intervals="+intervals.Length+"; fixed-tick request/state scheduling, not nominal RPM measurement");
                bool emptyAccepted=slot.ExecuteIfReady();
                BalanceCheck("m230.empty-blocked",!emptyAccepted && slot.stock==0,"empty ExecuteIfReady="+emptyAccepted+" stock="+slot.stock);
                float reload=skill.GetRechargeInterval(slot);
                yield return Until(()=>slot.stock>0,10f,"M230 native full reload");
                float elapsed=Time.fixedTime-lastRequest;
                BalanceCheck("m230.whole-reload",slot.stock==20 && slot.maxStock==20,"native stock="+slot.stock+" max="+slot.maxStock);
                BalanceCheck("m230.reload-timing",Mathf.Abs(elapsed-reload)<=Time.fixedDeltaTime*3f+0.001f,"native elapsed from last request="+elapsed.ToString("R",Invariant)+" expected="+reload.ToString("R",Invariant));
                yield return Until(()=>!(Machine("Weapon").state is FireChaingun),5f,"M230 firing state exit");
            } finally {slot.UnsetSkillOverride(this,skill,GenericSkill.SkillOverridePriority.Contextual);}
            BalanceCheck("m230.override-cleanup",slot.skillDef==original && !(Machine("Weapon").state is FireChaingun),"original primary restored; no live FireChaingun state");
            balanceComplete=true;
        }

        private bool FinishBalanceEvidence()
        {
            if(Environment.GetEnvironmentVariable("AH64_AUTOPILOT_BALANCE_CHECKS")!="m230-v1") return true;
            bool passed=balanceStarted && balanceComplete && balanceChecks.Count==9 && balanceChecks.All(c=>c.passed);
            File.WriteAllText(Path.Combine(output,"balance-result.json"),JsonUtility.ToJson(new BalanceResult {
                sourceSha=JsonUtility.FromJson<StageIdentity>(File.ReadAllText(Path.Combine(output,"identity.json"))).sourceSha,
                runId=Path.GetFileName(output),dllSha256=Hash(typeof(AH64Plugin).Assembly.Location),status=passed ? "passed" : "failed",complete=balanceComplete,checks=balanceChecks.ToArray()
            },true));
            return passed;
        }

        [Serializable] private sealed class BalanceResult {
            public int schema=1,expectedChecks=9;
            public string sourceSha,runId,dllSha256,status;
            public bool complete;
            public Check[] checks;
            public string limitation="SOLO native request/stock/state/reload pipeline with isolated M230 override, no inventory grants. Cadence is fixed-tick scheduled. Physical controls, real target damage, alternate-primary gameplay and item scaling remain unverified by this delta test.";
        }
    }
}
