using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    public sealed class AH64BombingRunDamage : MonoBehaviour, IOnDamageInflictedServerReceiver
    {
        private DamageInfo pendingInfo;
        private HealthComponent pendingTarget;
        private float reportedDamage;

        internal void Detonate(AH64BombingRunCast cast, int drop, bool crit)
        {
            if (!NetworkServer.active || !cast.OwnerAlive || !cast.SameStage) return;
            // Public native hit collection supplies team filtering, nearest hurtbox and world LOS.
            BlastAttack.Result candidates = new BlastAttack
            {
                attacker = cast.Owner.gameObject, teamIndex = cast.Team,
                attackerFiltering = AttackerFiltering.NeverHitSelf,
                position = transform.position, radius = AH64BombingRunStaticValues.BlastRadius,
                losType = BlastAttack.LoSType.NearestHit
            }.FireNoDamage();
            var seen = new HashSet<HealthComponent>();
            foreach (BlastAttack.HitPoint hit in candidates.hitPoints)
            {
                if (!cast.OwnerAlive || !cast.SameStage) break;
                HealthComponent target = hit.hurtBox ? hit.hurtBox.healthComponent : null;
                if (!target || !target.alive || !seen.Add(target)) continue;
                if (!cast.Policy.TryBeginHit(target))
                {
                    cast.Trace("hit-suppressed", drop, hit.hitPosition, reason: "overlap-budget", target: target);
                    continue;
                }
                ApplyHit(cast, drop, crit, target, hit);
            }
        }

        private void ApplyHit(AH64BombingRunCast cast, int drop, bool crit,
            HealthComponent target, BlastAttack.HitPoint hit)
        {
            pendingTarget = target;
            reportedDamage = 0f;
            pendingInfo = new DamageInfo
            {
                attacker = cast.Owner.gameObject, inflictor = gameObject,
                damage = cast.Damage, crit = crit, position = hit.hitPosition,
                damageType = new DamageTypeCombo { damageType = DamageType.AOE, damageSource = DamageSource.Special },
                procCoefficient = AH64BombingRunStaticValues.ProcCoefficient,
                procChainMask = default(ProcChainMask), inflictedHurtbox = hit.hurtBox,
                damageColorIndex = DamageColorIndex.Default
            };
            pendingInfo.ModifyDamageInfo(hit.hurtBox.damageModifier);
            bool accepted = false;
            try
            {
                target.TakeDamage(pendingInfo);
                accepted = reportedDamage > 0f && !pendingInfo.rejected;
                cast.Policy.CompleteHit(target, accepted);
                cast.Trace(accepted ? "hit" : "hit-rejected", drop, hit.hitPosition,
                    target: target, dealt: reportedDamage, crit: crit,
                    proc: accepted ? AH64BombingRunStaticValues.ProcCoefficient : 0f);
                if (accepted && GlobalEventManager.instance)
                {
                    GlobalEventManager.instance.OnHitEnemy(pendingInfo, target.gameObject);
                    GlobalEventManager.instance.OnHitAll(pendingInfo, target.gameObject);
                }
            }
            finally
            {
                // Idempotent when committed above; exceptions cannot strand a reservation.
                cast.Policy.CompleteHit(target, reportedDamage > 0f && pendingInfo != null && !pendingInfo.rejected);
                pendingInfo = null;
                pendingTarget = null;
            }
        }

        public void OnDamageInflictedServer(DamageReport report)
        {
            if (NetworkServer.active && report != null && ReferenceEquals(report.damageInfo, pendingInfo)
                && report.victim == pendingTarget && report.damageDealt > 0f)
                reportedDamage += report.damageDealt;
        }
    }
}
