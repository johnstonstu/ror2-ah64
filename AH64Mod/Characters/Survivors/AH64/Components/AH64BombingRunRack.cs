using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Each confirmed projectile opens one wing carrier. This component never schedules a release.
    [DefaultExecutionOrder(210)]
    public sealed class AH64BombingRunRack : MonoBehaviour
    {
        public SkillDef BombingSkill;
        public bool PresentationReady;
        public bool ManagedVisibility;
        public Renderer[] Renderers;
        public Renderer FeedBomb;
        public Renderer[] FeedBombs;
        public Transform LeftLatch;
        public Transform RightLatch;
        public Transform[] Latches;
        private CharacterBody body;
        private SkillLocator skills;
        private readonly float[] pulseAges = { float.PositiveInfinity, float.PositiveInfinity };
        private int nextSide;
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

        public bool FeedVisible(int side)
        {
            return Eligible && (Running || skills.special.stock > 0)
                && pulseAges[Mathf.Clamp(side, 0, 1)] >= AH64BombingRunVisualValues.FeedReturnTime;
        }

        private void LateUpdate()
        {
            if (!Eligible) { ResetPulse(); SetVisibility(false); return; }
            bool running = Running;
            if (wasRunning && !running) ResetPulse();
            wasRunning = running;
            for (int side = 0; side < 2; side++)
            {
                pulseAges[side] += Time.deltaTime;
                float t = Mathf.Clamp01(pulseAges[side] / AH64BombingRunVisualValues.LatchDuration);
                float opening = t < 0.25f ? t / 0.25f : (1f - t) / 0.75f;
                SetLatch(side * 2, -opening * AH64BombingRunVisualValues.LatchAngle);
                SetLatch(side * 2 + 1, opening * AH64BombingRunVisualValues.LatchAngle);
            }
            SetVisibility(true);
        }

        internal bool TryRelease(Vector3 projectilePosition, out Vector3 outlet, out Quaternion orientation)
        {
            outlet = Vector3.zero; orientation = Quaternion.identity;
            if (!isActiveAndEnabled || !Eligible || !Running) return false;
            int side = FeedBombs != null && FeedBombs.Length > 1 ? nextSide : 0;
            Renderer feed = FeedBombs != null && FeedBombs.Length > side ? FeedBombs[side] : FeedBomb;
            if (!feed) return false;
            Vector3 candidate = feed.transform.position;

            // Validate against the actual gameplay spawn, so moving the cosmetic outlet
            // to a wing cannot reject a fresh release or relax the stale-instance guard.
            Vector3 spawn = body.corePosition + Vector3.down * AH64BombingRunStaticValues.ReleaseOffset;
            float distance = (projectilePosition - spawn).sqrMagnitude;
            float limit = AH64BombingRunVisualValues.MaximumRackDistance;
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance > limit * limit) return false;
            outlet = candidate; orientation = feed.transform.rotation;
            pulseAges[side] = 0f; nextSide = 1 - side;
            SetVisibility(true);
            return true;
        }

        private void SetLatch(int index, float angle)
        {
            Transform latch = Latches != null && Latches.Length > index ? Latches[index]
                : index == 0 ? LeftLatch : index == 1 ? RightLatch : null;
            if (latch) latch.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void SetVisibility(bool visible)
        {
            if (ManagedVisibility || Renderers == null) return;
            foreach (Renderer renderer in Renderers)
                if (renderer) renderer.forceRenderingOff = !visible || (renderer == FeedBomb && !FeedVisible(0));
        }

        private void ResetPulse()
        {
            pulseAges[0] = pulseAges[1] = float.PositiveInfinity;
            wasRunning = false; nextSide = 0;
            for (int i = 0; i < 4; i++) SetLatch(i, 0f);
        }

        private void OnDisable() { ResetPulse(); SetVisibility(false); }
        private void OnDestroy() { SetVisibility(false); }
    }
}
