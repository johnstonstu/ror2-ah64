// Purpose-built API doubles. These model inspected call ordering, not Unity/KCC or UNet.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class GameObject : Object
    {
        readonly List<Component> components = new List<Component>();
        public readonly Transform transform = new Transform();
        public T AddComponent<T>() where T : Component, new() { var c = new T { gameObject = this }; components.Add(c); return c; }
        public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component { }
    public class Transform { public Vector3 forward = Vector3.forward; }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c) { x=a;y=b;z=c; }
        public static Vector3 zero => new Vector3();
        public static Vector3 up => new Vector3(0,1,0);
        public static Vector3 forward => new Vector3(0,0,1);
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0 ? this/magnitude : zero;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b)=>a*(1/b);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 MoveTowards(Vector3 a,Vector3 b,float delta)=>(b-a).magnitude <= delta ? b : a+(b-a).normalized*delta;
    }
    public struct Quaternion
    {
        public float x,y,z,w;
        public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
        public static Quaternion identity => new Quaternion(0,0,0,1);
        public static Quaternion AngleAxis(float degrees,Vector3 axis)
        {
            var q=System.Numerics.Quaternion.CreateFromAxisAngle(new System.Numerics.Vector3(axis.x,axis.y,axis.z),degrees*Mathf.Deg2Rad);
            return new Quaternion(q.X,q.Y,q.Z,q.W);
        }
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {
            var r=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(v.x,v.y,v.z),new System.Numerics.Quaternion(q.x,q.y,q.z,q.w));
            return new Vector3(r.X,r.Y,r.Z);
        }
    }
    public static class Mathf
    {
        public const float PI=(float)Math.PI, Deg2Rad=PI/180f, Rad2Deg=180f/PI;
        public static float Min(float a,float b)=>Math.Min(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float v,float lo,float hi)=>Max(lo,Min(hi,v));
        public static float Clamp01(float v)=>Clamp(v,0,1);
        public static float Abs(float v)=>Math.Abs(v);
        public static float Sign(float v)=>v<0?-1:1;
        public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
        public static float Sin(float v)=>(float)Math.Sin(v);
        public static float MoveTowards(float a,float b,float step)=>a+Clamp(b-a,-step,step);
    }
    public static class Time { public static float fixedDeltaTime=.02f; }
}
namespace UnityEngine.Networking
{
    using UnityEngine;
    public class NetworkWriter
    {
        readonly MemoryStream stream=new MemoryStream();
        public byte[] ToArray()=>stream.ToArray();
        public void Write(float v){var b=BitConverter.GetBytes(v);stream.Write(b,0,b.Length);}
        public void Write(Vector3 v){Write(v.x);Write(v.y);Write(v.z);}
        public void Write(Quaternion v){Write(v.x);Write(v.y);Write(v.z);Write(v.w);}
    }
    public class NetworkReader
    {
        readonly BinaryReader reader;
        public NetworkReader(byte[] data){reader=new BinaryReader(new MemoryStream(data));}
        public float ReadSingle()=>reader.ReadSingle();
        public Vector3 ReadVector3()=>new Vector3(ReadSingle(),ReadSingle(),ReadSingle());
        public Quaternion ReadQuaternion()=>new Quaternion(ReadSingle(),ReadSingle(),ReadSingle(),ReadSingle());
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public static class SceneManager
    {
        public static event Action<Scene,Scene> activeSceneChanged;
        public static int Subscribers => activeSceneChanged?.GetInvocationList().Length??0;
        public static void Change()=>activeSceneChanged?.Invoke(new Scene(),new Scene());
    }
}
namespace RoR2
{
    using UnityEngine;
    public class HealthComponent : Component { public bool alive=true; }
    public class CharacterBody : Component
    {
        public HealthComponent healthComponent=new HealthComponent();
        public bool isSprinting;
    }
    public class CharacterMotor : Component
    {
        public Vector3 velocity, moveDirection;
        public bool hasEffectiveAuthority=true,isFlying=true,useGravity,useCustomGravity,disableAirControlUntilCollision,isGrounded,rejectForces;
        public float acceleration=80,airControl=1,walkSpeed=8.5f;
        public struct MovementHitInfo { public Vector3 hitNormal; }
        public delegate void MovementHitDelegate(ref MovementHitInfo hit);
        public event MovementHitDelegate onMovementHit;
        public int HitSubscribers=>onMovementHit?.GetInvocationList().Length??0;
        public void Hit(Vector3 normal){var hit=new MovementHitInfo{hitNormal=normal};onMovementHit?.Invoke(ref hit);}
    }
    public struct PhysForceInfo { public Vector3 force; }
    public class InputBankTest : Component
    {
        public Vector3 moveVector,aimDirection=Vector3.forward;
        public Button jump=new Button(),rawMoveDown=new Button();
        public class Button { public bool down; }
    }
    public class CharacterDirection : Component { public Vector3 forward=Vector3.forward; }
    public class Machine
    {
        public bool main;
        public void SetNextStateToMain(){main=true;}
    }
}
namespace EntityStates
{
    using RoR2;using UnityEngine;using UnityEngine.Networking;
    public enum InterruptPriority { Any,Skill,PrioritySkill,Pain,Stun,Death }
    public class GenericCharacterMain : Component
    {
        public bool isAuthority=true;
        public float fixedAge,moveSpeedStat=8.5f;
        protected bool jumpInputReceived;
        public Machine outer=new Machine();
        public CharacterBody characterBody=>GetComponent<CharacterBody>();
        public CharacterMotor characterMotor=>GetComponent<CharacterMotor>();
        public CharacterDirection characterDirection=>GetComponent<CharacterDirection>();
        public InputBankTest inputBank=>GetComponent<InputBankTest>();
        public int InputTicks;
        public virtual void OnEnter(){}
        public virtual void OnExit(){}
        public virtual void FixedUpdate()
        {
            fixedAge+=Time.fixedDeltaTime;
            jumpInputReceived=inputBank && inputBank.jump.down;
            HandleMovements();
            InputTicks++;
        }
        public virtual void HandleMovements()
        {
            if(characterMotor)characterMotor.moveDirection=inputBank.moveVector;
            if(isAuthority)ProcessJump();
        }
        public virtual void ProcessJump(){}
        public virtual void OnSerialize(NetworkWriter w){}
        public virtual void OnDeserialize(NetworkReader r){}
        public virtual InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Any;
    }
}
namespace EntityStates.Headstompers
{
    public class BaseHeadstompersState
    {
        public static object Current;
        public static object FindForBody(RoR2.CharacterBody body)=>Current;
    }
    public class HeadstompersFall : BaseHeadstompersState { }
}
namespace On.RoR2
{
    using UnityEngine;using global::RoR2;
    public static class CharacterMotor
    {
        public delegate void orig_PreMove(global::RoR2.CharacterMotor m,float dt);
        public delegate void hook_PreMove(orig_PreMove orig,global::RoR2.CharacterMotor m,float dt);
        public static event hook_PreMove PreMove;
        public delegate void orig_ApplyForceImpulse(global::RoR2.CharacterMotor m,ref PhysForceInfo f);
        public delegate void hook_ApplyForceImpulse(orig_ApplyForceImpulse orig,global::RoR2.CharacterMotor m,ref PhysForceInfo f);
        public static event hook_ApplyForceImpulse ApplyForceImpulse;
        public static int Hooks => (PreMove?.GetInvocationList().Length??0)+(ApplyForceImpulse?.GetInvocationList().Length??0);
        public static Action<global::RoR2.CharacterMotor> DuringNative;
        public static Vector3 NativeResult;
        public static void Pre(global::RoR2.CharacterMotor motor,float dt)
        {
            orig_PreMove chain=(m,t)=>
            {
                if(!m.hasEffectiveAuthority)return;
                float accel=m.acceleration*(m.isGrounded?1:m.airControl);
                var target=m.moveDirection*m.walkSpeed;
                if(!m.isFlying)target.y=m.velocity.y;
                m.velocity=Vector3.MoveTowards(m.velocity,target,accel*t);
                if(m.useGravity)m.velocity.y-=9.81f*t;
                DuringNative?.Invoke(m);
                NativeResult=m.velocity;
            };
            if(PreMove!=null)foreach(hook_PreMove h in PreMove.GetInvocationList()){var inner=chain;chain=(m,t)=>h(inner,m,t);}
            chain(motor,dt);
        }
        public static void Force(global::RoR2.CharacterMotor motor,Vector3 force)
        {
            orig_ApplyForceImpulse chain=(global::RoR2.CharacterMotor m,ref PhysForceInfo f)=>{if(!m.rejectForces)m.velocity+=f.force;};
            if(ApplyForceImpulse!=null)foreach(hook_ApplyForceImpulse h in ApplyForceImpulse.GetInvocationList())
            {var inner=chain;chain=(global::RoR2.CharacterMotor m,ref PhysForceInfo f)=>h(inner,m,ref f);}
            var info=new PhysForceInfo{force=force};chain(motor,ref info);
        }
    }
}
namespace AH64.Survivors
{
    public static class AH64PlaytestConfig { public static bool ClassicAltitude; }
}
namespace AH64.Survivors.Components
{
    using UnityEngine;
    internal class AH64HoverController : Component
    {
        public int Calls,Taps; public float Airtime=10; public bool LastJump,LastDescend;
        public Action Movement;
        public void ApplyHover(bool jump,bool descend,float dt)
        {
            Calls++;LastJump=jump;LastDescend=descend;
            if(jump)Airtime-=dt;
            var m=GetComponent<RoR2.CharacterMotor>();
            var v=m.moveDirection;v.y=descend?-1:jump?1:.3f;m.moveDirection=v;
            Movement?.Invoke();
        }
        public void OnCollectiveTapped(){Taps++;}
    }
    internal class AH64FlightVisuals : Component
    {
        public int Captures;
        public Quaternion CaptureAttitude(){Captures++;return new Quaternion(.1f,.2f,.3f,.9f);}
    }
    internal static class AH64DescendInput
    {
        public static bool Held;
        public static bool IsHeld(RoR2.CharacterBody body)=>Held;
    }
}
