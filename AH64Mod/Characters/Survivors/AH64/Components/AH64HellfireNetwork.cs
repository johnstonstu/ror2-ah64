using System;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    // Existing UNet transport only. No damage/launch messages: vanilla ProjectileManager retains
    // firing, stock, crit and payload authority. Fixed IDs fail visibly on collision, never replace it.
    public static class AH64HellfireNetwork
    {
        private const short AimMessageId = 28064;
        private const short AckMessageId = 28065;
        private static bool initialized;

        public static void Init()
        {
            if (initialized)
                return;
            initialized = true;
            NetworkManagerSystem.onStartServerGlobal += RegisterServer;
            NetworkManagerSystem.onStartClientGlobal += RegisterClient;
        }

        public static void Shutdown()
        {
            if (!initialized)
                return;
            initialized = false;
            NetworkManagerSystem.onStartServerGlobal -= RegisterServer;
            NetworkManagerSystem.onStartClientGlobal -= RegisterClient;
            if (NetworkServer.handlers.TryGetValue(AimMessageId, out var serverHandler)
                && serverHandler == (NetworkMessageDelegate)ReceiveAim)
                NetworkServer.UnregisterHandler(AimMessageId);
            foreach (NetworkClient client in NetworkClient.allClients)
                if (client.handlers.TryGetValue(AckMessageId, out var clientHandler)
                    && clientHandler == (NetworkMessageDelegate)ReceiveAck)
                    client.UnregisterHandler(AckMessageId);
        }

        private static void RegisterServer()
        {
            if (NetworkServer.handlers.TryGetValue(AimMessageId, out var handler))
            {
                if (handler == (NetworkMessageDelegate)ReceiveAim)
                    return;
                throw new InvalidOperationException("AH64 Hellfire aim message ID 28064 already registered.");
            }
            NetworkServer.RegisterHandler(AimMessageId, ReceiveAim);
        }

        private static void RegisterClient(NetworkClient client)
        {
            if (client.handlers.TryGetValue(AckMessageId, out var handler))
            {
                if (handler == (NetworkMessageDelegate)ReceiveAck)
                    return;
                throw new InvalidOperationException("AH64 Hellfire acknowledgement ID 28065 already registered.");
            }
            client.RegisterHandler(AckMessageId, ReceiveAck);
        }

        internal static void SendAim(AH64HellfireOwner owner, uint token, uint sequence, bool held, Ray ray, bool cancel = false)
        {
            if (NetworkServer.active)
            {
                if (Util.HasEffectiveAuthority(owner.gameObject))
                    owner.Accept(token, sequence, held, ray, cancel: cancel);
                return;
            }
            NetworkIdentity identity = owner.GetComponent<NetworkIdentity>();
            NetworkClient client = NetworkManager.singleton ? NetworkManager.singleton.client : null;
            if (initialized && identity && client != null && client.isConnected)
                client.SendByChannel(AimMessageId, new AimMessage
                {
                    bodyId = identity.netId, token = token, sequence = sequence,
                    held = held, origin = ray.origin, direction = ray.direction, cancel = cancel
                }, QosChannelIndex.defaultReliable.intVal);
        }

        internal static void BeginRequest(AH64HellfireOwner owner, uint request)
        {
            if (NetworkServer.active)
            {
                owner.ReceiveRequest(request);
                return;
            }
            NetworkIdentity identity = owner.GetComponent<NetworkIdentity>();
            NetworkClient client = NetworkManager.singleton ? NetworkManager.singleton.client : null;
            // Same reliable channel as vanilla ProjectileManager's fire message, so the server
            // pairs the intent before initializing the lead. This message never launches a missile.
            if (initialized && identity && client != null && client.isConnected)
                client.SendByChannel(AimMessageId, new AimMessage
                { bodyId = identity.netId, begin = true, request = request }, QosChannelIndex.defaultReliable.intVal);
        }

        internal static void Acknowledge(AH64HellfireOwner owner, uint token, uint request, bool ended)
        {
            if (!owner || token == 0)
                return;
            if (Util.HasEffectiveAuthority(owner.gameObject))
            {
                owner.Acknowledge(token, request, ended);
                return;
            }
            NetworkIdentity identity = owner.GetComponent<NetworkIdentity>();
            if (initialized && identity && identity.clientAuthorityOwner != null)
                identity.clientAuthorityOwner.SendByChannel(AckMessageId, new AckMessage
                { bodyId = identity.netId, token = token, request = request, ended = ended }, QosChannelIndex.defaultReliable.intVal);
        }

        private static void ReceiveAim(NetworkMessage message)
        {
            AimMessage aim = message.ReadMessage<AimMessage>();
            GameObject bodyObject = NetworkServer.FindLocalObject(aim.bodyId);
            NetworkIdentity identity = bodyObject ? bodyObject.GetComponent<NetworkIdentity>() : null;
            // Bind sender to this live body's authority; an observer cannot redirect another player.
            if (!identity || message.conn == null || identity.clientAuthorityOwner != message.conn)
                return;
            AH64HellfireOwner owner = bodyObject.GetComponent<AH64HellfireOwner>();
            if (aim.begin)
            {
                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                if (body && body.healthComponent && body.healthComponent.alive)
                    AH64HellfireOwner.GetOrAdd(bodyObject).ReceiveRequest(aim.request);
                return;
            }
            if (owner)
            {
                // Unity's Ray constructor normalizes direction, so validate the wire vector first.
                bool valid = AH64HellfireAim.Finite(aim.direction)
                    && aim.direction.sqrMagnitude >= 0.9f && aim.direction.sqrMagnitude <= 1.1f;
                owner.Accept(aim.token, aim.sequence, aim.held, new Ray(aim.origin, aim.direction), valid, aim.cancel);
            }
        }

        private static void ReceiveAck(NetworkMessage message)
        {
            AckMessage ack = message.ReadMessage<AckMessage>();
            GameObject bodyObject = ClientScene.FindLocalObject(ack.bodyId);
            AH64HellfireOwner owner = bodyObject ? bodyObject.GetComponent<AH64HellfireOwner>() : null;
            if (owner && Util.HasEffectiveAuthority(bodyObject))
                owner.Acknowledge(ack.token, ack.request, ack.ended);
        }

        private sealed class AimMessage : MessageBase
        {
            public NetworkInstanceId bodyId;
            public uint token, sequence;
            public uint request;
            public bool begin;
            public bool held, cancel;
            public Vector3 origin, direction;
            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(bodyId); writer.Write(token); writer.Write(sequence);
                writer.Write(held); writer.Write(origin); writer.Write(direction); writer.Write(begin); writer.Write(request); writer.Write(cancel);
            }
            public override void Deserialize(NetworkReader reader)
            {
                bodyId = reader.ReadNetworkId(); token = reader.ReadUInt32(); sequence = reader.ReadUInt32();
                held = reader.ReadBoolean(); origin = reader.ReadVector3(); direction = reader.ReadVector3();
                begin = reader.ReadBoolean(); request = reader.ReadUInt32(); cancel = reader.ReadBoolean();
            }
        }

        private sealed class AckMessage : MessageBase
        {
            public NetworkInstanceId bodyId;
            public uint token;
            public uint request;
            public bool ended;
            public override void Serialize(NetworkWriter writer)
            { writer.Write(bodyId); writer.Write(token); writer.Write(request); writer.Write(ended); }
            public override void Deserialize(NetworkReader reader)
            { bodyId = reader.ReadNetworkId(); token = reader.ReadUInt32(); request = reader.ReadUInt32(); ended = reader.ReadBoolean(); }
        }
    }
}
