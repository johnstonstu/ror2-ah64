using System;
using System.Reflection;
using AH64.Survivors;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Hooks=On.RoR2.CharacterMotor;

internal static class BrakingChecks
{
    static int checks;
    static void Assert(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
    static void Near(float actual,float expected,string name,float tolerance=.0005f)
        =>Assert(Math.Abs(actual-expected)<=tolerance,name+": "+actual+" != "+expected);
    static void Vec(Vector3 actual,Vector3 expected,string name)
    {Near(actual.x,expected.x,name+".x");Near(actual.y,expected.y,name+".y");Near(actual.z,expected.z,name+".z");}
    static Vector3 Heading(float degrees)=>Quaternion.AngleAxis(degrees,Vector3.up)*Vector3.forward;
    static AH64BrakingTurnCapture Capture(float speed,float degrees=90,float move=8.5f)
        =>AH64BrakingTurnCapture.Create(new Vector3(0,2,speed),Heading(degrees),Vector3.forward,
            Vector3.forward,Quaternion.identity,move);
    static GameObject Body()
    {
        var go=new GameObject();go.AddComponent<CharacterMotor>();go.AddComponent<CharacterBody>();
        go.AddComponent<CharacterDirection>();go.AddComponent<InputBankTest>();
        go.AddComponent<AH64HoverController>();go.AddComponent<AH64FlightVisuals>();return go;
    }
    static void Invoke(object value,string method)=>value.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(value,null);

    static void MathChecks()
    {
        Assert(AH64BrakingTurnAcceptance.Priority(InterruptPriority.PrioritySkill),"native PrioritySkill accepted");
        Assert(!AH64BrakingTurnAcceptance.Priority(InterruptPriority.Skill),"wrong native Skill priority rejected");
        foreach(float correction in new[]{30f,180f,-30f,-180f})
        {
            var observedCapture=Capture(17f,correction);var observed=observedCapture.EntryVelocity;
            for(int i=0;i<50;i++)
            {
                observed=AH64BrakingTurnMath.Step(observedCapture,observed,i*.02f,.02f);
                if(i==18)Assert(AH64BrakingTurnAcceptance.Turned(observedCapture,observed,false),"actual velocity signed turn progress");
            }
            Assert(AH64BrakingTurnAcceptance.Turned(observedCapture,observed,true),"actual velocity final requested alignment");
            Assert(!AH64BrakingTurnAcceptance.Turned(observedCapture,observedCapture.EntryVelocity,true),"unturned actual velocity rejected");
            Assert(!AH64BrakingTurnAcceptance.Turned(observedCapture,Heading(-Math.Sign(observedCapture.SignedCorrection)*10f),false),"wrong direction actual velocity rejected");
        }
        var c=Capture(8.5f);
        Near(c.Duration,1,"duration");Near(c.BrakeSpeed,2.125f,"brake ratio");
        Near(Capture(30).BrakeSpeed,2.975f,"brake cap");Near(c.ExitAcceleration,25.5f,"exit acceleration");
        Near(Capture(30).ExitSpeed,8.5f,"exit cap");
        foreach(float speed in new[]{0f,.01f,1f,8.5f,20f,150f})
        foreach(float angle in new[]{-180f,-95f,-10f,0f,5f,90f,179f,180f})
        foreach(float dt in new[]{.01f,.02f,.073f})
        {
            c=Capture(speed,angle);var actual=c.EntryVelocity;float age=0;
            while(age<c.Duration-.00001f)
            {
                float step=Math.Min(dt,c.Duration-age);
                var before=actual;
                actual=AH64BrakingTurnMath.Step(c,actual,age,step);
                Near(actual.y,2,"math keeps y");
                float oldSpeed=AH64BrakingTurnMath.Horizontal(before).magnitude;
                float newSpeed=AH64BrakingTurnMath.Horizontal(actual).magnitude;
                Assert(newSpeed<=speed+.002f,"no launch gain");
                if(age+step<=.30001f)Assert(newSpeed<=oldSpeed+.001f,"braking monotonic");
                if(age>=.80001f)Assert(Math.Abs(newSpeed-oldSpeed)<=c.ExitAcceleration*step+.001f,"exit bounded");
                if(oldSpeed>.000001f && newSpeed>.000001f)
                {
                    float turnTime=Math.Max(0,Math.Min(age+step,.8f)-Math.Max(age,.3f));
                    float change=Math.Abs(AH64BrakingTurnMath.Angle(
                        AH64BrakingTurnMath.Horizontal(before).normalized,AH64BrakingTurnMath.Horizontal(actual).normalized));
                    Assert(change<=360*turnTime+.02f,"only turn phase changes heading");
                }
                age+=step;
            }
            if(speed>0)Near(AH64BrakingTurnMath.Angle(AH64BrakingTurnMath.Horizontal(actual).normalized,Heading(angle)),0,"requested correction",.025f);
            else Near(AH64BrakingTurnMath.Horizontal(actual).magnitude,0,"stationary pivot");
        }
        c=Capture(8.5f,25);
        var split=AH64BrakingTurnMath.Step(c,c.EntryVelocity,0,.3f);
        split=AH64BrakingTurnMath.Step(c,split,.3f,.5f);
        split=AH64BrakingTurnMath.Step(c,split,.8f,.2f);
        Vec(AH64BrakingTurnMath.Step(c,c.EntryVelocity,0,1),split,"boundary split consistency");
        Near(AH64BrakingTurnMath.Horizontal(split).magnitude,7.225f,"controlled exit need not hit cruise");
        Vec(AH64BrakingTurnMath.Step(c,split,1,.2f),split,"no writes beyond window");
        var slow=AH64BrakingTurnMath.Step(c,new Vector3(0,0,.5f),0,.02f);
        Assert(slow.magnitude<=.5f,"brake never replenishes slower actual momentum");
        c=AH64BrakingTurnCapture.Create(Vector3.zero,Vector3.zero,Heading(-30),Heading(70),Quaternion.identity,8.5f);
        Near(c.SignedCorrection,-100,"aim fallback relative facing");
        c=AH64BrakingTurnCapture.Create(Vector3.zero,Vector3.zero,Vector3.up,Heading(70),Quaternion.identity,8.5f);
        Vec(c.RequestedHeading,Heading(70),"vertical aim falls back to facing");
        c=AH64BrakingTurnCapture.Create(Vector3.zero,Vector3.zero,Vector3.zero,Vector3.zero,Quaternion.identity,0);
        Vec(c.RequestedHeading,Vector3.forward,"degenerate input fallback");
    }

    static void CaptureAndPresentation()
    {
        var c=Capture(20,-120);c.EntryAttitude=new Quaternion(.1f,.2f,.3f,.9f);
        var w=new NetworkWriter();c.Write(w);
        var restored=AH64BrakingTurnCapture.Read(new NetworkReader(w.ToArray()));
        var w2=new NetworkWriter();restored.Write(w2);
        Assert(Convert.ToBase64String(w.ToArray())==Convert.ToBase64String(w2.ToArray()),"full capture roundtrip");
        var go=Body();var p=go.AddComponent<AH64BrakingTurnPresentation>();var owner=new object();var stale=new object();
        p.Begin(owner,c);p.Progress(stale,.5f);
        Assert(p.TryGetFrame(owner,out var f),"owner frame");Near(f.Progress,0,"stale progress ignored");
        Assert(!p.TryGetFrame(stale,out f),"stale read denied");
        p.Progress(owner,.55f);p.Begin(owner,Capture(1,1)); // idempotent
        Assert(p.TryGetFrame(owner,out f),"frame after duplicate begin");
        Near(f.Progress,.55f,"duplicate begin no rewind");
        Assert(f.Phase==AH64BrakingTurnPhase.Turn,"turn phase");
        Near(f.SignedCorrection,-120,"duplicate begin no recapture");
        Near(f.EntryAttitude.w,.9f,"attitude captured");
        Vec(f.CommandedHeading,Heading(-90),"bounded presentation command");
        p.End(stale);Assert(p.TryGetFrame(owner,out f),"stale end ignored");
        var replacement=new object();p.Begin(replacement,Capture(0,30));
        p.End(owner);Assert(p.TryGetFrame(replacement,out f),"old exit cannot clear replacement");
        p.Progress(replacement,.8f);p.TryGetFrame(replacement,out f);
        Vec(f.CommandedHeading,Heading(30),"small pivot no forced reversal");
        SceneManager.Change();Assert(!p.TryGetFrame(replacement,out f),"stage clears presentation");
        p.Begin(owner,c);go.GetComponent<CharacterBody>().healthComponent.alive=false;
        Assert(!p.TryGetFrame(owner,out f),"death clears presentation");
        go.GetComponent<CharacterBody>().healthComponent.alive=true;
        p.Begin(owner,c);Invoke(p,"OnDisable");Assert(!p.TryGetFrame(owner,out f),"disable clears presentation");
        Assert(SceneManager.Subscribers==0,"presentation unsubscribed");
    }

    static void MotorChecks()
    {
        foreach(bool grounded in new[]{false,true})
        {
            var go=Body();var m=go.GetComponent<CharacterMotor>();m.isGrounded=grounded;
            m.velocity=new Vector3(0,3,8.5f);m.moveDirection=new Vector3(1,-.5f,0);m.airControl=.6f;
            var lease=go.AddComponent<AH64BrakingTurnMotor>();var owner=new object();var c=Capture(8.5f);
            lease.Begin(owner,()=>true,c);
            Hooks.Pre(m,.02f);
            Near(m.velocity.y,Hooks.NativeResult.y,"native vertical retained grounded="+grounded);
            Vec(AH64BrakingTurnMath.Horizontal(m.velocity),
                AH64BrakingTurnMath.Horizontal(AH64BrakingTurnMath.Step(c,c.EntryVelocity,0,.02f)),"native horizontal overridden");
            Near(m.airControl,.6f,"air control unchanged");Assert(m.isGrounded==grounded,"ground state unchanged");
            lease.Release(new object());Assert(lease.HasActiveLease,"stale motor release");
            var before=m.velocity;lease.Release(owner);lease.Release(owner);Vec(m.velocity,before,"exit no carry restoration");
            Assert(Hooks.Hooks==0 && m.HitSubscribers==0,"normal unhook");
        }
        foreach(string reason in new[]{"impulse","wall","roof","authority","state-authority","gravity","custom-gravity","air-lock","slam","death","disable","stage","main-handoff"})
        {
            var go=Body();var m=go.GetComponent<CharacterMotor>();m.velocity=new Vector3(0,2,8.5f);
            var lease=go.AddComponent<AH64BrakingTurnMotor>();var owner=new object();bool authority=true;
            lease.Begin(owner,()=>authority,Capture(8.5f));
            switch(reason)
            {
                case "impulse": Hooks.Force(m,new Vector3(10,5,0));break;
                case "wall": m.Hit(new Vector3(1,0,0));m.velocity=Vector3.zero;break;
                case "roof": m.Hit(new Vector3(0,-1,0));break;
                case "authority": m.hasEffectiveAuthority=false;lease.Check(owner);break;
                case "state-authority": authority=false;lease.Check(owner);break;
                case "gravity": m.useGravity=true;lease.Check(owner);break;
                case "custom-gravity": m.useCustomGravity=true;lease.Check(owner);break;
                case "air-lock": m.disableAirControlUntilCollision=true;lease.Check(owner);break;
                case "slam": EntityStates.Headstompers.BaseHeadstompersState.Current=new EntityStates.Headstompers.HeadstompersFall();lease.Check(owner);break;
                case "death": go.GetComponent<CharacterBody>().healthComponent.alive=false;lease.Check(owner);break;
                case "disable": Invoke(lease,"OnDisable");break;
                case "stage": SceneManager.Change();break;
                case "main-handoff": var old=m.velocity;m.velocity+=new Vector3(12,0,0);lease.ObserveMovement(owner,old);break;
            }
            var handoff=m.velocity;
            Assert(lease.IsYielding,reason+" yields");Assert(Hooks.Hooks==0 && m.HitSubscribers==0,reason+" unsubscribes");
            lease.Release(owner);Vec(m.velocity,handoff,reason+" does not restore");
            EntityStates.Headstompers.BaseHeadstompersState.Current=null;
        }
        {
            var go=Body();var m=go.GetComponent<CharacterMotor>();var lease=go.AddComponent<AH64BrakingTurnMotor>();var owner=new object();
            m.velocity=new Vector3(0,0,8.5f);lease.Begin(owner,()=>true,Capture(8.5f));
            m.rejectForces=true;Hooks.Force(m,new Vector3(3,4,5));Assert(lease.HasActiveLease,"rejected force preserves lease");
            m.Hit(Vector3.up);Assert(lease.HasActiveLease,"floor retains lease");lease.Release(owner);
        }
        foreach(string nested in new[]{"authority","gravity","impulse","wall","death"})
        {
            var go=Body();var m=go.GetComponent<CharacterMotor>();m.velocity=new Vector3(0,1,8.5f);
            var lease=go.AddComponent<AH64BrakingTurnMotor>();var owner=new object();lease.Begin(owner,()=>true,Capture(8.5f));
            Hooks.DuringNative=motor=>
            {
                if(nested=="authority")motor.hasEffectiveAuthority=false;
                if(nested=="gravity")motor.useGravity=true;
                if(nested=="impulse")Hooks.Force(motor,new Vector3(5,7,9));
                if(nested=="wall"){motor.Hit(new Vector3(1,0,0));motor.velocity=Vector3.zero;}
                if(nested=="death")go.GetComponent<CharacterBody>().healthComponent.alive=false;
            };
            Hooks.Pre(m,.02f);Hooks.DuringNative=null;
            Vec(m.velocity,Hooks.NativeResult,"nested "+nested+" wins");Assert(!lease.HasActiveLease,"nested "+nested+" unhooks");lease.Release(owner);
        }
        Assert(Hooks.Hooks==0 && SceneManager.Subscribers==0,"motor cleanup complete");
    }

    static void StateChecks()
    {
        var go=Body();var state=go.AddComponent<BrakingTurn>();var motor=go.GetComponent<CharacterMotor>();
        motor.velocity=new Vector3(0,2,8.5f);go.GetComponent<InputBankTest>().moveVector=Heading(25);
        state.OnEnter();Assert(state.GetMinimumInterruptPriority()==InterruptPriority.Pain,"reentry blocked");
        var writer=new NetworkWriter();state.OnSerialize(writer);
        var remote=Body();var observer=remote.AddComponent<BrakingTurn>();observer.isAuthority=false;
        remote.GetComponent<CharacterMotor>().hasEffectiveAuthority=false;
        remote.GetComponent<CharacterMotor>().velocity=new Vector3(71,72,73);
        observer.OnDeserialize(new NetworkReader(writer.ToArray()));observer.OnEnter();
        Assert(remote.GetComponent<AH64BrakingTurnMotor>()==null,"observer no motor lease");
        Assert(remote.GetComponent<AH64FlightVisuals>().Captures==0,"observer no recapture");
        Vec(observer.EntrySnapshot.EntryVelocity,state.EntrySnapshot.EntryVelocity,"observer receives authority momentum");
        var input=go.GetComponent<InputBankTest>();input.jump.down=true;input.aimDirection=Heading(-60);
        var hover=go.GetComponent<AH64HoverController>();
        state.FixedUpdate();Hooks.Pre(motor,.02f);
        Assert(hover.Calls==1 && hover.Taps==1,"inherited hover and collective paths");
        Near(hover.Airtime,9.98f,"airtime drains");Vec(input.aimDirection,Heading(-60),"native aim untouched");
        Assert(state.InputTicks==1,"native PerformInputs path retained");
        input.jump.down=false;AH64DescendInput.Held=true;
        state.FixedUpdate();Assert(hover.LastDescend,"modern descend retained");AH64DescendInput.Held=false;
        AH64PlaytestConfig.ClassicAltitude=true;input.rawMoveDown.down=true;state.FixedUpdate();
        Assert(hover.LastDescend,"classic descend retained");AH64PlaytestConfig.ClassicAltitude=false;
        observer.FixedUpdate();Vec(remote.GetComponent<CharacterMotor>().velocity,new Vector3(71,72,73),"observer leaves velocity");
        input.moveVector=Heading(160);Near(state.EntrySnapshot.SignedCorrection,25,"late input does not retarget");
        hover.Movement=()=>motor.velocity+=new Vector3(9,0,0);
        state.FixedUpdate();Assert(state.MotionYielded && state.outer.main,"direct Quail/handoff exits to Main");
        state.OnExit();observer.OnExit();
        Assert(Hooks.Hooks==0 && SceneManager.Subscribers==0,"state cleanup");
        go=Body();state=go.AddComponent<BrakingTurn>();motor=go.GetComponent<CharacterMotor>();
        motor.velocity=new Vector3(0,0,8.5f);state.OnEnter();
        for(int i=0;i<51 && !state.outer.main;i++){state.FixedUpdate();Hooks.Pre(motor,.02f);}
        Assert(state.outer.main,"normal completion to Main");
        var before=motor.velocity;state.OnExit();Vec(motor.velocity,before,"normal exit no momentum restoration");
    }

    public static int Main()
    {
        try { MathChecks();CaptureAndPresentation();MotorChecks();StateChecks();Console.WriteLine("PASS "+checks+" braking assertions (API doubles; no runtime proof).");return 0; }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
