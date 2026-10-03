using System;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Networking;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

internal static class TerminalLifecycleChecks
{
    private static AH64BombingRunPhase Outcome(NetworkConnection.Packet packet)=>(AH64BombingRunPhase)(byte)packet.Values[2];
    private static void LoseContext(string reason)
    { if(reason=="stage") SceneManager.Handle++; else RoR2.Run.instance=new RoR2.Run(); }

    private static void RetainedHost(string reason)
    {
        Checks.Reset(); var body=Checks.Owner(); var state=new BombingRun { characterBody=body,isAuthority=true };
        CompletionChecks.Bind(body,state); state.OnEnter();
        LoseContext(reason); Time.fixedTime=.02f; state.FixedUpdate();
        Checks.Check(state.outer.exited,"Host "+reason+" cancellation exits retained Weapon2");
        state.OnExit(); Time.fixedTime=10; state.FixedUpdate();
        Checks.Check(ProjectileManager.instance.Fired.Count==1,"Host "+reason+" loss prevents later drops");
        Console.WriteLine("PASS retained host body/state cancellation: "+reason);
    }

    private static void RetainedRemote(string reason)
    {
        var pair=CompletionChecks.Begin(100); LoseContext(reason); CompletionChecks.ServerTick(pair,100.02f);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"Remote "+reason+" loss sends terminal message");
        var packet=pair.ServerConnection.Packets[0];
        Checks.Check(Outcome(packet)==AH64BombingRunPhase.Cancelled,"Remote "+reason+" cancellation is explicit");
        CompletionChecks.Deliver(pair,packet); CompletionChecks.ClientTick(pair,.04f);
        Checks.Check(pair.Client.outer.exited,"Remote "+reason+" cancellation exits retained Weapon2");
        pair.Client.OnExit(); CompletionChecks.ServerTick(pair,101.9f);
        Checks.Check(ProjectileManager.instance.Fired.Count==1,"Cancellation pending native exit cannot launch later drops");
        pair.Server.OnExit(); int queued=pair.ServerConnection.Packets.Count;
        CompletionChecks.ServerTick(pair,103);
        Checks.Check(pair.ServerConnection.Packets.Count==queued,"Native server exit stops terminal retries");
        NetworkServer.active=false;
        var next=new BombingRun { characterBody=pair.ClientBody,isAuthority=true };
        CompletionChecks.Bind(pair.ClientBody,next); next.OnEnter(); pair.Client=next;
        CompletionChecks.Deliver(pair,packet); CompletionChecks.ClientTick(pair,4);
        Checks.Check(!next.outer.exited,"Stale cancellation cannot terminate a later cast");
        Console.WriteLine("PASS retained remote body/state cancellation and stale rejection: "+reason);
    }

    internal static CompletionChecks.Pair NewAuthority(CompletionChecks.Pair old)
    {
        var next=new CompletionChecks.Pair
        {
            Server=old.Server,ServerBody=old.ServerBody,ClientBody=Checks.Owner(),
            Connection=new NetworkClient(),ServerConnection=new NetworkConnection()
        };
        NetworkClient.allClients.Add(next.Connection); NetworkManagerSystem.StartClient(next.Connection);
        next.ClientBody.authority=false;
        next.Client=new BombingRun { characterBody=next.ClientBody,isAuthority=false };
        CompletionChecks.Bind(next.ClientBody,next.Client);
        var snapshot=new NetworkWriter(); old.Server.OnSerialize(snapshot);
        NetworkServer.active=false; next.Client.OnDeserialize(new NetworkReader(snapshot.Values.ToArray())); next.Client.OnEnter();
        old.ClientBody.authority=false; old.Client.isAuthority=false;
        next.ClientBody.authority=true; next.Client.isAuthority=true;
        next.ServerBody.GetComponent<NetworkIdentity>().clientAuthorityOwner=next.ServerConnection;
        return next;
    }

    private static void TransferAfterQueued(bool toHost,bool cancellation)
    {
        var old=CompletionChecks.Begin(100);
        if(cancellation) LoseContext("stage");
        float terminalTime=cancellation?100.02f:101.501f;
        CompletionChecks.ServerTick(old,terminalTime);
        var packet=old.ServerConnection.Packets[0];
        int expectedCount=cancellation?1:6;
        if(toHost)
        {
            old.ClientBody.authority=false; old.Client.isAuthority=false;
            old.ServerBody.authority=true; old.Server.isAuthority=true;
            old.ServerBody.GetComponent<NetworkIdentity>().clientAuthorityOwner=null;
            CompletionChecks.Deliver(old,packet);
            CompletionChecks.ServerTick(old,terminalTime+.02f);
            Checks.Check(old.Server.outer.exited,"Transfer to host recovers queued terminal outcome");
            old.Server.OnExit(); CompletionChecks.ServerTick(old,terminalTime+1);
        }
        else
        {
            var next=NewAuthority(old);
            ClientScene.Objects[1]=old.ClientBody.gameObject;
            CompletionChecks.Deliver(old,packet); CompletionChecks.ClientTick(old,2);
            Checks.Check(!old.Client.outer.exited,"Old recipient rejects after authority transfer");
            CompletionChecks.ServerTick(next,terminalTime+.02f);
            Checks.Check(next.ServerConnection.Packets.Count==1,"Authority transfer receives terminal outcome");
            var forwarded=next.ServerConnection.Packets[0];
            Checks.Check(Outcome(forwarded)==(cancellation?AH64BombingRunPhase.Cancelled:AH64BombingRunPhase.Succeeded),
                "Authority transfer preserves original terminal outcome");
            ClientScene.Objects[1]=next.ClientBody.gameObject;
            CompletionChecks.Deliver(next,forwarded); CompletionChecks.ClientTick(next,2);
            Checks.Check(next.Client.outer.exited,"New authority eventually exits retained Weapon2");
            next.Client.OnExit(); NetworkServer.active=true; next.Server.OnExit();
            NetworkServer.active=false;
            var later=new BombingRun { characterBody=next.ClientBody,isAuthority=true };
            CompletionChecks.Bind(next.ClientBody,later); later.OnEnter(); next.Client=later;
            CompletionChecks.Deliver(next,packet); CompletionChecks.Deliver(next,forwarded); CompletionChecks.ClientTick(next,5);
            Checks.Check(!later.outer.exited,"Pre-transfer terminal messages cannot affect next cast");
        }
        Checks.Check(ProjectileManager.instance.Fired.Count==expectedCount,"Authority transfer never changes consumed payload count");
        Console.WriteLine("PASS queued terminal transfer: host="+toHost+", cancellation="+cancellation);
    }

    private static void ReturningAuthority()
    {
        var pair=CompletionChecks.Begin(100); CompletionChecks.ServerTick(pair,101.501f);
        var original=pair.ServerConnection.Packets[0];
        // Authority moves away and back between server delivery polls; old delivery is rejected in between.
        pair.ClientBody.authority=false; pair.Client.isAuthority=false;
        CompletionChecks.Deliver(pair,original);
        pair.ClientBody.authority=true; pair.Client.isAuthority=true;
        CompletionChecks.ServerTick(pair,101.55f);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"Retry is rate bounded for unchanged recipient");
        CompletionChecks.ServerTick(pair,101.62f);
        Checks.Check(pair.ServerConnection.Packets.Count==2,"Terminal retries recover rejected delivery to returning authority");
        CompletionChecks.Deliver(pair,pair.ServerConnection.Packets[1]); CompletionChecks.ClientTick(pair,2);
        Checks.Check(pair.Client.outer.exited&&ProjectileManager.instance.Fired.Count==6,"Returning authority exits without duplicate drops");
    }

    private static void FailedQueue()
    {
        var pair=CompletionChecks.Begin(100); pair.ServerConnection.AcceptSends=false;
        CompletionChecks.ServerTick(pair,101.501f); CompletionChecks.ServerTick(pair,101.52f);
        Checks.Check(pair.ServerConnection.Attempts==1&&pair.ServerConnection.Packets.Count==0,"Failed send attempts are also rate bounded");
        pair.ServerConnection.AcceptSends=true; CompletionChecks.ServerTick(pair,101.62f);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"A failed queue cannot suppress later terminal delivery");
        CompletionChecks.Deliver(pair,pair.ServerConnection.Packets[0]); CompletionChecks.ClientTick(pair,2);
        Checks.Check(pair.Client.outer.exited,"Recovered transport releases waiting authority");
    }

    private static void PendingAndMalformed()
    {
        var pair=CompletionChecks.Begin(100); LoseContext("run"); CompletionChecks.ServerTick(pair,100.02f);
        var packet=pair.ServerConnection.Packets[0]; pair.Client.outer.pending=true;
        CompletionChecks.Deliver(pair,packet); CompletionChecks.ClientTick(pair,1);
        Checks.Check(!pair.Client.outer.exited,"Server cancellation preserves pending pain/death transition");
        pair=CompletionChecks.Begin(100); CompletionChecks.ServerTick(pair,101.501f);
        var valid=pair.ServerConnection.Packets[0];
        foreach(byte value in new byte[] { 0,3,255 })
        {
            var invalid=new NetworkConnection.Packet { Id=valid.Id,Values=(object[])valid.Values.Clone() };
            invalid.Values[2]=value; CompletionChecks.Deliver(pair,invalid); CompletionChecks.ClientTick(pair,2);
            Checks.Check(!pair.Client.outer.exited,"Running/exited/unknown wire outcome is not terminal approval");
        }
        CompletionChecks.Deliver(pair,valid); CompletionChecks.ClientTick(pair,2);
        Checks.Check(pair.Client.outer.exited,"Valid success remains acceptable after malformed packets");
    }

    internal static void Run()
    {
        RetainedHost("stage"); RetainedHost("run");
        RetainedRemote("stage"); RetainedRemote("run");
        TransferAfterQueued(false,false); TransferAfterQueued(true,false);
        TransferAfterQueued(false,true); TransferAfterQueued(true,true);
        ReturningAuthority(); FailedQueue(); PendingAndMalformed();
    }
}
