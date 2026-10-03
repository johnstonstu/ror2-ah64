using System;
using System.Collections;
using System.Linq;
using EntityStates;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private EntityStateMachine Machine(string name)
        {
            var machine = pilot.GetComponents<EntityStateMachine>().SingleOrDefault(m => m.customName == name);
            if (!machine || machine.state == null) throw new InvalidOperationException("Missing state machine " + name);
            return machine;
        }

        private IEnumerator ResetCase(string name)
        {
            segment = name; move = Vector3.zero;
            poseEpoch++;
            hasAttitude = false;
            // The restored GameLibs baseline exposes the legacy two-argument API only.
            TeleportHelper.TeleportBody(pilot, mark);
            pilot.characterMotor.velocity = Vector3.zero;
            pilot.characterDirection.forward = facing;
            Event("scenario", "reset; state injection isolates enter/exit, bypasses stock/loadout/input activation");
            Event("pose-epoch", "epoch="+poseEpoch+"; intentional harness teleport/facing reset; all poses retained, continuity comparison restarts at next observed pose");
            yield return FixedSeconds(2f);
        }

        private IEnumerator FixedSeconds(float seconds)
        {
            float end = Time.fixedTime + seconds;
            while (Time.fixedTime < end) yield return new WaitForFixedUpdate();
        }

        private IEnumerator MovementCase(string name, EntityState maneuver, Vector3 expectedTravel)
        {
            yield return ResetCase(name);
            var machine = Machine("Body");
            Type mainType = machine.state.GetType();
            Vector3 start = pilot.gameObject.transform.position;
            var camera = pilot.GetComponent<CameraTargetParams>();
            float entryFov = camera ? camera.fovOverride : float.NaN;
            float entryAirControl = pilot.characterMotor.airControl;
            Event("movement-entry-controls", "observed pre-entry legacy FOV="+entryFov.ToString("R",Invariant)+" airControl="+entryAirControl.ToString("R",Invariant));
            machine.SetNextState(maneuver);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert(name + ".enter", machine.state == maneuver, "requested state", machine.state.GetType().Name, 0f);
            yield return Capture(name + "-entered");
            float end = Time.realtimeSinceStartup + 8f;
            while (machine.state == maneuver && Time.realtimeSinceStartup < end) yield return new WaitForFixedUpdate();
            float travel = Vector3.Dot(pilot.gameObject.transform.position - start, expectedTravel);
            Assert(name + ".travel", travel > 0.1f, ">0.1m along expected travel", travel.ToString("R", Invariant), 0.1f);
            Assert(name + ".exit", machine.state.GetType() == mainType, mainType.Name, machine.state.GetType().Name, 0f);
            yield return FixedSeconds(2f);
            bool clean = MovementResourcesReleased(entryFov, entryAirControl);
            Assert(name + ".cleanup", clean, "observed entry legacy FOV/airControl restored; maneuver resources released", camera ? camera.fovOverride.ToString(Invariant)+" vs entry "+entryFov.ToString(Invariant) : "no camera", 0f);
            yield return Capture(name + "-cleanup");
        }

        private bool MovementResourcesReleased(float entryFov, float entryAirControl)
        {
            var camera=pilot.GetComponent<CameraTargetParams>();
            var motion=pilot.GetComponent<Survivors.Components.AH64ManeuverMotor>();
            var visuals=pilot.GetComponent<Survivors.Components.AH64FlightVisuals>();
            return camera && camera.fovOverride==entryFov && pilot.characterMotor.airControl==entryAirControl
                && !pilot.characterMotor.disableAirControlUntilCollision
                && (!motion || !motion.HasActiveLease) && (!visuals || !visuals.HasActiveManeuverResources);
        }

        private IEnumerator HellfireCase()
        {
            yield return ResetCase("hellfire");
            if (!Survivors.AH64Assets.hellfireProjectilePrefab) throw new InvalidOperationException("Hellfire prefab missing.");
            if (pilot.inventory && pilot.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile) != 0)
                throw new InvalidOperationException("Baseline requires no Pocket I.C.B.M.");
            var machine = Machine("Weapon2"); Type mainType = machine.state.GetType();
            int before = launches;
            var state = new Survivors.SkillStates.FireHellfire();
            machine.SetNextState(state);
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            Assert("hellfire.enter", machine.state == state, "FireHellfire", machine.state.GetType().Name, 0f);
            yield return FixedSeconds(0.2f);
            Assert("hellfire.launch", launches - before == 1 && observedProjectiles.Count == 1, "1 request and 1 observed owner projectile", (launches - before) + " requests; " + observedProjectiles.Count + " observed", 0f);
            yield return Capture("hellfire-launched");
            float deadline = Time.realtimeSinceStartup + 8f;
            while (machine.state == state && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert("hellfire.exit", machine.state.GetType() == mainType, mainType.Name, machine.state.GetType().Name, 0f);
            deadline = Time.realtimeSinceStartup + 30f;
            while (OwnedProjectiles() > 0 && Time.realtimeSinceStartup < deadline) yield return new WaitForFixedUpdate();
            Assert("hellfire.cleanup", observedProjectiles.Count == 1 && OwnedProjectiles() == 0, "1 observed then 0 surviving owner projectiles by 30s", OwnedProjectiles().ToString(), 0f);
            Event("cleanup", "projectile objects drained; audio/visual/hit attribution unverified");
        }

        private int OwnedProjectiles()
        {
            return FindObjectsOfType<ProjectileController>().Count(p => p && p.owner == pilot.gameObject);
        }

        private void OnProjectile(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo info)
        {
            if (scripting && !finished && pilot && info.owner == pilot.gameObject) {
                if (info.projectilePrefab == Survivors.AH64Assets.hellfireProjectilePrefab) launches++;
                Event("launch-request", "prefab=" + (info.projectilePrefab ? info.projectilePrefab.name : "null") + "; requestIndex=" + launches + "; token=unverified; impact/payload=unverified");
            }
            orig(self, info);
            if (scripting && !finished && pilot && info.owner == pilot.gameObject) ObserveProjectiles();
        }
    }
}
