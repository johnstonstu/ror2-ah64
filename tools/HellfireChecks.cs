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
    private static int assertions;
    private static void Check(bool condition,string id)
    { assertions++;if(!condition)throw new Exception(id); }
    private static void Near(float actual,float expected,float tolerance,string id)
        =>Check(MathF.Abs(actual-expected)<=tolerance,$"{id}: {actual}, expected {expected}");
    private static void Point(Vector3 actual,Vector3 expected,string id)
        =>Near((actual-expected).magnitude,0,0.001f,id);
    private static float Angle(Vector3 a,Vector3 b)
    {
        double cross=Vector3.Cross(a,b).magnitude;
        return (float)(Math.Atan2(cross,Vector3.Dot(a,b))*180/Math.PI);
    }
    private static CharacterBody Body(bool authority=true,uint id=1)
    {
        var go=new GameObject { authority=authority };
        var body=go.AddComponent<CharacterBody>();
        body.inputBank=go.AddComponent<InputBankTest>();
        body.healthComponent=go.AddComponent<HealthComponent>();
        go.AddComponent<EntityStateMachine>();
        var identity=go.AddComponent<NetworkIdentity>();
        identity.netId=new NetworkInstanceId(id);
        identity.clientAuthorityOwner=new NetworkConnection();
        NetworkServer.objects[identity.netId]=go;
        return body;
    }
    private static AH64HellfireGuidance Missile(CharacterBody body,byte combo=0)
    {
        var go=new GameObject();
        var controller=go.AddComponent<ProjectileController>();
        go.AddComponent<Rigidbody>();go.AddComponent<Collider>();go.AddComponent<ProjectileSimple>();
        var guidance=go.AddComponent<AH64HellfireGuidance>();
        controller.owner=body.gameObject;controller.combo=combo;controller.Initialize();
        return guidance;
    }
    private static void Wire(CharacterBody body,uint token,uint sequence,bool held,Vector3 origin,Vector3 direction,NetworkConnection sender=null)
    {
        var writer=new NetworkWriter();
        var identity=body.GetComponent<NetworkIdentity>();
        writer.Write(identity.netId);writer.Write(token);writer.Write(sequence);writer.Write(held);writer.Write(origin);writer.Write(direction);
        writer.Write(false);writer.Write((uint)0);
        NetworkServer.handlers[28064](new NetworkMessage
            { conn=sender??identity.clientAuthorityOwner,reader=new NetworkReader(writer.values) });
    }

    private static NetworkMessage Packet(MessageBase packet,NetworkConnection sender)
    {
        var writer=new NetworkWriter();packet.Serialize(writer);
        return new NetworkMessage { conn=sender,reader=new NetworkReader(writer.values) };
    }

    private static void Policy()
    {
        var p=new AH64HellfireGuidancePolicy();
        uint first=p.Launch();
        Check(!p.CanGuide(first,0),"no designation before first update");
        Check(!p.Update(0,1,0,true,true),"zero token rejected");
        Check(!p.Update(first+1,1,0,true,true),"wrong token rejected");
        Check(p.Update(first,1,0,true,true),"first designation");
        Check(p.CanGuide(first,0.1f),"fresh designation");
        Check(!p.Update(first,1,0.2f,true,true),"duplicate sequence rejected");
        Check(!p.Update(first,0,0.2f,true,true),"reordered sequence rejected");
        Check(!p.Update(first,2,0.01f,true,true),"rate limit");
        Check(!p.CanGuide(first,0.36f),"stale coasts");
        Check(p.Update(first,2,0.4f,true,false),"invalid designation consumes sequence");
        Check(!p.CanGuide(first,0.4f),"invalid coasts immediately");
        Check(p.Update(first,3,0.6f,true,true),"fresh valid ray resumes while held");
        Check(p.Update(first,4,0.61f,false,false),"release bypasses rate limit");
        Check(p.ActiveToken==0,"release clears active ownership token");
        Check(!p.CanGuide(first,0.61f),"release coasts immediately");
        Check(!p.Update(first,5,1,true,true),"re-press cannot reacquire released token");
        uint next=p.Launch();
        Check(next>first,"monotonic token");
        Check(!p.Update(first,100,2,true,true),"older missile never reacquires");
        p.Clear(first);Check(p.ActiveToken==next,"older destruction cannot clear latest");
        p.Clear(next);Check(p.ActiveToken==0,"latest destruction clears token");
        Check(p.Launch()>next,"cleanup preserves monotonic issuance");
    }

    private static void Geometry()
    {
        var body=Body();
        var self=body.gameObject.AddComponent<Collider>();
        var wall=new GameObject().AddComponent<Collider>();
        var target=new GameObject().AddComponent<Collider>();
        var ray=body.inputBank.GetAimRay();
        Physics.Hits=new[] { new RaycastHit { collider=target,distance=40,point=new Vector3(0,0,40) },
            new RaycastHit { collider=self,distance=1,point=new Vector3(0,0,1) },
            new RaycastHit { collider=wall,distance=8,point=new Vector3(0,0,8) } };
        Point(AH64HellfireAim.Resolve(body,ray),new Vector3(0,0,8),"first reachable hit wins, self ignored");
        Physics.Hits=Array.Empty<RaycastHit>();
        Point(AH64HellfireAim.Resolve(body,ray),new Vector3(0,0,500),"finite miss fallback");
        Vector3 rail=new Vector3(0.78f,0,0),point=new Vector3(0,0,5);
        Vector3 heading=AH64HellfireAim.Converge(rail,point,Vector3.forward);
        Point(rail+heading*(point-rail).magnitude,point,"close rail convergence");
        Point(AH64HellfireAim.Converge(point,point,Vector3.forward),Vector3.forward,"coincident rail fallback");
        Check(!AH64HellfireAim.ValidRay(body,new Ray(new Vector3(float.NaN,0,0),Vector3.forward)),"NaN origin");
        Check(!AH64HellfireAim.ValidRay(body,new Ray(new Vector3(9,0,0),Vector3.forward)),"out-of-range aim origin");
        Check(!AH64HellfireAim.ValidRay(body,new Ray(Vector3.zero,Vector3.zero)),"zero direction");
        Check(!AH64HellfireAim.Finite(float.PositiveInfinity),"infinite component");
        float budget=240,total=0;
        Vector3 rate=Vector3.zero;
        heading=Vector3.forward;
        for(int i=0;i<400;i++)
        {
            Vector3 desired=Quaternion.AngleAxis(60+i*3,Vector3.up)*Vector3.forward;
            Vector3 next=AH64HellfireAim.Turn(heading,desired,0.02f,ref rate,ref budget);
            float turned=Angle(heading,next);total+=turned;
            Check(turned<=2.401f,"per-tick angular cap");
            Near(next.magnitude,1,0.0001f,"normalized flight direction");
            Check(rate.magnitude<=120.001f && budget>=0,"bounded rate and budget");
            if(i==0)Check(turned<=0.289f,"angular acceleration starts bounded");
            heading=next;
        }
        Check(total<=240.5f,"whole-life turn budget");
        float stoppedBudget=240;
        Vector3 stoppedRate=Vector3.zero;
        Point(AH64HellfireAim.Turn(heading,Vector3.zero,0.02f,ref stoppedRate,ref stoppedBudget),heading,"point reached coasts");
    }

    private static void LaunchAndPayload()
    {
        foreach(int stacks in new[] {0,1,5,100})
        {
            var body=Body(true,(uint)(10+stacks));body.inventory.count=stacks;
            var fired=ProjectileManager.instance.Fired;fired.Clear();
            var state=new FireHellfire { gameObject=body.gameObject,characterBody=body,isAuthority=true };
            state.OnEnter();
            Check(state.firedProjectile,"launch marked fired on entry");
            Check(fired.Count==(stacks==0?1:3),"exactly two extras for any positive stack");
            var lead=fired[0];Check(lead.comboNumber==0,"lead marker");
            Near(lead.damage,135,0.001f,"unchanged 1350% payload");
            Check(lead.crit&&lead.damageTypeOverride.Value.source==4,"crit and special attribution preserved");
            Point(lead.position,AH64Muzzles.rail,"rail origin");
            if(stacks>0)
            {
                Check(fired[1].comboNumber==1&&fired[2].comboNumber==2,"extras unguided markers");
                Near(fired[1].damage,lead.damage,0,"first extra full payload, no stack scaling");
                Near(fired[2].damage,lead.damage,0,"second extra full payload, no stack scaling");
                Near(Angle(lead.rotation*Vector3.forward,fired[1].rotation*Vector3.forward),25,0.01f,"minus25 fan");
                Near(Angle(lead.rotation*Vector3.forward,fired[2].rotation*Vector3.forward),25,0.01f,"plus25 fan");
            }
        }
        NetworkServer.active=true;
        var recoveryBody=Body(true,450);
        var recoveryState=new FireHellfire { gameObject=recoveryBody.gameObject,characterBody=recoveryBody,isAuthority=true };
        recoveryState.OnEnter();var recoveryMissile=Missile(recoveryBody);Lifecycle.Call(recoveryMissile,"Start");
        var recoveryOwner=recoveryBody.GetComponent<AH64HellfireOwner>();uint recoveryToken=recoveryOwner.Policy.ActiveToken;
        recoveryState.stopwatch=recoveryState.duration;recoveryState.OnExit();
        Check(recoveryOwner.Policy.CanGuide(recoveryToken,Time.fixedTime),"normal state exit retains held guidance");
        recoveryState=new FireHellfire { gameObject=recoveryBody.gameObject,characterBody=recoveryBody,isAuthority=true };
        recoveryState.OnEnter();recoveryMissile=Missile(recoveryBody);Lifecycle.Call(recoveryMissile,"Start");
        recoveryToken=recoveryOwner.Policy.ActiveToken;recoveryState.OnExit();
        Check(!recoveryOwner.Policy.CanGuide(recoveryToken,Time.fixedTime),"interrupted launch state closes guidance");
        var prefab=new GameObject();var controller=prefab.AddComponent<ProjectileController>();
        var identity=prefab.AddComponent<NetworkIdentity>();prefab.AddComponent<Rigidbody>();
        AH64HellfireGuidance.Install(prefab);AH64HellfireGuidance.Install(prefab);
        Check(!controller.allowPrediction&&!controller.authorityHandlesCollisionEvents,"prediction and client damage authority disabled deliberately");
        Check(!identity.localPlayerAuthority,"projectile server authority");
        Check(prefab.GetComponents<AH64HellfireGuidance>().Length==1,"install idempotent");
        Check(!prefab.GetComponent<ProjectileNetworkTransform>().checkForLocalPlayerAuthority,"server owns replicated transform");
    }
    public static void Main()
    {
        Policy();Geometry();SteeringRegressions();LifecycleAndNetwork();Collision();NativeDeferredImpact();LaunchAndPayload();
        Console.WriteLine($"Hellfire checks passed: {assertions} assertions (API/physics doubles; runtime unverified).");
    }
}
