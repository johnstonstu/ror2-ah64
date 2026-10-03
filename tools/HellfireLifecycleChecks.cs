using System;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Networking;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

internal static partial class HellfireChecks
{
    private static void LifecycleAndNetwork()
    {
        NetworkServer.active=true;
        AH64HellfireNetwork.Init();NetworkManagerSystem.StartServer();NetworkManagerSystem.StartServer();
        var remote=Body(false,2);
        var missile=Missile(remote);
        var owner=remote.GetComponent<AH64HellfireOwner>();
        uint token=owner.Policy.ActiveToken;
        Lifecycle.Call(missile,"Start");
        Check(remote.GetComponent<NetworkIdentity>().clientAuthorityOwner.sent.Count==1,"one token acknowledgement");
        Time.fixedTime=0;
        Physics.Queries=0;
        Wire(remote,token,1,true,Vector3.zero,Vector3.forward,new NetworkConnection());
        Check(!owner.Policy.CanGuide(token,0)&&Physics.Queries==0,"foreign sender rejected before physics");
        Wire(remote,token,1,true,Vector3.zero,Vector3.forward);
        Check(owner.Policy.CanGuide(token,0),"owner authority accepts designation");
        Wire(remote,token,2,true,Vector3.zero,Vector3.forward);
        Check(Physics.Queries==1,"bounded expensive server raycasts");
        Time.fixedTime=0.11f;
        Wire(remote,token,2,true,Vector3.zero,new Vector3(float.PositiveInfinity,0,0));
        Check(!owner.Policy.CanGuide(token,Time.fixedTime),"infinite wire direction coasts");
        Time.fixedTime=0.22f;
        Wire(remote,token,3,true,Vector3.zero,Vector3.forward*100);
        Check(!owner.Policy.CanGuide(token,Time.fixedTime),"wire magnitude checked before Ray normalization");
        Time.fixedTime=0.33f;Wire(remote,token,4,true,Vector3.zero,Vector3.forward);
        Time.fixedTime=0.34f;Wire(remote,token,5,false,Vector3.zero,Vector3.zero);
        Check(!owner.Policy.CanGuide(token,Time.fixedTime),"network release bypasses rate limit");
        Time.fixedTime=0.5f;Wire(remote,token,6,true,Vector3.zero,Vector3.forward);
        Check(!owner.Policy.CanGuide(token,Time.fixedTime),"late held packet cannot undo release");
        var second=Missile(remote);uint secondToken=owner.Policy.ActiveToken;
        Check(secondToken>token,"new lead replaces active token");
        Missile(remote,1);Missile(remote,2);
        Check(owner.Policy.ActiveToken==secondToken,"fan extras never register tokens");
        Lifecycle.Call(missile,"OnDisable");
        Check(owner.Policy.ActiveToken==secondToken,"older projectile death preserves latest");
        Lifecycle.Call(second,"OnDisable");
        Check(owner.Policy.ActiveToken==0,"latest projectile death cleanup");
        var third=Missile(remote);uint thirdToken=owner.Policy.ActiveToken;
        remote.healthComponent.alive=false;Lifecycle.Call(owner,"FixedUpdate");
        Check(owner.Policy.ActiveToken==0,"owner death clears guidance");
        Wire(remote,thirdToken,1,true,Vector3.zero,Vector3.forward);
        Check(owner.Policy.ActiveToken==0,"dead owner packet cannot reacquire");
        remote.healthComponent.alive=true;Missile(remote);
        remote.GetComponent<EntityStateMachine>().state.priority=EntityStates.InterruptPriority.Pain;
        Lifecycle.Call(owner,"FixedUpdate");Check(owner.Policy.ActiveToken==0,"server stun cleanup");
        remote.GetComponent<EntityStateMachine>().state.priority=EntityStates.InterruptPriority.Any;
        Missile(remote);SceneManager.Change();Check(owner.Policy.ActiveToken==0,"stage change cleanup");
        Missile(remote);Lifecycle.Call(owner,"OnDisable");Check(owner.Policy.ActiveToken==0,"owner disable cleanup");

        var local=Body(true,3);var localOwner=AH64HellfireOwner.GetOrAdd(local.gameObject);
        localOwner.BeginLaunch();var localMissile=Missile(local);
        local.inputBank.skill4.down=false;Lifecycle.Call(localOwner,"FixedUpdate");
        local.inputBank.skill4.down=true;Lifecycle.Call(localMissile,"Start");
        Check(!localOwner.Policy.CanGuide(localOwner.Policy.ActiveToken,Time.fixedTime),"release before ack remains terminal after re-press");
        localOwner.BeginLaunch();var newest=Missile(local);Lifecycle.Call(newest,"Start");
        uint newestToken=localOwner.Policy.ActiveToken;
        Check(localOwner.Policy.CanGuide(newestToken,Time.fixedTime),"new launch while held guides");
        Lifecycle.Call(localOwner,"FixedUpdate");Check(localOwner.Policy.CanGuide(newestToken,Time.fixedTime),"normal launch recovery does not block guidance");
        foreach(var maneuver in new EntityStates.EntityState[] {new ServoDash(),new SmokeBackflip()})
        {
            local.GetComponent<EntityStateMachine>().state=maneuver;
            Time.fixedTime+=0.11f;
            Lifecycle.Call(localOwner,"FixedUpdate");
            Check(localOwner.Policy.CanGuide(newestToken,Time.fixedTime),"maneuver re-entry protection preserves held guidance: "+maneuver.GetType().Name);
            localOwner.BeginLaunch();var maneuverMissile=Missile(local);Lifecycle.Call(maneuverMissile,"Start");
            newestToken=localOwner.Policy.ActiveToken;
            Check(localOwner.ServerRequest!=0&&localOwner.Policy.CanGuide(newestToken,Time.fixedTime),"launch during maneuver retains begin/ack pairing: "+maneuver.GetType().Name);
            newest=maneuverMissile;
        }
        local.GetComponent<EntityStateMachine>().state=new EntityStates.EntityState {priority=EntityStates.InterruptPriority.Pain};
        Lifecycle.Call(localOwner,"FixedUpdate");Check(localOwner.Policy.ActiveToken==0,"genuine pain still clears local guidance after maneuver");
        local.GetComponent<EntityStateMachine>().state=new EntityStates.EntityState();
        Lifecycle.Call(newest,"OnDisable");Check(localOwner.Policy.ActiveToken==0,"local latest expiry cleanup");

        localOwner.BeginLaunch();var rapidOld=Missile(local);uint oldToken=localOwner.Policy.ActiveToken;
        uint oldRequest=localOwner.ServerRequest;
        localOwner.BeginLaunch();var rapidNew=Missile(local);uint newToken=localOwner.Policy.ActiveToken;
        uint newRequest=localOwner.ServerRequest;
        localOwner.Acknowledge(oldToken,oldRequest,false);
        localOwner.Acknowledge(oldToken,oldRequest,true);
        localOwner.Acknowledge(newToken,newRequest,false);
        Check(localOwner.Policy.CanGuide(newToken,Time.fixedTime),"older ack/death cannot cancel newest extra-stock press");
        localOwner.Acknowledge(oldToken,oldRequest,false);
        Check(localOwner.Policy.CanGuide(newToken,Time.fixedTime),"reordered ack cannot redirect newest");
        localOwner.StopWatching();
        localOwner.Acknowledge(newToken,newRequest,false);
        Check(!localOwner.Policy.CanGuide(newToken,Time.fixedTime),"duplicate ack cannot reopen released guidance");

        localOwner.BeginLaunch();var steering=Missile(local);Lifecycle.Call(steering,"Start");
        uint steeringToken=localOwner.Policy.ActiveToken;
        Time.fixedTime+=0.11f;
        localOwner.Accept(steeringToken,2,true,new Ray(Vector3.zero,new Vector3(1,0,1)));
        Lifecycle.Call(steering,"FixedUpdate");
        Check(Angle(Vector3.forward,steering.transform.forward)>0,"server actually turns projectile");
        Near(steering.GetComponent<Rigidbody>().velocity.magnitude,140,0.001f,"guidance preserves baseline speed");
        var coast=steering.transform.forward;
        Time.fixedTime+=0.36f;Lifecycle.Call(steering,"FixedUpdate");
        Point(steering.transform.forward,coast,"stale designation coasts last heading");
        localOwner.StopWatching();

        NetworkServer.active=false;
        var client=NetworkManager.singleton.client;NetworkClient.allClients.Add(client);
        NetworkManagerSystem.StartClient(client);NetworkManagerSystem.StartClient(client);
        var wireBody=Body(true,900);var wireOwner=AH64HellfireOwner.GetOrAdd(wireBody.gameObject);
        var wireIdentity=wireBody.GetComponent<NetworkIdentity>();client.connection=wireIdentity.clientAuthorityOwner;
        wireOwner.BeginLaunch();
        var beginPacket=client.connection.sent[client.connection.sent.Count-1].message;
        NetworkServer.active=true;wireBody.gameObject.authority=false;
        NetworkServer.handlers[28064](Packet(beginPacket,wireIdentity.clientAuthorityOwner));
        var wireMissile=Missile(wireBody);Lifecycle.Call(wireMissile,"Start");
        Check(wireOwner.ServerRequest==1,"begin intent serialized and paired with lead");
        var ackPacket=wireIdentity.clientAuthorityOwner.sent[wireIdentity.clientAuthorityOwner.sent.Count-1].message;
        NetworkServer.active=false;wireBody.gameObject.authority=true;
        client.handlers[28065](Packet(ackPacket,wireIdentity.clientAuthorityOwner));
        var aimPacket=client.connection.sent[client.connection.sent.Count-1].message;
        NetworkServer.active=true;wireBody.gameObject.authority=false;
        NetworkServer.handlers[28064](Packet(aimPacket,wireIdentity.clientAuthorityOwner));
        Check(wireOwner.Policy.CanGuide(wireOwner.Policy.ActiveToken,Time.fixedTime),"complete begin/ack/aim serialization roundtrip");
        NetworkServer.active=false;
        var observed=Missile(Body(false,4));Lifecycle.Call(observed,"Start");
        Check(!observed.GetComponent<ProjectileSimple>().enabled,"client straight velocity driver disabled");
        Check(observed.GetComponent<AH64HellfireGuidance>()!=null,"observer guidance component present");
        AH64HellfireNetwork.Shutdown();Check(!NetworkServer.handlers.ContainsKey(28064)&&!client.handlers.ContainsKey(28065),"network listeners removed");
        NetworkServer.handlers[28064]=m=>{};AH64HellfireNetwork.Init();
        bool collision=false;try { NetworkManagerSystem.StartServer(); } catch(InvalidOperationException) { collision=true; }
        Check(collision,"network id collision fails visibly");AH64HellfireNetwork.Shutdown();NetworkServer.handlers.Clear();
    }

