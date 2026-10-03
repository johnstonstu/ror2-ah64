using System;
using System.Reflection;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Networking;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

internal static class CompletionChecks
{
    internal sealed class Pair
    {
        internal BombingRun Client,Server;
        internal CharacterBody ClientBody,ServerBody;
        internal NetworkClient Connection;
        internal NetworkConnection ServerConnection;
    }

    internal static Pair Begin(float serverStart, Action<Pair> beforeServerEntry=null, Action<Exception> entryException=null)
    {
        Checks.Reset(); AH64BombingRunNetwork.Init(); AH64BombingRunNetwork.Init();
        var pair=new Pair { Connection=new NetworkClient(),ServerConnection=new NetworkConnection() };
        NetworkClient.allClients.Add(pair.Connection); NetworkManagerSystem.StartClient(pair.Connection);
        pair.ClientBody=Checks.Owner(); pair.ServerBody=Checks.Owner(); pair.ServerBody.authority=false;
        pair.Client=new BombingRun { characterBody=pair.ClientBody,isAuthority=true };
        pair.Server=new BombingRun { characterBody=pair.ServerBody,isAuthority=false };
        Bind(pair.ClientBody,pair.Client); Bind(pair.ServerBody,pair.Server);
        ClientScene.Objects[1]=pair.ClientBody.gameObject;
        var identity=pair.ServerBody.gameObject.AddComponent<NetworkIdentity>();
        identity.netId=pair.ServerBody.netId; identity.clientAuthorityOwner=pair.ServerConnection;
        NetworkServer.active=false; Time.fixedTime=500f; pair.Client.OnEnter();
        var entry=new NetworkWriter(); pair.Client.OnSerialize(entry);
        // Native order: deserialize before OnEnter; server clock is independent of client clock.
        NetworkServer.active=true; Time.fixedTime=serverStart;
        pair.Server.OnDeserialize(new NetworkReader(entry.Values.ToArray()));
        beforeServerEntry?.Invoke(pair);
        try { pair.Server.OnEnter(); }
        catch(Exception error) { if(entryException==null) throw; entryException(error); }
        return pair;
    }

    internal static void Bind(CharacterBody body,BombingRun state)
    {
        var machine=body.gameObject.GetComponent<EntityStateMachine>()??body.gameObject.AddComponent<EntityStateMachine>();
        machine.state=state; machine.exited=false; machine.pending=false; state.outer=machine;
    }

    internal static void ServerTick(Pair pair,float time)
    { NetworkServer.active=true; Time.fixedTime=time; pair.Server.FixedUpdate(); }
    internal static void ClientTick(Pair pair,float age)
    { NetworkServer.active=false; Time.fixedTime=500f+age; pair.Client.fixedAge=age; pair.Client.FixedUpdate(); }

    internal static void Deliver(Pair pair,NetworkConnection.Packet packet,NetworkConnection sender=null)
    {
        NetworkServer.active=false;
        pair.Connection.handlers[packet.Id](new NetworkMessage
        { conn=sender??pair.Connection.connection,reader=new NetworkReader(packet.Values) });
    }

    private static void Jitter(float entryDelay,float oldExitDelay,float completionDelay,float actualExitDelay)
    {
        const float serverClock=100f;
        var pair=Begin(serverClock+entryDelay);
        float earlyExitReceipt=serverClock+1.52f+oldExitDelay;
        // Replay the reviewed failure timing against independent clocks.
        ServerTick(pair,earlyExitReceipt); ClientTick(pair,1.52f);
        Checks.Check(!pair.Client.outer.exited,"Client age cannot end an unacknowledged cast");
        Checks.Check(pair.ServerConnection.Packets.Count==0,"No early completion before server consumes final opportunity");
        Checks.Check(ProjectileManager.instance.Fired.Count<6,"Reviewed early-exit timing precedes final server drop");
        ServerTick(pair,serverClock+entryDelay+1.501f);
        Checks.Check(ProjectileManager.instance.Fired.Count==6,"Jittered uninterrupted server cast consumes six drops");
        Checks.Check(pair.ServerConnection.Packets.Count==1,"One server completion acknowledgement");
        ServerTick(pair,serverClock+entryDelay+1.6f);
        Checks.Check(pair.ServerConnection.Packets.Count==1,"Terminal retry rate avoids per-tick sends");
        ClientTick(pair,entryDelay+1.501f+completionDelay);
        Checks.Check(!pair.Client.outer.exited,"Client waits even when clock far beyond duration");
        Deliver(pair,pair.ServerConnection.Packets[0]); ClientTick(pair,entryDelay+1.502f+completionDelay);
        Checks.Check(pair.Client.outer.exited,"Matching server completion permits natural exit");
        pair.Client.OnExit();
        NetworkServer.active=true; Time.fixedTime=serverClock+entryDelay+1.502f+completionDelay+actualExitDelay;
        pair.Server.OnExit(); pair.Server.FixedUpdate();
        Checks.Check(ProjectileManager.instance.Fired.Count==6,"Delayed normal exit cannot suppress or duplicate payloads");
        Checks.Check(Checks.traces.Exists(x=>x.kind=="end"&&x.reason=="complete"),"Server recognizes completed cast");
        Console.WriteLine("PASS independent clocks/delays: entry="+entryDelay+", oldExit="+oldExitDelay
            +", completion="+completionDelay+", actualExit="+actualExitDelay);
    }

