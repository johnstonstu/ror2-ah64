// API doubles for offline math, capture and state ownership. No game/runtime proof.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o) => o != null; }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public GameObject(){transform=new Transform{gameObject=this};}
        public bool activeSelf=true;public void SetActive(bool value){activeSelf=value;}
        public T AddComponent<T>() where T:Component,new() { var c=new T{gameObject=this}; components.Add(c); return c; }
        public T GetComponent<T>() where T:Component => components.OfType<T>().FirstOrDefault();
    }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T:Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour:Component { }
    public class Collider:Object { }
    public class Transform:Component
    {
        public Vector3 position,forward=Vector3.forward,up=Vector3.up;
        public Quaternion rotation=Quaternion.identity;
        public int PositionAndRotationWrites;
        public void SetPositionAndRotation(Vector3 p,Quaternion q){position=p;rotation=q;PositionAndRotationWrites++;}
    }
    public partial struct Quaternion
    {
        public float x,y,z,w;
        public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}
        public static Quaternion identity => new Quaternion(0,0,0,1);
    }
    public partial struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();
        public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 forward=>new Vector3(0,0,1);
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>0?this/magnitude:zero;
        public void Normalize(){this=normalized;}
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a)=>zero-a;
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);
        public static bool operator ==(Vector3 a,Vector3 b)=>(a-b).sqrMagnitude<1e-10f;
        public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
        public override bool Equals(object o)=>o is Vector3 v && this==v;
        public override int GetHashCode()=>HashCode.Combine(x,y,z);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 MoveTowards(Vector3 current,Vector3 target,float maxDelta)
        {var d=target-current; return d.magnitude<=maxDelta?target:current+d.normalized*maxDelta;}
    }
    public static partial class Mathf
    {
        public const float PI=(float)Math.PI;
        public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(b,x));
        public static float Clamp01(float x)=>Clamp(x,0,1);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Abs(float x)=>Math.Abs(x);
        public static float Cos(float x)=>(float)Math.Cos(x);
        public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float MoveTowards(float a,float b,float max)=>a+Clamp(b-a,-max,max);
    }
    public static class Random
    {public static Vector3 insideUnitSphere=>Vector3.zero;public static float value=>.5f;public static float Range(float a,float b)=>a;}
}
namespace UnityEngine.Networking
{
    using UnityEngine;
    public static class NetworkServer { public static bool active; }
    public class NetworkWriter
    {
        private readonly MemoryStream stream=new MemoryStream(); private readonly BinaryWriter writer;
        public NetworkWriter(){writer=new BinaryWriter(stream);}
        public void Write(float v)=>writer.Write(v);
        public void Write(Vector3 v){Write(v.x);Write(v.y);Write(v.z);}
        public void Write(Quaternion q){Write(q.x);Write(q.y);Write(q.z);Write(q.w);}
        public byte[] ToArray()=>stream.ToArray();
    }
    public class NetworkReader
    {
        private readonly BinaryReader reader;
        public NetworkReader(byte[] bytes){reader=new BinaryReader(new MemoryStream(bytes));}
        public float ReadSingle()=>reader.ReadSingle();
        public Vector3 ReadVector3()=>new Vector3(ReadSingle(),ReadSingle(),ReadSingle());
        public Quaternion ReadQuaternion()=>new Quaternion(ReadSingle(),ReadSingle(),ReadSingle(),ReadSingle());
    }
}
namespace RoR2
{
    using UnityEngine;
    public struct PhysForceInfo { public Vector3 force; }
    public class CharacterMotor:Component
    {
        public Vector3 velocity,moveDirection;public float acceleration=100,walkSpeed=12;public bool isAirControlForced; public bool hasEffectiveAuthority=true,isGrounded,disableAirControlUntilCollision,useCustomGravity;
        public float airControl=1; public bool isFlying=true,useGravity,rejectForces; public float capsuleYOffset,capsuleHeight=2;
        public readonly Kcc Motor=new Kcc();
        public class Kcc
        {
            public bool mustUnground;public int UngroundCalls;
            // Actual KCC only queues unground here; stable status survives until after PreMove.
            public void ForceUnground(){mustUnground=true;UngroundCalls++;}
        }
        public void PhysicsStep(float dt)
        {
            On.RoR2.CharacterMotor.RunPreMove(this,dt);
            if(Motor.mustUnground){isGrounded=false;Motor.mustUnground=false;}
            // Simplified stable-floor projection, not KCC geometry/sweeps/timer emulation.
            if(isGrounded)velocity.y=0;
        }
        public struct MovementHitInfo { public Vector3 hitNormal; }
        public delegate void MovementHitDelegate(ref MovementHitInfo info);
        public event MovementHitDelegate onMovementHit;
        public int HitSubscribers=>onMovementHit?.GetInvocationList().Length??0;
        public void Hit(Vector3 normal){var hit=new MovementHitInfo{hitNormal=normal};onMovementHit?.Invoke(ref hit);}
    }
    public class CharacterBody:Component
    {
        public bool isSprinting,hasCloakBuff;public HealthComponent healthComponent;
        public readonly List<(object buff,float duration)> buffs=new List<(object,float)>();
        public void AddTimedBuff(object buff,float duration)=>buffs.Add((buff,duration));
    }
    public class InputBankTest:Component { public Vector3 moveVector,aimDirection=Vector3.forward; public Button jump=new Button();public class Button{public bool down;} }
    public class CharacterDirection:Component { public Vector3 forward=Vector3.forward,moveVector; }
    public class CameraTargetParams:Component { public float fovOverride=-1; }
    public static class RoR2Content { public static class Buffs { public static object HiddenInvincibility=new object(),Cloak=new object(); } }
    public static class Util
    {
        public static readonly List<string> sounds=new List<string>();public static void PlaySound(string s,GameObject o){sounds.Add(s);}
        // Horizontal heading only: sufficient for these presentation tests, not native 3D look rotation.
        public static Quaternion QuaternionSafeLookRotation(Vector3 v)=>Quaternion.Euler(0,(float)(Math.Atan2(v.x,v.z)*180/Math.PI),0);
        public static Quaternion QuaternionSafeLookRotation(Vector3 v,Vector3 up)=>QuaternionSafeLookRotation(v);
    }
    public class EffectData { public Vector3 origin;public float scale;public Quaternion rotation; }
    public static class EffectManager { public static void SpawnEffect(GameObject g,EffectData d,bool network){} }
    public class EntityStateMachine:Component
    {
        public EntityStates.EntityState state;
        public static EntityStateMachine FindByCustomName(GameObject g,string name)=>g.GetComponent<EntityStateMachine>();
    }
}
namespace On.RoR2
{
    using global::RoR2;
    public static class CharacterMotor
    {
        public delegate void orig_ApplyForceImpulse(global::RoR2.CharacterMotor self,ref PhysForceInfo info);
        public delegate void hook_ApplyForceImpulse(orig_ApplyForceImpulse orig,global::RoR2.CharacterMotor self,ref PhysForceInfo info);
        public static event hook_ApplyForceImpulse ApplyForceImpulse;
        public static int Subscribers=>ApplyForceImpulse?.GetInvocationList().Length??0;
        public delegate void orig_PreMove(global::RoR2.CharacterMotor self,float dt);
        public delegate void hook_PreMove(orig_PreMove orig,global::RoR2.CharacterMotor self,float dt);
        public static event hook_PreMove PreMove;
        public static int PreMoveSubscribers=>PreMove?.GetInvocationList().Length??0;
        public static void RunPreMove(global::RoR2.CharacterMotor self,float dt)
        {
            // Source model of installed CharacterMotor.PreMove's authority/ground/air branch.
            orig_PreMove chain=(m,step)=>
            {
                if(!m.hasEffectiveAuthority)return;
                float accel=m.acceleration;
                if(m.isAirControlForced || !m.isGrounded)
                    accel*=m.disableAirControlUntilCollision && m.useGravity?0:m.airControl;
                var target=m.moveDirection*m.walkSpeed;
                if(!m.isFlying)target.y=m.velocity.y;
                m.velocity=UnityEngine.Vector3.MoveTowards(m.velocity,target,accel*step);
                if(m.useGravity)m.velocity.y-=9.81f*step;
            };
            if(PreMove!=null)foreach(hook_PreMove hook in PreMove.GetInvocationList())
            {var inner=chain;chain=(m,step)=>hook(inner,m,step);}
            chain(self,dt);
        }
        public static void Force(global::RoR2.CharacterMotor self,Vector3Proxy force)
        {
            var info=new PhysForceInfo{force=force.Value};
            orig_ApplyForceImpulse chain=(global::RoR2.CharacterMotor m,ref PhysForceInfo f)=>{if(!m.rejectForces)m.velocity+=f.force;};
            if(ApplyForceImpulse!=null) foreach(hook_ApplyForceImpulse hook in ApplyForceImpulse.GetInvocationList())
            {var inner=chain;chain=(global::RoR2.CharacterMotor m,ref PhysForceInfo f)=>hook(inner,m,ref f);}
            chain(self,ref info);
        }
    }
    public partial struct Vector3Proxy { public UnityEngine.Vector3 Value;public static implicit operator Vector3Proxy(UnityEngine.Vector3 v)=>new Vector3Proxy{Value=v}; }
}
namespace EntityStates
{
    using RoR2;using UnityEngine;using UnityEngine.Networking;
    public enum InterruptPriority { Any,Skill,PrioritySkill,Pain,Taunt,Stun,Immobilize,Frozen,Vehicle,Death }
    public class EntityState:Component
    {
        public virtual InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Any;
    }
    public class BaseSkillState:EntityState
    {
        public bool isAuthority; public float moveSpeedStat=12,fixedAge;
        public CharacterMotor characterMotor=>GetComponent<CharacterMotor>();
        public CharacterBody characterBody=>GetComponent<CharacterBody>();
        public InputBankTest inputBank=>GetComponent<InputBankTest>();
        public CharacterDirection characterDirection=>GetComponent<CharacterDirection>();
        public readonly Machine outer=new Machine();public class Machine{public bool main;public void SetNextStateToMain(){main=true;}}
        public virtual void OnEnter(){} public virtual void OnExit(){} public virtual void FixedUpdate(){fixedAge+=0.02f;}
        public virtual void OnSerialize(NetworkWriter w){} public virtual void OnDeserialize(NetworkReader r){}
        public override InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Any;
        public float GetDeltaTime()=>0.02f;
    }
}
namespace EntityStates.Commando { public static class DodgeState { public static float dodgeFOV=70; } }
namespace EntityStates.Headstompers
{
    public class BaseHeadstompersState {public static object current;public static object FindForBody(RoR2.CharacterBody b)=>current;}
    public class HeadstompersFall:BaseHeadstompersState{}
}
namespace AH64.Survivors.Components
{
    using UnityEngine;
    internal class AH64HoverController:Component
    {public float AscentPitchWeight=0;public bool IsAscending=false;public int limits,bumps; public float allowance=3;public float LimitUtilityClimb(float h){limits++;return allowance;}public void BumpTargetHeight(float h){bumps++;}}
    #if !VISUALS
    public class AH64FlightVisuals:Component
    {
        public int captures,plays,ends;public Quaternion attitude=new Quaternion(.1f,.2f,.3f,.9f),played;
        public Quaternion CaptureAttitude(){captures++;return attitude;}
        public void PlayBarrelRoll(object owner,float sign,float duration,Quaternion q){plays++;played=q;}
        public void PlayBackflip(object owner,float duration,Quaternion q){plays++;played=q;}
        public void SetManeuverProgress(object owner,float age){}
        public void EndManeuver(object owner){ends++;}
    }
    #endif
}
namespace AH64.Survivors
{
    public static class AH64PlaytestConfig { public static bool RotorWashEnabled; public static float DashPeakSpeed=2.35f,DashRampFraction=.28f,DashClimbHeight=10; }
    public static class AH64Buffs {public static object platingBuff=new object();}
    public static class AH64Assets
    {public static UnityEngine.GameObject dashThrusterEffect,dashDustEffect,SmokePuffEffect,dashFlareEffect,hydraMuzzleFlashEffect,RotorWashEffect;}
}
