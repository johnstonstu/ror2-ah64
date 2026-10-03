using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Special. One AGM-114 Hellfire off an inboard wing rail. Flies flat and fast rather than arcing, so
    /// it is aimed rather than lobbed, and it lives on the "Weapon2" state machine so it can be launched
    /// without ever dropping chain gun fire.
    ///
    /// <para>Press launches immediately; held special designates the latest lead with native aim.
    /// Release slows; holding again resumes the live lead. Guidance lives on the owner/projectile, so this state never holds other guns.</para>
    /// </summary>
    public class FireHellfire : GenericProjectileBaseState
    {
        public static float BaseDuration = 0.7f;
        //delays before a projectile leaves feel terrible. keep this at 0 unless the animation demands it
        public static float BaseDelayDuration = 0.0f;

        public static float DamageCoefficient = AH64StaticValues.hellfireDamageCoefficient;

        //Which rail this launch comes off. Static, so it alternates across launches rather than always
        //picking the left one — a state instance is created fresh per activation and can't remember.
        //Shared by every AH-64 in a lobby, which is harmless: it only decides which side the missile and
        //its flash come from, never whether the shot happens or what it does.
        private static int launchCount;

        //the rail this particular launch chose, resolved once in OnEnter so the flash and the spawn
        //position can't disagree
        private string railMuzzle;

        //the main missile as launched, copied for the Pocket I.C.B.M. extras
        private FireProjectileInfo launchedInfo;
        private Components.AH64HellfireOwner designation;

        //vanilla Wwise event — Engineer's seeker missile launch, verified against SoundbanksInfo.xml.
        //Deliberately heavier than the Hydra pods' AtG launch, so one Hellfire never sounds like one more
        //rocket out of the salvo.
        private const string fireSoundString = "Play_engi_seekerMissile_shoot";

        public override void OnEnter()
        {
            //both rails alternate; ResolveName falls back to the chin muzzle if a wing anchor is missing
            string requestedRail = (launchCount++ % 2 == 0) ? AH64Muzzles.MissileL : AH64Muzzles.MissileR;
            railMuzzle = AH64Muzzles.ResolveName(GetModelChildLocator(), requestedRail);

            projectilePrefab = AH64Assets.hellfireProjectilePrefab;
            effectPrefab = AH64Assets.hellfireMuzzleFlashEffect;
            targetMuzzle = railMuzzle;

            attackSoundString = fireSoundString;

            baseDuration = BaseDuration;
            baseDelayBeforeFiringProjectile = BaseDelayDuration;

            damageCoefficient = DamageCoefficient;
            //proc coefficient is set on the components of the projectile prefab
            force = 20f;

            recoilAmplitude = 1.5f;
            bloom = 8f;

            base.OnEnter();
            // GenericProjectileBaseState normally waits until FixedUpdate even for zero delay.
            // Fire on entry and mark it fired, preserving its usual short recovery/cooldown state.
            firedProjectile = true;
            FireProjectile();
            DoFireEffects();
        }

        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            fireProjectileInfo.damageTypeOverride = DamageTypeCombo.GenericSpecial;

            // Converge from the actual rail on the first reachable native-aim world point.
            Ray aim = GetAimRay();
            fireProjectileInfo.position = AH64Muzzles.Origin(GetModelChildLocator(), railMuzzle, aim);
            Vector3 point = Components.AH64HellfireAim.Resolve(characterBody, aim);
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(
                Components.AH64HellfireAim.Converge(fireProjectileInfo.position, point, aim.direction));
            fireProjectileInfo.comboNumber = 0;

            //Pocket I.C.B.M. extras go out in FireProjectile below. No MissileUtils damage scaling on top:
            //three full Hellfires already triple the payload, and with the multiplier it played far too strong.
            launchedInfo = fireProjectileInfo;
        }

        public override void FireProjectile()
        {
            //Runs on every client (the base only spawns the projectile on the authority), so remote
            //aircraft kick too. The launching rail's side lifts.
            Components.AH64FlightVisuals.Kick(gameObject, AH64StaticValues.kickHellfirePitch,
                (railMuzzle == AH64Muzzles.MissileL ? 1f : -1f) * AH64StaticValues.kickHellfireRoll);

            if (isAuthority && projectilePrefab)
            {
                Components.AH64HellfireOwner.Install(gameObject);
                designation = Components.AH64HellfireOwner.GetOrAdd(gameObject);
                designation.BeginLaunch();
            }
            base.FireProjectile();

            if (!isAuthority || MoreMissileCount() <= 0 || !launchedInfo.projectilePrefab)
                return;

            //See AH64StaticValues.hellfireIcbmFanAngle for why this is not vanilla's ±45°.
            for (int side = -1; side <= 1; side += 2)
            {
                FireProjectileInfo extra = launchedInfo;
                extra.comboNumber = (byte)(side < 0 ? 1 : 2);
                extra.rotation = Quaternion.AngleAxis(side * AH64StaticValues.hellfireIcbmFanAngle, Vector3.up)
                    * launchedInfo.rotation;
                ProjectileManager.instance.FireProjectile(extra);
            }
        }

        public override void OnExit()
        {
            // Normal recovery leaves guidance with the owner. An interrupted launch closes it.
            if (isAuthority && designation && stopwatch < duration)
                designation.StopWatching();
            base.OnExit();
        }

        private int MoreMissileCount()
        {
            return characterBody && characterBody.inventory
                ? characterBody.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile)
                : 0;
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
