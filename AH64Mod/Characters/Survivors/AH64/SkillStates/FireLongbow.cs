using System.Collections.Generic;
using EntityStates;
using RoR2;
using UnityEngine;
using AH64.Modules;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Launch half of Longbow. Walks the list <see cref="PaintLongbow"/> built and fires one guided
    /// missile every <see cref="AH64StaticValues.longbowFireInterval"/> from alternating inboard rails.
    /// Later locks in the salvo deal more damage (hybrid count + damage scale).
    /// </summary>
    public class FireLongbow : BaseSkillState
    {
        public static float baseFireInterval = AH64StaticValues.longbowFireInterval;

        public List<HurtBox> targets;

        /// <summary>
        /// Slot missiles were drawn from. Passed by hand because <c>SetNextState</c> does not set
        /// <c>activatorSkillSlot</c> the way SkillDef instantiation does.
        /// </summary>
        public GenericSkill launcher;

        private float fireInterval;
        private float stopwatch;
        private int fireIndex;

        public override void OnEnter()
        {
            base.OnEnter();

            fireInterval = baseFireInterval / attackSpeedStat;
            //primed so release-to-fire feels immediate
            stopwatch = fireInterval;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            characterBody.SetAimTimer(1f);

            if (!isAuthority)
                return;

            stopwatch += GetDeltaTime();

            if (stopwatch >= fireInterval)
            {
                stopwatch -= fireInterval;

                while (targets != null && fireIndex < targets.Count)
                {
                    HurtBox target = targets[fireIndex];
                    int lockIndex = fireIndex;
                    fireIndex++;

                    if (!target || !target.healthComponent || !target.healthComponent.alive)
                    {
                        launcher?.AddOneStock();
                        continue;
                    }

                    FireMissile(target, lockIndex);
                    break;
                }
            }

            if (targets == null || fireIndex >= targets.Count)
                outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (isAuthority && targets != null)
            {
                for (int i = fireIndex; i < targets.Count; i++)
                    launcher?.AddOneStock();
            }

            base.OnExit();
        }

        private void FireMissile(HurtBox target, int lockIndex)
        {
            if (!AH64Assets.LongbowProjectile)
            {
                //asset miss — don't eat the stock for a shot that never left the rail
                launcher?.AddOneStock();
                Log.Error("FireLongbow: LongbowProjectile is null; refunding stock.");
                return;
            }

            ChildLocator childLocator = GetModelChildLocator();
            string requestedRail = (lockIndex % 2 == 0)
                ? AH64Muzzles.MissileL
                : AH64Muzzles.MissileR;
            string rail = AH64Muzzles.ResolveName(childLocator, requestedRail);

            //Warm ignition core plus smoke-ring exhaust, both existing registered effects.
            if (AH64Assets.hellfireMuzzleFlashEffect)
                EffectManager.SimpleMuzzleFlash(AH64Assets.hellfireMuzzleFlashEffect, gameObject, rail, true);
            if (AH64Assets.hydraMuzzleFlashEffect)
                EffectManager.SimpleMuzzleFlash(AH64Assets.hydraMuzzleFlashEffect, gameObject, rail, true);

            //Same compact AtG launch cue as the Hydra pods: enough motor bite for an alternating
            //six-missile rail ripple without stacking like Engineer's long seeker launch.
            Components.AH64LaunchSound.Play(gameObject);

            float damageCoefficient = AH64StaticValues.longbowDamageBase
                + lockIndex * AH64StaticValues.longbowDamagePerLock;

            Ray aimRay = GetAimRay();
            Vector3 railDirection = AH64Muzzles.AimDirection(
                childLocator,
                requestedRail,
                aimRay,
                AH64StaticValues.longbowLockDistance);

            //Leave the inboard rail toward the reticle; inherited steering takes over immediately.
            MissileUtils.FireMissile(
                AH64Muzzles.Origin(childLocator, requestedRail, aimRay),
                characterBody,
                default(ProcChainMask),
                target.gameObject,
                damageStat * damageCoefficient,
                RollCrit(),
                AH64Assets.LongbowProjectile,
                DamageColorIndex.Default,
                railDirection,
                0f,
                addMissileProc: false);
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
