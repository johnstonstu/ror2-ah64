using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Lives on the existing server-spawned projectile. No new messages, prediction, colliders,
    // timers that emit bombs, or writes to the projectile root/ghost authority transform.
    [DefaultExecutionOrder(220)]
    public sealed class AH64BombingRunReleaseVisual : MonoBehaviour
    {
        private ProjectileController controller;
        private float receivedAt;
        private bool attempted;
        private Transform visual;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 initialOffset;
        private Quaternion initialRotation;
        private float blendAge;
        private bool blending;

        private void Start()
        {
            controller = GetComponent<ProjectileController>();
            receivedAt = Time.time;
        }

        private void LateUpdate()
        {
            if (!attempted)
            {
                if (!controller || controller.isPrediction || Time.time - receivedAt > AH64BombingRunVisualValues.ReleaseResolveWindow)
                    attempted = true;
                else if (controller.owner && controller.ghost) BeginPresentation();
            }
            if (!blending) return;
            if (!visual || !controller || !controller.ghost) { EndBlend(); return; }
            blendAge += Time.deltaTime;
            float t = Mathf.Clamp01(blendAge / AH64BombingRunVisualValues.ReleaseBlendDuration);
            float smooth = t * t * (3f - 2f * t);
            Transform parent = visual.parent;
            visual.position = parent.TransformPoint(restPosition) + initialOffset * (1f - smooth);
            visual.rotation = Quaternion.Slerp(initialRotation, parent.rotation * restRotation, smooth);
            if (t >= 1f) EndBlend();
        }

        private void BeginPresentation()
        {
            attempted = true; // Once per actual instance, including host; never retry an old release.
            CharacterBody owner = controller.owner.GetComponent<CharacterBody>();
            Transform model = owner && owner.modelLocator ? owner.modelLocator.modelTransform : null;
            AH64BombingRunRack rack = model ? model.GetComponentInChildren<AH64BombingRunRack>(true) : null;
            Transform candidate = controller.ghost.transform.Find("BombVisual");
            if (!rack || !candidate || !rack.TryRelease(transform.position, out Vector3 outlet, out Quaternion rotation)) return;
            visual = candidate;
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
            initialOffset = outlet - visual.position;
            initialRotation = rotation;
            blendAge = 0f;
            blending = true;
        }

        private void EndBlend()
        {
            if (visual)
            {
                visual.localPosition = restPosition;
                visual.localRotation = restRotation;
            }
            blending = false;
            visual = null;
        }

        private void OnDisable() { EndBlend(); }
        private void OnDestroy() { EndBlend(); }
    }
}
