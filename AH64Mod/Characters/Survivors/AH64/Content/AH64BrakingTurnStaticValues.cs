namespace AH64.Survivors
{
    // Reversible private-prototype defaults, approved in the workstream contract.
    internal static class AH64BrakingTurnStaticValues
    {
        public const float BrakeDuration = 0.30f;
        public const float TurnDuration = 0.50f;
        public const float ExitDuration = 0.20f;
        public const float Cooldown = 4f;
        public const int Stock = 1;
        public const float BrakeEntryFraction = 0.25f;
        public const float BrakeMoveFraction = 0.35f;
        public const float TurnDegreesPerSecond = 360f;
        public const float ExitAccelerationMultiplier = 3f;
        // Presentation only; modest flare/bank rather than another evasive revolution.
        public const float PitchDegrees = -18f;
        public const float BankDegrees = 25f;
    }
}
