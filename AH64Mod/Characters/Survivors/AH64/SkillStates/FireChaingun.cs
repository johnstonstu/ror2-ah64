using EntityStates;
using AH64.Survivors.Components;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Primary. The M230 chain gun in the chin turret: a hitscan 30mm round that auto-fires while the
    /// button is held, out of a fixed ammo drum. Each impact kicks a small HE splash so packs shred
    /// without raising the direct coeff (or the proc rate).
    /// </summary>
    public class FireChaingun : BaseSkillState
    {
        public static float damageCoefficient => AH64PlaytestConfig.ChaingunDamage;
        public static float baseDuration = AH64StaticValues.chaingunBaseDuration;
        public static float range = AH64StaticValues.chaingunRange;
        public static float force = AH64StaticValues.chaingunForce;
        public static float minSpread = AH64StaticValues.chaingunMinSpread;
        public static float maxSpread => AH64PlaytestConfig.ChaingunMaxSpread;
        public static float spreadBloom => AH64PlaytestConfig.ChaingunBloom;
        public static float procCoefficient = AH64StaticValues.chaingunProcCoefficient;
        public static float bulletRadius = AH64StaticValues.chaingunBulletRadius;
        public static float recoil = AH64StaticValues.chaingunRecoil;
        public static float splashDamageCoefficient => AH64PlaytestConfig.ChaingunSplashDamage;
        public static float splashRadius => AH64PlaytestConfig.ChaingunSplashRadius;
        public static float splashProcCoefficient = AH64StaticValues.chaingunSplashProcCoefficient;
        public static float splashForce = AH64StaticValues.chaingunSplashForce;

        //vanilla Wwise event - the Clay Templar's per-round minigun shot, verified against the game's
        //SoundbanksInfo.xml. Deliberately the "_bullet" one-shot rather than "_shootLoop": a loop would
        //need a paired stop event, and one shot per state entry is already the right cadence.
        private const string fireSoundString = "Play_clayBruiser_attack1_shoot_bullet";

        private float duration;
        private AH64ChinTurret chinTurret;
        private float splashDamage;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            characterBody.SetAimTimer(2f);
            chinTurret = GetComponent<AH64ChinTurret>();
            splashDamage = splashDamageCoefficient * damageStat;

            Fire();
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
        /// A state can be interrupted when the incoming priority is >= this one. The primary's own SkillDef
        /// is InterruptPriority.Any, so leaving this at the default (also Any) lets the chain gun re-enter
        /// itself every physics tick — one round per tick instead of one per <see cref="duration"/>.
        /// Returning Skill holds the state open for its full duration so it cannot re-enter every tick.
        /// Pods and special live on other machines; only the dash (Body / PrioritySkill) needs to cut in.
        /// </summary>
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        private void Fire()
        {
            ChildLocator childLocator = GetModelChildLocator();
            Ray aimRay = GetAimRay();
            Vector3 muzzleOrigin = AH64Muzzles.GunOrigin(childLocator, aimRay);
            Vector3 muzzleAim = AH64Muzzles.AimDirection(muzzleOrigin, aimRay);

            characterBody.AddSpreadBloom(spreadBloom);
            SpawnBarrelFlash(AH64Assets.chaingunMuzzleFlashEffect, muzzleOrigin, muzzleAim);
            Util.PlaySound(fireSoundString, gameObject);

            if (chinTurret)
                chinTurret.NotifyFired();

            if (!isAuthority)
                return;

            AddRecoil(-1f * recoil, -2f * recoil, -0.5f * recoil, 0.5f * recoil);

            bool crit = RollCrit();

            //Leave the barrel tip (ChildLocator Muzzle on ChinBarrel), not AimOrigin — otherwise the
            //tracer reads as coming out of the cockpit. Converge on the crosshair so chin parallax
            //doesn't walk shots past the aim point at close range.
            new BulletAttack
            {
                bulletCount = 1,
                aimVector = muzzleAim,
                origin = muzzleOrigin,
                damage = damageCoefficient * damageStat,
                damageColorIndex = DamageColorIndex.Default,
                //tagged by slot, like the other two weapons - the combo carries a damageSource that
                //items keying off "primary damage" read
                damageType = DamageTypeCombo.GenericPrimary,
                falloffModel = BulletAttack.FalloffModel.None,
                maxDistance = range,
                force = force,
                hitMask = LayerIndex.CommonMasks.bullet,
                minSpread = minSpread,
                maxSpread = maxSpread,
                isCrit = crit,
                owner = gameObject,
                //BulletAttack's tracer reattaches to muzzleName even when origin is correct. The
                //exported Muzzle empty is stale, so leave this blank and let the tracer use ray.origin.
                muzzleName = string.Empty,
                smartCollision = true,
                procChainMask = default,
                procCoefficient = procCoefficient,
                radius = bulletRadius,
                sniper = false,
                stopperMask = LayerIndex.CommonMasks.bullet,
                weapon = null,
                tracerEffectPrefab = AH64Assets.chaingunTracerEffect,
                spreadPitchScale = 1f,
                spreadYawScale = 1f,
                queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                hitEffectPrefab = AH64Assets.chaingunHitEffect,
                hitCallback = ChaingunHitCallback,
            }.Fire();
        }

        /// <summary>
        /// Spawn at the calculated barrel tip. The exported Muzzle empty can drift independently of
        /// the articulated ChinBarrel after an FBX update, so attaching the effect to that empty makes
        /// the flash disagree with the actual round origin.
        /// </summary>
        private void SpawnBarrelFlash(GameObject effectPrefab, Vector3 muzzleOrigin, Vector3 muzzleAim)
        {
            if (!effectPrefab)
                return;

            EffectManager.SpawnEffect(effectPrefab, new EffectData
            {
                origin = muzzleOrigin,
                rotation = Util.QuaternionSafeLookRotation(muzzleAim),
            }, false);
        }

        /// <summary>
        /// Direct hit first, then a small HE blast at the impact point. Splash proc is zero on purpose —
        /// at chaingun cadence any non-zero splash proc would dominate the item economy.
        /// </summary>
        private bool ChaingunHitCallback(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            bool result = BulletAttack.defaultHitCallback(bulletAttack, ref hitInfo);

            //Do not rely solely on BulletAttack's implicit hit-effect path: it is tiny on terrain
            //and can be obscured by the target model. This explicit, replicated spark is the M230's
            //readable white-hot core; the separate splash effect supplies the amber dust/flash around it.
            if (AH64Assets.chaingunHitEffect)
            {
                EffectManager.SpawnEffect(AH64Assets.chaingunHitEffect, new EffectData
                {
                    origin = hitInfo.point + hitInfo.surfaceNormal * 0.06f,
                    rotation = Util.QuaternionSafeLookRotation(hitInfo.surfaceNormal),
                    scale = Mathf.Max(1.8f, AH64PlaytestConfig.ChaingunSplashVfxScale * 0.75f),
                }, true);
            }

            if (AH64Assets.chaingunSplashEffect)
            {
                EffectManager.SpawnEffect(AH64Assets.chaingunSplashEffect, new EffectData
                {
                    //Lift clear of the struck surface so the small HE flash/dust is not buried
                    //inside enemies or terrain. Visual feedback remains available even if a tester
                    //sets splash damage/radius to zero while comparing balance values.
                    origin = hitInfo.point + hitInfo.surfaceNormal * 0.08f,
                    rotation = Util.QuaternionSafeLookRotation(hitInfo.surfaceNormal),
                    scale = AH64PlaytestConfig.ChaingunSplashVfxScale,
                }, true);
            }

            if (splashDamage <= 0f || splashRadius <= 0f)
                return result;

            //terrain hits still splash — packs clustered on a rock deserve the HE tip
            TeamIndex team = teamComponent ? teamComponent.teamIndex : TeamIndex.None;

            new BlastAttack
            {
                attacker = gameObject,
                inflictor = gameObject,
                teamIndex = team,
                baseDamage = splashDamage,
                baseForce = splashForce,
                position = hitInfo.point,
                radius = splashRadius,
                falloffModel = BlastAttack.FalloffModel.Linear,
                crit = bulletAttack.isCrit,
                damageColorIndex = DamageColorIndex.Default,
                damageType = DamageTypeCombo.GenericPrimary,
                procCoefficient = splashProcCoefficient,
                procChainMask = default,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
                //None, not NearestHit: at 11 rps a LoS reject on cluttered geometry would make the HE tip
                //feel intermittent, which reads as "no splash" rather than "blocked".
                losType = BlastAttack.LoSType.None,
            }.Fire();

            return result;
        }
    }
}
