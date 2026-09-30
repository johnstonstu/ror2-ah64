using EntityStates.Headstompers;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Jump- and landing-based items. The hover never grounds and <c>AH64Main.ProcessJump</c>
    /// swallows the vanilla jump, so without this Wax Quail does nothing, H3AD-5T v2 starts a slam
    /// that can never land, and nothing listening to <c>CharacterBody.onJump</c> ever fires.
    /// See <c>AH64StaticValues.restAltitudeBand</c>.
    /// </summary>
    internal partial class AH64HoverController
    {
        private static bool itemHooksInstalled;

        //Stands in for the jump count a grounded survivor gets back on landing: one collective tap
        //per climb, re-armed once the chopper settles back to rest altitude with the collective released.
        private bool collectiveTapArmed = true;
        private float waxQuailReadyTime;

        /// <summary>True while the chopper is holding (roughly) its resting altitude over real terrain.</summary>
        public bool IsAtRestAltitude =>
            GroundDistance >= 0f
            && GroundDistance <= GetRestHeight() + AH64StaticValues.restAltitudeBand;

        internal static void InstallItemHooks()
        {
            if (itemHooksInstalled)
                return;

            itemHooksInstalled = true;
            On.EntityStates.Headstompers.HeadstompersIdle.FixedUpdateAuthority += HeadstompersIdle_FixedUpdateAuthority;
            On.EntityStates.Headstompers.HeadstompersFall.FixedUpdateAuthority += HeadstompersFall_FixedUpdateAuthority;
            On.RoR2.CharacterBody.UpdateNotMoving += CharacterBody_UpdateNotMoving;
            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
        }

        //server-only; see AH64StaticValues.airtimeKillPause
        private float killPauseReadyTime;

        private static void OnCharacterDeathGlobal(DamageReport report)
        {
            CharacterBody attacker = report.attackerBody;
            if (!NetworkServer.active || !attacker || !AH64Buffs.killAirtimeBuff)
                return;

            AH64HoverController hover = attacker.GetComponent<AH64HoverController>();
            if (!hover || Time.fixedTime < hover.killPauseReadyTime)
                return;

            hover.killPauseReadyTime = Time.fixedTime + AH64StaticValues.airtimeKillPauseCooldown;
            attacker.AddTimedBuff(AH64Buffs.killAirtimeBuff, AH64StaticValues.airtimeKillPause);
        }

        private static BodyIndex ah64BodyIndex = BodyIndex.None;

        //Bustling Fungus (and anything else reading GetNotMoving) needs under 0.1 u/s of travel for a
        //second. The altitude hold never sits perfectly still vertically, so the stopwatch kept resetting
        //and the zone never appeared. For the AH-64 only horizontal travel counts as moving.
        private static void CharacterBody_UpdateNotMoving(
            On.RoR2.CharacterBody.orig_UpdateNotMoving orig, CharacterBody self, float deltaTime)
        {
            if (ah64BodyIndex == BodyIndex.None)
                ah64BodyIndex = BodyCatalog.FindBodyIndex("AH64Body");

            if (!NetworkServer.active || ah64BodyIndex == BodyIndex.None || self.bodyIndex != ah64BodyIndex)
            {
                orig(self, deltaTime);
                return;
            }

            Vector3 position = self.transform.position;
            Vector3 travel = position - self.previousPosition;
            travel.y = 0f;
            float threshold = 0.1f * deltaTime;
            self.notMovingStopwatch = travel.sqrMagnitude <= threshold * threshold
                ? self.notMovingStopwatch + deltaTime
                : 0f;
            self.previousPosition = position;
        }

        /// <summary>
        /// A collective tap from <c>AH64Main.ProcessJump</c>, i.e. the moment a vanilla survivor jumps.
        /// Authority only, like vanilla <c>ProcessJump</c>.
        /// </summary>
        public void OnCollectiveTapped()
        {
            if (!motor || !body || externalLaunchActive || equipmentFlightActive)
                return;

            if (Time.fixedTime >= waxQuailReadyTime && ApplyWaxQuailBoost())
                waxQuailReadyTime = Time.fixedTime + AH64StaticValues.waxQuailCooldown;

            if (collectiveTapArmed)
            {
                collectiveTapArmed = false;
                body.TriggerJumpEventGlobally();
            }
        }

        private void UpdateCollectiveTapArm(bool jumpHeld)
        {
            if (!jumpHeld && IsAtRestAltitude)
                collectiveTapArmed = true;
        }

        /// <summary>
        /// Vanilla's Wax Quail formula from <c>GenericCharacterMain.ProcessJump</c>: the extra speed is
        /// sized against the air deceleration, so the boost covers the same distance on any body.
        /// On the hover's full air control that is a short, hard burst rather than a long glide.
        /// </summary>
        private bool ApplyWaxQuailBoost()
        {
            int quails = body.inventory ? body.inventory.GetItemCountEffective(RoR2Content.Items.JumpBoost) : 0;
            if (quails <= 0 || !body.isSprinting)
                return false;

            float deceleration = body.acceleration * motor.airControl;
            Vector3 direction = motor.moveDirection;
            direction.y = 0f;
            if (deceleration <= 0f || direction.sqrMagnitude < 0.01f)
                return false;

            direction.Normalize();
            float boost = Mathf.Sqrt(10f * quails * deceleration);
            Vector3 velocity = direction * (body.moveSpeed + boost);
            velocity.y = motor.velocity.y;
            motor.velocity = velocity;

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BoostJumpEffect"), new EffectData
            {
                origin = body.footPosition,
                rotation = Util.QuaternionSafeLookRotation(velocity),
            }, true);
            return true;
        }

        /// <summary>
        /// While H3AD-5T v2 is slamming, hand the vertical axis to vanilla physics so the fall the item
        /// drives isn't braked by the altitude hold. The slam detonates at rest altitude (hook below).
        /// </summary>
        private bool YieldToHeadstompSlam()
        {
            if (!(BaseHeadstompersState.FindForBody(body) is HeadstompersFall))
                return false;

            UseVanillaPhysics(AH64StaticValues.hoverAirControl);
            ResetAltitudeToRest();
            timeWithoutGround = 0f;
            return true;
        }

        private static AH64HoverController FindForHeadstompers(BaseHeadstompersState state)
        {
            return state.body ? state.body.GetComponent<AH64HoverController>() : null;
        }

        //At rest altitude the chopper is "standing": no slam can be armed, so interacting with a chest
        //(the slam key is interact) never drops it. Above the band, vanilla arms the slam as usual.
        private static void HeadstompersIdle_FixedUpdateAuthority(
            On.EntityStates.Headstompers.HeadstompersIdle.orig_FixedUpdateAuthority orig, HeadstompersIdle self)
        {
            AH64HoverController hover = FindForHeadstompers(self);
            if (hover && hover.IsAtRestAltitude)
            {
                self.inputStopwatch = 0f;
                return;
            }

            orig(self);
        }

        //Vanilla detonates on isGrounded, which the hover never reports. Reaching rest altitude is our landing.
        private static void HeadstompersFall_FixedUpdateAuthority(
            On.EntityStates.Headstompers.HeadstompersFall.orig_FixedUpdateAuthority orig, HeadstompersFall self)
        {
            AH64HoverController hover = FindForHeadstompers(self);
            if (hover && hover.IsAtRestAltitude)
            {
                self.DoStompExplosionAuthority();
                return;
            }

            orig(self);
        }
    }
}
