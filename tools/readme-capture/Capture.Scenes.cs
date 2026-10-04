using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace ReadmeCapture
{
    public sealed partial class CapturePlugin
    {
        private readonly List<CharacterBody> targets=new List<CharacterBody>();
        private readonly List<CharacterMaster> targetMasters=new List<CharacterMaster>();
        private readonly Dictionary<GenericSkill,SkillDef> overrides=new Dictionary<GenericSkill,SkillDef>();
        private IEnumerator Script()
        {
            yield return Bootstrap(); DisableCombatDirectors();
            pilot.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            RaycastHit floor;
            if(!Physics.Raycast(new Vector3(-112.88f,-129.02f,-374.76f),Vector3.down,out floor,30f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)) throw new InvalidOperationException("Arena terrain missing");
            mark=floor.point+Vector3.up*2f;
            float best=-1;
            for(int i=0;i<24;i++) {
                var d=Quaternion.Euler(0,i*15,0)*Vector3.forward; RaycastHit wall;
                float clear=Physics.Raycast(mark+Vector3.up,d,out wall,70,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)?wall.distance:70;
                if(clear>best) { best=clear; facing=d; }
            }
            if(best<35) throw new InvalidOperationException("Arena sightline shorter than 35m");
            aim=facing; scripting=true;
            Event("arena","controlled golemplains; seed1301; scripted inputs, camera and healthy native targets");
            yield return Hellfire(); yield return Brake(); yield return Bombs(); yield return Loadouts(); yield return M230();
        }
        private IEnumerator Seconds(float seconds) { float end=Time.fixedTime+seconds; while(Time.fixedTime<end) yield return new WaitForFixedUpdate(); }
        private IEnumerator Ready(GenericSkill slot)
        { float end=Time.realtimeSinceStartup+15f; while(!slot.CanExecute()) { if(Time.realtimeSinceStartup>end) throw new TimeoutException("Skill readiness "+slot.skillDef.skillName); yield return null; } }
        private void Choose(GenericSkill slot,string state)
        {
            SkillDef previous;
            if(overrides.TryGetValue(slot,out previous)) slot.UnsetSkillOverride(this,previous,GenericSkill.SkillOverridePriority.Contextual);
            var def=slot.skillFamily.variants.Select(v=>v.skillDef).Single(s=>s.activationState.stateType.Name==state);
            slot.SetSkillOverride(this,def,GenericSkill.SkillOverridePriority.Contextual); overrides[slot]=def;
            Event("loadout",slot.skillName+"="+def.skillName);
        }
        private void Execute(GenericSkill slot)
        { Input(); if(!slot.ExecuteIfReady()) throw new InvalidOperationException("Native skill refused "+slot.skillDef.skillName); Event("activation",slot.skillDef.skillName); }
        private IEnumerator Reset(string scene)
        {
            foreach(var master in targetMasters) if(master) { if(master.GetBody()) NetworkServer.Destroy(master.GetBody().gameObject); NetworkServer.Destroy(master.gameObject); }
            targetMasters.Clear(); targets.Clear();
            segment=scene; move=Vector3.zero; held=primaryHeld=collective=false; aim=facing;
            TeleportHelper.TeleportBody(pilot,mark); pilot.characterMotor.velocity=Vector3.zero; pilot.characterDirection.forward=facing;
            var hover=pilot.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AH64HoverController");
            hover.GetType().GetMethod("ResetAfterTeleport").Invoke(hover,null);
            fixedCamera=false; cameraOffset=-facing*17f+Vector3.up*9f+Vector3.Cross(Vector3.up,facing)*7f;
            yield return Seconds(1.5f);
        }
        private IEnumerator Target(Vector3 point)
        {
            RaycastHit floor;
            if(!Physics.Raycast(point+Vector3.up*15f,Vector3.down,out floor,45f,LayerIndex.world.mask,QueryTriggerInteraction.Ignore)) throw new InvalidOperationException("Target lacks terrain");
            var master=new MasterSummon {masterPrefab=MasterCatalog.FindMasterPrefab("GolemMaster"),position=floor.point+Vector3.up,rotation=Quaternion.LookRotation(-facing),teamIndexOverride=TeamIndex.Monster,ignoreTeamMemberLimit=true,
                preSpawnSetupCallback=m=>{foreach(var ai in m.GetComponents<BaseAI>()) ai.enabled=false;} }.Perform();
            if(!master) throw new InvalidOperationException("Target summon failed"); targetMasters.Add(master);
            float end=Time.realtimeSinceStartup+5;
            while(!master.GetBody()) { if(Time.realtimeSinceStartup>end) throw new TimeoutException("Target body"); yield return null; }
            var body=master.GetBody(); foreach(var ai in master.GetComponents<BaseAI>()) ai.enabled=false;
            // Damage still runs normally and yields hit numbers; large disposable health avoids fixture death.
            body.baseMaxHealth=100000f; body.RecalculateStats(); body.healthComponent.Networkhealth=body.maxHealth;
            targets.Add(body); Event("target","native Golem; AI off; 100000HP; position="+body.corePosition);
            yield return Seconds(0.4f);
        }
        private IEnumerator Hellfire()
        {
            yield return Reset("hellfire"); yield return Target(mark+facing*34f);
            Choose(pilot.skillLocator.special,"FireHellfire"); yield return Ready(pilot.skillLocator.special);
            var target=targets[0]; cameraFocus=(pilot.corePosition+target.corePosition)*0.5f;
            fixedCamera=true; cameraOffset=-facing*28f+Vector3.up*12f+Vector3.Cross(Vector3.up,facing)*10f;
            Clip(true); yield return Seconds(1f);
            aim=(target.corePosition-pilot.inputBank.aimOrigin).normalized; held=true; Execute(pilot.skillLocator.special);
            yield return Seconds(0.22f); Event("phase","release: original missile coasts"); held=false;
            yield return Seconds(0.8f); Event("phase","rehold: guidance reacquisition"); held=true;
            yield return Seconds(3f); held=false; yield return Seconds(1f); Clip(false);
        }
        private IEnumerator Brake()
        {
            yield return Reset("banked-break");
            // Offset the real target so native captured aim creates a visible moving arc.
            yield return Target(mark+Quaternion.AngleAxis(100f,Vector3.up)*facing*22f);
            fixedCamera=true; cameraFocus=(mark+targets[0].corePosition)*0.5f;
            cameraOffset=-facing*26f+Vector3.up*17f-Vector3.Cross(Vector3.up,facing)*15f;
            Choose(pilot.skillLocator.utility,"BrakingTurn"); yield return Ready(pilot.skillLocator.utility);
            Clip(true); move=facing; yield return Seconds(1.3f);
            aim=(targets[0].corePosition-pilot.inputBank.aimOrigin).normalized; move=Vector3.zero; Execute(pilot.skillLocator.utility);
            yield return Seconds(2f); aim=(targets[0].corePosition-pilot.inputBank.aimOrigin).normalized; yield return Seconds(1f); Clip(false);
        }
        private IEnumerator Bombs()
        {
            yield return Reset("path-bombing");
            for(int i=0;i<6;i++) yield return Target(mark+facing*(3f+i*4f));
            Choose(pilot.skillLocator.special,"BombingRun"); yield return Ready(pilot.skillLocator.special);
            cameraFocus=mark+facing*12f; fixedCamera=true; cameraOffset=-facing*17f+Vector3.up*24f+Vector3.Cross(Vector3.up,facing)*23f;
            Clip(true); collective=true; yield return Seconds(0.8f); collective=false;
            move=facing; yield return Seconds(0.4f); Execute(pilot.skillLocator.special);
            yield return Seconds(3f); move=Vector3.zero; yield return Seconds(4f); Clip(false);
        }
        private IEnumerator Loadouts()
        {
            yield return Reset("loadouts"); fixedCamera=true; cameraFocus=pilot.corePosition; cameraOffset=facing*7f+Vector3.up*3f+Vector3.Cross(Vector3.up,facing)*9f;
            Clip(true);
            string[] specials={"PaintLongbow","FireHellfire","BombingRun"}; string[] utilities={"ServoDash","SmokeBackflip","BrakingTurn"};
            for(int i=0;i<3;i++) { Choose(pilot.skillLocator.special,specials[i]); Choose(pilot.skillLocator.utility,utilities[i]); Event("phase","attachment pair "+specials[i]+" + "+utilities[i]); yield return Seconds(3f); }
            Clip(false);
        }
        private IEnumerator M230()
        {
            yield return Reset("m230"); yield return Target(mark+facing*18f); Choose(pilot.skillLocator.primary,"FireChaingun"); yield return Ready(pilot.skillLocator.primary);
            aim=(targets[0].corePosition-pilot.inputBank.aimOrigin).normalized; Clip(true); yield return Seconds(0.8f);
            primaryHeld=true; Execute(pilot.skillLocator.primary); yield return Seconds(4f); primaryHeld=false; yield return Seconds(1f); Clip(false);
        }
    }
}
