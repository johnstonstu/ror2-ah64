using System;
using AH64.Survivors.Components;
using UnityEngine;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private void OnBombingTrace(AH64BombingRunTrace.Record value)
        {
            if (!scripting || finished || !pilot || value == null) return;
            // Keep one owned writer and the existing versioned event envelope. Fields retain
            // cast/owner/target attribution; a reported hit is not downstream proc evidence.
            Event("bombing-" + value.kind, JsonUtility.ToJson(new BombingPayload {
                castId = value.castId, ownerId = value.ownerId, kind = value.kind, reason = value.reason,
                dropIndex = value.dropIndex, scheduledTime = value.scheduledTime, actualTime = value.actualTime,
                position = value.position, velocity = value.velocity, targetId = value.targetId,
                acceptedHits = value.acceptedHits, remainingBaseCoefficient = value.remainingBaseCoefficient,
                damage = value.damage, procCoefficient = value.procCoefficient, crit = value.crit }));
        }

        [Serializable] private sealed class BombingPayload
        {
            public ulong castId; public uint ownerId, targetId; public string kind, reason;
            public int dropIndex, acceptedHits; public float scheduledTime, actualTime, remainingBaseCoefficient, damage, procCoefficient;
            public Vector3 position, velocity; public bool crit;
        }
    }
}
