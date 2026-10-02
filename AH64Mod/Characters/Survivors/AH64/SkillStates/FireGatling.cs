using EntityStates;
using AH64.Survivors.Components;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Alternate primary. The XM301 rotary cannon: a hitscan round at high cadence out of a
    /// 60-round drum, fired from a visibly spinning barrel cluster.
    ///
    /// <para>The identity is the <b>ramp</b>. Rate of fire is interpolated from the spool
    /// state held by <see cref="AH64GatlingSpin"/>, so the gun genuinely starts near the
    /// M230's cadence and winds up to roughly twice it. A gatling that reaches full rate on
    /// the first round reads as a reskin — the wind-up is the whole reason to pick it.</para>
    ///
    /// <para>Because rounds cost stock, a slower opening cadence also spends the drum more
    /// slowly, which is the correct behaviour: you are not being charged for the spool.</para>
    /// </summary>
    public class FireGatling : BaseSkillState
    {
        public static float damageCoefficient => AH64PlaytestConfig.GatlingDamage;
        public static float spooledDuration => AH64PlaytestConfig.GatlingSpooledDuration;
        public static float unspooledDuration = AH64StaticValues.gatlingUnspooledDuration;
        public static float range = AH64StaticValues.gatlingRange;
        public static float force = AH64StaticValues.gatlingForce;
        public static float minSpread = AH64StaticValues.gatlingMinSpread;
        public static float maxSpread => AH64PlaytestConfig.GatlingMaxSpread;
        public static float spreadBloom => AH64PlaytestConfig.GatlingBloom;
        public static float procCoefficient = AH64StaticValues.gatlingProcCoefficient;
        public static float bulletRadius = AH64StaticValues.gatlingBulletRadius;
        public static float recoil = AH64StaticValues.gatlingRecoil;
        public static float splashDamageCoefficient => AH64PlaytestConfig.GatlingSplashDamage;
        public static float splashRadius => AH64PlaytestConfig.GatlingSplashRadius;
        public static float splashProcCoefficient = AH64StaticValues.gatlingSplashProcCoefficient;
        public static float splashForce = AH64StaticValues.gatlingSplashForce;

        //Per-round report, layered UNDER the spool set that AH64GatlingSpin owns. Playtest
        //2026-08-03: the spool/loop pair alone read as "a motor spinning up" with no gunfire
        //on top. The loop supplies the continuous body, this supplies the individual reports.
        //Same event the M230 uses — one 30mm-class shot per round is the right cadence.
        private const string fireSoundString = "Play_clayBruiser_attack1_shoot_bullet";

        private float duration;
        private AH64ChinTurret chinTurret;
        private AH64GatlingSpin gatlingSpin;

        public override void OnEnter()
        {
            base.OnEnter();
            characterBody.SetAimTimer(2f);
            chinTurret = GetComponent<AH64ChinTurret>();
            gatlingSpin = GetComponent<AH64GatlingSpin>();

            //Tell the spool this round happened BEFORE reading the fraction, so a cold
            //start still registers as firing and begins winding up immediately.
            if (gatlingSpin)
                gatlingSpin.NotifyFiring();

            float spool = gatlingSpin ? gatlingSpin.SpoolFraction : 1f;
            duration = Mathf.Lerp(unspooledDuration, spooledDuration, spool) / attackSpeedStat;

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
        /// Same trap as <see cref="FireChaingun"/>: the SkillDef is InterruptPriority.Any, so
        /// leaving this at the default lets the state re-enter itself every physics tick and
        /// fire ~60 rounds/sec regardless of <see cref="duration"/>. Returning Skill holds it
        /// open for its full duration. Only the dash (Body / PrioritySkill) should cut in.
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

            Components.AH64FlightVisuals.Kick(gameObject, AH64StaticValues.kickGatlingPitch, 0f);

            if (!isAuthority)
                return;

            //Lighter than the M230's per round — there are nearly twice as many of them, and
            //stacking the M230's recoil at this cadence walks the crosshair off the screen.
            AddRecoil(-0.6f * recoil, -1.2f * recoil, -0.4f * recoil, 0.4f * recoil);

            bool crit = RollCrit();

            //Full-range coefficient. GatlingHitCallback applies the same 10m–30m ramp as the M230
            //before the hit lands.
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
                //Same reason as the M230: BulletAttack reattaches its tracer to muzzleName
                //even when origin is correct, and the exported Muzzle empty is stale.
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
                hitCallback = GatlingHitCallback,
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
        /// Direct hit, then a small blast. Splash proc is hard zero for the same reason as the
        /// M230's — at this cadence any non-zero splash proc would dominate the item economy,
        /// and the gatling fires faster still. Both take the M230's close-range damage ramp.
        /// </summary>
        private bool GatlingHitCallback(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            float rangeScale = AH64StaticValues.PrimaryRangeDamageScale(
                Vector3.Distance(bulletAttack.origin, hitInfo.point));
            bulletAttack.damage = damageCoefficient * damageStat * rangeScale;

            bool result = BulletAttack.defaultHitCallback(bulletAttack, ref hitInfo);

            if (AH64Assets.gatlingSplashEffect)
            {
                EffectManager.SpawnEffect(AH64Assets.gatlingSplashEffect, new EffectData
                {
                    origin = hitInfo.point + hitInfo.surfaceNormal * 0.08f,
                    rotation = Util.QuaternionSafeLookRotation(hitInfo.surfaceNormal),
                    scale = AH64PlaytestConfig.GatlingSplashVfxScale,
                }, true);
            }

            float splashDamage = splashDamageCoefficient * damageStat * rangeScale;
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
                //None, not NearestHit — same reasoning as the M230, more so at this cadence.
                losType = BlastAttack.LoSType.None,
            }.Fire();

            return result;
        }
    }
}
