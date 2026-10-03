// Source-model regressions informed by the installed game/KCC assemblies. Runtime remains a gate.
using System;
using AH64.Survivors.Components;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

internal static partial class MovementChecks
{
    // Installed EntityStateMachine.CanInterruptState, including pending-state precedence.
    private static bool CanInterrupt(BaseSkillState current, BaseSkillState pending, InterruptPriority priority)
        => (pending ?? current).GetMinimumInterruptPriority() <= priority;

    private sealed class OldPriorityUtility : BaseSkillState
    { public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.PrioritySkill; }

    private static void ReviewRegressionChecks()
    {
        GroundRecoveryChecks();
        foreach(bool flip in new[]{false,true})
        {
            NetworkServer.active=true;
            var g=Body(Vector3.forward*12);var state=State(g,flip);state.isAuthority=true;state.OnEnter();
            int stock=2,entryBuffs=g.GetComponent<CharacterBody>().buffs.Count;
            int ticks=(int)Math.Floor((flip?1.65f:.95f)/.02f)-1;
            // Source-modeled SkillDef.CanExecute gates priority BEFORE OnExecute consumes stock.
            for(int i=0;i<ticks;i++)
            {
                bool ready=stock>0 && CanInterrupt(state,null,InterruptPriority.PrioritySkill);
                if(ready){stock--;state.OnEnter();}
                Check(!ready,"held utility/extra stocks cannot re-enter an active maneuver");
                state.FixedUpdate();
            }
            Check(stock==2,"held source-model attempts preserve remaining stocks");
            Check(g.GetComponent<CharacterBody>().buffs.Count==entryBuffs,"held source-model attempts do not renew defense");
            Check(CanInterrupt(new OldPriorityUtility(),null,InterruptPriority.PrioritySkill),"old equal minimum permits re-entry in actual engine comparison");
            Check(!CanInterrupt(new OldPriorityUtility(),state,InterruptPriority.PrioritySkill),"pending maneuver minimum takes precedence");
            foreach(var incoming in new[]{InterruptPriority.Pain,InterruptPriority.Stun,InterruptPriority.Frozen,InterruptPriority.Death})
                Check(CanInterrupt(state,null,incoming),"replacing pain/stun/frozen/death priority remains permitted");
            var velocity=g.GetComponent<CharacterMotor>().velocity;state.OnExit();
            Check(g.GetComponent<CharacterMotor>().velocity==velocity,"priority interruption retains actual momentum");

            // H3AD-5T lives on its own state machine: returning Body to Main is needed to run
            // existing ApplyHover/YieldToHeadstompSlam. This test proves the return request only.
            var slam=State(g,flip);slam.isAuthority=true;slam.OnEnter();
            var motor=g.GetComponent<CharacterMotor>();motor.velocity=new Vector3(3,-10,1);
            EntityStates.Headstompers.BaseHeadstompersState.current=new EntityStates.Headstompers.HeadstompersFall();
            slam.FixedUpdate();Check(slam.outer.main,"active slam requests Main-owned physics handoff");
            Check(motor.velocity==new Vector3(3,-10,1),"slam handoff does not rewrite item velocity");
            slam.OnExit();Check(On.RoR2.CharacterMotor.PreMoveSubscribers==0,"slam exit releases PreMove hook");
            EntityStates.Headstompers.BaseHeadstompersState.current=null;
        }
        NetworkServer.active=false;
    }

    private static void GroundRecoveryChecks()
    {
        // Negative entry lands after Begin; Main's OnExit has cleared the move target.
        var g=Body(new Vector3(0,-8,12));var motor=g.GetComponent<CharacterMotor>();
        motor.moveDirection=Vector3.zero;
        var motion=g.AddComponent<AH64ManeuverMotor>();var owner=new object();
        motion.Begin(owner,Capture(motor.velocity));motion.Step(owner,Vector3.forward,.02f,.02f);
        Check(motor.velocity.y<0,"descending entry reaches floor before upward ramp");
        motor.isGrounded=true;motor.Hit(Vector3.up);motor.PhysicsStep(.02f);
        Check(motor.isGrounded && motor.velocity.y==0 && !motion.IsYielding,"floor keeps eligible utility lease");
        motion.Step(owner,Vector3.forward,.04f,.02f);var commanded=motor.velocity;
        Check(commanded.y>0 && motor.Motor.UngroundCalls==1,"owned upward step queues re-unground after landing");
        Check(motor.isGrounded,"queued unground does not immediately clear real KCC stable status");
        motor.PhysicsStep(.02f);
        Check(motor.velocity==commanded,"grounded PreMove cannot brake owned trajectory before unground is consumed");
        Check(!motor.isGrounded,"queued re-unground reaches airborne source-model phase");
        Check(!motor.isAirControlForced,"PreMove guard restores normal surface flag");
        motor.isAirControlForced=true;motor.PhysicsStep(.02f);
        Check(motor.isAirControlForced,"PreMove guard preserves pre-existing slippery-surface flag");
        motor.isAirControlForced=false;
        motion.Release(owner);
        Check(On.RoR2.CharacterMotor.PreMoveSubscribers==0,"ground recovery releases PreMove hook");

        // A wall remains a stop: no subsequent up-thrust or unground request is restored.
        motion.Begin(owner,Capture(motor.velocity));motor.Hit(new Vector3(-1,0,0));
        motor.isGrounded=true;motor.velocity=Vector3.zero;int calls=motor.Motor.UngroundCalls;
        motion.Step(owner,Vector3.forward,.2f,.02f);motor.PhysicsStep(.02f);
        Check(motor.velocity==Vector3.zero && calls==motor.Motor.UngroundCalls,"wall yield prevents recovery thrust");
        motion.Release(owner);
        // Negative control: ForceUnground alone still allows grounded acceleration before KCC consumes it.
        motor.isGrounded=true;motor.airControl=0;motor.velocity=new Vector3(0,1,1);motor.Motor.ForceUnground();
        var withoutGuard=motor.velocity;motor.PhysicsStep(.02f);
        Check(motor.velocity!=withoutGuard,"source model exposes ForceUnground-only grounded PreMove failure");
        motor.airControl=1;
    }
}
