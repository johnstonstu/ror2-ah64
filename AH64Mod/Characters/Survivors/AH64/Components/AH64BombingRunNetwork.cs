using System;
using RoR2;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    // Server -> owning client only, over existing reliable UNet transport. No launch/damage requests.
    public static class AH64BombingRunNetwork
    {
        private const short TerminalMessageId = 28066;
        private static bool initialized;

        public static void Init()
        {
            if (initialized) return;
            try
            {
                foreach (NetworkClient client in NetworkClient.allClients) RegisterClient(client);
                NetworkManagerSystem.onStartClientGlobal += RegisterClient;
                initialized = true;
            }
            catch { Shutdown(); throw; }
        }

        public static void Shutdown()
        {
            NetworkManagerSystem.onStartClientGlobal -= RegisterClient;
            foreach (NetworkClient client in NetworkClient.allClients)
                if (client.handlers.TryGetValue(TerminalMessageId, out var handler)
                    && handler == (NetworkMessageDelegate)ReceiveTerminal)
                    client.UnregisterHandler(TerminalMessageId);
            initialized = false;
        }

        private static void RegisterClient(NetworkClient client)
        {
            if (client.handlers.TryGetValue(TerminalMessageId, out var handler))
            {
                if (handler == (NetworkMessageDelegate)ReceiveTerminal) return;
                throw new InvalidOperationException("AH64 bombing terminal message ID 28066 already registered.");
            }
            client.RegisterHandler(TerminalMessageId, ReceiveTerminal);
        }

        internal static void Deliver(GameObject bodyObject, uint request, SkillStates.BombingRun state,
            AH64BombingRunTerminal terminal)
        {
            if (!NetworkServer.active || !bodyObject || request == 0 || !terminal.IsTerminal) return;
            if (Util.HasEffectiveAuthority(bodyObject))
            {
                terminal.ForgetRecipient();
                state.ReceiveTerminal(request, terminal.Phase);
                return;
            }
            NetworkIdentity identity = bodyObject.GetComponent<NetworkIdentity>();
            NetworkConnection recipient = identity ? identity.clientAuthorityOwner : null;
            if (recipient == null) { terminal.ForgetRecipient(); return; }
            if (!terminal.TryDelivery(recipient, Time.fixedTime)) return;
            if (!initialized) throw new InvalidOperationException("AH64 bombing terminal transport was not initialized.");
            // Re-target immediately on authority change; retry the current recipient until native state exit.
            // A reliable queue acceptance is not proof that the recipient still owns this body on arrival.
            recipient.SendByChannel(TerminalMessageId,
                new TerminalMessage { bodyId = identity.netId, request = request, outcome = terminal.Phase },
                QosChannelIndex.defaultReliable.intVal);
        }

        private static void ReceiveTerminal(NetworkMessage message)
        {
            bool fromConnectedServer = false;
            foreach (NetworkClient client in NetworkClient.allClients)
                if (client.isConnected && client.connection != null && client.connection == message.conn)
                    fromConnectedServer = true;
            if (!fromConnectedServer) return;
            TerminalMessage completion = message.ReadMessage<TerminalMessage>();
            var bodyObject = ClientScene.FindLocalObject(completion.bodyId);
            if (!bodyObject || !bodyObject.activeInHierarchy || !Util.HasEffectiveAuthority(bodyObject)) return;
            EntityStateMachine machine = EntityStateMachine.FindByCustomName(bodyObject, "Weapon2");
            var state = machine ? machine.state as SkillStates.BombingRun : null;
            state?.ReceiveTerminal(completion.request, completion.outcome);
        }

        private sealed class TerminalMessage : MessageBase
        {
            public NetworkInstanceId bodyId;
            public uint request;
            public AH64BombingRunPhase outcome;
            public override void Serialize(NetworkWriter writer)
            { writer.Write(bodyId); writer.Write(request); writer.Write((byte)outcome); }
            public override void Deserialize(NetworkReader reader)
            { bodyId = reader.ReadNetworkId(); request = reader.ReadUInt32(); outcome = (AH64BombingRunPhase)reader.ReadByte(); }
        }
    }
}
