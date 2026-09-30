using AH64.Survivors.Components;
using EntityStates;
using RoR2;
using RoR2.Audio;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// The AH-64's death: a crash. The body was cloned from Commando, whose death state freezes the
    /// motor and ragdolls the model. The airframe has no ragdoll, so the aircraft just hung in the air
    /// until the body timed out and vanished.
    ///
    /// <para>Shaped like vanilla <c>EntityStates.Drone.DeathState</c>, verified in RoR2.dll: the server
    /// destroys the body on impact (or after <c>crashMaxDuration</c>), and every client plays the
    /// explosion from <see cref="OnExit"/>, which <c>EntityStateMachine.OnDestroy</c> calls when the
    /// body goes. No extra network messages. The descent itself is scripted by the authority through
    /// <see cref="AH64HoverController.ApplyCrash"/>; the tail spin and smoke are local presentation.</para>
    /// </summary>
    public class AH64Death : GenericCharacterDeath
    {
        //Vanilla drone crash, verified in the game's SoundbanksInfo.xml: failing engine, then the wreck.
        private const string crashStartSound = "Play_drone_deathpt1";
        private const string crashImpactSound = "Play_drone_deathpt2";

        private AH64HoverController hover;
        private AH64FlightVisuals flightVisuals;
        private bool crashing;
        private bool exploded;
        private float smokeTimer;
        private bool smokeLeft;

        //The server ends the state by destroying the body on impact; GenericCharacterDeath's rest and
        //fall timers would destroy a slow crash early, or leave a hovering wreck for 10 seconds.
        public override bool shouldAutoDestroy => false;

        public override void OnEnter()
        {
            base.OnEnter();

            hover = GetComponent<AH64HoverController>();
            flightVisuals = GetComponent<AH64FlightVisuals>();

            //Void and model-destroying deaths have already removed the airframe, and a body that dies
            //in its drop pod has nowhere to fall.
            crashing = cachedModelTransform && !isVoidDeath && !destroyModelOnDeath
                && !(characterBody && characterBody.currentVehicle);

            //The wreck goes up with the body. A preserved model would be left behind as an intact,
            //motionless corpse at the crash site.
            if (modelLocator)
                modelLocator.preserveModel = false;

            if (!crashing)
                return;

            //Same spin on every client, without syncing anything.
            float spinSign = characterBody && characterBody.netId.Value % 2 == 0 ? 1f : -1f;
            if (flightVisuals)
                flightVisuals.PlayCrash(spinSign);
            if (isAuthority && hover)
                hover.BeginCrash();
        }

        public override void PlayDeathSound()
        {
            //Replaces the death cry inherited from the Commando clone.
            Util.PlaySound(crashStartSound, gameObject);
        }

        public override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
        {
            //Rigid airframe: no death clip. The crash is procedural (AH64FlightVisuals.PlayCrash).
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (crashing && isAuthority && hover)
                hover.ApplyCrash(fixedAge);

            if (!NetworkServer.active || fixedAge < AH64StaticValues.crashMinDuration)
                return;

            if (!crashing || fixedAge >= AH64StaticValues.crashMaxDuration || HasHitGround())
                EntityState.Destroy(gameObject);
        }

        private bool HasHitGround()
        {
            if (characterMotor && characterMotor.isGrounded)
                return true;
            if (!characterBody)
                return false;

            //Same test as Drone.DeathState. On the server a client pilot's body is only a synced
            //transform, so isGrounded alone can't be trusted there.
            return Physics.Raycast(characterBody.corePosition, Vector3.down, characterBody.radius + 1f,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
        }

        public override void Update()
        {
            base.Update();
            if (!crashing || exploded || !flightVisuals)
                return;

            smokeTimer -= Time.deltaTime;
            if (smokeTimer > 0f)
                return;
            smokeTimer = AH64StaticValues.crashSmokeInterval;

            GameObject smoke = AH64Assets.hydraMuzzleFlashEffect;
            if (!smoke)
                return;

            smokeLeft = !smokeLeft;
            EffectManager.SpawnEffect(smoke, new EffectData
            {
                origin = flightVisuals.GetEngineSmokeOrigin(smokeLeft),
                rotation = Util.QuaternionSafeLookRotation(Vector3.up),
            }, false);
        }

        public override void OnExit()
        {
            if (crashing && !exploded)
                Explode();

            base.OnExit();
        }

        private void Explode()
        {
            exploded = true;

            Vector3 position = cachedModelTransform
                ? cachedModelTransform.position + Vector3.up * 0.8f
                : characterBody ? characterBody.corePosition : transform.position;

            //Local on every client: each one reaches OnExit when the body is destroyed. The Hellfire
            //warhead effect carries its own blast sound and camera shake.
            if (AH64Assets.hellfireExplosionEffect)
            {
                EffectManager.SpawnEffect(AH64Assets.hellfireExplosionEffect, new EffectData
                {
                    origin = position,
                    scale = 1f,
                }, false);
            }

            //A point emitter, because the body's own sound object is being destroyed with it.
            PointSoundManager.EmitSoundLocal(crashImpactSound, position);

            DestroyModel();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Death;
        }
    }
}
