using System;
using EntityStates.Headstompers;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    // Horizontal-only lease. AH64Main + native PreMove still own vertical motion.
    internal sealed class AH64BrakingTurnMotor : MonoBehaviour
    {
        private CharacterMotor motor;
        private CharacterBody body;
        private object owner;
        private Func<bool> authority;
        private AH64BrakingTurnCapture capture;
        private float stepAge;
        private bool hooked;
        public bool IsYielding { get; private set; }
        public string YieldReason { get; private set; }
        internal bool HasActiveLease => hooked;
        internal float AppliedAge => stepAge;

        public void Begin(object state, Func<bool> hasAuthority, AH64BrakingTurnCapture snapshot)
        {
            if (owner == state) return;
            Release(owner);
            owner = state;
            authority = hasAuthority;
            capture = snapshot;
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            stepAge = 0f;
            IsYielding = false;
            YieldReason = null;
            if (!CanWrite()) { Yield("entry-external-or-authority"); return; }
            motor.onMovementHit += OnMovementHit;
            On.RoR2.CharacterMotor.PreMove += OnPreMove;
            On.RoR2.CharacterMotor.ApplyForceImpulse += OnForce;
            SceneManager.activeSceneChanged += SceneChanged;
            hooked = true;
        }

        private bool CanWrite()
        {
            return owner != null && authority != null && authority() && motor
                && motor.hasEffectiveAuthority && body && body.healthComponent
                && body.healthComponent.alive && motor.isFlying && !motor.useGravity
                && !motor.disableAirControlUntilCollision && !motor.useCustomGravity
                && !(BaseHeadstompersState.FindForBody(body) is HeadstompersFall);
        }

        public void Check(object state)
        {
            if (state == owner && !IsYielding && !CanWrite()) Yield("external-or-authority");
        }

        // Called after inherited HandleMovements. Quail writes velocity directly rather than
        // through ApplyForceImpulse; let it (or another direct movement handoff) win.
        public void ObserveMovement(object state, Vector3 before)
        {
            if (state != owner || IsYielding) return;
            if (motor && (motor.velocity - before).sqrMagnitude > 0f)
                Yield("main-movement-handoff");
            else Check(state);
        }

        private void OnPreMove(On.RoR2.CharacterMotor.orig_PreMove orig, CharacterMotor self, float dt)
        {
            if (self != motor || !hooked) { orig(self, dt); return; }
            if (!CanWrite()) { Yield("pre-move-handoff"); orig(self, dt); return; }
            if (stepAge >= capture.Duration) { Yield("completed"); orig(self, dt); return; }
            object lease = owner;
            Vector3 before = self.velocity;
            // Do not touch airControl, grounding, flags, input, or Y. Native computes its real
            // grounded/airborne/sprint/hover result before our horizontal-only replacement.
            orig(self, dt);
            if (!hooked || lease != owner) return; // nested impulse, collision or cleanup won
            if (!CanWrite()) { Yield("post-move-handoff"); return; }
            Vector3 next = AH64BrakingTurnMath.Step(capture, before, stepAge, dt);
            next.y = self.velocity.y;
            self.velocity = next;
            stepAge = Mathf.Min(capture.Duration, stepAge + Mathf.Max(dt, 0f));
        }

        private void OnForce(On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig,
            CharacterMotor self, ref PhysForceInfo info)
        {
            Vector3 before = self.velocity;
            orig(self, ref info);
            if (self == motor && hooked && (self.velocity - before).sqrMagnitude > 0f)
                Yield("accepted-impulse");
        }

        private void OnMovementHit(ref CharacterMotor.MovementHitInfo hit)
        {
            if (hit.hitNormal.y < 0.5f) Yield("wall-or-roof");
        }

        private void SceneChanged(Scene previous, Scene next) { Yield("stage-change"); }
        private void FixedUpdate() { Check(owner); }
        private void OnDisable() { Yield("disabled"); Release(owner); }

        private void Yield(string reason)
        {
            IsYielding = true;
            YieldReason = reason;
            Unhook();
        }

        private void Unhook()
        {
            if (!hooked) return;
            On.RoR2.CharacterMotor.PreMove -= OnPreMove;
            On.RoR2.CharacterMotor.ApplyForceImpulse -= OnForce;
            SceneManager.activeSceneChanged -= SceneChanged;
            if (motor) motor.onMovementHit -= OnMovementHit;
            hooked = false;
        }

        public void Release(object state)
        {
            if (state != owner) return;
            Unhook();
            owner = null;
            authority = null;
            // No velocity, air-control, hover-anchor or stock restoration.
        }
    }
}
