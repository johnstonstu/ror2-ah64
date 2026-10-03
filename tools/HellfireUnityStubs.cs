using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using N = System.Numerics;

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) => value != null; }
    public class GameObject : Object
    {
        private readonly List<Component> components = new List<Component>();
        public readonly Transform transform;
        public bool authority;
        public GameObject() { transform = new Transform { gameObject = this }; components.Add(transform); }
        public T AddComponent<T>() where T : Component, new()
        { var c = new T { gameObject = this }; components.Add(c); Lifecycle.Call(c, "Awake"); Lifecycle.Call(c,"OnEnable");return c; }
        public T GetComponent<T>() => components.OfType<T>().FirstOrDefault();
        public T[] GetComponents<T>() => components.OfType<T>().ToArray();
        public T[] GetComponentsInChildren<T>(bool includeInactive) => GetComponents<T>();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() => gameObject.GetComponent<T>();
        public T[] GetComponents<T>() => gameObject.GetComponents<T>();
    }
    public class MonoBehaviour : Component
    { public bool enabled = true; public bool isActiveAndEnabled => enabled; }
    public class Transform : Component
    {
        public Vector3 position;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 forward => rotation * Vector3.forward;
        public bool IsChildOf(Transform other) => this == other;
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new Vector3();
        public static Vector3 forward => new Vector3(0,0,1);
        public static Vector3 up => new Vector3(0,1,0);
        public float sqrMagnitude => x*x+y*y+z*z;
        public float magnitude => MathF.Sqrt(sqrMagnitude);
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b) => new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator -(Vector3 a) => a*-1;
        public static Vector3 operator *(Vector3 a,float b) => new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a,float b) => a*(1/b);
        public static float Dot(Vector3 a,Vector3 b) => a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b) => new Vector3(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static float Angle(Vector3 a,Vector3 b) => MathF.Acos(Math.Clamp(Dot(a.normalized,b.normalized),-1,1))*180/MathF.PI;
        public static Vector3 RotateTowards(Vector3 current,Vector3 target,float radians,float magnitudeDelta)
        {
            float angle = Angle(current,target)*MathF.PI/180;
            if (angle <= radians) return target;
            Vector3 axis = Cross(current,target).normalized;
            if (axis.sqrMagnitude < 0.001f) axis=Cross(current, MathF.Abs(current.y)<0.9f?up:new Vector3(1,0,0)).normalized;
            return Quaternion.AngleAxis(radians*180/MathF.PI,axis)*current;
        }
        public static Vector3 MoveTowards(Vector3 current,Vector3 target,float distance)
        {
            Vector3 delta=target-current;
            return delta.magnitude<=distance?target:current+delta/delta.magnitude*distance;
        }
    }
    public struct Quaternion
    {
        internal N.Quaternion q;
        public static Quaternion identity => new Quaternion { q=N.Quaternion.Identity };
        public static Quaternion AngleAxis(float degrees,Vector3 axis) => new Quaternion
            { q=N.Quaternion.CreateFromAxisAngle(new N.Vector3(axis.x,axis.y,axis.z),degrees*MathF.PI/180) };
        public static Quaternion operator *(Quaternion a,Quaternion b) => new Quaternion { q=a.q*b.q };
        public static Vector3 operator *(Quaternion a,Vector3 b)
        { var v=N.Vector3.Transform(new N.Vector3(b.x,b.y,b.z),a.q);return new Vector3(v.X,v.Y,v.Z); }
    }
    public struct Ray
    {
        public Vector3 origin,direction;
        public Ray(Vector3 origin,Vector3 direction) { this.origin=origin;this.direction=direction.normalized; }
        public Vector3 GetPoint(float distance) => origin+direction*distance;
    }
    public static class Mathf
    {
        public const float Deg2Rad = MathF.PI/180;
        public const float Rad2Deg = 180/MathF.PI;
        public static float Atan2(float y,float x)=>MathF.Atan2(y,x);
        public static float Sqrt(float value)=>MathF.Sqrt(value);
        public static float Min(float a,float b) => MathF.Min(a,b);
        public static float Max(float a,float b) => MathF.Max(a,b);
        public static float MoveTowards(float a,float b,float delta) => MathF.Abs(b-a)<=delta?b:a+MathF.Sign(b-a)*delta;
    }
    public struct Bounds { public Vector3 extents; }
    public class Collider : Component
    {
        public Bounds bounds=new Bounds { extents=new Vector3(0.2f,0.2f,0.2f) };
        public Vector3 ClosestPoint(Vector3 point)=>point;
    }
    public enum CollisionDetectionMode { Discrete,ContinuousDynamic }
    public class Rigidbody : Component { public Vector3 velocity; public CollisionDetectionMode collisionDetectionMode; }
    public enum QueryTriggerInteraction { Collide }
    public struct RaycastHit { public float distance; public Collider collider; public Vector3 point,normal; }
    public static class Physics
    {
        public static RaycastHit[] Hits = Array.Empty<RaycastHit>();
        public static int Queries,Ignored;
        public static int CollisionQueries;
        public static Collider[] Overlaps=Array.Empty<Collider>();
        public static RaycastHit[] Sweeps=Array.Empty<RaycastHit>();
        public static RaycastHit[] RaycastAll(Ray ray,float distance,int mask,QueryTriggerInteraction triggers)
        { Queries++;return Hits; }
        public static void IgnoreCollision(Collider a,Collider b,bool ignore) { if(ignore) Ignored++; }
        public static Collider[] OverlapSphere(Vector3 position,float radius,int mask,QueryTriggerInteraction query)
        { CollisionQueries++;return Overlaps; }
        public static RaycastHit[] SphereCastAll(Vector3 position,float radius,Vector3 direction,float distance,int mask,QueryTriggerInteraction query)
        { CollisionQueries++;return Sweeps; }
    }
    public static class Time { public static float fixedTime; public static float fixedDeltaTime=0.02f; }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrderAttribute : Attribute
    { public DefaultExecutionOrderAttribute(int order) { } }
    public static class Lifecycle
    {
        public static void Call(object component,string name)
        { component.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(component,null); }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public static class SceneManager
    {
        public static event Action<Scene,Scene> activeSceneChanged;
        public static void Change()=>activeSceneChanged?.Invoke(new Scene(),new Scene());
    }
}

