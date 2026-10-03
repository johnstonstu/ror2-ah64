using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    internal enum AH64BrakingTurnPhase { Brake, Turn, Exit }

    internal struct AH64BrakingTurnFrame
    {
        public Quaternion EntryAttitude;
        public Vector3 RequestedHeading, CommandedHeading;
        public float SignedCorrection, Progress, PhaseProgress, PitchDegrees, BankDegrees;
        public AH64BrakingTurnPhase Phase;
    }

    // Data only: FlightVisuals remains the sole attitude/model/camera owner.
    internal sealed class AH64BrakingTurnPresentation : MonoBehaviour
    {
        private object owner;
        private AH64BrakingTurnCapture capture;
        private CharacterBody body;
        private float age;
        private bool subscribed;

        public void Begin(object state, AH64BrakingTurnCapture snapshot)
        {
            if (state == null || owner == state) return;
            End(owner);
            owner = state;
            capture = snapshot;
            age = 0f;
            body = GetComponent<CharacterBody>();
            SceneManager.activeSceneChanged += SceneChanged;
            subscribed = true;
        }

        public void Progress(object state, float value)
        {
            if (state == owner) age = Mathf.Clamp(value, 0f, capture.Duration);
        }

        public bool TryGetFrame(object state, out AH64BrakingTurnFrame frame)
        {
            frame = default(AH64BrakingTurnFrame);
            if (owner == null || state != owner) return false;
            if (!body || !body.healthComponent || !body.healthComponent.alive)
            {
                End(owner);
                return false;
            }
            float turnEnd = capture.BrakeDuration + capture.TurnDuration;
            AH64BrakingTurnPhase phase = age < capture.BrakeDuration ? AH64BrakingTurnPhase.Brake
                : age < turnEnd ? AH64BrakingTurnPhase.Turn : AH64BrakingTurnPhase.Exit;
            float phaseProgress = phase == AH64BrakingTurnPhase.Brake ? age / capture.BrakeDuration
                : phase == AH64BrakingTurnPhase.Turn ? (age - capture.BrakeDuration) / capture.TurnDuration
                : (age - turnEnd) / capture.ExitDuration;
            float flare = phase == AH64BrakingTurnPhase.Brake
                ? Mathf.Sin(phaseProgress * Mathf.PI * 0.5f)
                : phase == AH64BrakingTurnPhase.Turn ? 1f : 1f - phaseProgress;
            float turnTime = Mathf.Min(capture.TurnDuration,
                Mathf.Abs(capture.SignedCorrection) / capture.TurnRate);
            float turnAge = age - capture.BrakeDuration;
            float bank = turnTime > 0f && turnAge > 0f && turnAge < turnTime
                ? Mathf.Sin(Mathf.PI * turnAge / turnTime) * Mathf.Sign(capture.SignedCorrection) : 0f;
            frame = new AH64BrakingTurnFrame
            {
                EntryAttitude = capture.EntryAttitude, RequestedHeading = capture.RequestedHeading,
                SignedCorrection = capture.SignedCorrection,
                CommandedHeading = AH64BrakingTurnMath.Heading(capture, age),
                Progress = age / capture.Duration, Phase = phase, PhaseProgress = phaseProgress,
                PitchDegrees = AH64BrakingTurnStaticValues.PitchDegrees * flare,
                BankDegrees = -AH64BrakingTurnStaticValues.BankDegrees * bank
            };
            return true;
        }

        public void End(object state)
        {
            if (state != owner) return;
            if (subscribed) SceneManager.activeSceneChanged -= SceneChanged;
            subscribed = false;
            owner = null;
        }

        private void FixedUpdate()
        {
            if (owner != null && (!body || !body.healthComponent || !body.healthComponent.alive)) End(owner);
        }
        private void SceneChanged(Scene previous, Scene next) { End(owner); }
        private void OnDisable() { End(owner); }
    }
}
