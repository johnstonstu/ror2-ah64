using EntityStates;

namespace AH64.Survivors.Components
{
    internal static class AH64HellfireInterruption
    {
        // Typed utilities use Pain to reject stock re-entry while keeping weapons available.
        // Other Pain/stun/death states still close guidance.
        internal static bool IsInterrupted(EntityState state)
        {
            return state != null && !(state is SkillStates.ServoDash)
                && !(state is SkillStates.SmokeBackflip) && !(state is SkillStates.BrakingTurn)
                && state.GetMinimumInterruptPriority() >= InterruptPriority.Pain;
        }
    }
}
