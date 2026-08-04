using EntityStates;
using RoR2;
using RoR2.Projectile;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Special. One AGM-114 Hellfire off an inboard wing rail. Flies flat and fast rather than arcing, so
    /// it is aimed rather than lobbed, and it lives on the "Weapon2" state machine so it can be launched
    /// without ever dropping chain gun fire.
    ///
    /// <para>Deliberately dumb-fire. Real Hellfires are laser-guided and RoR2 models that with
    /// ProjectileSteerTowardTarget + ProjectileTargetComponent, but guidance changes how the skill plays
    /// enough to want its own playtest instead of riding along with the Phase 3 kit swap.</para>
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
        }

        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            fireProjectileInfo.damageTypeOverride = DamageTypeCombo.GenericSpecial;

            //GenericProjectileBaseState spawns at the AIM RAY ORIGIN, not at the muzzle — targetMuzzle
            //only ever drives the muzzle flash. Left alone the missile would appear out of the pilot's
            //eyeline while the flash lit up a wing rail, which is exactly the kind of mismatch that looks
            //broken without being obvious why. No crosshair convergence needed to go with it: the rail is
            //0.78u off centre and the warhead's blast radius is 12u, so the offset is inside the splash.
            fireProjectileInfo.position = AH64Muzzles.Origin(GetModelChildLocator(), railMuzzle, GetAimRay());
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
