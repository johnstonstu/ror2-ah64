using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Mounted on a model child, so model/body replacement destroys the whole attachment.
    // CharacterModel owns materials/cloak; this component only owns forceRenderingOff and latch pose.
    [DefaultExecutionOrder(210)]
    public sealed class AH64BombingRunRack : MonoBehaviour
    {
        public SkillDef BombingSkill;
        public bool PresentationReady; // Coordinator sets only after CharacterModel/skin registration.
        public Renderer[] Renderers;
        public Renderer FeedBomb;
        public Transform LeftLatch;
        public Transform RightLatch;
        private CharacterBody body;
        private SkillLocator skills;
        private float pulseAge = float.PositiveInfinity;
        private bool wasRunning;

        private void OnEnable()
        {
            body = GetComponentInParent<CharacterBody>();
            skills = body ? body.GetComponent<SkillLocator>() : null;
            ResetPulse();
        }

        private bool Eligible => PresentationReady && body && body.gameObject.activeInHierarchy
            && body.healthComponent && body.healthComponent.alive && skills && skills.special
            && BombingSkill && skills.special.skillDef == BombingSkill && body.modelLocator
            && body.modelLocator.modelTransform && transform.IsChildOf(body.modelLocator.modelTransform);

        private bool Running
        {
            get
            {
                AH64BombingRunOwner owner = body ? body.GetComponent<AH64BombingRunOwner>() : null;
                return owner && owner.PresentationRunning;
            }
        }

        private void LateUpdate()
        {
            if (!Eligible) { ResetPulse(); SetVisibility(false, false); return; }
            bool running = Running;
            if (wasRunning && !running) ResetPulse();
            wasRunning = running;
            pulseAge += Time.deltaTime;
            // Opens promptly, then returns within the 0.3-second cadence. This clock only poses
            // a latch after a confirmed instance; it can never create a release or projectile.
            float t = Mathf.Clamp01(pulseAge / AH64BombingRunVisualValues.LatchDuration);
            float opening = t < 0.25f ? t / 0.25f : (1f - t) / 0.75f;
            if (LeftLatch) LeftLatch.localRotation = Quaternion.Euler(0f, 0f, -opening * AH64BombingRunVisualValues.LatchAngle);
            if (RightLatch) RightLatch.localRotation = Quaternion.Euler(0f, 0f, opening * AH64BombingRunVisualValues.LatchAngle);
            bool loaded = (running || skills.special.stock > 0) && pulseAge >= AH64BombingRunVisualValues.FeedReturnTime;
            SetVisibility(true, loaded);
        }

        internal bool TryRelease(Vector3 projectilePosition, out Vector3 outlet, out Quaternion orientation)
        {
            outlet = Vector3.zero;
            orientation = Quaternion.identity;
            if (!isActiveAndEnabled || !Eligible || !Running || !FeedBomb) return false;
            Vector3 candidate = FeedBomb.transform.position;
            float distance = (projectilePosition - candidate).sqrMagnitude;
            float limit = AH64BombingRunVisualValues.MaximumRackDistance;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance > limit * limit) return false;
            outlet = candidate;
            orientation = FeedBomb.transform.rotation;
            pulseAge = 0f;
            SetVisibility(true, false);
            return true;
        }

        private void SetVisibility(bool visible, bool loaded)
        {
            if (Renderers == null) return;
            foreach (Renderer renderer in Renderers)
                if (renderer) renderer.forceRenderingOff = !visible || (renderer == FeedBomb && !loaded);
        }

        private void ResetPulse()
        {
            pulseAge = float.PositiveInfinity;
            wasRunning = false;
            if (LeftLatch) LeftLatch.localRotation = Quaternion.identity;
            if (RightLatch) RightLatch.localRotation = Quaternion.identity;
        }

        private void OnDisable() { ResetPulse(); SetVisibility(false, false); }
        private void OnDestroy() { SetVisibility(false, false); }
    }
}
