using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // World-space presentation only. The caller retains the sole model transform write.
    internal static class AH64FlightBrakingMath
    {
        internal static Quaternion Target(AH64BrakingTurnFrame frame,
            Quaternion entryWorld, Quaternion ordinaryWorld)
        {
            Quaternion command = Util.QuaternionSafeLookRotation(frame.CommandedHeading, Vector3.up)
                * Quaternion.Euler(frame.PitchDegrees, 0f, frame.BankDegrees);
            float blend = AH64ManeuverMath.Smooth(frame.PhaseProgress);
            if (frame.Phase == AH64BrakingTurnPhase.Brake)
                return Quaternion.Slerp(entryWorld, command, blend);
            if (frame.Phase == AH64BrakingTurnPhase.Exit)
                return Quaternion.Slerp(command, ordinaryWorld, blend);
            return command;
        }
    }
}
