using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    public sealed class AH64BombingRunProjectile : MonoBehaviour
    {
        public GameObject ImpactEffect;
        private AH64BombingRunCast cast;
        private Vector3 velocity;
        private float age;
        private int drop;
        private bool crit;
        private bool finished;

        internal void Initialize(AH64BombingRunCast source, int index, Vector3 initialVelocity, bool critical)
        {
            if (!NetworkServer.active || cast != null) return;
            cast = source;
            drop = index;
            velocity = initialVelocity;
            crit = critical;
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || finished) return;
            if (cast == null || !cast.SameStage) { Finish("stage-or-context-lost"); return; }
            float dt = Mathf.Min(Time.fixedDeltaTime, AH64BombingRunStaticValues.Lifetime - age);
            if (dt <= 0f) { Finish("expired"); return; }
            Vector3 start = transform.position;
            Vector3 delta = velocity * dt + Vector3.down * (0.5f * AH64BombingRunStaticValues.Gravity * dt * dt);
            velocity += Vector3.down * (AH64BombingRunStaticValues.Gravity * dt);
            age += dt;
            if (!AH64BombingRunCast.Finite(start) || !AH64BombingRunCast.Finite(delta))
            { Finish("nonfinite-motion"); return; }
            if (Physics.CheckSphere(start, AH64BombingRunStaticValues.CollisionRadius,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
            { Finish("inside-world"); return; }
            if (FindCollision(start, delta, out RaycastHit hit))
            {
                // Keep the blast center outside the wall/floor so line-of-sight is meaningful.
                transform.position = start + delta.normalized * Mathf.Max(0f,
                    hit.distance - AH64BombingRunStaticValues.SurfaceClearance);
                Impact();
                return;
            }
            transform.position = start + delta;
            transform.rotation = Util.QuaternionSafeLookRotation(velocity);
            if (age >= AH64BombingRunStaticValues.Lifetime) Finish("expired");
        }

        private bool FindCollision(Vector3 start, Vector3 delta, out RaycastHit nearest)
        {
            nearest = default(RaycastHit);
            float distance = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.SphereCastAll(start, AH64BombingRunStaticValues.CollisionRadius,
                delta.normalized, delta.magnitude, LayerIndex.world.mask | LayerIndex.entityPrecise.mask,
                QueryTriggerInteraction.Collide))
            {
                HurtBox hurtBox = hit.collider.GetComponent<HurtBox>();
                if (hurtBox)
                {
                    if (!hurtBox.healthComponent || !hurtBox.healthComponent.alive
                        || (cast.Owner && hurtBox.healthComponent == cast.Owner.healthComponent)
                        || !FriendlyFireManager.ShouldSplashHitProceed(hurtBox.healthComponent, cast.Team)) continue;
                }
                else if (hit.collider.isTrigger) continue;
                if (hit.distance < distance) { nearest = hit; distance = hit.distance; }
            }
            return distance < float.PositiveInfinity;
        }

        private void Impact()
        {
            if (finished) return;
            finished = true;
            try
            {
                cast.Trace("impact", drop, transform.position, velocity, crit: crit);
                if (ImpactEffect)
                    EffectManager.SpawnEffect(ImpactEffect, new EffectData
                    { origin = transform.position, scale = AH64BombingRunVisualValues.ImpactScale }, true);
                if (cast.OwnerAlive && cast.SameStage)
                    GetComponent<AH64BombingRunDamage>().Detonate(cast, drop, crit);
                else cast.Trace("suppressed", drop, transform.position, velocity, "invalid-owner-at-impact");
            }
            finally
            {
                // Let Unity report the original callback exception after guaranteed server cleanup.
                NetworkServer.Destroy(gameObject);
            }
        }

        private void Finish(string reason)
        {
            if (finished) return;
            finished = true;
            cast?.Trace("despawn", drop, transform.position, velocity, reason);
            NetworkServer.Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (NetworkServer.active && !finished)
                cast?.Trace("despawn", drop, transform.position, velocity, "external-destruction");
        }
    }
}
