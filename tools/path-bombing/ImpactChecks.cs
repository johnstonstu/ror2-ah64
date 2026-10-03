using System;
using System.Reflection;
using AH64.Survivors.Components;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

internal static class ImpactChecks
{
    internal static void Run()
    {
        foreach(string failure in new[] { "effect", "pre-report", "post-report", "item-callback" })
            ThrowThroughImpact(failure);
    }

    private static void ThrowThroughImpact(string failure)
    {
        Checks.Reset();
        var cast=new AH64BombingRunCast(Checks.Owner()); cast.Tick();
        var bomb=ProjectileManager.instance.Bombs[0];
        bomb.GetComponent<AH64BombingRunProjectile>().ImpactEffect=new GameObject();
        var target=new GameObject().AddComponent<HealthComponent>();
        var hurt=new GameObject().AddComponent<HurtBox>(); hurt.healthComponent=target;
        BlastAttack.Hits=new[] { new BlastAttack.HitPoint { hurtBox=hurt } };
        Physics.Hits=new[] { new RaycastHit { collider=new GameObject().AddComponent<Collider>(),distance=.04f } };
        var expected=new InvalidOperationException("injected "+failure);
        Action throwHook=()=>throw expected;
        if(failure=="effect") EffectManager.Callback=throwHook;
        if(failure=="pre-report") target.BeforeDamage=throwHook;
        if(failure=="post-report") target.AfterDamage=throwHook;
        if(failure=="item-callback") GlobalEventManager.instance.EnemyCallback=throwHook;
        Exception observed=null;
        try { Checks.TickProjectile(bomb); }
        catch(TargetInvocationException exception) { observed=exception.InnerException; }
        Checks.Check(ReferenceEquals(expected,observed),failure+": original impact exception propagates");
        Checks.Check(bomb.destroyed,failure+": impact finally destroys projectile");
        int expectedAccepted=(failure=="post-report"||failure=="item-callback")?1:0;
        Checks.Check(cast.Policy.HitCount(target)==expectedAccepted,failure+": accepted damage accounting preserved");
        int attempts=target.attempts;
        Checks.TickProjectile(bomb); Checks.TickProjectile(bomb);
        Checks.Check(EffectManager.calls==1&&target.attempts==attempts,failure+": no repeated effect or damage");
        Checks.Check(cast.Policy.TryBeginHit(target),failure+": no stranded pending reservation");
        cast.Policy.CompleteHit(target,false);
        Console.WriteLine("PASS impact exception cleanup/accounting/reporting: "+failure);
    }
}
