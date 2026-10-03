namespace AH64.Survivors.Components
{
    internal enum AH64BombingRunPhase : byte { Running, Succeeded, Cancelled, Exited }

    // First terminal outcome wins. Delivery tracks attempts, never treats queue acceptance as receipt.
    internal sealed class AH64BombingRunTerminal
    {
        internal AH64BombingRunPhase Phase { get; private set; }
        internal bool IsTerminal => Phase == AH64BombingRunPhase.Succeeded || Phase == AH64BombingRunPhase.Cancelled;
        private object lastRecipient;
        private float nextAttempt;

        internal bool Resolve(AH64BombingRunPhase outcome)
        {
            if (outcome != AH64BombingRunPhase.Succeeded && outcome != AH64BombingRunPhase.Cancelled) return false;
            if (Phase == AH64BombingRunPhase.Running) Phase = outcome;
            return Phase == outcome;
        }

        internal bool TryDelivery(object recipient, float now)
        {
            if (!IsTerminal || recipient == null) return false;
            if (ReferenceEquals(lastRecipient, recipient) && now < nextAttempt) return false;
            lastRecipient = recipient;
            nextAttempt = now + AH64BombingRunStaticValues.TerminalRetryInterval;
            return true;
        }

        internal void ForgetRecipient() { lastRecipient = null; }
        internal void Exit() { Phase = AH64BombingRunPhase.Exited; ForgetRecipient(); }
    }
}
