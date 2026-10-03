using AH64.Survivors.Components;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.SkillStates
{
    // Inheriting Main keeps collective/descent, airtime and native weapon inputs alive.
    // All bespoke motion is horizontal and applied by a narrowly owned PreMove hook.
    public sealed class BrakingTurn : AH64Main
    {
        private AH64BrakingTurnCapture capture;
        private bool hasCapture;
        private AH64BrakingTurnMotor motion;
        private AH64BrakingTurnPresentation presentation;
        internal AH64BrakingTurnCapture EntrySnapshot => capture;
        internal float ManeuverProgress => hasCapture ? Mathf.Clamp01(fixedAge / capture.Duration) : 0f;
        internal bool MotionYielded => motion && motion.IsYielding;

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority && !hasCapture)
            {
                AH64FlightVisuals visuals = GetComponent<AH64FlightVisuals>();
                capture = AH64BrakingTurnCapture.Create(
                    characterMotor ? characterMotor.velocity : Vector3.zero,
                    inputBank ? inputBank.moveVector : Vector3.zero,
                    inputBank ? inputBank.aimDirection : Vector3.zero,
                    characterDirection ? characterDirection.forward : transform.forward,
                    visuals ? visuals.CaptureAttitude() : Quaternion.identity, moveSpeedStat);
                hasCapture = true;
            }
            // Received capture is never overwritten by remote OnEnter.
            if (!hasCapture) return;
            presentation = GetComponent<AH64BrakingTurnPresentation>();
            if (!presentation) presentation = gameObject.AddComponent<AH64BrakingTurnPresentation>();
            presentation.Begin(this, capture);
            if (isAuthority && characterMotor)
            {
                motion = GetComponent<AH64BrakingTurnMotor>();
                if (!motion) motion = gameObject.AddComponent<AH64BrakingTurnMotor>();
                motion.Begin(this, () => isAuthority, capture);
            }
        }

        public override void HandleMovements()
        {
            if (motion) motion.Check(this);
            Vector3 before = characterMotor ? characterMotor.velocity : Vector3.zero;
            base.HandleMovements();
            if (motion) motion.ObserveMovement(this, before);
        }

        public override void FixedUpdate()
        {
            if (motion) motion.Check(this);
            base.FixedUpdate();
            if (!hasCapture) return;
            if (presentation) presentation.Progress(this, fixedAge);
            if (isAuthority && (fixedAge >= capture.Duration || MotionYielded))
                outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            if (motion) motion.Release(this);
            if (presentation) presentation.End(this);
            base.OnExit();
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            capture.Write(writer);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            capture = AH64BrakingTurnCapture.Read(reader);
            hasCapture = true;
        }

        public override InterruptPriority GetMinimumInterruptPriority() { return InterruptPriority.Pain; }
    }
}
