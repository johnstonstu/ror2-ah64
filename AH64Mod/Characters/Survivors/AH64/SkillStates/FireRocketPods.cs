using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Secondary. Hydra-70 pods: one unguided rocket per stock, alternating pylons. Hold to ripple.
    /// Stock is the pod magazine — Backup Magazine adds rockets to the drum the same way it buffs
    /// any other secondary's maxStock, and the HUD number above the skill icon is that ammo count.
    /// </summary>
    public class FireRocketPods : BaseSkillState
    {
        public static float damageCoefficient = AH64StaticValues.hydraDamageCoefficient;
        public static float fireInterval = AH64StaticValues.hydraFireInterval;
        public static float spreadAngle = AH64StaticValues.hydraSpreadAngle;
        public static float force = AH64StaticValues.hydraForce;
        public static float recoil = 0.6f;

        //vanilla Wwise event - the AtG Missile Mk.1 launch, verified against SoundbanksInfo.xml. Light
        //enough to fire on a hold-to-ripple cadence without turning into noise; the Hellfire gets the heavy one.
        private const string fireSoundString = "Play_item_proc_missile_fire";

        private float duration;
        private int pylonIndex;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = fireInterval / attackSpeedStat;
            characterBody.SetAimTimer(2f);

            //alternate pylons across consecutive shots in a hold-to-ripple, not across different bodies
            pylonIndex = (skillLocator && skillLocator.secondary)
                ? skillLocator.secondary.stock
                : 0;
            FireOneRocket(pylonIndex);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        /// <summary>
        /// Hold the state open for its full interval so the skill cannot re-enter every physics tick
        /// (same trap as <see cref="FireChaingun"/>), while still letting the dash cut in.
        /// </summary>
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        private void FireOneRocket(int index)
        {
            ChildLocator childLocator = GetModelChildLocator();
            string requestedMuzzle = (index % 2 == 0) ? AH64Muzzles.RocketL : AH64Muzzles.RocketR;
            string muzzleName = AH64Muzzles.ResolveName(childLocator, requestedMuzzle);

            if (AH64Assets.hydraMuzzleFlashEffect)
                EffectManager.SimpleMuzzleFlash(AH64Assets.hydraMuzzleFlashEffect, gameObject, muzzleName, false);
            Util.PlaySound(fireSoundString, gameObject);

            if (!isAuthority)
                return;

            if (!AH64Assets.hydraRocketProjectilePrefab)
                return;

            Ray aimRay = GetAimRay();
            AddRecoil(-1f * recoil, -2f * recoil, -0.5f * recoil, 0.5f * recoil);

            Vector3 converged = AH64Muzzles.AimDirection(childLocator, requestedMuzzle, aimRay);
            Vector3 direction = Util.ApplySpread(converged, 0f, spreadAngle, 1f, 1f);

            FireProjectileInfo info = new FireProjectileInfo
            {
                projectilePrefab = AH64Assets.hydraRocketProjectilePrefab,
                position = AH64Muzzles.Origin(childLocator, requestedMuzzle, aimRay),
                rotation = Util.QuaternionSafeLookRotation(direction),
                owner = gameObject,
                damage = damageCoefficient * damageStat,
                force = force,
                crit = RollCrit(),
                damageTypeOverride = DamageTypeCombo.GenericSecondary,
            };

            ProjectileManager.instance.FireProjectile(info);
        }
    }
}
