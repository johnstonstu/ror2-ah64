using EntityStates.Headstompers;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Runtime-only lease. No prefab/registry changes; disable also releases force subscriptions.
    internal sealed class AH64ManeuverMotor : MonoBehaviour
    {
        private CharacterMotor motor;
        private CharacterBody body;
        private object owner;
        private AH64ManeuverCapture capture;
        private float priorAirControl;
        private bool yielding;
        private bool hooked;
        public bool IsYielding => yielding;
        public bool NeedsMainHandoff => motor && motor.hasEffectiveAuthority
            && BaseHeadstompersState.FindForBody(body) is HeadstompersFall;

        public void Begin(object state, AH64ManeuverCapture snapshot)
        {
            Release(owner);
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            owner = state;
            capture = snapshot;
            yielding = false;
            if (!motor || !motor.hasEffectiveAuthority) { yielding = true; return; }
            priorAirControl = motor.airControl;
            if (ExternalMotion()) { yielding = true; return; }
            // PreMove otherwise accelerates toward Main's stale input after the state writes velocity.
            motor.airControl = 0f;
            motor.onMovementHit += OnMovementHit;
            On.RoR2.CharacterMotor.ApplyForceImpulse += OnForce;
            On.RoR2.CharacterMotor.PreMove += OnPreMove;
            hooked = true;
            if (motor.isGrounded) motor.Motor.ForceUnground();
            // Deliberately no entry velocity write: first step begins at the captured world momentum.
        }

        public void Step(object state, Vector3 direction, float age, float dt)
        {
            if (state != owner || yielding || !motor) return;
            if (!motor.hasEffectiveAuthority) { Yield(); return; }
            if (ExternalMotion()) { Yield(); return; }
            Vector3 velocity = AH64ManeuverMath.Step(capture, motor.velocity, direction,
                age, transform.position.y, dt);
            // Descending entry may touch a floor after Begin. Resume the positive climb rather
            // than letting stable grounding project it onto the floor for the rest of the cast.
            if (motor.isGrounded && velocity.y > 0f) motor.Motor.ForceUnground();
            motor.velocity = velocity;
        }

        private bool ExternalMotion()
        {
            return !motor.isFlying || motor.useGravity
                || motor.disableAirControlUntilCollision || motor.useCustomGravity
                || BaseHeadstompersState.FindForBody(body) is HeadstompersFall;
        }

        private void OnPreMove(On.RoR2.CharacterMotor.orig_PreMove orig, CharacterMotor self, float dt)
        {
            if (self != motor || !hooked || yielding || !self.hasEffectiveAuthority)
            {
                orig(self, dt);
                return;
            }
            if (ExternalMotion()) { Yield(); orig(self, dt); return; }
            // KCC calls BeforeCharacterUpdate/PreMove BEFORE consuming ForceUnground. While
            // grounded, airControl=0 alone does not suspend acceleration. Use the existing air
            // branch only for this call, preserving the surface-owned flag outside PreMove.
            bool forced = self.isAirControlForced;
            self.isAirControlForced = true;
            try { orig(self, dt); }
            finally { self.isAirControlForced = forced; }
        }

        private void OnMovementHit(ref CharacterMotor.MovementHitInfo hit)
        {
            // A wall/roof ends scripted thrust. KCC's resolved velocity is the handoff; floor
            // contact can still unground into the utility, rather than cancelling at launch.
            if (hit.hitNormal.y < 0.5f) Yield();
        }

        private void OnForce(On.RoR2.CharacterMotor.orig_ApplyForceImpulse orig,
            CharacterMotor self, ref PhysForceInfo info)
        {
            if (self != motor || yielding) { orig(self, ref info); return; }
            Vector3 before = self.velocity;
            // Restore before orig so the hover hook can install its own external-motion settings.
            // Observe the applied result: an immune/rejected force must not cancel the dodge.
            if (motor.airControl == 0f) motor.airControl = priorAirControl;
            orig(self, ref info);
            if ((self.velocity - before).sqrMagnitude > 0f) Yield();
            else if (hooked) motor.airControl = 0f;
        }

        private void Yield()
        {
            yielding = true;
            Unhook();
        }

        private void Unhook()
        {
            if (!hooked) return;
            On.RoR2.CharacterMotor.ApplyForceImpulse -= OnForce;
            On.RoR2.CharacterMotor.PreMove -= OnPreMove;
            if (motor)
            {
                motor.onMovementHit -= OnMovementHit;
                if (motor.airControl == 0f) motor.airControl = priorAirControl;
            }
            hooked = false;
        }

        public void Release(object state)
        {
            if (state != owner) return;
            Unhook();
            owner = null;
            // No carry restoration and no disableAirControlUntilCollision write, even on interruption.
        }

        private void OnDisable() { Release(owner); }
    }
}
