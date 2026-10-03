// Offline negative-control fixture: actual states/motor/math, engine API doubles.
// The same spare-stock and signed-lateral outcomes are observed independently in the native suite.
using System;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

internal static class ProbeValidityChecks
{
    private static GameObject Body()
    {
        var body=new GameObject();body.transform.position=new Vector3(0,40,0);
        body.AddComponent<CharacterMotor>().velocity=new Vector3(3,0,7);
        body.AddComponent<CharacterBody>();body.AddComponent<CharacterDirection>();
        body.AddComponent<InputBankTest>().moveVector=new Vector3(1,0,0)*.25f;
        body.AddComponent<AH64HoverController>();body.AddComponent<AH64FlightVisuals>();
        return body;
    }

    private static BaseSkillState State(GameObject body,bool backflip)
    {
        var state=backflip ? (BaseSkillState)body.AddComponent<SmokeBackflip>() : body.AddComponent<ServoDash>();
        state.isAuthority=true;return state;
    }

    private static void Reentry(bool backflip)
    {
        var body=Body();var current=State(body,backflip);current.OnEnter();
        var original=current;int spareStock=2,originalStock=spareStock;
        int defenseGrants=body.GetComponent<CharacterBody>().buffs.Count;
        // Native SkillDef.CanExecute uses this installed-game minimum<=incoming comparison
        // before consuming stock. Stock deliberately remains available at both attempts.
        bool accepted=false;
        for(int attempt=0;attempt<2;attempt++)
        {
            current.FixedUpdate();
            if(spareStock>0 && current.GetMinimumInterruptPriority()<=InterruptPriority.PrioritySkill)
            {
                accepted=true;spareStock--;current.OnExit();
                current=State(body,backflip);current.OnEnter();
            }
        }
        bool pass=!accepted && originalStock>=1 && spareStock==originalStock
            && ReferenceEquals(current,original)
            && body.GetComponent<CharacterBody>().buffs.Count==defenseGrants;
        current.OnExit();
        if(!pass) throw new Exception("REGRESSION_PRIORITY_SPARE_STOCK: accepted="+accepted+" stock="+spareStock+" sameState="+ReferenceEquals(current,original));
    }

    private static void Steering(bool backflip)
    {
        var body=Body();var current=State(body,backflip);current.OnEnter();
        current.FixedUpdate();current.FixedUpdate();
        var motor=body.GetComponent<CharacterMotor>();float before=motor.velocity.x;
        body.GetComponent<InputBankTest>().moveVector=new Vector3(-1,0,0)*.5f;
        for(int tick=0;tick<8;tick++) current.FixedUpdate();
        float after=motor.velocity.x;current.OnExit();
        if(!(before>.1f && after<-.1f))
            throw new Exception("REGRESSION_STEERING_SIGN: before="+before+" after="+after);
    }

    public static int Main(string[] args)
    {
        NetworkServer.active=true;
        try {
            foreach(bool backflip in new[]{false,true})
            {
                if(args[0]=="priority") Reentry(backflip);
                else if(args[0]=="steering") Steering(backflip);
                else throw new ArgumentException("Unknown fixture mode.");
            }
            Console.WriteLine("PROBE_VALIDITY_PASS "+args[0]+" roll/backflip; API doubles, not native gameplay");return 0;
        } catch(Exception error) {Console.WriteLine(error.Message);return 1;}
    }
}
