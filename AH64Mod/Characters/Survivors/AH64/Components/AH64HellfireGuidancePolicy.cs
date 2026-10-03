using System;

namespace AH64.Survivors.Components
{
    // One instance per body lifetime. No static owner registry, token reuse or fallback to older shots.
    internal sealed class AH64HellfireGuidancePolicy
    {
        private uint issued;
        private uint sequence;
        private float nextUpdate;
        private float lastUpdate;
        private bool released;
        private bool hasPoint;
        public uint ActiveToken { get; private set; }

        public uint Launch()
        {
            if (issued == uint.MaxValue)
                throw new InvalidOperationException("Hellfire launch tokens exhausted for this body lifetime.");
            ActiveToken = ++issued;
            sequence = 0;
            nextUpdate = float.NegativeInfinity;
            released = hasPoint = false;
            return ActiveToken;
        }

        public bool Update(uint token, uint incomingSequence, float now, bool held, bool valid)
        {
            if (token == 0 || token != ActiveToken || incomingSequence <= sequence || released)
                return false;
            // Release bypasses rate limiting and permanently closes this missile's designation.
            if (!held)
            {
                sequence = incomingSequence;
                released = true;
                hasPoint = false;
                ActiveToken = 0;
                return true;
            }
            if (now < nextUpdate)
                return false;
            sequence = incomingSequence;
            nextUpdate = now + AH64HellfirePrototype.UpdateInterval;
            lastUpdate = now;
            hasPoint = valid;
            return true;
        }

        public bool CanGuide(uint token, float now)
        {
            return token != 0 && token == ActiveToken && !released && hasPoint
                && now >= lastUpdate && now - lastUpdate <= AH64HellfirePrototype.StaleAfter;
        }

        public void Clear(uint token)
        {
            if (token == ActiveToken)
            {
                ActiveToken = 0;
                hasPoint = false;
                released = true;
            }
        }
    }
}
