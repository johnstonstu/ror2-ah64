using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    internal struct AH64BrakingTurnCapture
    {
        public Vector3 EntryVelocity, EntryHeading, RequestedHeading;
        public Quaternion EntryAttitude;
        public float MoveSpeed, ExitSpeed, SignedCorrection;
        public float BrakeDuration, TurnDuration, ExitDuration, TurnRate, SpeedResponse;
        public float Duration => BrakeDuration + TurnDuration + ExitDuration;

        public static AH64BrakingTurnCapture Create(Vector3 velocity, Vector3 input,
            Vector3 aim, Vector3 facing, Quaternion attitude, float moveSpeed)
        {
            Vector3 fallback = AH64BrakingTurnMath.Direction(aim,
                AH64BrakingTurnMath.Direction(facing, Vector3.forward));
            Vector3 travel = AH64BrakingTurnMath.Horizontal(velocity);
            Vector3 heading = travel.magnitude >= AH64BrakingTurnStaticValues.MeaningfulTravelSpeed
                ? travel.normalized : fallback;
            Vector3 right = Vector3.Cross(Vector3.up, heading);
            float lateral = Vector3.Dot(AH64BrakingTurnMath.Horizontal(input), right);
            // Neutral, forward/back input and small stick noise choose RIGHT consistently.
            float sign = lateral < -AH64BrakingTurnStaticValues.SideDeadzone ? -1f : 1f;
            float correction = sign * AH64BrakingTurnStaticValues.ArcDegrees;
            float budget = Mathf.Max(0f, moveSpeed);
            return new AH64BrakingTurnCapture
            {
                EntryVelocity = velocity, EntryHeading = heading,
                RequestedHeading = Quaternion.AngleAxis(correction, Vector3.up) * heading,
                EntryAttitude = attitude, MoveSpeed = budget,
                ExitSpeed = budget * AH64BrakingTurnStaticValues.CruiseMultiplier,
                SignedCorrection = correction,
                BrakeDuration = AH64BrakingTurnStaticValues.BrakeDuration,
                TurnDuration = AH64BrakingTurnStaticValues.TurnDuration,
                ExitDuration = AH64BrakingTurnStaticValues.ExitDuration,
                TurnRate = AH64BrakingTurnStaticValues.TurnDegreesPerSecond,
                SpeedResponse = AH64BrakingTurnStaticValues.SpeedResponseSeconds
            };
        }

        public void Write(NetworkWriter writer)
        {
            writer.Write(EntryVelocity); writer.Write(EntryHeading); writer.Write(RequestedHeading);
            writer.Write(EntryAttitude); writer.Write(MoveSpeed); writer.Write(ExitSpeed);
            writer.Write(SignedCorrection); writer.Write(BrakeDuration); writer.Write(TurnDuration);
            writer.Write(ExitDuration); writer.Write(TurnRate); writer.Write(SpeedResponse);
        }

        public static AH64BrakingTurnCapture Read(NetworkReader reader)
        {
            return new AH64BrakingTurnCapture
            {
                EntryVelocity = reader.ReadVector3(), EntryHeading = reader.ReadVector3(),
                RequestedHeading = reader.ReadVector3(), EntryAttitude = reader.ReadQuaternion(),
                MoveSpeed = reader.ReadSingle(), ExitSpeed = reader.ReadSingle(),
                SignedCorrection = reader.ReadSingle(), BrakeDuration = reader.ReadSingle(),
                TurnDuration = reader.ReadSingle(), ExitDuration = reader.ReadSingle(),
                TurnRate = reader.ReadSingle(), SpeedResponse = reader.ReadSingle()
            };
        }
    }

    internal static class AH64BrakingTurnMath
    {
        public static Vector3 Horizontal(Vector3 value) { value.y = 0f; return value; }
        public static Vector3 Direction(Vector3 value, Vector3 fallback)
        {
            value = Horizontal(value);
            return value.sqrMagnitude > 0.000001f ? value.normalized : fallback;
        }
        public static float Angle(Vector3 from, Vector3 to)
        {
            return Mathf.Atan2(Vector3.Cross(from, to).y, Vector3.Dot(from, to)) * Mathf.Rad2Deg;
        }
        public static float Smooth(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
        public static Vector3 Heading(AH64BrakingTurnCapture c, float age)
        {
            return Quaternion.AngleAxis(c.SignedCorrection * Smooth(age / c.Duration),
                Vector3.up) * c.EntryHeading;
        }

        // Continuous speed response and a captured quarter-turn. No stop phase or entry write.
        // Collision/force handoffs terminate the motor lease before this can replace their result.
        public static Vector3 Step(AH64BrakingTurnCapture c, Vector3 actual, float age, float dt)
        {
            float step = Mathf.Min(Mathf.Max(dt, 0f), Mathf.Max(c.Duration - age, 0f));
            if (step <= 0f) return actual;
            Vector3 horizontal = Horizontal(actual);
            float speed = c.ExitSpeed + (horizontal.magnitude - c.ExitSpeed)
                * Mathf.Exp(-step / c.SpeedResponse);
            Vector3 direction = Direction(horizontal, c.EntryHeading);
            Vector3 command = Heading(c, age + step);
            float angle = Mathf.Clamp(Angle(direction, command), -c.TurnRate * step, c.TurnRate * step);
            // Tiny drift must not make a hover launch spend half its lifetime facing backwards.
            // Acquire the aim-based path through bounded thrust, rather than snapping that drift.
            horizontal = horizontal.magnitude < AH64BrakingTurnStaticValues.MeaningfulTravelSpeed
                ? Vector3.MoveTowards(horizontal, command * speed, c.MoveSpeed * AH64BrakingTurnStaticValues.LowSpeedAccelerationMultiplier * step)
                : Quaternion.AngleAxis(angle, Vector3.up) * direction * speed;
            horizontal.y = actual.y;
            return horizontal;
        }
    }
}
