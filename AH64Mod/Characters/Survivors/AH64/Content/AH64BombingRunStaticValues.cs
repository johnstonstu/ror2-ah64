namespace AH64.Survivors
{
    // Coordinator-approved reversible prototype values, not final encounter balance.
    public static class AH64BombingRunStaticValues
    {
        public const int DropCount = 6;
        public const float DropInterval = 0.3f;
        public const float Duration = (DropCount - 1) * DropInterval;
        public const float Cooldown = 10f;
        public const float DamageCoefficient = 3f;
        public const int HitsPerTarget = 3;
        public const float BlastRadius = 6f;
        public const float ProcCoefficient = 0.5f;
        public const float MaximumHorizontalSpeed = 25f;
        public const float DownwardSpeed = 4f;
        public const float Gravity = 30f;
        public const float Lifetime = 6f;
        // Transport retry only; never a grace period or a gameplay completion deadline.
        public const float TerminalRetryInterval = 0.1f;
        // Geometry safety margins, distinct from the damaging splash radius.
        public const float CollisionRadius = 0.2f;
        public const float ReleaseOffset = 0.5f;
        public const float SurfaceClearance = 0.02f;
    }
}
