using System;
using System.Reflection;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static partial class VisualChecks
{
    private sealed class OtherBodyState : EntityState
    {
        public InterruptPriority minimum;
        public override InterruptPriority GetMinimumInterruptPriority()=>minimum;
    }
    private static object Field(object value,string name)
        =>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(value);

    private static void BrakingIntegrationChecks()
    {
        Check(!AH64HellfireInterruption.IsInterrupted(null),"guidance null body state unchanged");
        foreach(EntityState utility in new EntityState[]{new BrakingTurn(),new ServoDash(),new SmokeBackflip()})
            Check(!AH64HellfireInterruption.IsInterrupted(utility),"typed utility retains guidance");
        foreach(InterruptPriority priority in new[]{InterruptPriority.Any,InterruptPriority.Skill,InterruptPriority.PrioritySkill,InterruptPriority.Pain,InterruptPriority.Stun,InterruptPriority.Death})
            Check(AH64HellfireInterruption.IsInterrupted(new OtherBodyState{minimum=priority})==(priority>=InterruptPriority.Pain),"other Pain/stun/death interrupts guidance");

        foreach(bool recoil in new[]{false,true})
        {
            var g=Body();var visuals=g.GetComponent<AH64FlightVisuals>();var locator=g.GetComponent<ModelLocator>();
            var machine=g.AddComponent<EntityStateMachine>();var state=g.AddComponent<BrakingTurn>();
            locator.modelBaseTransform.rotation=Quaternion.Euler(0,35,0);
            Call(visuals,"Start");if(recoil)visuals.AddKick(6,-3);
            float savedDelta=Time.deltaTime;Time.deltaTime=0;Call(visuals,"LateUpdate");
            Quaternion entryWorld=locator.modelTransform.rotation;
            Quaternion entryBasis=(Quaternion)Field(visuals,"lastRenderedBaseWorld");
            var capture=AH64BrakingTurnCapture.Create(new Vector3(0,0,8.5f),new Vector3(1,0,0),
                Vector3.forward,Vector3.forward,visuals.CaptureAttitude(),8.5f);
            machine.state=state;
            // Component is created after Start, exercising the central retry instead of a null cache.
            var presentation=g.AddComponent<AH64BrakingTurnPresentation>();presentation.Begin(state,capture);
            locator.modelBaseTransform.rotation=Quaternion.Euler(0,-120,0);
            int writes=locator.modelTransform.PositionAndRotationWrites;
            var aim=g.GetComponent<InputBankTest>().aimDirection;
            Time.deltaTime=.001f;
            Call(visuals,"LateUpdate");
            Check(Angle(entryBasis,(Quaternion)Field(visuals,"lastRenderedBaseWorld"))<.06f,"braking entry preserves displayed world attitude basis across yaw change");
            Check(Angle(entryWorld,locator.modelTransform.rotation)<.2f,"braking entry preserves displayed world attitude while recoil decays normally");
            Check(locator.modelTransform.PositionAndRotationWrites==writes+1,"single central transform write");
            Check(ReferenceEquals(Field(visuals,"brakingVisualOwner"),state),"central visual lease matches Body owner");
            Check(g.GetComponent<InputBankTest>().aimDirection==aim,"braking presentation leaves native aim");
            Check(g.GetComponent<CharacterMotor>().velocity==Vector3.zero,"braking presentation leaves native motor");
            Check(g.GetComponent<CameraTargetParams>().fovOverride==83,"braking presentation adds no camera FOV lease");
            Time.deltaTime=.001f;presentation.Progress(state,.55f);
            for(int i=0;i<100;i++)Call(visuals,"LateUpdate");
            Quaternion beforeInterrupt=locator.modelTransform.rotation;
            presentation.End(state);machine.state=new OtherBodyState{minimum=InterruptPriority.Stun};
            locator.modelBaseTransform.rotation=Quaternion.Euler(0,140,0);
            Call(visuals,"LateUpdate");
            Check(Angle(beforeInterrupt,locator.modelTransform.rotation)<3,"interruption recovery preserves world continuity when native yaw changes");
            Check(Field(visuals,"brakingVisualOwner")==null,"interruption releases visual owner");
            for(int i=0;i<1500;i++)Call(visuals,"LateUpdate");
            Check(!(bool)Field(visuals,"brakingRecovering"),"world recovery converges and releases bookkeeping");
            Check(Angle(locator.modelTransform.rotation,locator.modelBaseTransform.rotation)<1,"world recovery returns to native yaw and ordinary lean");
            machine.state=state;presentation.Begin(state,capture);presentation.Progress(state,.45f);
            Call(visuals,"LateUpdate");SceneManager.Change();Call(visuals,"LateUpdate");
            Check(Field(visuals,"brakingVisualOwner")==null,"stage transition clears central owner through data lease");
            presentation.Begin(state,capture);Call(visuals,"LateUpdate");visuals.PlayCrash(1);
            Check(Field(visuals,"brakingVisualOwner")==null && !(bool)Field(visuals,"brakingRecovering"),"death crash precedence clears braking presentation");
            Call(presentation,"OnDisable");Call(visuals,"OnDisable");Call(visuals,"OnDestroy");Time.deltaTime=savedDelta;
        }

        var frame=new AH64BrakingTurnFrame{Phase=AH64BrakingTurnPhase.Brake,PhaseProgress=0,CommandedHeading=Vector3.forward};
        Quaternion entry=Quaternion.Euler(17,80,-23),ordinary=Quaternion.Euler(3,-40,6);
        Check(Angle(AH64FlightBrakingMath.Target(frame,entry,ordinary),entry)<.06f,"visual math brake begins at actual displayed world basis");
        frame.Phase=AH64BrakingTurnPhase.Exit;frame.PhaseProgress=1;
        Check(Angle(AH64FlightBrakingMath.Target(frame,entry,ordinary),ordinary)<.06f,"visual math exit ends at ordinary world basis");

        // Native 180-degree exits exposed a faster return than the commanded turn.
        // Exercise the actual central writer with changing hover/sway targets.
        foreach(float fps in new[]{30f,60f,144f})
        {
            var g=Body();var visuals=g.GetComponent<AH64FlightVisuals>();var locator=g.GetComponent<ModelLocator>();
            var machine=g.AddComponent<EntityStateMachine>();var state=g.AddComponent<BrakingTurn>();
            Call(visuals,"Start");float saved=Time.deltaTime;Time.deltaTime=1f/fps;
            var capture=AH64BrakingTurnCapture.Create(new Vector3(0,0,17),-Vector3.forward,
                Vector3.forward,Vector3.forward,Quaternion.identity,8.5f);
            var presentation=g.AddComponent<AH64BrakingTurnPresentation>();machine.state=state;presentation.Begin(state,capture);
            Quaternion previous=locator.modelTransform.rotation;
            for(int i=0;i<(int)fps;i++)
            {
                Time.time+=Time.deltaTime;presentation.Progress(state,i/fps);
                Call(visuals,"LateUpdate");Quaternion now=locator.modelTransform.rotation;
                Check(Angle(previous,now)<=360f*Time.deltaTime+.08f,"180-degree braking presentation obeys rendered angular rate");previous=now;
            }
            presentation.End(state);machine.state=new OtherBodyState{minimum=InterruptPriority.Stun};
            for(int i=0;i<(int)(fps*1.4f);i++)
            {
                Time.time+=Time.deltaTime;
                g.GetComponent<CharacterMotor>().velocity=new Vector3(0,(i%2==0 ? 1f : -1f),0);
                Call(visuals,"LateUpdate");Quaternion now=locator.modelTransform.rotation;
                Check(Angle(previous,now)<=360f*Time.deltaTime+.08f,"changing native hover target has bounded recovery");previous=now;
            }
            Check(!visuals.IsBrakingRecovering && !visuals.HasBrakingOwner,"hover/sway recovery releases bookkeeping within fixture window");
            Call(visuals,"OnDestroy");Time.deltaTime=saved;
        }
    }
}
