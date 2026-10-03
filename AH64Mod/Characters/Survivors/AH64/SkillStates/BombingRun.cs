using AH64.Survivors.Components;
using EntityStates;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.SkillStates
{
    // Weapon2 only. Body utilities, native aim and other weapon state machines remain independent.
    public sealed class BombingRun : BaseSkillState
    {
        private AH64BombingRunCast cast;
        private AH64BombingRunOwner owner;
        private GameObject bodyObject;
        private uint request;
        private readonly AH64BombingRunTerminal terminal = new AH64BombingRunTerminal();

        public override void OnEnter()
        {
            try
            {
                // Keep the routable state-machine object even when its CharacterBody is absent.
                bodyObject = outer ? outer.gameObject : null;
                base.OnEnter();
                if (characterBody) bodyObject = characterBody.gameObject;
                if (!bodyObject) { Cancel(); return; }
                owner = bodyObject.GetComponent<AH64BombingRunOwner>();
                if (!owner) owner = bodyObject.AddComponent<AH64BombingRunOwner>();
                if (!owner) { Cancel(); return; }
                if (isAuthority && request == 0) request = owner.NextRequest();
                owner.Observe(this, request);
                if (NetworkServer.active)
                {
                    cast = owner.Begin(characterBody);
                    if (cast == null) Cancel();
                    else cast.Tick();
                }
            }
            catch
            {
                Cancel();
                // A failed first release cannot leave a partly entered cast scheduling more payloads.
                cast?.Policy.Stop();
                throw;
            }
            finally
            {
                if (NetworkServer.active && cast == null) Cancel();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (terminal.Phase == AH64BombingRunPhase.Exited) return;
            if (NetworkServer.active)
            {
                cast?.Tick();
                if (cast != null)
                {
                    if (cast.Policy.Stopped) terminal.Resolve(AH64BombingRunPhase.Cancelled);
                    else if (cast.Policy.ConsumedDrops == AH64BombingRunStaticValues.DropCount)
                        terminal.Resolve(AH64BombingRunPhase.Succeeded);
                }
                if (terminal.IsTerminal)
                {
                    cast?.Stop(terminal.Phase == AH64BombingRunPhase.Succeeded ? "complete" : "cancelled");
                    AH64BombingRunNetwork.Deliver(bodyObject, request, this, terminal);
                }
            }
            // Both server success and cancellation end recovery without overriding stronger transitions.
            if (isAuthority && terminal.IsTerminal && outer && !outer.HasPendingState())
                outer.SetNextStateToMain();
        }

        internal bool ReceiveTerminal(uint completedRequest, AH64BombingRunPhase outcome)
        {
            if (!isAuthority || request == 0 || completedRequest != request
                || !bodyObject || !bodyObject.activeInHierarchy)
                return false;
            // Cancellation must also release a retained dead-body Weapon2 state; pending death wins below.
            return terminal.Resolve(outcome);
        }

        internal void Cancel() { terminal.Resolve(AH64BombingRunPhase.Cancelled); }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(request);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            request = reader.ReadUInt32();
        }

        public override void OnExit()
        {
            terminal.Exit();
            if (owner) owner.Forget(this);
            if (cast != null)
            {
                string reason = cast.Policy.ConsumedDrops == AH64BombingRunStaticValues.DropCount
                    ? "complete" : "interrupted";
                if (owner) owner.End(cast, reason);
                else cast.Stop(reason);
            }
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority() => InterruptPriority.Pain;
    }
}
