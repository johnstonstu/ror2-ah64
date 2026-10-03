using System;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Dev harness can subscribe; no disk writes or global logging during normal gameplay.
    public static class AH64BombingRunTrace
    {
        public sealed class Record
        {
            public ulong castId;
            public uint ownerId;
            public string kind;
            public string reason;
            public int dropIndex = -1;
            public float scheduledTime;
            public float actualTime;
            public Vector3 position;
            public Vector3 velocity;
            public uint targetId;
            public int acceptedHits;
            public float remainingBaseCoefficient;
            public float damage;
            public float procCoefficient;
            public bool crit;
        }

        public static event Action<Record> Emitted;
        internal static void Emit(Record record)
        {
            if (Emitted == null) return;
            foreach (Action<Record> sink in Emitted.GetInvocationList())
            {
                try { sink(record); }
                catch (Exception error) { Log.Error("Bombing trace subscriber failed: " + error); }
            }
        }
    }
}
