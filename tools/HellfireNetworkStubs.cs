using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Networking
{
    public struct NetworkInstanceId : IEquatable<NetworkInstanceId>
    {
        public uint value;
        public NetworkInstanceId(uint value) { this.value=value; }
        public bool Equals(NetworkInstanceId other)=>value==other.value;
        public override int GetHashCode()=>(int)value;
    }
    public class NetworkIdentity : Component
    { public NetworkInstanceId netId;public bool localPlayerAuthority;public NetworkConnection clientAuthorityOwner; }
    public class MessageBase
    { public virtual void Serialize(NetworkWriter writer){}public virtual void Deserialize(NetworkReader reader){} }
    public class NetworkWriter
    {
        public readonly List<object> values=new List<object>();
        public void Write(NetworkInstanceId x)=>values.Add(x);
        public void Write(uint x)=>values.Add(x);
        public void Write(bool x)=>values.Add(x);
        public void Write(Vector3 x)=>values.Add(x);
    }
    public class NetworkReader
    {
        private readonly Queue<object> values;
        public NetworkReader(IEnumerable<object> values) { this.values=new Queue<object>(values); }
        public NetworkInstanceId ReadNetworkId()=>(NetworkInstanceId)values.Dequeue();
        public uint ReadUInt32()=>(uint)values.Dequeue();
        public bool ReadBoolean()=>(bool)values.Dequeue();
        public Vector3 ReadVector3()=>(Vector3)values.Dequeue();
    }
    public class NetworkMessage
    {
        public NetworkConnection conn;
        public NetworkReader reader;
        public T ReadMessage<T>() where T:MessageBase,new() { var message=new T();message.Deserialize(reader);return message; }
    }
    public delegate void NetworkMessageDelegate(NetworkMessage message);
    public class NetworkConnection
    {
        public readonly List<(short id,MessageBase message,int channel)> sent=new List<(short,MessageBase,int)>();
        public bool SendByChannel(short id,MessageBase message,int channel) { sent.Add((id,message,channel));return true; }
    }
    public class NetworkClient
    {
        public static List<NetworkClient> allClients=new List<NetworkClient>();
        public readonly Dictionary<short,NetworkMessageDelegate> handlers=new Dictionary<short,NetworkMessageDelegate>();
        public bool isConnected=true;
        public NetworkConnection connection=new NetworkConnection();
        public bool SendByChannel(short id,MessageBase message,int channel)=>connection.SendByChannel(id,message,channel);
        public void RegisterHandler(short id,NetworkMessageDelegate handler)=>handlers.Add(id,handler);
        public void UnregisterHandler(short id)=>handlers.Remove(id);
    }
    public static class NetworkServer
    {
        public static bool active;
        public static readonly Dictionary<short,NetworkMessageDelegate> handlers=new Dictionary<short,NetworkMessageDelegate>();
        public static readonly Dictionary<NetworkInstanceId,GameObject> objects=new Dictionary<NetworkInstanceId,GameObject>();
        public static GameObject FindLocalObject(NetworkInstanceId id)=>objects.TryGetValue(id,out var value)?value:null;
        public static void RegisterHandler(short id,NetworkMessageDelegate handler)=>handlers.Add(id,handler);
        public static void UnregisterHandler(short id)=>handlers.Remove(id);
    }
    public class NetworkManager : UnityEngine.Object
    { public static NetworkManager singleton=new NetworkManager();public NetworkClient client=new NetworkClient(); }
    public static class ClientScene
    { public static GameObject FindLocalObject(NetworkInstanceId id)=>NetworkServer.FindLocalObject(id); }
}
namespace RoR2.Networking
{
    using UnityEngine.Networking;
    public static class NetworkManagerSystem
    {
        public static event Action onStartServerGlobal;
        public static event Action<NetworkClient> onStartClientGlobal;
        public static void StartServer()=>onStartServerGlobal?.Invoke();
        public static void StartClient(NetworkClient client)=>onStartClientGlobal?.Invoke(client);
    }
    public struct QosChannelIndex
    { public int intVal;public static QosChannelIndex defaultReliable=>new QosChannelIndex { intVal=0 }; }
}
