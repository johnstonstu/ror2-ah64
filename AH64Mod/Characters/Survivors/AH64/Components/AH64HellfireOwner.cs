using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    // Observes native Special independently of stock and Weapon2 recovery.
    [DefaultExecutionOrder(-300)]
    public sealed class AH64HellfireOwner : MonoBehaviour
    {
        internal readonly AH64HellfireGuidancePolicy Policy = new AH64HellfireGuidancePolicy();
        internal Vector3 Point { get; private set; }
        private CharacterBody body;
        private EntityStateMachine bodyMachine;
        private uint localToken, newestAcknowledged, localRequest, pendingRequest, newestPeerRequest;
        internal uint ServerRequest { get; private set; }
        private uint sequence;
        private bool acceptingLaunch, awaiting, sentHeld;
        private float nextSend, awaitUntil;

        public static AH64HellfireOwner GetOrAdd(GameObject owner)
        {
            return owner.GetComponent<AH64HellfireOwner>() ?? owner.AddComponent<AH64HellfireOwner>();
        }

        // Coordinator attaches this to the body prefab, so aiming also works before the first shot.
        public static void Install(GameObject bodyPrefab)
        {
            GetOrAdd(bodyPrefab);
            if (!bodyPrefab.GetComponent<AH64HellfireLaser>())
                bodyPrefab.AddComponent<AH64HellfireLaser>();
        }

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            bodyMachine = EntityStateMachine.FindByCustomName(gameObject, "Body");
        }

        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void SceneChanged(Scene previous, Scene next)
        {
            StopWatching();
            pendingRequest = 0;
            if (NetworkServer.active)
                EndLead(Policy.ActiveToken);
        }

        internal bool Alive => isActiveAndEnabled && body && body.healthComponent
            && body.healthComponent.alive && body.inputBank;
        internal bool Eligible
        {
            get
            {
                if (!Alive || (bodyMachine && AH64HellfireInterruption.IsInterrupted(bodyMachine.state)))
                    return false;
                SkillLocator skills = body.skillLocator;
                return skills && skills.special && skills.special.skillDef
                    && skills.special.skillDef.skillName == "AH64Hellfire";
            }
        }
        internal bool LocalHeld => Eligible && Util.HasEffectiveAuthority(gameObject) && body.inputBank.skill4.down;
        internal Ray AimRay => body && body.inputBank ? body.inputBank.GetAimRay() : default(Ray);
        internal CharacterBody Body => body;

        public void BeginLaunch()
        {
            StopWatching();
            if (localRequest == uint.MaxValue)
                throw new System.InvalidOperationException("Hellfire requests exhausted for this body lifetime.");
            ++localRequest;
            acceptingLaunch = awaiting = true;
            awaitUntil = Time.fixedTime + AH64StaticValues.hellfireLifetime;
            AH64HellfireNetwork.BeginRequest(this, localRequest);
        }

        internal void ReceiveRequest(uint request)
        {
            if (!NetworkServer.active || !Eligible || request <= newestPeerRequest)
                return;
            newestPeerRequest = pendingRequest = request;
        }

        internal uint RegisterLead()
        {
            ServerRequest = pendingRequest;
            pendingRequest = 0;
            return Policy.Launch();
        }

        internal void Acknowledge(uint token, uint request, bool ended)
        {
            if (request == 0 || request != localRequest)
                return;
            if (ended)
            {
                ClearLocal();
                newestAcknowledged = System.Math.Max(newestAcknowledged, token);
                return;
            }
            if (token <= newestAcknowledged)
                return;
            newestAcknowledged = token;
            localToken = token;
            sequence = 0;
            awaiting = false;
            if (!acceptingLaunch || !Eligible || Time.fixedTime > awaitUntil)
            {
                StopWatching();
                return;
            }
            // Release before the ack is a pause; an interruption before it is terminal above.
            Send(LocalHeld);
        }

        internal void Accept(uint token, uint incomingSequence, bool held, Ray ray,
            bool validPacket = true, bool cancel = false)
        {
            if (!NetworkServer.active)
                return;
            if (cancel)
            {
                if (Policy.Cancel(token, incomingSequence))
                    AH64HellfireNetwork.Acknowledge(this, token, ServerRequest, true);
                return;
            }
            if (!Eligible)
                return;
            bool valid = held && validPacket && AH64HellfireAim.ValidRay(body, ray);
            if (Policy.Update(token, incomingSequence, Time.fixedTime, held, valid) && valid)
                Point = AH64HellfireAim.Resolve(body, ray);
        }

        internal void EndLead(uint token)
        {
            if (token == 0 || token != Policy.ActiveToken)
                return;
            Policy.Clear(token);
            AH64HellfireNetwork.Acknowledge(this, token, ServerRequest, true);
        }

        private void FixedUpdate()
        {
            if (!Eligible)
            {
                StopWatching();
                if (NetworkServer.active)
                    EndLead(Policy.ActiveToken);
                return;
            }
            if (!Util.HasEffectiveAuthority(gameObject))
            {
                ClearLocal();
                return;
            }
            if ((awaiting || localToken != 0) && Time.fixedTime > awaitUntil)
            {
                StopWatching();
                return;
            }
            if (localToken == 0)
                return;
            bool held = LocalHeld;
            if (!held && sentHeld)
                Send(false); // Pause is immediate, including inside the update interval.
            else if (held && Time.fixedTime >= nextSend)
                Send(true); // Same live token/sequence resumes, without touching GenericSkill stock.
        }

        private void Send(bool held, bool cancel = false)
        {
            if (localToken == 0 || sequence == uint.MaxValue)
                return;
            AH64HellfireNetwork.SendAim(this, localToken, ++sequence, held, AimRay, cancel);
            sentHeld = held && !cancel;
            nextSend = Time.fixedTime + AH64HellfirePrototype.UpdateInterval;
        }

        private void ClearLocal()
        {
            acceptingLaunch = awaiting = sentHeld = false;
            localToken = 0;
        }

        // Terminal only: interruption, new launch, expiry, unequip, disable or stage change.
        // Ordinary input release never calls this.
        public void StopWatching(bool sendRelease = true)
        {
            if (sendRelease && localToken != 0 && Util.HasEffectiveAuthority(gameObject))
                Send(false, true);
            ClearLocal();
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            StopWatching();
            pendingRequest = 0;
            if (NetworkServer.active)
                EndLead(Policy.ActiveToken);
        }
    }
}