namespace RoR2
{
    using UnityEngine;
    public class HealthComponent : Component { public bool alive=true; }
    public class HurtBox : Component { public HealthComponent healthComponent; }
    public class InputBankTest : Component
    {
        public Vector3 aimOrigin,aimDirection=Vector3.forward;
        public ButtonState skill4=new ButtonState();
        public class ButtonState { public bool down=true; }
        public Ray GetAimRay() => new Ray(aimOrigin,aimDirection);
    }
    public class CharacterBody : Component
    { public InputBankTest inputBank;public HealthComponent healthComponent;public Inventory inventory=new Inventory(); }
    public class Inventory : Object { public int count; public int GetItemCountEffective(object item) => count; }
    public static class DLC1Content { public static class Items { public static object MoreMissile=new object(); } }
    public class EntityStateMachine : Component
    {
        public EntityStates.EntityState state=new EntityStates.EntityState();
        public static EntityStateMachine FindByCustomName(GameObject owner,string name) => owner.GetComponent<EntityStateMachine>();
    }
    public struct LayerIndex
    { public int mask; public static LayerIndex world => new LayerIndex { mask=1 };public static LayerIndex entityPrecise=>new LayerIndex { mask=2 }; }
    public static class Util
    {
        public static bool HasEffectiveAuthority(GameObject owner) => owner.authority;
        public static Quaternion QuaternionSafeLookRotation(Vector3 forward)
        {
            forward=forward.normalized;
            Vector3 axis=Vector3.Cross(Vector3.forward,forward).normalized;
            if(axis.sqrMagnitude<0.001f) axis=Vector3.up;
            return Quaternion.AngleAxis(Vector3.Angle(Vector3.forward,forward),axis);
        }
    }
    public struct DamageTypeCombo { public int source; public static DamageTypeCombo GenericSpecial => new DamageTypeCombo { source=4 }; }
}

