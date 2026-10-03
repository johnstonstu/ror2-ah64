using System;
using System.Collections.Generic;
using System.Reflection;
using AH64.Survivors;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

internal static class Checks
{
    private static int assertions;
    internal static readonly List<AH64BombingRunTrace.Record> traces=new List<AH64BombingRunTrace.Record>();
    internal static void Check(bool condition,string message)
    { assertions++; if (!condition) throw new Exception(message); }
    private static bool Near(float a,float b)=>Math.Abs(a-b)<0.001f;
    internal static void TickProjectile(GameObject bomb)=>typeof(AH64BombingRunProjectile)
        .GetMethod("FixedUpdate",BindingFlags.Instance|BindingFlags.NonPublic)
        .Invoke(bomb.GetComponent<AH64BombingRunProjectile>(),null);
    internal static CharacterBody Owner()
    {
        var go=new GameObject();
        var body=go.AddComponent<CharacterBody>();
        body.healthComponent=go.AddComponent<HealthComponent>();
        body.characterMotor=new CharacterMotor();
        body.netId=new NetworkInstanceId { Value=1 };
        body.transform.position=new Vector3(0,10,0);
        return body;
    }
    internal static void Reset()
    {
        AH64BombingRunNetwork.Shutdown(); NetworkClient.allClients.Clear(); ClientScene.Objects.Clear();
        GameObject.RejectComponent=null; TeamComponent.BeforeLookup=null; ProjectileManager.BeforeSpawn=null;
        EntityStates.BaseSkillState.EntryCallback=null;
        NetworkServer.active=true; Time.fixedTime=0; Time.fixedDeltaTime=.02f;
        SceneManager.Handle=1; Run.instance=new Run();
        Physics.OriginBlocked=false; Physics.InsideWorld=false; Physics.Hits=Array.Empty<RaycastHit>();
        ProjectileManager.instance=new ProjectileManager(); GlobalEventManager.instance=new GlobalEventManager();
        BlastAttack.Hits=Array.Empty<BlastAttack.HitPoint>();
        AH64BombingRunProjectiles.Prefab=new GameObject(); traces.Clear();
        EffectManager.calls=0; EffectManager.Callback=null;
    }
    private static void Policy()
    {
        var policy=new AH64BombingRunPolicy<object>();
        Check(!policy.TryTakeDrop(double.NaN,out _),"NaN cannot release");
        Check(!policy.TryTakeDrop(double.PositiveInfinity,out _),"Infinity cannot release");
        Check(policy.TryTakeDrop(0,out int index)&&index==0,"First drop immediate");
        Check(!policy.TryTakeDrop(0,out _),"Duplicate tick cannot repeat drop");
        Check(!policy.TryTakeDrop(.29,out _),"Cadence holds");
        Check(policy.TryTakeDrop(.3,out index)&&index==1,"Second scheduled drop");
        Check(!policy.TryTakeDrop(.2,out _),"Clock rewind cannot release");
        while(policy.TryTakeDrop(100,out _)) { }
        Check(policy.ConsumedDrops==6,"Hitch retains finite count");
        Check(!policy.TryTakeDrop(101,out _),"Never replenish sequence");
        var target=new object();
        Check(policy.TryBeginHit(target),"Hit reservation");
        Check(!policy.TryBeginHit(target),"Reentrant target guarded");
        policy.CompleteHit(target,false);
        Check(policy.HitCount(target)==0,"Rejected report does not spend budget");
        for(int i=0;i<3;i++) { Check(policy.TryBeginHit(target),"Accepted hit slot"); policy.CompleteHit(target,true); }
        Check(!policy.TryBeginHit(target)&&policy.HitCount(target)==3,"Three-hit cap");
        policy.CompleteHit(target,true);
        Check(policy.HitCount(target)==3,"Completion idempotent");
        Check(policy.TryBeginHit(new object()),"Distinct target owns distinct budget");
        policy.Stop(); Check(!policy.TryTakeDrop(200,out _),"Stopped schedule cannot restart");
    }
    private static void PathAndState()
    {
        Reset(); var body=Owner(); var state=new BombingRun { characterBody=body,isAuthority=true };
        state.OnEnter(); body.damage=100;
        for(int i=1;i<6;i++)
        {
            body.transform.position=new Vector3(i*i,10+i,-i);
            body.characterMotor.velocity=new Vector3(100,100,0);
            Time.fixedTime=i*.3f; state.fixedAge=Time.fixedTime; state.FixedUpdate();
        }
        var shots=ProjectileManager.instance.Fired;
        Check(shots.Count==6,"Exactly six host releases");
        Check(Near(shots[5].position.x,25)&&Near(shots[5].position.z,-5),"Actual turning path sampled");
        Check(Near(shots[5].position.y,14.5f),"Utility altitude sampled");
        foreach(var shot in shots) Check(Near(shot.damage,30),"Cast damage snapshotted");
        Check(shots.TrueForAll(x=>x.damageTypeOverride.damageSource==DamageSource.Special),"Special damage attribution");
        Check(body.critRolls==6,"Crit rolled once per released bomb");
        var release=traces.FindLast(x=>x.kind=="release");
        Check(Near(release.velocity.x,25)&&Near(release.velocity.y,-4),"Horizontal cap and downward launch");
        Check(Near(release.scheduledTime,1.5f)&&Near(release.actualTime,1.5f),"Drop trace timing");
        state.fixedAge=1.53f; state.FixedUpdate(); Check(state.outer.exited,"Authority finishes state");
        state.OnExit(); state.OnExit(); Check(traces.FindAll(x=>x.kind=="end").Count==1,"Idempotent exit");

        Reset(); body=Owner(); NetworkServer.active=false;
        state=new BombingRun { characterBody=body,isAuthority=true }; state.OnEnter();
        Time.fixedTime=2; state.FixedUpdate();
        Check(ProjectileManager.instance.Fired.Count==0,"Client authority never spawns");
        Reset(); body=Owner(); state=new BombingRun { characterBody=body,isAuthority=false }; state.OnEnter();
        Check(ProjectileManager.instance.Fired.Count==1,"Server releases for remote-owned body");
        state.OnExit(); Time.fixedTime=2; state.FixedUpdate();
        Check(ProjectileManager.instance.Fired.Count==1,"Interruption stops pending drops");
        Reset(); body=Owner(); state=new BombingRun { characterBody=body,isAuthority=true }; state.OnEnter();
        var lease=body.GetComponent<AH64BombingRunOwner>();
        typeof(AH64BombingRunOwner).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lease,null);
        Time.fixedTime=2; state.FixedUpdate();
        Check(ProjectileManager.instance.Fired.Count==1,"Disable then reenable cannot resume missed drops");
        Reset(); body=Owner(); lease=body.gameObject.AddComponent<AH64BombingRunOwner>();
        var older=lease.Begin(body); var newer=lease.Begin(body); lease.End(older,"late-old-exit");
        Check(older.Policy.Stopped&&!newer.Policy.Stopped,"Old exit preserves newer lease");
        typeof(AH64BombingRunOwner).GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(lease,null);
        Check(newer.Policy.Stopped,"Disable cancels current owner lease");
    }
    private static void Suppression()
    {
        Reset(); var body=Owner(); var cast=new AH64BombingRunCast(body);
        Physics.OriginBlocked=true; cast.Tick(); Physics.OriginBlocked=false;
        Time.fixedTime=.3f; cast.Tick();
        Check(cast.Policy.ConsumedDrops==2&&ProjectileManager.instance.Fired.Count==1,"Blocked drop consumed without replacement");
        body.healthComponent.alive=false; Time.fixedTime=.6f; cast.Tick();
        Check(cast.Policy.Stopped&&ProjectileManager.instance.Fired.Count==1,"Death stops release");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); SceneManager.Handle=2; cast.Tick();
        Check(cast.Policy.Stopped&&ProjectileManager.instance.Fired.Count==0,"Stage change stops release");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); body.gameObject.activeInHierarchy=false; cast.Tick();
        Check(cast.Policy.Stopped,"Disable stops release");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); body.characterMotor.velocity=new Vector3(float.NaN,0,0); cast.Tick();
        Check(ProjectileManager.instance.Fired.Count==0&&cast.Policy.ConsumedDrops==1,"Nonfinite velocity suppresses drop");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); AH64BombingRunProjectiles.Prefab=null; cast.Tick();
        Check(ProjectileManager.instance.Fired.Count==0&&cast.Policy.ConsumedDrops==1,"Missing prefab consumes opportunity");
    }
    private static void Damage()
    {
        Reset(); var body=Owner(); var cast=new AH64BombingRunCast(body);
        var target=new GameObject().AddComponent<HealthComponent>(); target.netId=new NetworkInstanceId { Value=2 };
        var hurt=new GameObject().AddComponent<HurtBox>(); hurt.healthComponent=target;
        var second=new GameObject().AddComponent<HurtBox>(); second.healthComponent=target;
        BlastAttack.Hits=new[] { new BlastAttack.HitPoint { hurtBox=hurt },new BlastAttack.HitPoint { hurtBox=second } };
        var damage=new GameObject().AddComponent<AH64BombingRunDamage>();
        target.silentReject=true; damage.Detonate(cast,0,false);
        Check(target.attempts==1&&cast.Policy.HitCount(target)==0,"Dedup plus silent native rejection");
        Check(GlobalEventManager.instance.enemyHits==0,"Rejected damage has no proc callbacks");
        target.silentReject=false; target.reject=true; damage.Detonate(cast,1,false);
        Check(cast.Policy.HitCount(target)==0,"Explicit native rejection preserves budget");
        target.reject=false; target.nativeDamage=0; damage.Detonate(cast,2,false);
        Check(cast.Policy.HitCount(target)==0,"Zero report preserves budget");
        target.nativeDamage=10;
        for(int i=0;i<6;i++) damage.Detonate(cast,i,false);
        Check(cast.Policy.HitCount(target)==3&&target.attempts==6,"Only three positive damaging hits");
        Check(GlobalEventManager.instance.enemyHits==3&&GlobalEventManager.instance.allHits==3,"One pair of item callbacks per accepted hit");
        var record=traces.FindLast(x=>x.kind=="hit");
        Check(record.acceptedHits==3&&record.remainingBaseCoefficient==0&&Near(record.procCoefficient,.5f),"Trace accepted budget and proc");
        var otherCast=new AH64BombingRunCast(body); damage.Detonate(otherCast,0,true);
        Check(otherCast.Policy.HitCount(target)==1,"Casts have independent overlap budgets");
        target.BeforeDamage=()=>throw new InvalidOperationException("test native exception");
        var exceptional=new AH64BombingRunCast(body);
        try { damage.Detonate(exceptional,0,false); } catch(InvalidOperationException) { }
        Check(exceptional.Policy.TryBeginHit(target),"Exception releases target reservation");
        target.BeforeDamage=null; target.AfterDamage=()=>throw new InvalidOperationException("post-report exception");
        var positiveException=new AH64BombingRunCast(body);
        try { damage.Detonate(positiveException,0,false); } catch(InvalidOperationException) { }
        Check(positiveException.Policy.HitCount(target)==1,"Positive damage still spends budget if later native hook throws");
    }
    private static void Flight()
    {
        Reset(); var body=Owner(); var cast=new AH64BombingRunCast(body); cast.Tick();
        var bomb=ProjectileManager.instance.Bombs[0]; TickProjectile(bomb);
        Check(Near(bomb.transform.position.y,9.414f),"Ballistic gravity integrates exactly over tick");
        NetworkServer.active=false; float y=bomb.transform.position.y; TickProjectile(bomb);
        Check(Near(y,bomb.transform.position.y),"Observers never simulate projectile motion");
        NetworkServer.active=true; body.healthComponent.alive=false;
        for(int i=0;i<305;i++) TickProjectile(bomb);
        Check(bomb.destroyed,"Released payload bounded despite owner death");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); cast.Tick(); bomb=ProjectileManager.instance.Bombs[0];
        SceneManager.Handle=2; TickProjectile(bomb); Check(bomb.destroyed,"Stage loss destroys released payload");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); cast.Tick(); bomb=ProjectileManager.instance.Bombs[0];
        Physics.InsideWorld=true; TickProjectile(bomb);
        Check(bomb.destroyed&&!traces.Exists(x=>x.kind=="impact"),"Inside-world payload fizzles without wall splash");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); cast.Tick(); bomb=ProjectileManager.instance.Bombs[0];
        var wall=new GameObject().AddComponent<Collider>();
        Physics.Hits=new[] { new RaycastHit { collider=wall,distance=.04f } };
        TickProjectile(bomb); TickProjectile(bomb);
        Check(bomb.destroyed&&traces.FindAll(x=>x.kind=="impact").Count==1,"Collision detonates once");
        Reset(); body=Owner(); cast=new AH64BombingRunCast(body); cast.Tick(); bomb=ProjectileManager.instance.Bombs[0];
        body.healthComponent.alive=false;
        Physics.Hits=new[] { new RaycastHit { collider=wall,distance=.04f } };
        TickProjectile(bomb);
        Check(bomb.destroyed&&traces.Exists(x=>x.reason=="invalid-owner-at-impact"),"Owner-dead impact suppresses damage safely");
    }
    public static void Main(string[] args)
    {
        AH64BombingRunTrace.Emitted+=traces.Add;
        if(Array.IndexOf(args,"completion")>=0) CompletionChecks.Run();
        else if(Array.IndexOf(args,"terminal")>=0) TerminalLifecycleChecks.Run();
        else
        {
            Policy(); PathAndState(); Suppression(); Damage(); Flight();
            EntryChecks.Run(); TerminalLifecycleChecks.Run(); CompletionChecks.Run(); ImpactChecks.Run();
        }
        Console.WriteLine("PASS: "+assertions+" path bombing API-double assertions; native runtime unverified.");
    }
}
