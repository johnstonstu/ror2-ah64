using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    internal sealed class AH64BombingRunCast
    {
        private static ulong nextId;
        internal readonly AH64BombingRunPolicy<HealthComponent> Policy = new AH64BombingRunPolicy<HealthComponent>();
        internal readonly CharacterBody Owner;
        internal readonly ulong Id;
        internal readonly uint OwnerId;
        internal readonly float Damage;
        internal readonly TeamIndex Team;
        private readonly Run run;
        private readonly int scene;
        private readonly float startTime;
        internal bool SameStage => Run.instance == run && SceneManager.GetActiveScene().handle == scene;
        internal bool OwnerAlive => Owner && Owner.gameObject.activeInHierarchy
            && Owner.healthComponent && Owner.healthComponent.alive;

        internal AH64BombingRunCast(CharacterBody owner)
        {
            Owner = owner;
            Id = ++nextId;
            OwnerId = owner.netId.Value;
            Damage = owner.damage * AH64BombingRunStaticValues.DamageCoefficient;
            Team = TeamComponent.GetObjectTeam(owner.gameObject);
            run = Run.instance;
            scene = SceneManager.GetActiveScene().handle;
            startTime = Time.fixedTime;
            Trace("begin");
        }

        internal void Tick()
        {
            if (!NetworkServer.active || Policy.Stopped) return;
            if (!SameStage || !OwnerAlive) { Stop("owner-or-stage-lost"); return; }
            float elapsed = Time.fixedTime - startTime;
            while (Policy.TryTakeDrop(elapsed, out int drop)) Release(drop);
        }

        private void Release(int drop)
        {
            Vector3 center = Owner.corePosition;
            Vector3 origin = center + Vector3.down * AH64BombingRunStaticValues.ReleaseOffset;
            Vector3 inherited = Owner.characterMotor ? Owner.characterMotor.velocity : Vector3.zero;
            Vector3 horizontal = new Vector3(inherited.x, 0f, inherited.z);
            Vector3 velocity = Vector3.ClampMagnitude(horizontal, AH64BombingRunStaticValues.MaximumHorizontalSpeed)
                + Vector3.down * AH64BombingRunStaticValues.DownwardSpeed;
            if (!Finite(center) || !Finite(inherited) || !SafeOrigin(center, origin))
            {
                Trace("suppressed", drop, origin, velocity, "invalid-or-obstructed-origin");
                return;
            }
            GameObject prefab = AH64BombingRunProjectiles.Prefab;
            if (!prefab || !ProjectileManager.instance || !Finite(Damage) || Damage <= 0f)
            {
                Trace("suppressed", drop, origin, velocity, "missing-prefab-manager-or-damage");
                return;
            }
            bool crit = Owner.RollCrit();
            // Public immediate server API returns the instance; no shared dispatch context or owner RPC.
            GameObject bomb = ProjectileManager.instance.FireProjectileImmediateServer(new FireProjectileInfo
            {
                projectilePrefab = prefab, owner = Owner.gameObject,
                position = origin, rotation = Util.QuaternionSafeLookRotation(velocity),
                damage = Damage, crit = crit, force = 0f,
                damageTypeOverride = new DamageTypeCombo { damageSource = DamageSource.Special }
            });
            bomb.GetComponent<AH64BombingRunProjectile>().Initialize(this, drop, velocity, crit);
            Trace("release", drop, origin, velocity, crit: crit, proc: AH64BombingRunStaticValues.ProcCoefficient);
        }

        internal static bool SafeOrigin(Vector3 center, Vector3 origin)
        {
            return !Physics.CheckCapsule(center, origin, AH64BombingRunStaticValues.CollisionRadius,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        internal void Stop(string reason)
        {
            if (Policy.Stopped) return;
            Policy.Stop();
            Trace("end", reason: reason);
        }

        internal void Trace(string kind, int drop = -1, Vector3 position = default(Vector3),
            Vector3 velocity = default(Vector3), string reason = null, HealthComponent target = null,
            float dealt = 0f, bool crit = false, float proc = 0f)
        {
            int count = target ? Policy.HitCount(target) : 0;
            AH64BombingRunTrace.Emit(new AH64BombingRunTrace.Record
            {
                castId = Id, ownerId = OwnerId, kind = kind, reason = reason, dropIndex = drop,
                scheduledTime = drop < 0 ? 0f : drop * AH64BombingRunStaticValues.DropInterval,
                actualTime = Time.fixedTime - startTime, position = position, velocity = velocity,
                targetId = target ? target.netId.Value : 0, acceptedHits = count,
                remainingBaseCoefficient = (AH64BombingRunStaticValues.HitsPerTarget - count)
                    * AH64BombingRunStaticValues.DamageCoefficient,
                damage = kind == "release" ? Damage : dealt, crit = crit, procCoefficient = proc
            });
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    }
}
