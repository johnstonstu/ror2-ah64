using AH64.Survivors.Components;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors
{
    public static class AH64BombingRunProjectiles
    {
        public static GameObject Prefab { get; private set; }

        // Caller owns catalog registration. Only reuse presentation, never Hellfire's flight/damage components.
        public static GameObject Build(GameObject ghost, GameObject impactEffect)
        {
            if (Prefab) return Prefab;
            if (!ghost || !ghost.GetComponent<ProjectileGhostController>())
                throw new System.ArgumentException("Bombing requires an existing projectile ghost.", nameof(ghost));
            GameObject seed = new GameObject("AH64BombingRunSeed");
            seed.SetActive(false);
            seed.layer = LayerIndex.projectile.intVal;
            seed.AddComponent<NetworkIdentity>().localPlayerAuthority = false;
            seed.AddComponent<TeamFilter>();
            ProjectileController controller = seed.AddComponent<ProjectileController>();
            controller.allowPrediction = false;
            controller.authorityHandlesCollisionEvents = false;
            controller.ghostPrefab = ghost;
            controller.procCoefficient = AH64BombingRunStaticValues.ProcCoefficient;
            seed.AddComponent<ProjectileDamage>().damageType = new DamageTypeCombo { damageSource = DamageSource.Special };
            ProjectileNetworkTransform network = seed.AddComponent<ProjectileNetworkTransform>();
            network.checkForLocalPlayerAuthority = false;
            network.allowClientsideCollision = false;
            network.positionTransmitInterval = 1f / 30f;
            // Kinematic sphere sweeps own server collision; no native grenade impact can double-hit.
            seed.AddComponent<AH64BombingRunProjectile>().ImpactEffect = impactEffect;
            seed.AddComponent<AH64BombingRunDamage>();
            seed.AddComponent<AH64BombingRunReleaseVisual>();
            Prefab = PrefabAPI.InstantiateClone(seed, "AH64BombingRunProjectile", false);
            Object.Destroy(seed);
            // PrefabAPI keeps clones beneath its inactive prefab parent.
            Prefab.SetActive(true);
            return Prefab;
        }
    }
}
