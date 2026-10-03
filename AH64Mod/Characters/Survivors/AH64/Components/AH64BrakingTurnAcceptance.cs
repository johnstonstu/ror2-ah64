using EntityStates;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Shared by the native diagnostic and isolated regressions; inputs are measured velocity.
    internal static class AH64BrakingTurnAcceptance
    {
        internal static bool Priority(InterruptPriority actual) => actual == InterruptPriority.PrioritySkill;

        internal static float SignedProgress(AH64BrakingTurnCapture capture, Vector3 actual)
        {
            Vector3 horizontal = AH64BrakingTurnMath.Horizontal(actual);
            if (horizontal.sqrMagnitude < 0.000001f) return 0f;
            float signed = AH64BrakingTurnMath.Angle(capture.EntryHeading, horizontal.normalized);
            // +/-180 share an endpoint. Earlier signed observations still verify direction.
            return Mathf.Abs(signed) > 179.9f ? Mathf.Abs(signed) : signed * Mathf.Sign(capture.SignedCorrection);
        }

        internal static float Alignment(AH64BrakingTurnCapture capture, Vector3 actual)
        {
            Vector3 horizontal = AH64BrakingTurnMath.Horizontal(actual);
            return horizontal.sqrMagnitude < 0.000001f ? 180f
                : Mathf.Abs(AH64BrakingTurnMath.Angle(horizontal.normalized, capture.RequestedHeading));
        }

        internal static bool Turned(AH64BrakingTurnCapture capture, Vector3 actual, bool completed)
        {
            float progress = SignedProgress(capture, actual);
            float alignment = Alignment(capture, actual);
            return progress > 1f && alignment < Mathf.Abs(capture.SignedCorrection) - 1f
                && (!completed || alignment <= 3f && progress >= Mathf.Abs(capture.SignedCorrection) - 3f);
        }
    }
}
