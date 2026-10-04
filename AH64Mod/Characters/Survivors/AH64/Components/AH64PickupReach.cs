using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Collects walk-over pickups directly below the hovering body. <c>GenericPickupController</c> grants
    /// from <c>OnTriggerStay</c> when a body collider enters its trigger, and a body held at resting height
    /// never does. Running the pickup's own callback keeps all of its rules: wait time, permissions,
    /// confirm-first tiers, full equipment slots.
    ///
    /// <para>Server only, like the pickup's own trigger. Items still need the pilot to be down near resting
    /// height, so this is not a magnet.</para>
    /// </summary>
    internal class AH64PickupReach : MonoBehaviour
    {
        private readonly Collider[] hits = new Collider[16];
        private CharacterMotor motor;
        private CharacterBody body;
        private AH64HoverController hover;
        private CapsuleCollider capsule;
        private float timer;

        private void Awake()
        {
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            hover = GetComponent<AH64HoverController>();
            capsule = GetComponent<CapsuleCollider>();
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active || !motor || !hover || !capsule)
                return;

            //A dead aircraft lingers through its crash; it must not collect anything on the way down.
            if (!body || !body.healthComponent || !body.healthComponent.alive)
                return;

            timer -= Time.fixedDeltaTime;
            if (timer > 0f)
                return;
            timer = AH64StaticValues.pickupReachInterval;

            float radius = motor.capsuleRadius;
            Vector3 feet = transform.position
                + Vector3.up * (motor.capsuleYOffset - motor.capsuleHeight * 0.5f);
            float reach = hover.GetRestHeight() + AH64StaticValues.pickupReachMargin;
            Vector3 top = feet + Vector3.up * radius;
            Vector3 bottom = feet + Vector3.up * (radius - reach);

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, hits,
                Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider hit = hits[i];
                if (!hit || !hit.enabled || !hit.isTrigger)
                    continue;

                GenericPickupController pickup = hit.GetComponentInParent<GenericPickupController>();
                //The closed drop pod disables its Fuel Array controller until the panel opens.
                //A direct callback bypasses that gate unless we honor the component state here.
                if (pickup && pickup.isActiveAndEnabled && !pickup.consumed)
                    pickup.OnTriggerStay(capsule);
            }
        }
    }
}
