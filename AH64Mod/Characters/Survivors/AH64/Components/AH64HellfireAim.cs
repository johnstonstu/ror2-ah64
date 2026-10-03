using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    internal static class AH64HellfireAim
    {
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);

        public static bool ValidRay(CharacterBody body, Ray ray)
        {
            return body && body.inputBank && Finite(ray.origin) && Finite(ray.direction)
                && ray.direction.sqrMagnitude >= 0.9f && ray.direction.sqrMagnitude <= 1.1f
                && (ray.origin - body.inputBank.aimOrigin).sqrMagnitude
                    <= AH64HellfirePrototype.OriginTolerance * AH64HellfirePrototype.OriginTolerance;
        }

        public static bool IsOwner(Collider collider, CharacterBody owner)
        {
            if (!collider || !owner)
                return false;
            HurtBox hurtBox = collider.GetComponent<HurtBox>();
            return collider.transform.IsChildOf(owner.transform)
                || (hurtBox && hurtBox.healthComponent == owner.healthComponent);
        }

        // Native aim, first reachable surface/hurtbox; a miss remains a finite world point.
        // The server re-resolves the ray rather than trusting a client-selected target or distance.
        public static Vector3 Resolve(CharacterBody owner, Ray ray)
        {
            ray.direction = ray.direction.normalized;
            float distance = AH64HellfirePrototype.DesignationRange;
            RaycastHit[] hits = Physics.RaycastAll(ray, distance,
                LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Collide);
            Vector3 point = ray.GetPoint(distance);
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance < distance && !IsOwner(hit.collider, owner))
                {
                    distance = hit.distance;
                    point = hit.point;
                }
            }
            return point;
        }

        public static Vector3 Converge(Vector3 rail, Vector3 point, Vector3 fallback)
        {
            Vector3 delta = point - rail;
            return delta.sqrMagnitude < 0.0001f ? fallback.normalized : delta.normalized;
        }

        public static Vector3 Turn(Vector3 heading, Vector3 desired, float dt,
            ref Vector3 angularVelocity, ref float remainingDegrees)
        {
            if (desired.sqrMagnitude < 0.0001f || dt <= 0f || remainingDegrees <= 0f)
            {
                angularVelocity = Vector3.zero;
                return heading;
            }
            heading = heading.normalized;
            desired = desired.normalized;
            Vector3 axis = Vector3.Cross(heading, desired);
            float angle = Mathf.Atan2(axis.magnitude, Vector3.Dot(heading, desired)) * Mathf.Rad2Deg;
            // Braking speed approaches zero near alignment; straight travel cannot precharge a turn.
            float neededSpeed = Mathf.Min(AH64HellfirePrototype.TurnDegreesPerSecond,
                Mathf.Min(angle / dt, Mathf.Sqrt(2f * AH64HellfirePrototype.TurnAcceleration * angle)));
            if (axis.sqrMagnitude < 0.00000001f && angle > 90f)
            {
                axis = Vector3.Cross(heading, Vector3.up);
                if (axis.sqrMagnitude < 0.00000001f)
                    axis = Vector3.Cross(heading, Vector3.forward);
            }
            Vector3 neededVelocity = axis.normalized * neededSpeed;
            // Bound the vector change too: a designation reversal cannot flip a full-rate turn.
            angularVelocity = Vector3.MoveTowards(angularVelocity, neededVelocity,
                AH64HellfirePrototype.TurnAcceleration * dt);
            float allowed = Mathf.Min(angularVelocity.magnitude * dt, remainingDegrees);
            Vector3 next = (Quaternion.AngleAxis(allowed, angularVelocity.normalized) * heading).normalized;
            Vector3 appliedAxis = Vector3.Cross(heading, next);
            float used = Mathf.Atan2(appliedAxis.magnitude, Vector3.Dot(heading, next)) * Mathf.Rad2Deg;
            // Retain the actually applied turning, including the terminal budget clamp.
            angularVelocity = used > 0f ? appliedAxis / appliedAxis.magnitude * (used / dt) : Vector3.zero;
            remainingDegrees = Mathf.Max(0f, remainingDegrees - used);
            return next;
        }
    }
}
