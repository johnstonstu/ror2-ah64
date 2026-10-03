using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    [DefaultExecutionOrder(-200)]
    public sealed class AH64HellfireGuidance : MonoBehaviour, IProjectileImpactBehavior
    {
        private ProjectileController controller;
        private ProjectileSimple flight;
        private Rigidbody physicsBody;
        private AH64HellfireOwner owner;
        private uint token;
        private float turnRate;
        private float remainingTurn = AH64HellfirePrototype.TotalTurnDegrees;
        internal uint Token => token;
        internal float LastTurnDegrees { get; private set; }
        internal float RemainingTurnDegrees => remainingTurn;

        // Integrator calls this once on the Hellfire clone before projectile catalog registration.
        public static void Install(GameObject prefab)
        {
            ProjectileController controller = prefab.GetComponent<ProjectileController>();
            controller.allowPrediction = false;
            controller.authorityHandlesCollisionEvents = false;
            NetworkIdentity identity = prefab.GetComponent<NetworkIdentity>();
            identity.localPlayerAuthority = false;
            ProjectileNetworkTransform networkTransform = prefab.GetComponent<ProjectileNetworkTransform>();
            if (!networkTransform)
                networkTransform = prefab.AddComponent<ProjectileNetworkTransform>();
            networkTransform.checkForLocalPlayerAuthority = false;
            networkTransform.allowClientsideCollision = false;
            networkTransform.positionTransmitInterval = 1f / 30f;
            Rigidbody physicsBody = prefab.GetComponent<Rigidbody>();
            if (physicsBody)
                physicsBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (!prefab.GetComponent<AH64HellfireGuidance>())
                prefab.AddComponent<AH64HellfireGuidance>();
            if (!prefab.GetComponent<AH64HellfireCollision>())
                prefab.AddComponent<AH64HellfireCollision>();
        }

        private void Awake()
        {
            controller = GetComponent<ProjectileController>();
            flight = GetComponent<ProjectileSimple>();
            physicsBody = GetComponent<Rigidbody>();
            if (controller)
                controller.onInitialized += Initialized;
        }

        private void Initialized(ProjectileController initialized)
        {
            if (!NetworkServer.active || !initialized.owner)
                return;
            // combo 0 is the lead; 1/2 are the deliberately unguided I.C.B.M. fan shots.
            if (initialized.combo != 0)
                return;
            owner = AH64HellfireOwner.GetOrAdd(initialized.owner);
            token = owner.RegisterLead();
        }

        private void Start()
        {
            if (!NetworkServer.active)
            {
                // Vanilla ProjectileSimple writes velocity even on clients. Disable it here so
                // the existing ProjectileNetworkTransform alone presents server-corrected flight.
                if (flight)
                    flight.enabled = false;
                return;
            }
            if (owner && token == owner.Policy.ActiveToken)
                AH64HellfireNetwork.Acknowledge(owner, token, owner.ServerRequest, false);
        }

        private void FixedUpdate()
        {
            LastTurnDegrees = 0f;
            if (!NetworkServer.active || !owner || !flight || !physicsBody)
                return;
            if (!owner.Alive || !owner.Policy.CanGuide(token, Time.fixedTime))
            {
                turnRate = 0f;
                return; // ProjectileSimple retains the last heading and the existing lifetime.
            }
            Vector3 desired = owner.Point - transform.position;
            Vector3 heading = AH64HellfireAim.Turn(transform.forward, desired, Time.fixedDeltaTime,
                ref turnRate, ref remainingTurn);
            LastTurnDegrees = Vector3.Angle(transform.forward, heading);
            transform.rotation = Util.QuaternionSafeLookRotation(heading);
            physicsBody.velocity = heading * flight.desiredForwardSpeed;
            // No target, translation, explosion or damage writer here. Stock physics and impact
            // components resolve collisions; bounded turning cannot jump through a wall/ceiling.
        }

        public void OnProjectileImpact(ProjectileImpactInfo impact)
        {
            if (NetworkServer.active && owner && impact.collider
                && !AH64HellfireAim.IsOwner(impact.collider, owner.GetComponent<CharacterBody>()))
                owner.EndLead(token);
        }

        private void OnDisable()
        {
            if (controller)
                controller.onInitialized -= Initialized;
            if (NetworkServer.active && owner)
                owner.EndLead(token);
            owner = null;
            token = 0;
        }
    }
}
