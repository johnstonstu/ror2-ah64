using System;
using System.Collections;
using RoR2;
using UnityEngine;

namespace ReadmeCapture
{
    public sealed partial class CapturePlugin
    {
        private void PrepareArena()
        {
            foreach (var tutorial in FindObjectsOfType<RoR2.UI.HUDTutorialController>()) {
                tutorial.enabled = false;
                if (tutorial.sprintTutorialObject) tutorial.sprintTutorialObject.SetActive(false);
                if (tutorial.equipmentTutorialObject) tutorial.equipmentTutorialObject.SetActive(false);
                if (tutorial.remoteOpTutorialObject) tutorial.remoteOpTutorialObject.SetActive(false);
            }
            // Pick nearby terrain with an actual ground-backed firing lane, rather
            // than transferring coordinates from a different stage or scene variant.
            Vector3 origin = pilot.footPosition;
            float best = -1f;
            for (int ring = 0; ring < 4; ring++) for (int offset = 0; offset < (ring == 0 ? 1 : 8); offset++) {
                Vector3 probe = origin + Quaternion.Euler(0, offset * 45f, 0) * Vector3.forward * (ring * 16f);
                RaycastHit floor;
                if (!Physics.Raycast(probe + Vector3.up * 40f, Vector3.down, out floor, 100f,
                    LayerIndex.world.mask, QueryTriggerInteraction.Ignore) || floor.normal.y < 0.75f) continue;
                Vector3 candidate = floor.point + Vector3.up * 2f;
                for (int yaw = 0; yaw < 24; yaw++) {
                    Vector3 direction = Quaternion.Euler(0, yaw * 15f, 0) * Vector3.forward;
                    RaycastHit wall;
                    float clear = Physics.Raycast(candidate + Vector3.up * 3f, direction, out wall, 60f,
                        LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? wall.distance : 60f;
                    if (clear < 38f) continue;
                    bool supported = true;
                    for (int step = 1; step <= 4; step++) {
                        RaycastHit ground;
                        if (!Physics.Raycast(candidate + direction * (step * 9f) + Vector3.up * 12f,
                            Vector3.down, out ground, 25f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore)
                            || Math.Abs(ground.point.y - floor.point.y) > 5f) { supported = false; break; }
                    }
                    float score = clear - Vector3.Distance(candidate, origin) * 0.03f;
                    if (supported && score > best) { best = score; mark = candidate; facing = direction; }
                }
            }
            if (best < 0f) throw new InvalidOperationException("No clear terrain-backed showcase lane");
            Event("arena-lane", "mark=" + mark + "; facing=" + facing + "; score=" + best);
        }

        private void FrameTargets(float distance)
        {
            fixedCamera = true;
            cameraFocus = mark + facing * (distance * 0.45f) + Vector3.up * 3f;
            cameraOffset = -facing * 24f + Vector3.up * 11f + Vector3.Cross(Vector3.up, facing) * 16f;
        }

        private IEnumerator Radar()
        {
            yield return Reset("fire-control-radar");
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            yield return Target(mark + facing * 24f - right * 5f, "BeetleMaster");
            yield return Target(mark + facing * 30f + right * 3f);
            FrameTargets(30f);
            Clip(true); yield return Seconds(1.5f);
            // Looking and strafing away from the painted Golem makes independent
            // turret tracking visible; this scene does not assert aim assistance.
            aim = (targets[0].corePosition - pilot.inputBank.aimOrigin).normalized;
            move = -right * 0.5f;
            yield return Seconds(2f); move = Vector3.zero;
            aim = (targets[1].corePosition - pilot.inputBank.aimOrigin).normalized;
            yield return Seconds(4.5f); Clip(false);
        }

        private IEnumerator Hydra()
        {
            yield return Reset("hydra-70-pods");
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            for (int i = -1; i <= 1; i++) yield return Target(mark + facing * 32f + right * (i * 3f));
            Choose(pilot.skillLocator.secondary, "FireRocketPods");
            yield return Ready(pilot.skillLocator.secondary);
            FrameTargets(32f);
            aim = (targets[1].corePosition - pilot.inputBank.aimOrigin).normalized;
            Clip(true); yield return Seconds(0.8f);
            secondaryHeld = true; Execute(pilot.skillLocator.secondary);
            yield return Seconds(0.85f); secondaryHeld = false;
            yield return Seconds(3.3f); Clip(false);
        }

        private IEnumerator Longbow()
        {
            yield return Reset("agm-114l-longbow");
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            for (int i = -1; i <= 1; i++) yield return Target(mark + facing * 34f + right * (i * 5f));
            Choose(pilot.skillLocator.special, "PaintLongbow");
            yield return Ready(pilot.skillLocator.special);
            FrameTargets(34f);
            aim = (targets[0].corePosition - pilot.inputBank.aimOrigin).normalized;
            Clip(true); yield return Seconds(0.8f);
            held = true; Execute(pilot.skillLocator.special);
            for (int i = 0; i < targets.Count; i++) {
                aim = (targets[i].corePosition - pilot.inputBank.aimOrigin).normalized;
                yield return Seconds(0.6f);
            }
            Event("phase", "release painted Longbow salvo"); held = false;
            yield return Seconds(4f); Clip(false);
        }

        private IEnumerator Gun(string scene, string state)
        {
            yield return Reset(scene);
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            for (int i = -1; i <= 1; i++) yield return Target(mark + facing * 32f + right * (i * 3.5f));
            Choose(pilot.skillLocator.primary, state); yield return Ready(pilot.skillLocator.primary);
            FrameTargets(32f);
            aim = (targets[1].corePosition - pilot.inputBank.aimOrigin).normalized;
            Clip(true); yield return Seconds(0.8f);
            primaryHeld = true; Execute(pilot.skillLocator.primary);
            yield return Seconds(7.5f); primaryHeld = false;
            yield return Seconds(0.8f); Clip(false);
        }

        private IEnumerator Maneuver(string scene, string state)
        {
            yield return Reset(scene);
            Choose(pilot.skillLocator.utility, state); yield return Ready(pilot.skillLocator.utility);
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            fixedCamera = false;
            cameraOffset = -facing * 14f + right * 12f + Vector3.up * 9f;
            Clip(true); move = facing;
            yield return Seconds(1.2f); Execute(pilot.skillLocator.utility);
            yield return Seconds(state == "ServoDash" ? 1.4f : 2.1f);
            move = Vector3.zero; yield return Seconds(1.5f); Clip(false);
        }
    }
}