    private static void Interruptions()
    {
        foreach(string reason in new[] { "pain", "death", "disable", "client-disable" })
        {
            var pair=Begin(100.1f); ServerTick(pair,100.42f);
            if(reason=="pain")
            {
                NetworkServer.active=false; pair.Client.OnExit();
                // Explicit interruption arrives independently, before server schedule completion.
                NetworkServer.active=true; Time.fixedTime=100.6f; pair.Server.OnExit();
            }
            if(reason=="death") pair.ServerBody.healthComponent.alive=false;
            if(reason=="disable") typeof(AH64BombingRunOwner)
                .GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(pair.ServerBody.GetComponent<AH64BombingRunOwner>(),null);
            if(reason=="client-disable")
            {
                NetworkServer.active=false;
                typeof(AH64BombingRunOwner).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(pair.ClientBody.GetComponent<AH64BombingRunOwner>(),null);
                ClientTick(pair,.34f); Checks.Check(pair.Client.outer.exited,"Local disable does not wait for server completion");
                pair.Client.OnExit(); NetworkServer.active=true; Time.fixedTime=100.6f; pair.Server.OnExit();
            }
            ServerTick(pair,102f);
            Checks.Check(ProjectileManager.instance.Fired.Count==2,reason+": future drops cancelled promptly");
            Checks.Check(pair.ServerConnection.Packets.TrueForAll(p=>(byte)p.Values[2]==(byte)AH64BombingRunPhase.Cancelled),
                reason+": interruption never reports natural completion");
            if(reason=="death"||reason=="disable")
            {
                Checks.Check(pair.ServerConnection.Packets.Count==1,reason+": server cancellation sends terminal outcome");
                if(reason=="death") pair.ClientBody.healthComponent.alive=false;
                Deliver(pair,pair.ServerConnection.Packets[0]); ClientTick(pair,3f);
                Checks.Check(pair.Client.outer.exited,reason+": cancellation releases retained remote state");
            }
        }
    }

    private static void StaleAndUntrusted()
    {
        var pair=Begin(100.1f); ServerTick(pair,101.601f);
        var packet=pair.ServerConnection.Packets[0];
        Deliver(pair,packet,new NetworkConnection()); ClientTick(pair,4f);
        Checks.Check(!pair.Client.outer.exited,"Completion from unrelated connection rejected");
        NetworkServer.active=false; pair.Client.OnExit();
        var next=new BombingRun { characterBody=pair.ClientBody,isAuthority=true };
        Bind(pair.ClientBody,next); next.OnEnter(); pair.Client=next;
        Deliver(pair,packet); ClientTick(pair,5f);
        Checks.Check(!next.outer.exited,"Old-cast completion cannot finish next cast");

        pair=Begin(100.1f); ServerTick(pair,101.601f); packet=pair.ServerConnection.Packets[0];
        pair.Client.outer.pending=true; Deliver(pair,packet); ClientTick(pair,5f);
        Checks.Check(!pair.Client.outer.exited,"Natural completion cannot overwrite a pending pain/death transition");
        pair=Begin(100.1f); ServerTick(pair,101.601f); packet=pair.ServerConnection.Packets[0];
        pair.ClientBody.authority=false; Deliver(pair,packet); ClientTick(pair,5f);
        Checks.Check(!pair.Client.outer.exited,"Observer cannot acknowledge another owner's completion");
        pair=Begin(100.1f); ServerTick(pair,101.601f); packet=pair.ServerConnection.Packets[0];
        ClientScene.Objects.Remove(1); Deliver(pair,packet); ClientTick(pair,5f);
        Checks.Check(!pair.Client.outer.exited,"Removed body cannot consume stale acknowledgement");
    }

    private static void Registration()
    {
        Checks.Reset(); var client=new NetworkClient(); NetworkClient.allClients.Add(client);
        NetworkMessageDelegate foreign=message=>{}; client.RegisterHandler(28066,foreign);
        bool collision=false; try { AH64BombingRunNetwork.Init(); } catch(InvalidOperationException) { collision=true; }
        Checks.Check(collision&&client.handlers[28066]==foreign,"Transport ID collision fails without replacing handler");
        client.UnregisterHandler(28066); AH64BombingRunNetwork.Init(); AH64BombingRunNetwork.Shutdown();
        Checks.Check(client.handlers.Count==0,"Shutdown removes owned completion handler");
    }

    private static void SuppressedCompletion()
    {
        var pair=Begin(100.1f); Physics.OriginBlocked=true;
        ServerTick(pair,101.601f);
        Checks.Check(ProjectileManager.instance.Fired.Count==1,"Blocked future drops remain consumed, not replaced");
        Checks.Check(pair.ServerConnection.Packets.Count==1,"All six consumed opportunities permit completion despite suppression");
        Deliver(pair,pair.ServerConnection.Packets[0]); ClientTick(pair,3f);
        Checks.Check(pair.Client.outer.exited,"Suppressed opportunities do not strand client state");
    }

    internal static void Run()
    {
        Jitter(.100f,.020f,.200f,.010f);
        Jitter(.450f,.010f,2f,.300f);
        Jitter(.220f,.040f,.020f,1f);
        Interruptions(); StaleAndUntrusted(); SuppressedCompletion(); Registration();
    }
}
