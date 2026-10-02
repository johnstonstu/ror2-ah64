using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Scales a Hydra rocket's <see cref="ProjectileDamage"/> by how far it has flown from where it
    /// spawned. 75% at or inside <see cref="AH64StaticValues.hydraCloseRange"/>, full at
    /// <see cref="AH64StaticValues.hydraFullRange"/>. The blast reads that damage, so the warhead
    /// and any direct hit share one scale.
    ///
    /// <para>Runs before the stock explosion (<see cref="DefaultExecutionOrderAttribute"/>) and only
    /// on the server, which is where the hit is resolved. The base damage is captured once, after
    /// <c>FireProjectile</c> has assigned it, so later writes replace the scaled value instead of
    /// compounding it.</para>
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class AH64HydraRangeRamp : MonoBehaviour
    {
        private ProjectileDamage projectileDamage;
        private Vector3 origin;
        private float baseDamage;
        private bool captured;

        private void Awake()
        {
            //Spawn position. Awake runs during Instantiate, before the rocket is stepped forward.
            origin = transform.position;
        }

        private void Start()
        {
            Apply();
        }

        private void FixedUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (!NetworkServer.active)
                return;

            if (!captured)
            {
                projectileDamage = GetComponent<ProjectileDamage>();
                if (!projectileDamage)
                    return;
                //Prefab Awake runs before FireProjectile assigns damage, so capture on the first
                //Apply, which is after that assignment and before the explosion can detonate.
                baseDamage = projectileDamage.damage;
                captured = true;
            }

            float flown = Vector3.Distance(origin, transform.position);
            float scale = AH64StaticValues.RangeDamageScale(
                flown,
                AH64StaticValues.hydraCloseRange,
                AH64StaticValues.hydraFullRange,
                AH64StaticValues.hydraCloseRangeDamageScale);
            projectileDamage.damage = baseDamage * scale;
        }
    }
}
