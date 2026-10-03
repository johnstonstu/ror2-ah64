namespace AH64.Survivors
{
    // Banked Break private prototype. Class/state identifiers stay registration-compatible.
    internal static class AH64BrakingTurnStaticValues
    {
        public const float BrakeDuration = 0.20f; // historical field name: bank-in, never a braking phase
        public const float TurnDuration = 0.80f;
        public const float ExitDuration = 0.25f;
        public const float Cooldown = 4f;
        public const int Stock = 1;
        public const float CruiseMultiplier = 1.35f;
        public const float SpeedResponseSeconds = 0.25f;
        public const float LowSpeedAccelerationMultiplier = 4f;
        public const float ArcDegrees = 90f;
        public const float MeaningfulTravelSpeed = 0.5f;
        public const float SideDeadzone = 0.15f;
        public const float TurnDegreesPerSecond = 360f; // safety cap; commanded curve peaks at 108 deg/s
        public const float PitchDegrees = 5f; // mild forward thrust cue, not nose-up braking
        public const float BankDegrees = 32f;
    }
}