    private sealed class Warhead : Component,IProjectileImpactBehavior
    {
        public int impacts;
        public Collider hit;
        public void OnProjectileImpact(ProjectileImpactInfo impact)
        { if(impacts==0){impacts++;hit=impact.collider;} }
    }
    private sealed class Reject : Component,IProjectileImpactFilter
    { public bool PassesFilters(ProjectileImpactInfo impact)=>false; }
    private static void Collision()
    {
        NetworkServer.active=true;
        var ownerBody=Body(true,300);
        var self=ownerBody.gameObject.AddComponent<Collider>();
        var ceiling=new GameObject().AddComponent<Collider>();
        var distant=new GameObject().AddComponent<Collider>();
        var missile=Missile(ownerBody);var go=missile.gameObject;
        var warhead=go.AddComponent<Warhead>();
        var guard=go.AddComponent<AH64HellfireCollision>();
        go.GetComponent<Rigidbody>().velocity=Vector3.forward*140;
        Physics.Overlaps=new[] {self,ceiling};Physics.Sweeps=Array.Empty<RaycastHit>();
        Lifecycle.Call(guard,"FixedUpdate");
        Check(warhead.impacts==1&&warhead.hit==ceiling,"rail terrain/ceiling overlap hits stock warhead, self skipped");
        Check(Physics.Ignored>0,"physical owner collision ignored after collider activation");
        Check(ownerBody.GetComponent<AH64HellfireOwner>().Policy.ActiveToken==0,"impact clears designation immediately");
        Lifecycle.Call(guard,"FixedUpdate");
        warhead.OnProjectileImpact(new ProjectileImpactInfo { collider=ceiling });
        Check(warhead.impacts==1,"stock impact path remains single payload after repeat callback");
        Near(go.GetComponent<ProjectileSimple>().desiredForwardSpeed,0,0,"stop velocity writer after contact");

        missile=Missile(ownerBody);go=missile.gameObject;warhead=go.AddComponent<Warhead>();
        guard=go.AddComponent<AH64HellfireCollision>();go.GetComponent<Rigidbody>().velocity=Vector3.forward*140;
        Physics.Overlaps=Array.Empty<Collider>();
        Physics.Sweeps=new[] { new RaycastHit { collider=distant,distance=2.5f,point=new Vector3(0,0,2.5f) },
            new RaycastHit { collider=ceiling,distance=1.5f,point=new Vector3(0,0,1.5f) } };
        Lifecycle.Call(guard,"FixedUpdate");
        Check(warhead.hit==ceiling,"sweep earliest obstruction wins for fast flight");
        Point(go.transform.position,new Vector3(0,0,1.5f),"stock blast at swept contact, no passage through obstruction");

        missile=Missile(ownerBody);go=missile.gameObject;warhead=go.AddComponent<Warhead>();
        guard=go.AddComponent<AH64HellfireCollision>();go.AddComponent<Reject>();
        go.GetComponent<Rigidbody>().velocity=Vector3.forward*140;
        Lifecycle.Call(guard,"FixedUpdate");
        Check(warhead.impacts==0,"stock impact filters honored");
        Near(go.GetComponent<ProjectileSimple>().desiredForwardSpeed,140,0,"filtered contact does not stop missile");

        NetworkServer.active=false;
        go=new GameObject();go.AddComponent<ProjectileController>();go.AddComponent<Collider>();
        go.AddComponent<ProjectileSimple>();go.AddComponent<Rigidbody>();warhead=go.AddComponent<Warhead>();
        guard=go.AddComponent<AH64HellfireCollision>();Physics.Overlaps=new[] {ceiling};
        Lifecycle.Call(guard,"FixedUpdate");Check(warhead.impacts==0,"observer never dispatches damage impacts");
        Physics.Overlaps=Array.Empty<Collider>();Physics.Sweeps=Array.Empty<RaycastHit>();
    }

}
