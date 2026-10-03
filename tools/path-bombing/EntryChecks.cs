using System;
using System.Reflection;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

internal static class EntryChecks
{
    private sealed class Failure
    {
        internal Exception Expected;
        internal Action Repair;
    }

    private static Failure Arrange(string mode,CharacterBody body,BombingRun state)
    {
        var result=new Failure(); var health=body.healthComponent;
        if(mode=="dead") body.healthComponent.alive=false;
        if(mode=="missing-health") body.healthComponent=null;
        if(mode=="inactive") body.gameObject.activeInHierarchy=false;
        if(mode=="disabled-owner") body.gameObject.AddComponent<AH64BombingRunOwner>().enabled=false;
        if(mode=="null-owner") GameObject.RejectComponent=typeof(AH64BombingRunOwner);
        if(mode=="missing-body")
        {
            if(!body.GetComponent<NetworkIdentity>())
                body.gameObject.AddComponent<NetworkIdentity>().netId=body.netId;
            state.characterBody=null; body.gameObject.RemoveComponent<CharacterBody>();
        }
        if(mode.EndsWith("throws"))
        {
            result.Expected=new InvalidOperationException("injected "+mode);
            Action fail=()=>throw result.Expected;
            if(mode=="constructor-throws") TeamComponent.BeforeLookup=fail;
            if(mode=="base-throws") EntityStates.BaseSkillState.EntryCallback=fail;
            if(mode=="first-release-throws") ProjectileManager.BeforeSpawn=fail;
        }
        result.Repair=()=>
        {
            health.alive=true; body.healthComponent=health; body.gameObject.activeInHierarchy=true;
            if(mode=="missing-body") { body.gameObject.RestoreComponent(body); state.characterBody=body; }
            var owner=body.GetComponent<AH64BombingRunOwner>(); if(owner) owner.enabled=true;
            ClearHooks();
        };
        return result;
    }

    private static void ClearHooks()
    {
        GameObject.RejectComponent=null; TeamComponent.BeforeLookup=null;
        EntityStates.BaseSkillState.EntryCallback=null; ProjectileManager.BeforeSpawn=null;
    }

    private static void Host(string mode)
    {
        Checks.Reset(); var body=Checks.Owner(); var state=new BombingRun { characterBody=body,isAuthority=true };
        CompletionChecks.Bind(body,state); var failure=Arrange(mode,body,state); Exception observed=null;
        try { state.OnEnter(); } catch(Exception error) { observed=error; }
        Checks.Check(ReferenceEquals(observed,failure.Expected),"Host "+mode+" preserves entry exception identity");
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Host "+mode+" entry releases no initial payload");
        ClearHooks();
        // Exercise missing CharacterBody routing before repairing it, and reactivation for other failures.
        if(mode!="missing-body") failure.Repair();
        Time.fixedTime=2; state.FixedUpdate();
        Checks.Check(state.outer.exited,"Host "+mode+" entry exits after cancellation");
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Host "+mode+" entry releases no later drops");
        failure.Repair(); state.OnExit(); Time.fixedTime=10; state.FixedUpdate();
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Host entry cannot restart after owner recovery");
    }

