namespace AH64.Survivors.Components
{
    // Integrator tuning contract: move these reversible guidance-only defaults to shared
    // StaticValues/config after the first measured run. Warhead/cooldown stay baseline; lead speed is in AH64HellfireFeedbackValues.
    internal static class AH64HellfirePrototype
    {
        public const float DesignationRange = 500f;
        public const float OriginTolerance = 8f;
        public const float UpdateInterval = 0.1f;
        public const float StaleAfter = 0.35f;
        public const float TurnDegreesPerSecond = 120f;
        public const float TurnAcceleration = 720f;
        public const float TotalTurnDegrees = 240f;
    }
}
