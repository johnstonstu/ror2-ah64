using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    internal struct AH64BrakingTurnCapture
    {
        public Vector3 EntryVelocity, EntryHeading, RequestedHeading;
        public Quaternion EntryAttitude;
        public float MoveSpeed, BrakeSpeed, ExitSpeed, SignedCorrection;
        public float BrakeDuration, TurnDuration, ExitDuration, TurnRate, ExitAcceleration;
        public float Duration => BrakeDuration + TurnDuration + ExitDuration;

        public static AH64BrakingTurnCapture Create(Vector3 velocity, Vector3 input,
            Vector3 aim, Vector3 facing, Quaternion attitude, float moveSpeed)
        {
            float speed = AH64BrakingTurnMath.Horizontal(velocity).magnitude;
            Vector3 fallback = AH64BrakingTurnMath.Direction(facing, Vector3.forward);
            Vector3 heading = AH64BrakingTurnMath.Direction(velocity, fallback);
            Vector3 requested = AH64BrakingTurnMath.Direction(input,
                AH64BrakingTurnMath.Direction(aim, fallback));
            float budget = Mathf.Max(0f, moveSpeed);
            return new AH64BrakingTurnCapture
            {
                EntryVelocity = velocity, EntryHeading = heading, RequestedHeading = requested,
                EntryAttitude = attitude, MoveSpeed = budget,
                BrakeSpeed = Mathf.Min(speed * AH64BrakingTurnStaticValues.BrakeEntryFraction,
                    budget * AH64BrakingTurnStaticValues.BrakeMoveFraction),
                ExitSpeed = Mathf.Min(speed, budget),
                SignedCorrection = AH64BrakingTurnMath.Angle(heading, requested),
                BrakeDuration = AH64BrakingTurnStaticValues.BrakeDuration,
                TurnDuration = AH64BrakingTurnStaticValues.TurnDuration,
                ExitDuration = AH64BrakingTurnStaticValues.ExitDuration,
                TurnRate = AH64BrakingTurnStaticValues.TurnDegreesPerSecond,
                ExitAcceleration = budget * AH64BrakingTurnStaticValues.ExitAccelerationMultiplier
            };
        }

        public void Write(NetworkWriter writer)
        {
            writer.Write(EntryVelocity); writer.Write(EntryHeading); writer.Write(RequestedHeading);
            writer.Write(EntryAttitude); writer.Write(MoveSpeed); writer.Write(BrakeSpeed);
            writer.Write(ExitSpeed); writer.Write(SignedCorrection); writer.Write(BrakeDuration);
            writer.Write(TurnDuration); writer.Write(ExitDuration); writer.Write(TurnRate);
            writer.Write(ExitAcceleration);
        }

        public static AH64BrakingTurnCapture Read(NetworkReader reader)
        {
            return new AH64BrakingTurnCapture
            {
                EntryVelocity = reader.ReadVector3(), EntryHeading = reader.ReadVector3(),
                RequestedHeading = reader.ReadVector3(), EntryAttitude = reader.ReadQuaternion(),
                MoveSpeed = reader.ReadSingle(), BrakeSpeed = reader.ReadSingle(),
                ExitSpeed = reader.ReadSingle(), SignedCorrection = reader.ReadSingle(),
                BrakeDuration = reader.ReadSingle(), TurnDuration = reader.ReadSingle(),
                ExitDuration = reader.ReadSingle(), TurnRate = reader.ReadSingle(),
                ExitAcceleration = reader.ReadSingle()
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

        public static Vector3 Heading(AH64BrakingTurnCapture c, float age)
        {
            float time = Mathf.Clamp(age - c.BrakeDuration, 0f, c.TurnDuration);
            float angle = Mathf.MoveTowards(0f, c.SignedCorrection, c.TurnRate * time);
            return Quaternion.AngleAxis(angle, Vector3.up) * c.EntryHeading;
        }

        // Split boundary-crossing steps so a long tick cannot steer during brake/exit.
        // Actual momentum is the input each time; never restore the entry or a planned carry.
        public static Vector3 Step(AH64BrakingTurnCapture c, Vector3 actual, float age, float dt)
        {
            Vector3 horizontal = Horizontal(actual);
            float stop = Mathf.Min(c.Duration, age + Mathf.Max(dt, 0f));
            float brakeEnd = c.BrakeDuration;
            float turnEnd = brakeEnd + c.TurnDuration;
            float brakeTime = Mathf.Max(0f, Mathf.Min(stop, brakeEnd) - Mathf.Max(0f, age));
            float turnTime = Mathf.Max(0f, Mathf.Min(stop, turnEnd) - Mathf.Max(brakeEnd, age));
            float exitTime = Mathf.Max(0f, stop - Mathf.Max(turnEnd, age));
            if (brakeTime > 0f)
            {
                float entry = Horizontal(c.EntryVelocity).magnitude;
                float deceleration = Mathf.Max(0f, entry - c.BrakeSpeed) / c.BrakeDuration;
                // Braking never accelerates a slower actual body back toward the target.
                float speed = Mathf.Max(Mathf.Min(horizontal.magnitude, c.BrakeSpeed),
                    horizontal.magnitude - deceleration * brakeTime);
                horizontal = horizontal.normalized * speed;
            }
            if (turnTime > 0f && horizontal.sqrMagnitude > 0f)
            {
                float correction = Angle(horizontal.normalized, c.RequestedHeading);
                float angle = Mathf.MoveTowards(0f, correction, c.TurnRate * turnTime);
                horizontal = Quaternion.AngleAxis(angle, Vector3.up) * horizontal;
            }
            if (exitTime > 0f)
            {
                float speed = Mathf.MoveTowards(horizontal.magnitude, c.ExitSpeed,
                    c.ExitAcceleration * exitTime);
                // A stationary entry is a pivot. It never gains a translation direction.
                horizontal = horizontal.normalized * speed;
            }
            horizontal.y = actual.y;
            return horizontal;
        }
    }
}