namespace RoR2.Projectile
{
    using UnityEngine;
    public struct ProjectileImpactInfo { public Collider collider;public Vector3 estimatedPointOfImpact,estimatedImpactNormal; }
    public interface IProjectileImpactBehavior { void OnProjectileImpact(ProjectileImpactInfo impact); }
    public interface IProjectileImpactFilter { bool PassesFilters(ProjectileImpactInfo impact); }
    public struct FireProjectileInfo
    { public GameObject projectilePrefab,owner;public Vector3 position;public Quaternion rotation;public float damage,force;public bool crit;public byte comboNumber;public RoR2.DamageTypeCombo? damageTypeOverride; }
    public class ProjectileController : Component
    {
        public bool allowPrediction=true,authorityHandlesCollisionEvents=true;
        public GameObject owner;
        public byte combo;
        public event Action<ProjectileController> onInitialized;
        public void Initialize() => onInitialized?.Invoke(this);
        public void NativeImpact(ProjectileImpactInfo impact)
        {
            foreach(var filter in GetComponents<IProjectileImpactFilter>())
                if(!filter.PassesFilters(impact))return;
            foreach(var behavior in GetComponents<IProjectileImpactBehavior>())behavior.OnProjectileImpact(impact);
        }
    }
    public class ProjectileSimple : MonoBehaviour { public float desiredForwardSpeed=140; }
    public class ProjectileNetworkTransform : Component
    { public bool checkForLocalPlayerAuthority,allowClientsideCollision;public float positionTransmitInterval; }
    public class ProjectileManager
    {
        public static ProjectileManager instance=new ProjectileManager();
        public readonly List<FireProjectileInfo> Fired=new List<FireProjectileInfo>();
        public void FireProjectile(FireProjectileInfo info) => Fired.Add(info);
    }
}

namespace EntityStates
{
    using UnityEngine;
    using RoR2;
    using RoR2.Projectile;
    public enum InterruptPriority { Any,Skill,PrioritySkill,Pain,Frozen,Death }
    public class EntityState
    { public InterruptPriority priority;public virtual InterruptPriority GetMinimumInterruptPriority()=>priority; }
    public class GenericProjectileBaseState : EntityState
    {
        public GameObject gameObject,projectilePrefab,effectPrefab;
        public CharacterBody characterBody;
        public string targetMuzzle,attackSoundString;
        public bool isAuthority,firedProjectile;
        public float baseDuration,baseDelayBeforeFiringProjectile,damageCoefficient,force,recoilAmplitude,bloom;
        public float stopwatch,duration;
        public virtual void OnEnter() { duration=baseDuration; }
        public virtual void OnExit() { }
        public virtual void ModifyProjectileInfo(ref FireProjectileInfo info) { }
        public virtual void FireProjectile()
        {
            if (!isAuthority)return;
            var info=new FireProjectileInfo { projectilePrefab=projectilePrefab,owner=gameObject,
                position=GetAimRay().origin,rotation=Util.QuaternionSafeLookRotation(GetAimRay().direction),damage=damageCoefficient*10,crit=true,force=force };
            ModifyProjectileInfo(ref info);ProjectileManager.instance.FireProjectile(info);
        }
        public void DoFireEffects() { }
        public object GetModelChildLocator() => null;
        public Ray GetAimRay() => characterBody.inputBank.GetAimRay();
    }
}

namespace AH64.Survivors
{
    using UnityEngine;
    public static class AH64StaticValues
    {
        public const float hellfireDamageCoefficient=13.5f,hellfireLifetime=8f;
        public const float hellfireIcbmFanAngle=25f,kickHellfirePitch=1f,kickHellfireRoll=1f;
    }
    public static class AH64Assets { public static GameObject hellfireProjectilePrefab=new GameObject(),hellfireMuzzleFlashEffect; }
}
namespace AH64.Survivors.Components
{ public static class AH64FlightVisuals { public static void Kick(UnityEngine.GameObject owner,float pitch,float roll) { } } }
namespace AH64.Survivors.SkillStates
{
    using UnityEngine;
    public sealed class ServoDash : EntityStates.EntityState
    { public ServoDash() { priority = EntityStates.InterruptPriority.Pain; } }
    public sealed class SmokeBackflip : EntityStates.EntityState
    { public SmokeBackflip() { priority = EntityStates.InterruptPriority.Pain; } }
    public static class AH64Muzzles
    {
        public const string MissileL="left",MissileR="right";
        public static Vector3 rail=new Vector3(0.78f,0,0);
        public static string ResolveName(object locator,string name)=>name;
        public static Vector3 Origin(object locator,string name,Ray ray)=>rail;
    }
}