    private static void Remote(string mode)
    {
        Failure failure=null; Exception observed=null;
        var pair=CompletionChecks.Begin(100,p=>failure=Arrange(mode,p.ServerBody,p.Server),error=>observed=error);
        Checks.Check(ReferenceEquals(observed,failure.Expected),"Remote "+mode+" preserves entry exception identity");
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Remote "+mode+" entry releases no initial payload");
        ClearHooks(); if(mode!="missing-body") failure.Repair();
        CompletionChecks.ServerTick(pair,102);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"Remote "+mode+" entry delivers cancellation");
        var packet=pair.ServerConnection.Packets[0];
        Checks.Check((byte)packet.Values[2]==(byte)AH64BombingRunPhase.Cancelled,"Failed entry never reports success");
        CompletionChecks.ServerTick(pair,102.02f);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"Failed-entry terminal retry is bounded");
        CompletionChecks.ServerTick(pair,102.12f);
        Checks.Check(pair.ServerConnection.Packets.Count==2,"Failed-entry cancellation retries until state exit");
        failure.Repair();
        // Transfer before delivery, with a new authority retaining the same body/request.
        var next=TerminalLifecycleChecks.NewAuthority(pair);
        ClientScene.Objects[1]=pair.ClientBody.gameObject;
        CompletionChecks.Deliver(pair,packet); CompletionChecks.ClientTick(pair,3);
        Checks.Check(!pair.Client.outer.exited,"Former owner rejects failed-entry cancellation");
        CompletionChecks.ServerTick(next,102.14f);
        Checks.Check(next.ServerConnection.Packets.Count==1,"Failed-entry cancellation reroutes immediately");
        ClientScene.Objects[1]=next.ClientBody.gameObject;
        CompletionChecks.Deliver(next,next.ServerConnection.Packets[0]); CompletionChecks.ClientTick(next,3);
        Checks.Check(next.Client.outer.exited,"Current owner exits after failed-entry cancellation");
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Remote failed entry cannot restart after owner recovery");
        next.Client.OnExit(); NetworkServer.active=true; next.Server.OnExit();
        NetworkServer.active=false;
        var later=new BombingRun { characterBody=next.ClientBody,isAuthority=true };
        CompletionChecks.Bind(next.ClientBody,later); later.OnEnter(); next.Client=later;
        CompletionChecks.Deliver(next,packet); CompletionChecks.ClientTick(next,5);
        Checks.Check(!later.outer.exited,"Failed-entry stale terminal cannot cancel later cast");
    }

    private static void BeginRejection()
    {
        Checks.Reset(); var body=Checks.Owner(); var owner=body.gameObject.AddComponent<AH64BombingRunOwner>();
        var previous=owner.Begin(body); body.healthComponent.alive=false;
        Checks.Check(owner.Begin(body)==null&&previous.Policy.Stopped,"Rejected Begin stops and releases the prior cast");
        body.healthComponent.alive=true; previous.Tick();
        Checks.Check(ProjectileManager.instance.Fired.Count==0,"Rejected prior cast cannot resume on health recovery");
        Checks.Check(owner.Begin(Checks.Owner())==null,"Begin rejects a different body's owner lease");
        Checks.Check(owner.Begin(null)==null,"Begin rejects missing CharacterBody");
        NetworkServer.active=false; Checks.Check(owner.Begin(body)==null,"Begin rejects non-server construction");
    }

    private static void MissingAddressAndPending()
    {
        Checks.Reset(); var state=new BombingRun { isAuthority=true };
        state.OnEnter(); state.FixedUpdate();
        Checks.Check(state.outer.exited,"Missing owner/address still terminates local retained state");
        Checks.Reset(); var body=Checks.Owner(); body.healthComponent.alive=false;
        state=new BombingRun { characterBody=body,isAuthority=true }; CompletionChecks.Bind(body,state);
        state.OnEnter(); state.outer.pending=true; state.FixedUpdate();
        Checks.Check(!state.outer.exited,"Failed entry preserves a stronger pending transition");
        Checks.Reset(); body=Checks.Owner(); var owner=body.gameObject.AddComponent<AH64BombingRunOwner>();
        typeof(AH64BombingRunOwner).GetField("lastRequest",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,uint.MaxValue);
        state=new BombingRun { characterBody=body,isAuthority=true }; CompletionChecks.Bind(body,state);
        bool threw=false; try { state.OnEnter(); } catch(InvalidOperationException) { threw=true; }
        state.FixedUpdate();
        Checks.Check(threw&&state.outer.exited&&ProjectileManager.instance.Fired.Count==0,"Request setup exception cancels without hidden recovery");
    }

    internal static void Run()
    {
        foreach(string mode in new[] { "dead","missing-health","inactive","disabled-owner","missing-body",
            "null-owner","constructor-throws","base-throws","first-release-throws" })
        {
            Host(mode); Remote(mode); Console.WriteLine("PASS failed-entry host/remote lifecycle: "+mode);
        }
        BeginRejection(); MissingAddressAndPending();
    }
}
