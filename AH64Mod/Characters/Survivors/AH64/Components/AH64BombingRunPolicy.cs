using System;
using System.Collections.Generic;

namespace AH64.Survivors.Components
{
    // Pure policy: a scheduled opportunity is consumed even if the caller suppresses its payload.
    internal sealed class AH64BombingRunPolicy<T> where T : class
    {
        private readonly Dictionary<T, int> hits = new Dictionary<T, int>();
        private readonly HashSet<T> pending = new HashSet<T>();
        private int nextDrop;
        private double lastTime = -1;
        public bool Stopped { get; private set; }
        public int ConsumedDrops => nextDrop;

        public bool TryTakeDrop(double elapsed, out int index)
        {
            index = -1;
            if (Stopped || double.IsNaN(elapsed) || double.IsInfinity(elapsed)
                || elapsed < lastTime || elapsed < 0)
                return false;
            lastTime = elapsed;
            if (nextDrop >= AH64BombingRunStaticValues.DropCount
                || elapsed + 0.000001 < nextDrop * (double)AH64BombingRunStaticValues.DropInterval)
                return false;
            index = nextDrop++;
            return true;
        }

        public void Stop() { Stopped = true; }
        public int HitCount(T target) => target != null && hits.TryGetValue(target, out int count) ? count : 0;

        public bool TryBeginHit(T target)
        {
            // Reserve across callbacks/reentrancy, but do not spend a successful-hit budget yet.
            return target != null && HitCount(target) < AH64BombingRunStaticValues.HitsPerTarget
                && pending.Add(target);
        }

        public void CompleteHit(T target, bool positiveNativeDamage)
        {
            if (target == null || !pending.Remove(target)) return;
            if (positiveNativeDamage) hits[target] = HitCount(target) + 1;
        }
    }
}
