using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    // EntityState has no MonoBehaviour disable callback. This owner closes that lifecycle gap.
    public sealed class AH64BombingRunOwner : MonoBehaviour
    {
        private AH64BombingRunCast active;
        private SkillStates.BombingRun observed;
        private uint lastRequest;
        internal bool PresentationRunning => observed != null;

        internal uint NextRequest()
        {
            // Never recycle a request within the same body lifetime (including authority changes).
            if (lastRequest == uint.MaxValue) throw new System.InvalidOperationException("Bombing request counter exhausted.");
            return ++lastRequest;
        }

        internal void Observe(SkillStates.BombingRun state, uint request)
        {
            observed = state;
            if (request > lastRequest) lastRequest = request;
        }

        internal void Forget(SkillStates.BombingRun state)
        {
            if (ReferenceEquals(observed, state)) observed = null;
        }

        internal AH64BombingRunCast Begin(CharacterBody body)
        {
            active?.Stop("replaced");
            active = null;
            if (!NetworkServer.active || !isActiveAndEnabled || !body || body.gameObject != gameObject
                || !body.gameObject.activeInHierarchy || !body.healthComponent || !body.healthComponent.alive)
                return null;
            // Assignment occurs only after construction succeeds; rejection never retains an old cast.
            active = new AH64BombingRunCast(body);
            return active;
        }

        internal void End(AH64BombingRunCast source, string reason)
        {
            source?.Stop(reason);
            if (ReferenceEquals(active, source)) active = null;
        }

        private void OnDisable() { Cancel("owner-disabled"); }
        private void OnDestroy() { Cancel("owner-destroyed"); }

        private void Cancel(string reason)
        {
            observed?.Cancel();
            observed = null;
            active?.Stop(reason);
            active = null;
        }
    }
}
