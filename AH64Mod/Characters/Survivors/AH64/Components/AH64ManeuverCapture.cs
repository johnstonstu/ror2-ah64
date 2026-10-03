using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    // One authority snapshot; observers never derive movement budgets from their local body.
    internal struct AH64ManeuverCapture
    {
        public Vector3 EntryVelocity, Facing, Direction;
        public Quaternion EntryAttitude;
        public float Sign, Duration, EntrySpeed, PeakSpeed, ExitSpeed, Ramp, Climb, StartY;

        public void Write(NetworkWriter writer)
        {
            writer.Write(EntryVelocity); writer.Write(Facing); writer.Write(Direction);
            writer.Write(EntryAttitude); writer.Write(Sign); writer.Write(Duration);
            writer.Write(EntrySpeed); writer.Write(PeakSpeed); writer.Write(ExitSpeed);
            writer.Write(Ramp); writer.Write(Climb); writer.Write(StartY);
        }

        public static AH64ManeuverCapture Read(NetworkReader reader)
        {
            return new AH64ManeuverCapture
            {
                EntryVelocity = reader.ReadVector3(), Facing = reader.ReadVector3(),
                Direction = reader.ReadVector3(), EntryAttitude = reader.ReadQuaternion(),
                Sign = reader.ReadSingle(), Duration = reader.ReadSingle(),
                EntrySpeed = reader.ReadSingle(), PeakSpeed = reader.ReadSingle(),
                ExitSpeed = reader.ReadSingle(), Ramp = reader.ReadSingle(),
                Climb = reader.ReadSingle(), StartY = reader.ReadSingle()
            };
        }
    }

    internal static class AH64ManeuverMath
    {
        public static Vector3 Horizontal(Vector3 v) { v.y = 0f; return v; }

        // Keep analog lateral intent. Rearward stick cannot reverse the utility's role.
        public static Vector3 Direction(Vector3 facing, Vector3 input, bool backflip, float diagonalBlend)
        {
            facing = Horizontal(facing);
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            float side = Mathf.Clamp(Vector3.Dot(Horizontal(input), right), -1f, 1f);
            return (facing * (backflip ? -1f : 1f)
                + right * (side / Mathf.Max(diagonalBlend, 0.1f))).normalized;
        }

        public static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public static float Speed(AH64ManeuverCapture c, float age)
        {
            float t = Mathf.Clamp01(age / c.Duration);
            return t <= c.Ramp
                ? Mathf.Lerp(c.EntrySpeed, c.PeakSpeed, Smooth(t / c.Ramp))
                : Mathf.Lerp(c.PeakSpeed, c.ExitSpeed, Smooth((t - c.Ramp) / (1f - c.Ramp)));
        }

        // Entry momentum above ordinary move speed survives capture, but never multiplies itself.
        public static void Speeds(ref AH64ManeuverCapture c, float moveSpeed, float floor, float peak, float carry)
        {
            float budget = Mathf.Max(moveSpeed, 0.01f);
            c.EntrySpeed = Mathf.Max(Horizontal(c.EntryVelocity).magnitude, budget * floor);
            c.PeakSpeed = Mathf.Max(budget * floor, Mathf.Min(c.EntrySpeed, budget) * Mathf.Max(peak, 1f));
            c.ExitSpeed = Mathf.Lerp(Mathf.Min(c.EntrySpeed, budget), c.PeakSpeed, carry);
        }

        public static Vector3 Step(AH64ManeuverCapture c, Vector3 actual, Vector3 direction,
            float age, float worldY, float dt)
        {
            // Enough acceleration to cross a reverse entry, bounded in world velocity rather than
            // instantly snapping heading. Mechanical prototype defaults: no new balance controls.
            float acceleration = c.PeakSpeed * 2f / (c.Duration * c.Ramp);
            Vector3 horizontal = Vector3.MoveTowards(Horizontal(actual),
                direction * Speed(c, age), acceleration * dt);
            float t = Mathf.Clamp01(age / c.Duration);
            float climbSpeed = c.Climb * (Mathf.PI * 0.5f / c.Duration) * Mathf.Cos(Mathf.PI * 0.5f * t);
            float verticalAcceleration = (Mathf.Abs(c.EntryVelocity.y) + c.Climb * Mathf.PI / c.Duration)
                / (c.Duration * c.Ramp);
            float vertical = Mathf.MoveTowards(actual.y, climbSpeed, verticalAcceleration * dt);
            // Captured utility allowance is a world-height budget, including incoming ascent.
            // Never pull an externally lifted aircraft down to a stale capture: the motor yields first.
            vertical = Mathf.Min(vertical, Mathf.Max(0f, c.StartY + c.Climb - worldY) / Mathf.Max(dt, 0.001f));
            horizontal.y = vertical;
            return horizontal;
        }

        public static float Revolution(float progress, float degrees) { return Smooth(progress) * degrees; }
    }
}
