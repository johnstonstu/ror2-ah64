using EntityStates;
using AH64.Survivors.Components;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Alternate primary. The M789 heavy cannon: eight very heavy, very accurate hitscan
    /// rounds, each with a real blast, out of a dedicated reinforced barrel.
    ///
    /// <para>Deliberately still a <b>cannon</b> — hitscan, flat, instant. That is the whole
    /// answer to "why not just use Hydra". Drop the rate of fire far enough and a slow
    /// primary starts competing with the rocket pods for the same fantasy; travel time and
    /// arc are what Hydra owns, and instant precision is what this owns.</para>
    ///
    /// <para>This is the only primary with per-shot camera shake. At the M230's 11 rounds
    /// per second, or the gatling's 18, shake would be a permanent rumble; at 2.5 each
    /// shot gets to land.</para>
    /// </summary>
    public class FireCannon : BaseSkillState
    {
        public static float damageCoefficient => AH64PlaytestConfig.CannonDamage;
        public static float baseDuration => AH64PlaytestConfig.CannonDuration;
        public static float range = AH64StaticValues.cannonRange;
        public static float force = AH64StaticValues.cannonForce;
        public static float minSpread = AH64StaticValues.cannonMinSpread;
        public static float maxSpread = AH64StaticValues.cannonMaxSpread;
        public static float spreadBloom = AH64StaticValues.cannonSpreadBloom;
        public static float procCoefficient = AH64StaticValues.cannonProcCoefficient;
        public static float bulletRadius = AH64StaticValues.cannonBulletRadius;
        public static float recoil = AH64StaticValues.cannonRecoil;
        public static float splashDamageCoefficient => AH64PlaytestConfig.CannonSplashDamage;
        public static float splashRadius => AH64PlaytestConfig.CannonSplashRadius;
        public static float splashProcCoefficient = AH64StaticValues.cannonSplashProcCoefficient;
        public static float splashForce = AH64StaticValues.cannonSplashForce;

        //Playtest 2026-08-03: Play_clayboss_m1_shoot was "hollow and weak". It is a mortar
        //LAUNCH — the soft thump of a shell leaving a tube, which is the wrong event
        //entirely for a cannon. What a heavy gun needs is the detonation of the charge.
        //
        //Play_clayboss_M1_explo (tried next) turned out to be a random container —
        //DurationMin/Max differ, meaning random selection across ten files from three
        //unrelated families, including env_vase_shatter_*. A report that changes character
        //shot to shot, and sometimes carries ceramic debris, reads as wrong even at the
        //right volume (see AUDIO_INVESTIGATION.md). Replaced with two deterministic Global
        //events layered on the same frame: a crack for the transient a detonation event
        //structurally lacks, and a heavy body for weight. Both have fixed (or near-fixed)
        //durations, so the report is identical every shot.
        private const string fireCrackSoundString = "Play_clayBruiser_attack1_shoot_bullet";
        private const string fireBodySoundString = "Play_clayboss_m2_explo";

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
        /// Same trap as the other two primaries: the SkillDef is InterruptPriority.Any, so
        /// leaving this at the default lets the state re-enter every physics tick and fire
        /// ~60 rounds/sec regardless of <see cref="duration"/>. On a weapon with an 8-round
        /// drum that would empty it in under a fifth of a second.
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

            Vector3 visualOrigin = AH64Muzzles.Origin(childLocator, "MuzzleCannon", aimRay);
            characterBody.AddSpreadBloom(spreadBloom);
            SpawnBarrelFlash(AH64Assets.chaingunMuzzleFlashEffect, visualOrigin, muzzleAim);
            SpawnBarrelFlash(AH64Assets.chaingunShellEjectEffect, visualOrigin, muzzleAim);
            Util.PlaySound(fireCrackSoundString, gameObject);
            Util.PlaySound(fireBodySoundString, gameObject);

            if (chinTurret)
                chinTurret.NotifyFired();

            //Shake is presentation, so it runs on every client, not just the authority.
            ShakeEmitter shake = ShakeEmitter.CreateSimpleShakeEmitter(
                muzzleOrigin,
                new Wave
                {
                    amplitude = AH64StaticValues.cannonShakeAmplitude,
                    frequency = AH64StaticValues.cannonShakeFrequency,
                    cycleOffset = 0f,
                },
                AH64StaticValues.cannonShakeDuration,
                AH64StaticValues.cannonShakeRadius,
                true);
            if (shake)
                shake.amplitudeTimeDecay = true;

            if (!isAuthority)
                return;

            //Four times the M230's kick. At 2.5 rounds/sec the crosshair has time to settle
            //between shots, so this reads as weight rather than as losing control.
            AddRecoil(-2.4f * recoil, -3.6f * recoil, -0.8f * recoil, 0.8f * recoil);

            bool crit = RollCrit();

            new BulletAttack
            {
                bulletCount = 1,
                aimVector = muzzleAim,
                origin = muzzleOrigin,
                damage = damageCoefficient * damageStat,
                damageColorIndex = DamageColorIndex.Default,
                damageType = DamageTypeCombo.GenericPrimary,
                falloffModel = BulletAttack.FalloffModel.None,
                maxDistance = range,
                force = force,
                hitMask = LayerIndex.CommonMasks.bullet,
                minSpread = minSpread,
                maxSpread = maxSpread,
                isCrit = crit,
                owner = gameObject,
                //The verified cosmetic anchor moves only the tracer start to the short
                //cannon muzzle. BulletAttack ray origin/aim above retain their gameplay values.
                muzzleName = "MuzzleCannon",
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
                hitCallback = CannonHitCallback,
            }.Fire();
        }

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
        /// Direct hit, then a large blast. The blast is where this weapon beats the other
        /// two primaries: at 9u it covers a pack, so against three targets it is well ahead
        /// even though its single-target sustained is slightly behind.
        /// </summary>
        private bool CannonHitCallback(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            bool result = BulletAttack.defaultHitCallback(bulletAttack, ref hitInfo);

            if (AH64Assets.cannonSplashEffect)
            {
                EffectManager.SpawnEffect(AH64Assets.cannonSplashEffect, new EffectData
                {
                    origin = hitInfo.point + hitInfo.surfaceNormal * 0.1f,
                    rotation = Util.QuaternionSafeLookRotation(hitInfo.surfaceNormal),
                    scale = AH64PlaytestConfig.CannonSplashVfxScale,
                }, true);
            }

            if (splashDamage <= 0f || splashRadius <= 0f)
                return result;

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
                //NearestHit here, unlike the other two primaries. Their reasoning was that
                //a LoS reject at 11-18 rps reads as "no splash"; at 2.5 rps each blast is
                //individually legible, so honouring cover is a feature rather than noise.
                losType = BlastAttack.LoSType.NearestHit,
            }.Fire();

            return result;
        }
    }
}
