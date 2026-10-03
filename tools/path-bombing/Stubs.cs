// Deliberately small API doubles, not native physics, networking or item acceptance evidence.
using System;
using System.Collections.Generic;
using AH64.Survivors.Components;

namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static implicit operator bool(Object value) => value != null && !value.destroyed;
        public static void Destroy(Object value) { value.destroyed = true; }
    }
    public class GameObject : Object
    {
        public static Type RejectComponent;
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public bool activeInHierarchy = true;
        public Transform transform;
        public GameObject() { transform = AddComponent<Transform>(); }
        public T AddComponent<T>() where T : Component, new()
        {
            if(typeof(T)==RejectComponent) return null;
            T value = new T { gameObject = this };
            components[typeof(T)] = value;
            return value;
        }
        public void RemoveComponent<T>() where T:Component { components.Remove(typeof(T)); }
        public void RestoreComponent(Component value) { components[value.GetType()]=value; }
        public T GetComponent<T>() where T : class
        {
            foreach (Component value in components.Values) if (value is T result) return result;
            return null;
        }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        public bool enabled=true;
        public bool isActiveAndEnabled=>enabled&&gameObject.activeInHierarchy;
    }
    public class Transform : Component { public Vector3 position; public Quaternion rotation; }
    public class Collider : Component { public bool isTrigger; }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => default(Vector3);
        public static Vector3 down => new Vector3(0,-1,0);
        public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z);
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x*b,a.y*b,a.z*b);
        public static Vector3 operator /(Vector3 a, float b) => a*(1/b);
        public static Vector3 ClampMagnitude(Vector3 a, float limit) => a.magnitude > limit ? a.normalized*limit : a;
    }
    public struct Quaternion { }
    public static class Time { public static float fixedTime; public static float fixedDeltaTime=0.02f; }
    public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
    public enum QueryTriggerInteraction { Ignore, Collide }
    public struct RaycastHit { public Collider collider; public float distance; }
    public static class Physics
    {
        public static bool OriginBlocked, InsideWorld;
        public static RaycastHit[] Hits = Array.Empty<RaycastHit>();
        public static bool CheckCapsule(Vector3 a,Vector3 b,float r,int mask,QueryTriggerInteraction q)=>OriginBlocked;
        public static bool CheckSphere(Vector3 a,float r,int mask,QueryTriggerInteraction q)=>InsideWorld;
        public static RaycastHit[] SphereCastAll(Vector3 a,float r,Vector3 dir,float distance,int mask,QueryTriggerInteraction q)=>Hits;
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public int handle; }
    public static class SceneManager { public static int Handle=1; public static Scene GetActiveScene()=>new Scene { handle=Handle }; }
}
namespace UnityEngine.Networking
{
    public struct NetworkInstanceId { public uint Value; }
    public class NetworkIdentity : UnityEngine.Component
    {
        public bool hasAuthority;
        public NetworkInstanceId netId;
        public NetworkConnection clientAuthorityOwner;
    }
    public class MessageBase
    {
        public virtual void Serialize(NetworkWriter writer) { }
        public virtual void Deserialize(NetworkReader reader) { }
    }
    public class NetworkWriter
    {
        public readonly List<object> Values=new List<object>();
        public void Write(uint value) { Values.Add(value); }
        public void Write(byte value) { Values.Add(value); }
        public void Write(NetworkInstanceId value) { Values.Add(value); }
    }
    public class NetworkReader
    {
        private readonly object[] values;
        private int index;
        public NetworkReader(object[] values) { this.values=values; }
        public uint ReadUInt32()=>(uint)values[index++];
        public byte ReadByte()=>(byte)values[index++];
        public NetworkInstanceId ReadNetworkId()=>(NetworkInstanceId)values[index++];
    }
    public delegate void NetworkMessageDelegate(NetworkMessage message);
    public class NetworkMessage
    {
        public NetworkConnection conn;
        public NetworkReader reader;
        public T ReadMessage<T>() where T:MessageBase
        {
            var result=(T)Activator.CreateInstance(typeof(T),true);
            result.Deserialize(reader); return result;
        }
    }
    public class NetworkConnection
    {
        public sealed class Packet { public short Id; public object[] Values; }
        public readonly List<Packet> Packets=new List<Packet>();
        public bool AcceptSends=true;
        public int Attempts;
        public bool SendByChannel(short id,MessageBase message,int channel)
        {
            Attempts++; if(!AcceptSends) return false;
            var writer=new NetworkWriter(); message.Serialize(writer);
            Packets.Add(new Packet { Id=id,Values=writer.Values.ToArray() }); return true;
        }
    }
    public class NetworkClient
    {
        public static readonly List<NetworkClient> allClients=new List<NetworkClient>();
        public readonly Dictionary<short,NetworkMessageDelegate> handlers=new Dictionary<short,NetworkMessageDelegate>();
        public NetworkConnection connection=new NetworkConnection();
        public bool isConnected=true;
        public void RegisterHandler(short id,NetworkMessageDelegate handler) { handlers.Add(id,handler); }
        public void UnregisterHandler(short id) { handlers.Remove(id); }
    }
    public static class ClientScene
    {
        public static readonly Dictionary<uint,UnityEngine.GameObject> Objects=new Dictionary<uint,UnityEngine.GameObject>();
        public static UnityEngine.GameObject FindLocalObject(NetworkInstanceId id)=>Objects.TryGetValue(id.Value,out var obj)?obj:null;
    }
    public static class NetworkServer
    {
        public static bool active=true;
        public static void Destroy(UnityEngine.GameObject value) { value.destroyed=true; }
    }
}
namespace RoR2.Networking
{
    public static class NetworkManagerSystem
    {
        public static event Action<UnityEngine.Networking.NetworkClient> onStartClientGlobal;
        public static void StartClient(UnityEngine.Networking.NetworkClient client) { onStartClientGlobal?.Invoke(client); }
    }
    public struct QosChannelIndex { public int intVal; public static QosChannelIndex defaultReliable=>new QosChannelIndex(); }
}
namespace RoR2
{
    using UnityEngine;
    using UnityEngine.Networking;
    public class CharacterBody : MonoBehaviour
    {
        public float damage=10f;
        public NetworkInstanceId netId;
        public HealthComponent healthComponent;
        public CharacterMotor characterMotor;
        public Vector3 corePosition => transform.position;
        public int critRolls;
        public bool authority=true;
        public bool RollCrit() { return ++critRolls % 2 == 0; }
    }
    public class CharacterMotor : Object { public Vector3 velocity; }
    public class Run : Object { public static Run instance=new Run(); }
    public enum TeamIndex { Player, Monster }
    public static class TeamComponent
    {
        public static Action BeforeLookup;
        public static TeamIndex GetObjectTeam(GameObject value) { BeforeLookup?.Invoke(); return TeamIndex.Player; }
    }
    public class HealthComponent : MonoBehaviour
    {
        public bool alive=true;
        public bool reject, silentReject;
        public float nativeDamage=10;
        public Action BeforeDamage;
        public Action AfterDamage;
        public int attempts;
        public NetworkInstanceId netId;
        public void TakeDamage(DamageInfo info)
        {
            attempts++;
            BeforeDamage?.Invoke();
            if (silentReject) return;
            info.rejected=reject;
            if (!reject) info.inflictor.GetComponent<IOnDamageInflictedServerReceiver>()?.OnDamageInflictedServer(
                new DamageReport { damageInfo=info, victim=this, damageDealt=nativeDamage });
            AfterDamage?.Invoke();
        }
    }
    public class HurtBox : MonoBehaviour { public HealthComponent healthComponent; public int damageModifier; }
    public enum AttackerFiltering { NeverHitSelf }
    public enum DamageColorIndex { Default }
    public enum DamageType { AOE }
    public enum DamageSource { None=0, Special=4 }
    public struct DamageTypeCombo
    {
        public DamageSource damageSource;
        public DamageType damageType;
        public static DamageTypeCombo GenericSpecial=>new DamageTypeCombo();
        public static DamageTypeCombo operator |(DamageTypeCombo a,DamageType b)=>a;
    }
    public struct ProcChainMask { }
    public class DamageInfo
    {
        public GameObject attacker,inflictor;
        public float damage,procCoefficient;
        public bool crit,rejected;
        public Vector3 position;
        public DamageTypeCombo damageType;
        public ProcChainMask procChainMask;
        public HurtBox inflictedHurtbox;
        public DamageColorIndex damageColorIndex;
        public void ModifyDamageInfo(int modifier) { }
    }
    public class DamageReport { public DamageInfo damageInfo; public HealthComponent victim; public float damageDealt; }
    public interface IOnDamageInflictedServerReceiver { void OnDamageInflictedServer(DamageReport report); }
    public class GlobalEventManager : Object
    {
        public static GlobalEventManager instance=new GlobalEventManager();
        public int enemyHits,allHits;
        public Action EnemyCallback;
        public void OnHitEnemy(DamageInfo info,GameObject target) { enemyHits++; EnemyCallback?.Invoke(); }
        public void OnHitAll(DamageInfo info,GameObject target) { allHits++; }
    }
    public class BlastAttack
    {
        public struct HitPoint { public HurtBox hurtBox; public Vector3 hitPosition; }
        public struct Result { public HitPoint[] hitPoints; }
        public enum LoSType { NearestHit }
        public GameObject attacker;
        public TeamIndex teamIndex;
        public AttackerFiltering attackerFiltering;
        public Vector3 position;
        public float radius;
        public LoSType losType;
        public static HitPoint[] Hits=Array.Empty<HitPoint>();
        public Result FireNoDamage()=>new Result { hitPoints=Hits };
    }
    public static class FriendlyFireManager
    {
        public static bool Allow=true;
        public static bool ShouldSplashHitProceed(HealthComponent h,TeamIndex t)=>Allow;
    }
    public struct LayerIndex
    {
        public int mask;
        public static LayerIndex world=>new LayerIndex { mask=1 };
        public static LayerIndex entityPrecise=>new LayerIndex { mask=2 };
    }
    public static class Util
    {
        public static Quaternion QuaternionSafeLookRotation(Vector3 v)=>new Quaternion();
        public static bool HasEffectiveAuthority(GameObject obj)
        {
            var body=obj.GetComponent<CharacterBody>();
            if(body) return body.authority;
            var identity=obj.GetComponent<NetworkIdentity>();
            return identity&&(identity.hasAuthority||(NetworkServer.active&&identity.clientAuthorityOwner==null));
        }
    }
    public class EffectData { public Vector3 origin; public float scale; }
    public static class EffectManager
    {
        public static int calls;
        public static Action Callback;
        public static void SpawnEffect(GameObject o,EffectData d,bool transmit) { calls++; Callback?.Invoke(); }
    }
    public class EntityStateMachine : Component
    {
        public EntityStates.BaseSkillState state;
        public bool exited,pending;
        public void SetNextStateToMain() { exited=true; pending=true; }
        public bool HasPendingState()=>pending;
        public static EntityStateMachine FindByCustomName(GameObject obj,string name)=>obj.GetComponent<EntityStateMachine>();
    }
}
namespace RoR2.Projectile
{
    using UnityEngine;
    public struct FireProjectileInfo
    {
        public GameObject projectilePrefab,owner;
        public Vector3 position;
        public Quaternion rotation;
        public float damage,force;
        public bool crit;
        public DamageTypeCombo damageTypeOverride;
    }
    public class ProjectileManager : Object
    {
        public static Action BeforeSpawn;
        public static ProjectileManager instance=new ProjectileManager();
        public readonly List<FireProjectileInfo> Fired=new List<FireProjectileInfo>();
        public readonly List<GameObject> Bombs=new List<GameObject>();
        public GameObject FireProjectileImmediateServer(FireProjectileInfo info)
        {
            BeforeSpawn?.Invoke();
            Fired.Add(info);
            GameObject bomb=new GameObject();
            bomb.transform.position=info.position;
            bomb.AddComponent<AH64BombingRunProjectile>();
            bomb.AddComponent<AH64BombingRunDamage>();
            Bombs.Add(bomb);
            return bomb;
        }
    }
}
namespace EntityStates
{
    public enum InterruptPriority { Pain }
    public class BaseSkillState
    {
        public static Action EntryCallback;
        public RoR2.CharacterBody characterBody;
        public bool isAuthority;
        public float fixedAge;
        public RoR2.EntityStateMachine outer=new RoR2.EntityStateMachine();
        public virtual void OnEnter() { EntryCallback?.Invoke(); }
        public virtual void FixedUpdate() { }
        public virtual void OnExit() { }
        public virtual void OnSerialize(UnityEngine.Networking.NetworkWriter writer) { }
        public virtual void OnDeserialize(UnityEngine.Networking.NetworkReader reader) { }
        public virtual InterruptPriority GetMinimumInterruptPriority()=>InterruptPriority.Pain;
    }
}
namespace AH64 { public static class Log { public static void Error(string value) { Console.Error.WriteLine(value); } } }
namespace AH64.Survivors { public static class AH64BombingRunProjectiles { public static UnityEngine.GameObject Prefab=new UnityEngine.GameObject(); } }
