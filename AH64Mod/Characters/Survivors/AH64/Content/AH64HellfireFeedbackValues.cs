using UnityEngine;

namespace AH64.Survivors
{
    // Reversible hands-on feedback defaults. Only the eligible lead uses this profile;
    // I.C.B.M. extras keep their original ballistic speed and full payload.
    internal static class AH64HellfireFeedbackValues
    {
        public const float CrawlSpeed = 6f;
        public const float GuidedSpeed = 140f;
        public const float Acceleration = 280f; // Crawl to boost in about 0.48 seconds.
        public const float Deceleration = 420f; // Release returns to crawl in about 0.32 seconds.
        public const float LaserWidth = 0.055f;

        public static float StepSpeed(float current, bool guiding, float dt)
        {
            if (float.IsNaN(current) || float.IsInfinity(current))
                current = CrawlSpeed;
            current = Mathf.Clamp(current, CrawlSpeed, GuidedSpeed);
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f)
                return current;
            return Mathf.MoveTowards(current, guiding ? GuidedSpeed : CrawlSpeed,
                (guiding ? Acceleration : Deceleration) * dt);
        }
    }
}
