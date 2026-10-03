using System;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    // Request only. The shared hover controller must accept/clamp it inside its normal
    // resource/terrain/ceiling path; this component never writes position or velocity.
    internal sealed class AH64BankedBreakAltitude : MonoBehaviour
    {
        private object owner;
        private Func<bool> eligible;
        private float entryBodyAltitude;
        private bool subscribed;
        internal bool HasRequest => owner != null;
        internal bool WasConsumed { get; private set; }

        public void Begin(object state, Func<bool> canHold, float bodyAltitude)
        {
            if (state == null || state == owner) return;
            End(owner);
            owner = state;
            eligible = canHold;
            entryBodyAltitude = bodyAltitude;
            WasConsumed = false;
            SceneManager.activeSceneChanged += SceneChanged;
            MapZone.onBodyTeleportGlobal += Teleported;
            subscribed = true;
        }

        // Called by hover AFTER external-motion early returns and airtime accounting.
        // A vertical input relinquishes the captured hold permanently for this cast:
        // releasing collective must retain the pilot's newly chosen altitude, not old entry Y.
        public bool TryGetBodyAltitude(bool jumpHeld, bool descendHeld, bool hasGround,
            bool resourceLocked, bool classicControls, out float altitude)
        {
            altitude = entryBodyAltitude;
            if (owner == null) return false;
            if (eligible == null || !eligible() || jumpHeld || descendHeld || !hasGround || resourceLocked)
            {
                End(owner);
                return false;
            }
            // Classic controls deliberately settle on release; do not change that global contract.
            if (classicControls) return false;
            WasConsumed = true;
            return true;
        }

        public void End(object state)
        {
            if (state != owner) return;
            if (subscribed)
            {
                SceneManager.activeSceneChanged -= SceneChanged;
                MapZone.onBodyTeleportGlobal -= Teleported;
            }
            subscribed = false;
            owner = null;
            eligible = null;
        }

        private void Teleported(CharacterBody body)
        {
            if (body && body.gameObject == gameObject) End(owner);
        }
        private void FixedUpdate() { if (owner != null && (eligible == null || !eligible())) End(owner); }
        private void SceneChanged(Scene previous, Scene next) { End(owner); }
        private void OnDisable() { End(owner); }
    }
}
