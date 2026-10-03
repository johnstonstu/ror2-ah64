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
        StateAuthorityLossChecks();
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

    private static void StateAuthorityLossChecks()
    {
        foreach(bool flip in new[]{false,true})
        foreach(int authorityLoss in new[]{0,1,2})
        {
            var g=Body(new Vector3(4,2,9));var motor=g.GetComponent<CharacterMotor>();
            motor.airControl=.75f;
            var state=State(g,flip);state.isAuthority=true;state.OnEnter();
            Check(motor.airControl==0 && motor.HitSubscribers==1
                && On.RoR2.CharacterMotor.Subscribers==1 && On.RoR2.CharacterMotor.PreMoveSubscribers==1,
                "active state owns controls and all motor subscriptions before authority loss");
            // Loss 0 drops BOTH flags: the previously missed observer FixedUpdate path.
            // Loss 1/2 also cover temporary disagreement between state and motor authority.
            state.isAuthority=authorityLoss==2;
            motor.hasEffectiveAuthority=authorityLoss==1;
            var velocity=motor.velocity;
            state.FixedUpdate(); // Exercise actual ServoDash/SmokeBackflip, never direct Step.
            Near(motor.airControl,.75f,"state tick restores original air control after authority loss");
            Check(motor.HitSubscribers==0 && On.RoR2.CharacterMotor.Subscribers==0
                && On.RoR2.CharacterMotor.PreMoveSubscribers==0,
                "state tick removes collision/force/PreMove subscriptions after authority loss");
            Check(motor.velocity==velocity,"authority-loss cleanup preserves replicated/replacing velocity");
            Check(!state.outer.main,"authority-loss cleanup does not request an unrelated state transition");
            state.FixedUpdate();state.OnExit();
            Near(motor.airControl,.75f,"later observer tick and exit leave restored controls alone");
        }
        // PreMove is a fallback if the motor loses authority before the next state tick.
        var fallback=Body(Vector3.forward*12);var fallbackMotor=fallback.GetComponent<CharacterMotor>();
        var lease=fallback.AddComponent<AH64ManeuverMotor>();var owner=new object();
        lease.Begin(owner,Capture(fallbackMotor.velocity));fallbackMotor.hasEffectiveAuthority=false;
        On.RoR2.CharacterMotor.RunPreMove(fallbackMotor,.02f);
        Near(fallbackMotor.airControl,1,"PreMove authority-loss fallback restores controls");
        Check(fallbackMotor.HitSubscribers==0 && On.RoR2.CharacterMotor.Subscribers==0
            && On.RoR2.CharacterMotor.PreMoveSubscribers==0,"PreMove authority-loss fallback removes subscriptions");
        lease.Release(owner);
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
