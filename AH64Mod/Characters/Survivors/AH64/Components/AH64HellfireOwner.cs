using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    // Native special input is observed here, after the short Weapon2 launch state has finished.
    // Nothing consumes input, replaces aim, or owns the Body/Weapon/Weapon3 state machines.
    [DefaultExecutionOrder(-300)]
    public sealed class AH64HellfireOwner : MonoBehaviour
    {
        internal readonly AH64HellfireGuidancePolicy Policy = new AH64HellfireGuidancePolicy();
        internal Vector3 Point { get; private set; }
        private CharacterBody body;
        private EntityStateMachine bodyMachine;
        private uint localToken;
        private uint newestAcknowledged;
        private uint localRequest;
        private uint pendingRequest;
        private uint newestPeerRequest;
        internal uint ServerRequest { get; private set; }
        private uint sequence;
        private bool watching;
        private bool awaiting;
        private float nextSend;
        private float awaitUntil;

        public static AH64HellfireOwner GetOrAdd(GameObject owner)
        {
            return owner.GetComponent<AH64HellfireOwner>() ?? owner.AddComponent<AH64HellfireOwner>();
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            bodyMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneChanged;
        }

        private void SceneChanged(Scene previous, Scene next)
        {
            StopWatching();
            awaiting = false;
            pendingRequest = 0;
            if (NetworkServer.active)
                EndLead(Policy.ActiveToken);
        }

        internal bool Alive => isActiveAndEnabled && body && body.healthComponent
            && body.healthComponent.alive && body.inputBank;
        private bool Interrupted => bodyMachine && bodyMachine.state != null
            // These utilities use Pain priority to reject re-entry, while weapons remain usable.
            && !(bodyMachine.state is SkillStates.ServoDash)
            && !(bodyMachine.state is SkillStates.SmokeBackflip)
            && bodyMachine.state.GetMinimumInterruptPriority() >= InterruptPriority.Pain;

        public void BeginLaunch()
        {
            StopWatching();
            if (localRequest == uint.MaxValue)
                throw new System.InvalidOperationException("Hellfire requests exhausted for this body lifetime.");
            ++localRequest;
            watching = awaiting = true;
            awaitUntil = Time.fixedTime + AH64StaticValues.hellfireLifetime;
            AH64HellfireNetwork.BeginRequest(this, localRequest);
        }

        internal void ReceiveRequest(uint request)
        {
            if (!NetworkServer.active || !Alive || Interrupted || request <= newestPeerRequest)
                return;
            newestPeerRequest = pendingRequest = request;
        }

        internal uint RegisterLead()
        {
            // Launch invalidates the older missile even before this missile sends its acknowledgement.
            ServerRequest = pendingRequest;
            pendingRequest = 0;
            return Policy.Launch();
        }

        internal void Acknowledge(uint token, uint request, bool ended)
        {
            // Reliable begin precedes vanilla firing; replies carry that request. Older acknowledgements
            // cannot attach to or cancel a newer press, including rapid extra-stock launches.
            if (request == 0 || request != localRequest)
                return;
            if (ended)
            {
                StopWatching(false);
                awaiting = false;
                newestAcknowledged = System.Math.Max(newestAcknowledged, token);
                return;
            }
            if (token <= newestAcknowledged)
                return;
            newestAcknowledged = token;
            localToken = token;
            sequence = 0;
            awaiting = false;
            // A release/interruption that preceded the ack must close the server's token too.
            if (!watching || !Alive || !body.inputBank.skill4.down)
            {
                StopWatching();
                return;
            }
            Send(true);
        }

        internal void Accept(uint token, uint incomingSequence, bool held, Ray ray, bool validPacket = true)
        {
            if (!NetworkServer.active || !Alive || Interrupted)
                return;
            bool valid = held && validPacket && AH64HellfireAim.ValidRay(body, ray);
            if (Policy.Update(token, incomingSequence, Time.fixedTime, held, valid) && valid)
                Point = AH64HellfireAim.Resolve(body, ray);
        }

        internal void EndLead(uint token)
        {
            if (token != Policy.ActiveToken)
                return;
            Policy.Clear(token);
            AH64HellfireNetwork.Acknowledge(this, token, ServerRequest, true);
        }

        private void FixedUpdate()
        {
            if (!Alive || Interrupted)
            {
                StopWatching();
                if (NetworkServer.active)
                    EndLead(Policy.ActiveToken);
                return;
            }
            if (!Util.HasEffectiveAuthority(gameObject))
            {
                watching = awaiting = false;
                localToken = 0;
                return;
            }
            if (watching && !body.inputBank.skill4.down)
                StopWatching();
            if ((awaiting || watching) && Time.fixedTime > awaitUntil)
            {
                StopWatching();
                awaiting = false;
            }
            if (watching && localToken != 0 && Time.fixedTime >= nextSend)
                Send(true);
        }

        private void Send(bool held)
        {
            if (localToken == 0 || sequence == uint.MaxValue)
                return;
            Ray ray = body && body.inputBank ? body.inputBank.GetAimRay() : default(Ray);
            AH64HellfireNetwork.SendAim(this, localToken, ++sequence, held, ray);
            nextSend = Time.fixedTime + AH64HellfirePrototype.UpdateInterval;
        }

        public void StopWatching(bool sendRelease = true)
        {
            if (sendRelease && localToken != 0 && Util.HasEffectiveAuthority(gameObject))
                Send(false);
            watching = false;
            localToken = 0;
            // Retain awaiting until the late ack arrives, so it gets a terminal release response.
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            StopWatching();
            awaiting = false;
            if (NetworkServer.active)
                EndLead(Policy.ActiveToken);
        }
    }
}
