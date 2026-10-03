using System;

namespace AH64.Survivors.Components
{
    // One instance per body lifetime; release pauses this identity, only Clear/Cancel retires it.
    internal sealed class AH64HellfireGuidancePolicy
    {
        private uint issued, sequence;
        private float nextUpdate, lastUpdate;
        private bool hasPoint;
        public uint ActiveToken { get; private set; }

        public uint Launch()
        {
            if (issued == uint.MaxValue)
                throw new InvalidOperationException("Hellfire launch tokens exhausted for this body lifetime.");
            ActiveToken = ++issued;
            sequence = 0;
            nextUpdate = float.NegativeInfinity;
            hasPoint = false;
            return ActiveToken;
        }

        public bool Update(uint token, uint incomingSequence, float now, bool held, bool valid)
        {
            if (token == 0 || token != ActiveToken || incomingSequence <= sequence)
                return false;
            // A pause bypasses rate limiting but preserves identity, lifetime and turn budget.
            // Its newer sequence rejects held packets that were sent before the pause.
            if (!held)
            {
                sequence = incomingSequence;
                hasPoint = false;
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

        public bool Cancel(uint token, uint incomingSequence)
        {
            if (token == 0 || token != ActiveToken || incomingSequence <= sequence)
                return false;
            sequence = incomingSequence;
            Clear(token);
            return true;
        }

        public bool CanGuide(uint token, float now)
        {
            return token != 0 && token == ActiveToken && hasPoint
                && now >= lastUpdate && now - lastUpdate <= AH64HellfirePrototype.StaleAfter;
        }

        public void Clear(uint token)
        {
            if (token == ActiveToken)
            {
                ActiveToken = 0;
                hasPoint = false;
            }
        }
    }
}
