using System;
using System.Reflection;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

internal static partial class MovementChecks
{
    private static int assertions;
    private static void Check(bool pass,string id){assertions++;if(!pass)throw new Exception(id);}
    private static void Near(float a,float b,string id,float epsilon=.0001f)=>Check(Math.Abs(a-b)<=epsilon,id+": "+a+" vs "+b);
    private static AH64ManeuverCapture Capture(Vector3 entry,bool flip=false)
    {
        var c=new AH64ManeuverCapture{EntryVelocity=entry,Facing=Vector3.forward,
            Direction=flip?-Vector3.forward:Vector3.forward,EntryAttitude=new Quaternion(.1f,.2f,.3f,.9f),
            Sign=-1,Duration=flip?1.65f:.95f,Ramp=.28f,Climb=10,StartY=40};
        AH64ManeuverMath.Speeds(ref c,12,flip?.75f:.8f,2.35f,.3f);return c;
    }
    private static GameObject Body(Vector3 velocity)
    {
        var g=new GameObject();g.transform.position=new Vector3(0,40,0);
        g.AddComponent<CharacterMotor>().velocity=velocity;g.AddComponent<CharacterBody>();
        g.AddComponent<CharacterDirection>();g.AddComponent<InputBankTest>();
        g.AddComponent<AH64HoverController>();g.AddComponent<AH64FlightVisuals>();return g;
    }
    public static void Main()
    {
        foreach(bool flip in new[]{false,true})
        {
            foreach(float side in new[]{-1f,-.25f,0f,.25f,1f})
            {
                var direction=AH64ManeuverMath.Direction(Vector3.forward,new Vector3(side,0,-1),flip,.7f);
                Near(direction.magnitude,1,"unit direction");Check(flip?direction.z<0:direction.z>0,"role retained");
                if(side!=0)Check(Math.Sign(direction.x)==Math.Sign(side),"analog side sign");
            }
            var partial=AH64ManeuverMath.Direction(Vector3.forward,new Vector3(.25f,0,0),flip,.7f);
            var full=AH64ManeuverMath.Direction(Vector3.forward,new Vector3(1,0,0),flip,.7f);
            Check(Math.Abs(partial.x)<Math.Abs(full.x),"partial input differs from full");
            foreach(var entry in new[]{Vector3.zero,new Vector3(12,0,0),new Vector3(0,0,-12),new Vector3(0,8,12),new Vector3(0,-8,12),new Vector3(0,0,200)})
            {
                var c=Capture(entry,flip);var velocity=entry;float y=c.StartY;float roleDistance=0;
                int steps=(int)Math.Ceiling(c.Duration/.02f);
                for(int i=0;i<steps;i++)
                {
                    var before=velocity;
                    velocity=AH64ManeuverMath.Step(c,velocity,c.Direction,(i+1)*.02f,y,.02f);
                    float delta=AH64ManeuverMath.Horizontal(velocity-before).magnitude;
                    Check(delta<=c.PeakSpeed*2/(c.Duration*c.Ramp)*.02f+.0002f,"bounded horizontal acceleration");
                    Check(AH64ManeuverMath.Horizontal(velocity).magnitude<=Math.Max(entry.magnitude,c.PeakSpeed)+.0002f,"speed budget");
                    y+=velocity.y*.02f;Check(y<=c.StartY+c.Climb+.0002f,"world climb budget");
                    roleDistance+=Vector3.Dot(velocity,c.Direction)*.02f;
                }
                if(entry.magnitude<=15)Check(roleDistance>3,"minimum role dodge from ordinary entry");
                Near(AH64ManeuverMath.Speed(c,0),c.EntrySpeed,"entry profile");
                Near(AH64ManeuverMath.Speed(c,c.Duration*c.Ramp),c.PeakSpeed,"peak profile");
                Near(AH64ManeuverMath.Speed(c,c.Duration),c.ExitSpeed,"exit profile");
            }
        }
        var chained=Capture(Vector3.zero);
        for(int i=0;i<1000;i++)
        {
            Check(chained.PeakSpeed<=12*2.35f+.001f,"chained peak cap");
            chained=Capture(new Vector3(0,0,chained.PeakSpeed));
        }
        foreach(float degrees in new[]{360f,-360f})
        {
            float last=0;
            for(int i=0;i<=100;i++)
            {float angle=AH64ManeuverMath.Revolution(i/100f,degrees);Check(degrees>0?angle>=last:angle<=last,"unwrapped spin monotonic");last=angle;}
            Near(last,degrees,"full revolution");Near(AH64ManeuverMath.Revolution(.5f,degrees),degrees*.5f,"half revolution");
        }
        var snapshot=Capture(new Vector3(4,-7,9));var writer=new NetworkWriter();snapshot.Write(writer);
        var copy=AH64ManeuverCapture.Read(new NetworkReader(writer.ToArray()));
        foreach(var field in typeof(AH64ManeuverCapture).GetFields())
            Check(field.GetValue(snapshot).Equals(field.GetValue(copy)),"semantic snapshot field: "+field.Name);
        var encoded=new NetworkWriter();copy.Write(encoded);
        Check(Convert.ToBase64String(writer.ToArray())==Convert.ToBase64String(encoded.ToArray()),"all snapshot fields round-trip");
        foreach(bool flip in new[]{false,true})StateChecks(flip);
        MotorChecks();
        ReviewRegressionChecks();
        Console.WriteLine("Movement offline checks passed: "+assertions+" assertions (API doubles; runtime unverified).");
    }
    private static BaseSkillState State(GameObject g,bool flip)
        =>flip?(BaseSkillState)g.AddComponent<SmokeBackflip>():g.AddComponent<ServoDash>();
    private static void StateChecks(bool flip)
    {
        NetworkServer.active=true;
        var g=Body(new Vector3(5,-3,-9));var motor=g.GetComponent<CharacterMotor>();var state=State(g,flip);state.isAuthority=true;
        var entry=motor.velocity;state.OnEnter();Check(motor.velocity==entry,"entry world velocity preserved");
        Check(g.GetComponent<AH64HoverController>().limits==1,"one authority climb capture");
        Check(g.GetComponent<AH64FlightVisuals>().captures==1,"one authority attitude capture");
        Check(state.GetMinimumInterruptPriority()==InterruptPriority.Pain,"minimum above utility activation priority");
        Check(g.GetComponent<CharacterBody>().buffs.Count==2,"baseline defensive grant count");
        var w=new NetworkWriter();state.OnSerialize(w);
        NetworkServer.active=false;
        var remote=Body(new Vector3(70,80,90));remote.GetComponent<CharacterMotor>().hasEffectiveAuthority=false;
        remote.GetComponent<AH64HoverController>().allowance=99;
        var observed=State(remote,flip);observed.OnDeserialize(new NetworkReader(w.ToArray()));observed.OnEnter();
        Check(remote.GetComponent<CharacterMotor>().velocity==new Vector3(70,80,90),"remote OnEnter never writes velocity");
        Check(remote.GetComponent<AH64HoverController>().limits==0,"remote does not resolve climb");
        Check(remote.GetComponent<AH64FlightVisuals>().captures==0,"remote does not recapture pose");
        Near(remote.GetComponent<AH64FlightVisuals>().played.x,g.GetComponent<AH64FlightVisuals>().played.x,"received pose used");
        var remoteW=new NetworkWriter();observed.OnSerialize(remoteW);
        Check(Convert.ToBase64String(w.ToArray())==Convert.ToBase64String(remoteW.ToArray()),"OnEnter preserves received snapshot");
        observed.FixedUpdate();Check(remote.GetComponent<CharacterMotor>().velocity==new Vector3(70,80,90),"remote tick never writes velocity");
        state.FixedUpdate();Check(g.GetComponent<CharacterDirection>().forward==Vector3.forward,"no facing snap");
        motor.velocity=new Vector3(2,-6,1);state.OnExit();Check(motor.velocity==new Vector3(2,-6,1),"interruption preserves replacing velocity");
        Near(motor.airControl,1,"air control restored");Check(On.RoR2.CharacterMotor.Subscribers==0 && On.RoR2.CharacterMotor.PreMoveSubscribers==0,"state motor hooks released");
        observed.OnExit();Check(remote.GetComponent<CharacterMotor>().velocity==new Vector3(70,80,90),"remote exit never writes velocity");
        var complete=State(g,flip);complete.isAuthority=true;complete.OnEnter();
        while(!complete.outer.main)complete.FixedUpdate();
        var achieved=motor.velocity;complete.OnExit();Check(motor.velocity==achieved,"normal exit preserves achieved carry");
    }
    private static void MotorChecks()
    {
        var g=Body(Vector3.forward*12);var motor=g.GetComponent<CharacterMotor>();var motion=g.AddComponent<AH64ManeuverMotor>();var owner=new object();
        motion.Begin(owner,Capture(motor.velocity));Near(motor.airControl,0,"owns motor PreMove");
        motor.Hit(new Vector3(-1,0,0));Check(motion.IsYielding,"wall yields");
        motor.velocity=Vector3.zero;motion.Step(owner,Vector3.forward,.5f,.02f);Check(motor.velocity==Vector3.zero,"wall stop not restored");
        motion.Release(owner);Check(motor.HitSubscribers==0,"collision hook released");
        motion.Begin(owner,Capture(motor.velocity));motor.Hit(new Vector3(0,-1,0));Check(motion.IsYielding,"ceiling yields");motion.Release(owner);
        motion.Begin(owner,Capture(motor.velocity));On.RoR2.CharacterMotor.Force(motor,new Vector3(1,8,2));
        var impulse=motor.velocity;motion.Step(owner,Vector3.forward,.5f,.02f);Check(motor.velocity==impulse,"external impulse wins");motion.Release(owner);
        motor.disableAirControlUntilCollision=true;motion.Begin(owner,Capture(motor.velocity));Check(motion.IsYielding,"entry launch flag wins");
        motion.Release(owner);Check(motor.disableAirControlUntilCollision,"launch flag never cleared");motor.disableAirControlUntilCollision=false;
        motion.Begin(owner,Capture(motor.velocity));EntityStates.Headstompers.BaseHeadstompersState.current=new EntityStates.Headstompers.HeadstompersFall();
        motion.Step(owner,Vector3.forward,.5f,.02f);Check(motion.IsYielding,"H3AD5T slam wins");motion.Release(owner);EntityStates.Headstompers.BaseHeadstompersState.current=null;
        motor.isFlying=false;motion.Begin(owner,Capture(motor.velocity));Check(motion.IsYielding,"existing hover external/void handoff retained");motion.Release(owner);motor.isFlying=true;
        motion.Begin(owner,Capture(motor.velocity));motor.rejectForces=true;
        On.RoR2.CharacterMotor.Force(motor,new Vector3(0,80,0));Check(!motion.IsYielding,"rejected impulse retains maneuver ownership");
        Near(motor.airControl,0,"rejected impulse retains controlled trajectory");motion.Release(owner);motor.rejectForces=false;
        motion.Begin(owner,Capture(motor.velocity));motor.hasEffectiveAuthority=false;
        motion.Step(owner,Vector3.forward,.5f,.02f);Check(motion.IsYielding,"authority loss releases lease");
        Near(motor.airControl,1,"authority loss restores control");motion.Release(owner);motor.hasEffectiveAuthority=true;
        foreach(bool hoverOutside in new[]{false,true})
        {
            On.RoR2.CharacterMotor.hook_ApplyForceImpulse hoverHook=(On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig,CharacterMotor m,ref PhysForceInfo info)=>
            {var before=m.velocity;orig(m,ref info);if(m.velocity!=before)m.airControl=.25f;};
            if(!hoverOutside)On.RoR2.CharacterMotor.ApplyForceImpulse+=hoverHook;
            motion.Begin(owner,Capture(motor.velocity));
            if(hoverOutside)On.RoR2.CharacterMotor.ApplyForceImpulse+=hoverHook;
            On.RoR2.CharacterMotor.Force(motor,new Vector3(0,8,0));
            Near(motor.airControl,.25f,"existing hover force hook wins in either hook order");
            motion.Release(owner);On.RoR2.CharacterMotor.ApplyForceImpulse-=hoverHook;motor.airControl=1;
        }
        motion.Begin(owner,Capture(motor.velocity));typeof(AH64ManeuverMotor).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(motion,null);
        Near(motor.airControl,1,"disable restores control");Check(On.RoR2.CharacterMotor.Subscribers==0 && On.RoR2.CharacterMotor.PreMoveSubscribers==0 && motor.HitSubscribers==0,"disable releases subscriptions");
    }
}
