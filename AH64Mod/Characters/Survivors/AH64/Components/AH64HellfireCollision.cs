using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    // Supplement stock physical collisions for rail overlap and hurtboxes crossed in a fast tick.
    // Forward one impact through the same filters/behaviors as ProjectileController; never deal damage.
    [DefaultExecutionOrder(-190)]
    public sealed class AH64HellfireCollision : MonoBehaviour, IProjectileImpactBehavior
    {
        private ProjectileController controller;
        private ProjectileSimple flight;
        private Rigidbody physicsBody;
        private Collider shape;
        private CharacterBody owner;
        private bool stopped;
        private bool ignoredOwner;

        private void Awake()
        {
            controller = GetComponent<ProjectileController>();
            flight = GetComponent<ProjectileSimple>();
            physicsBody = GetComponent<Rigidbody>();
            shape = GetComponent<Collider>();
        }

        private bool Eligible(Collider candidate)
        {
            return candidate && candidate != shape && !AH64HellfireAim.IsOwner(candidate, owner);
        }

        public void OnProjectileImpact(ProjectileImpactInfo impact)
        {
            if (!NetworkServer.active || !controller)
                return;
            if (!owner && controller.owner)
                owner = controller.owner.GetComponent<CharacterBody>();
            // This Hellfire warhead destroys on both enemy and world contact. Native controller
            // dispatch reaches us after its filters pass, even if detonation waits until next tick.
            // Never sweep/reposition that pending native impact when its collider moves away.
            if (Eligible(impact.collider))
                stopped = true;
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || stopped || !controller || !flight || !physicsBody || !shape)
                return;
            if (!owner && controller.owner)
                owner = controller.owner.GetComponent<CharacterBody>();
            // Read the instantiated physical collider after ProjectileController.Start enables it.
            // The ghost's cosmetic missile length is unrelated to its collision envelope.
            Vector3 extents = shape.bounds.extents;
            float radius = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
            if (radius <= 0f)
                return;
            if (!ignoredOwner && controller.owner)
            {
                // ProjectileController disables colliders in Awake and enables them in Start.
                // Apply this after Start, including the motor capsule its hurtbox-only helper omits.
                foreach (Collider missileCollider in GetComponents<Collider>())
                    foreach (Collider aircraftCollider in controller.owner.GetComponentsInChildren<Collider>(true))
                        Physics.IgnoreCollision(missileCollider, aircraftCollider, true);
                ignoredOwner = true;
            }
            int mask = LayerIndex.world.mask | LayerIndex.entityPrecise.mask;
            foreach (Collider overlap in Physics.OverlapSphere(transform.position, radius, mask,
                QueryTriggerInteraction.Collide))
            {
                if (Eligible(overlap) && Dispatch(overlap, overlap.ClosestPoint(transform.position),
                    -transform.forward, transform.position))
                    return;
            }
            float distance = physicsBody.velocity.magnitude * Time.fixedDeltaTime;
            if (distance <= 0f)
                return;
            Vector3 direction = physicsBody.velocity.normalized;
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction, distance, mask,
                QueryTriggerInteraction.Collide);
            // Nearest reachable collision wins, independent of Physics result ordering.
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (Eligible(hit.collider) && Dispatch(hit.collider, hit.point, hit.normal,
                    transform.position + direction * hit.distance))
                    return;
            }
        }

        private bool Dispatch(Collider collider, Vector3 point, Vector3 normal, Vector3 centre)
        {
            ProjectileImpactInfo impact = new ProjectileImpactInfo
            { collider = collider, estimatedPointOfImpact = point, estimatedImpactNormal = normal };
            foreach (IProjectileImpactFilter filter in GetComponents<IProjectileImpactFilter>())
                if (!filter.PassesFilters(impact))
                    return false;
            stopped = true;
            // Stop at the swept contact, so the stock blast originates at impact rather than
            // one velocity tick behind it. Never advance beyond the first accepted obstruction.
            transform.position = centre;
            physicsBody.velocity = Vector3.zero;
            // ProjectileSimple otherwise overwrites zero velocity later in this same tick.
            // Its stock timer remains enabled until the stock warhead detonates/destroys the missile.
            flight.desiredForwardSpeed = 0f;
            foreach (IProjectileImpactBehavior behavior in GetComponents<IProjectileImpactBehavior>())
                behavior.OnProjectileImpact(impact);
            return true;
        }
    }
}
