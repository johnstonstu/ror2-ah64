using System.Collections.Generic;
using EntityStates;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Special (Longbow). Hold special to paint locks on the reticle target; release to fire.
    /// Primary and secondary stay free the whole time (no Engi confirm/cancel overrides). Missiles
    /// are spent here via <c>DeductStock</c>, not by the SkillDef (<c>stockToConsume = 0</c>).
    ///
    /// <para>Runs on "Weapon2" so painting never interrupts the chain gun or Hydra pods.</para>
    /// </summary>
    public class PaintLongbow : BaseSkillState
    {
        public static float lockInterval = AH64StaticValues.longbowLockInterval;
        public static float maxLockAngle = AH64StaticValues.longbowLockAngle;
        public static float maxLockDistance = AH64StaticValues.longbowLockDistance;
        public static float maxPaintDuration = AH64StaticValues.longbowMaxPaintDuration;

        private const string openSoundString = "Play_engi_seekerMissile_HUD_open";
        private const string closeSoundString = "Play_engi_seekerMissile_HUD_close";
        private const string loopSoundString = "Play_engi_seekerMissile_HUD_loop";
        private const string stopLoopSoundString = "Stop_engi_seekerMissile_HUD_loop";
        private const string lockOnSoundString = "Play_engi_seekerMissile_lockOn";

        private List<HurtBox> targets;
        private Dictionary<HurtBox, LongbowLockIndicator> indicators;
        private Indicator stickyTargetIndicator;
        private BullseyeSearch search;
        private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

        private float lockStopwatch;
        private float paintStopwatch;
        private bool handedOff;
        //true once we've seen special held this activation — release after that fires or exits
        private bool sawButtonDown;

        private HealthComponent previousHighlightTargetHealthComponent;
        private HurtBox previousHighlightTargetHurtBox;

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority)
            {
                targets = new List<HurtBox>();
                indicators = new Dictionary<HurtBox, LongbowLockIndicator>();
                search = new BullseyeSearch();

                if (AH64Assets.LongbowLockIndicatorPrefab)
                    stickyTargetIndicator = new Indicator(gameObject, AH64Assets.LongbowLockIndicatorPrefab);
            }

            Util.PlaySound(openSoundString, gameObject);
            Util.PlaySound(loopSoundString, gameObject);

            if (AH64Assets.LongbowCrosshair)
                crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(characterBody, AH64Assets.LongbowCrosshair, CrosshairUtils.OverridePriority.Skill);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            characterBody.SetAimTimer(3f);
            paintStopwatch += GetDeltaTime();

            if (!isAuthority)
                return;

            if (paintStopwatch >= maxPaintDuration)
            {
                Salvo();
                return;
            }

            AuthorityFixedUpdate();
        }

        public override void OnExit()
        {
            if (isAuthority && !handedOff)
                RefundAll();

            if (indicators != null)
            {
                foreach (KeyValuePair<HurtBox, LongbowLockIndicator> pair in indicators)
                    pair.Value.active = false;
            }

            if (stickyTargetIndicator != null)
                stickyTargetIndicator.active = false;

            crosshairOverrideRequest?.Dispose();

            Util.PlaySound(closeSoundString, gameObject);
            Util.PlaySound(stopLoopSoundString, gameObject);

            base.OnExit();
        }

        private void AuthorityFixedUpdate()
        {
            DropDeadLocks();

            HurtBox candidate = FindTarget();

            //Special is inputBank.skill4 (skill3 is utility) — see GenericCharacterMain.HandleSkill.
            InputBankTest.ButtonState special = inputBank.skill4;

            if (special.down)
            {
                sawButtonDown = true;

                //hold special: auto-lock under the reticle at the usual Engi cadence
                if (candidate)
                {
                    lockStopwatch += GetDeltaTime();
                    bool sameTarget = previousHighlightTargetHealthComponent == candidate.healthComponent;
                    if (activatorSkillSlot
                        && activatorSkillSlot.stock > 0
                        && (!sameTarget || lockStopwatch >= lockInterval / attackSpeedStat || special.justPressed))
                    {
                        lockStopwatch = 0f;
                        AddLock(candidate);
                    }
                }
            }
            else if (sawButtonDown)
            {
                //release: fire if anything was locked, otherwise just exit
                Salvo();
                return;
            }

            if ((object)candidate != previousHighlightTargetHurtBox)
            {
                previousHighlightTargetHurtBox = candidate;
                previousHighlightTargetHealthComponent = candidate ? candidate.healthComponent : null;
                if (stickyTargetIndicator != null)
                {
                    stickyTargetIndicator.targetTransform =
                        (candidate && activatorSkillSlot && activatorSkillSlot.stock > 0) ? candidate.transform : null;
                }
                lockStopwatch = 0f;
            }

            if (stickyTargetIndicator != null)
                stickyTargetIndicator.active = stickyTargetIndicator.targetTransform;
        }

        private HurtBox FindTarget()
        {
            Ray aimRay = GetAimRay();

            search.filterByDistinctEntity = true;
            search.filterByLoS = true;
            search.minDistanceFilter = 0f;
            search.maxDistanceFilter = maxLockDistance;
            search.minAngleFilter = 0f;
            search.maxAngleFilter = maxLockAngle;
            search.viewer = characterBody;
            search.searchOrigin = aimRay.origin;
            search.searchDirection = aimRay.direction;
            search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            search.teamMaskFilter = TeamMask.GetUnprotectedTeams(GetTeam());
            search.RefreshCandidates();
            search.FilterOutGameObject(gameObject);

            foreach (HurtBox result in search.GetResults())
            {
                if (result && result.healthComponent && result.healthComponent.alive)
                    return result;
            }

            return null;
        }

        private void AddLock(HurtBox hurtBox)
        {
            targets.Add(hurtBox);
            activatorSkillSlot.DeductStock(1);
            Util.PlaySound(lockOnSoundString, gameObject);

            if (!AH64Assets.LongbowLockIndicatorPrefab)
                return;

            if (!indicators.TryGetValue(hurtBox, out LongbowLockIndicator indicator))
            {
                indicator = new LongbowLockIndicator(gameObject, AH64Assets.LongbowLockIndicatorPrefab)
                {
                    targetTransform = hurtBox.transform,
                    active = true,
                };

                indicators[hurtBox] = indicator;
            }

            indicator.missileCount++;
        }

        private void DropDeadLocks()
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                HurtBox target = targets[i];

                if (target && target.healthComponent && target.healthComponent.alive)
                    continue;

                targets.RemoveAt(i);

                if (target && indicators.TryGetValue(target, out LongbowLockIndicator indicator))
                {
                    indicator.missileCount--;

                    if (indicator.missileCount <= 0)
                    {
                        indicator.active = false;
                        indicators.Remove(target);
                    }
                }

                activatorSkillSlot?.AddOneStock();
            }
        }

        private void Salvo()
        {
            if (targets == null || targets.Count == 0)
            {
                outer.SetNextStateToMain();
                return;
            }

            handedOff = true;
            outer.SetNextState(new FireLongbow { targets = targets, launcher = activatorSkillSlot });
        }

        private void RefundAll()
        {
            if (targets == null || activatorSkillSlot == null)
                return;

            for (int i = 0; i < targets.Count; i++)
                activatorSkillSlot.AddOneStock();

            targets.Clear();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
