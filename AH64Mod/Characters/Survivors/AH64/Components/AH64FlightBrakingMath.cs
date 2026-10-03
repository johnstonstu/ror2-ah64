using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // World-space presentation only. The caller retains the sole model transform write.
    internal static class AH64FlightBrakingMath
    {
        internal static Quaternion Blend(Quaternion previous, Quaternion target, float deltaTime)
        {
            float dt = Mathf.Max(deltaTime, 0f);
            Quaternion smoothed = Quaternion.Slerp(previous, target,
                1f - Mathf.Exp(-AH64StaticValues.leanSmoothing * dt));
            float angle = Quaternion.Angle(previous, smoothed);
            float maximum = AH64BrakingTurnStaticValues.TurnDegreesPerSecond * dt;
            return angle > maximum && angle > 0f
                ? Quaternion.Slerp(previous, smoothed, maximum / angle) : smoothed;
        }

        internal static Quaternion Target(AH64BrakingTurnFrame frame,
            Quaternion entryWorld, Quaternion ordinaryWorld)
        {
            Quaternion command = Util.QuaternionSafeLookRotation(frame.CommandedHeading, Vector3.up)
                * Quaternion.Euler(frame.PitchDegrees, 0f, frame.BankDegrees);
            float blend = AH64ManeuverMath.Smooth(frame.PhaseProgress);
            if (frame.Phase == AH64BrakingTurnPhase.Brake)
                return Quaternion.Slerp(entryWorld, command, blend);
            // Keep the aircraft tangent to the moving arc through exit. The central
            // owner already recovers smoothly to ordinaryWorld after the lease ends.
            return command;
        }
    }
}
