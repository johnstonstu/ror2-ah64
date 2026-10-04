using System;
using System.Collections.Generic;
using AH64.Survivors.Components;
using UnityEngine;

namespace AH64.Survivors
{
    public static class AH64LoadoutAttachmentBuilder
    {
        private static Mesh rails, tubes, carrier, latch, nozzle, canister, brake, fallbackBomb, fallbackMissile;

        // Build on both prefabs before renderer/skin registration. Names are deliberately identical.
        public static AH64LoadoutAttachments Build(Transform model, Material frameMaterial)
        {
            if (!model || !frameMaterial) { Log.Warning("Loadout attachments omitted: model/material missing."); return null; }
            var existing = model.GetComponentInChildren<AH64LoadoutAttachments>(true);
            if (existing) return existing;
            Transform root = null;
            try
            {
                EnsureMeshes();
                root = Child(model, "AH64LoadoutAttachments", Vector3.zero);
                var result = root.gameObject.AddComponent<AH64LoadoutAttachments>();
                var all = new List<Renderer>();
                result.SpecialRoots = new Transform[3]; result.UtilityRoots = new Transform[3];
                string[] specials = { "Longbow", "Hellfire", "Bombing" };
                for (int i = 0; i < 3; i++) result.SpecialRoots[i] = Child(root, "AH64" + specials[i] + "Mounts", Vector3.zero);
                string[] utilities = { "Jink", "Smoke", "Banked" };
                for (int i = 0; i < 3; i++) result.UtilityRoots[i] = Child(root, "AH64" + utilities[i] + "Hardware", Vector3.zero);
                var rack = result.SpecialRoots[2].gameObject.AddComponent<AH64BombingRunRack>();
                rack.ManagedVisibility = true;
                rack.FeedBombs = new Renderer[2]; rack.Latches = new Transform[4];
                result.BombRack = rack;
                var legacy = new List<Renderer>();
                var recoil = new List<Transform>();
                var stores = new List<Renderer>();
                Mesh bomb = BombMesh();
                for (int side = 0; side < 2; side++)
                {
                    string suffix = side == 0 ? "L" : "R";
                    Transform anchor = Find(model, "PodMissile" + suffix);
                    if (!anchor) Log.Warning("Wing attachment uses authored fallback: PodMissile" + suffix);
                    Vector3 position = anchor ? model.InverseTransformPoint(anchor.position) : new Vector3(side == 0 ? -0.78f : 0.78f, 0.6142f, -0.05f);
                    Renderer original = anchor ? anchor.GetComponent<Renderer>() : null;
                    if (original) legacy.Add(original);
                    for (int missile = 0; missile < 4; missile++)
                    {
                        Transform store = Find(model, "Missile" + suffix + missile);
                        if (store && store.GetComponent<Renderer>()) legacy.Add(store.GetComponent<Renderer>());
                        MeshFilter filter = store ? store.GetComponent<MeshFilter>() : null;
                        {
                            Renderer copy = Part(result.SpecialRoots[0], "AH64LongbowStore" + suffix + missile,
                                filter && filter.sharedMesh ? filter.sharedMesh : fallbackMissile, frameMaterial, position, all);
                            if (store && filter && filter.sharedMesh)
                            {
                                copy.transform.position = store.position; copy.transform.rotation = store.rotation;
                                Vector3 scale = model.lossyScale;
                                copy.transform.localScale = new Vector3(store.lossyScale.x / scale.x, store.lossyScale.y / scale.y, store.lossyScale.z / scale.z);
                            }
                            else copy.transform.localPosition = position + new Vector3(missile % 2 == 0 ? -0.08f : 0.08f, missile < 2 ? -0.09f : -0.20f, 0.03f);
                            stores.Add(copy);
                        }
                    }
                    recoil.Add(Part(result.SpecialRoots[0], "AH64LongbowRail" + suffix, rails, frameMaterial, position, all).transform);
                    recoil.Add(Part(result.SpecialRoots[1], "AH64HellfireTube" + suffix, tubes, frameMaterial, position, all).transform);
                    Part(result.SpecialRoots[2], "AH64BombCarrier" + suffix, carrier, frameMaterial, position, all);
                    rack.FeedBombs[side] = Part(result.SpecialRoots[2], "AH64BombFeed" + suffix, bomb, frameMaterial,
                        position + new Vector3(0f, -0.19f, 0f), all);
                    for (int jaw = 0; jaw < 2; jaw++)
                    {
                        float sign = jaw == 0 ? -1f : 1f;
                        Transform pivot = Child(result.SpecialRoots[2], "AH64BombLatch" + suffix + jaw,
                            position + new Vector3(sign * 0.19f, -0.07f, 0f));
                        Part(pivot, pivot.name + "Mesh", latch, frameMaterial, new Vector3(-sign * 0.055f, -0.07f, 0f), all);
                        rack.Latches[side * 2 + jaw] = pivot;
                    }
                }
                rack.FeedBomb = rack.FeedBombs[0];
                rack.Renderers = result.SpecialRoots[2].GetComponentsInChildren<Renderer>(true);
                Transform chest = Find(model, "Chest");
                Vector3 fuselage = chest ? model.InverseTransformPoint(chest.position) : Vector3.up;
                result.UtilityPivots = new Transform[5];
                // A dedicated lighter mechanical finish improves small-module readability;
                // normal CharacterModel/skin material ownership still applies.
                Material utilityMaterial = new Material(frameMaterial) { name = frameMaterial.name + "_Utility" };
                if (utilityMaterial.HasProperty("_Color"))
                {
                    Color tint = utilityMaterial.GetColor("_Color");
                    utilityMaterial.SetColor("_Color", new Color(Mathf.Min(0.65f, tint.r * 1.55f),
                        Mathf.Min(0.65f, tint.g * 1.55f), Mathf.Min(0.65f, tint.b * 1.55f), tint.a));
                }
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    string suffix = side == 0 ? "L" : "R";
                    Transform jet = Part(result.UtilityRoots[0], "AH64JinkNozzle" + suffix, nozzle, utilityMaterial,
                        fuselage + new Vector3(sign * 0.50f, 0.02f, -0.22f), all).transform;
                    jet.localRotation = Quaternion.Euler(0f, sign * 90f, 0f);
                    result.UtilityPivots[side] = jet;
                    Transform fin = Part(result.UtilityRoots[2], "AH64BankedFin" + suffix, brake, utilityMaterial,
                        fuselage + new Vector3(sign * 0.43f, 0.15f, -0.58f), all).transform;
                    fin.localRotation = Quaternion.Euler(12f, 0f, 0f);
                    result.UtilityPivots[side + 3] = fin;
                }
                result.UtilityPivots[2] = Part(result.UtilityRoots[1], "AH64SmokeCanister", canister, utilityMaterial,
                    fuselage + new Vector3(0f, 0.15f, -0.73f), all).transform;
                // Interleave sides, outside stations first, to keep depletion balanced.
                result.LongbowStores = new[] { stores[3], stores[7], stores[2], stores[6], stores[1], stores[5], stores[0], stores[4] };
                result.RecoilPivots = recoil.ToArray();
                result.LegacyRenderers = legacy.ToArray(); result.Renderers = all.ToArray();
                foreach (Renderer renderer in result.Renderers) renderer.forceRenderingOff = true;
                return result;
            }
            catch (Exception error)
            {
                // Cosmetic initialization must never prevent survivor registration.
                Log.Warning("Loadout attachment construction omitted: " + error);
                if (root) { root.gameObject.SetActive(false); UnityEngine.Object.Destroy(root.gameObject); }
                return null;
            }
        }

        private static Mesh BombMesh()
        {
            GameObject ghost = AH64BombingRunPresentationAssets.Ghost;
            Transform visual = ghost ? ghost.transform.Find("BombVisual") : null;
            MeshFilter filter = visual ? visual.GetComponent<MeshFilter>() : null;
            return filter && filter.sharedMesh ? filter.sharedMesh : fallbackBomb;
        }

        private static Transform Find(Transform model, string name)
        {
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child;
            return null;
        }

        private static Transform Child(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position;
            return child;
        }

        private static Renderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, List<Renderer> all)
        {
            Transform child = Child(parent, name, position);
            child.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            all.Add(renderer); return renderer;
        }

        private static void EnsureMeshes()
        {
            if (rails) return;
            var m = new AH64AttachmentMesh();
            foreach (float x in new[] { -0.16f, 0.16f })
                m.Box(new Vector3(x, -0.07f, 0f), new Vector3(0.055f, 0.065f, 0.84f));
            foreach (float z in new[] { -0.26f, 0.25f })
                m.Box(new Vector3(0f, 0.02f, z), new Vector3(0.43f, 0.055f, 0.065f));
            // Squared forward lock frame distinguishes the otherwise open rails.
            foreach (float x in new[] { -0.20f, 0.20f })
                m.Box(new Vector3(x, -0.075f, 0.28f), new Vector3(0.035f, 0.23f, 0.035f));
            m.Box(new Vector3(0f, -0.19f, 0.28f), new Vector3(0.43f, 0.035f, 0.035f));
            rails = m.Finish("AH64OpenLongbowRails");
            m = new AH64AttachmentMesh();
            foreach (float x in new[] { -0.125f, 0.125f })
            {
                m.Tube(new Vector3(x, -0.10f, 0f), 0.115f, 0.083f, 0.65f);
                m.Tube(new Vector3(x, -0.10f, 0.29f), 0.135f, 0.083f, 0.09f);
            }
            m.Box(new Vector3(0f, 0.025f, 0f), new Vector3(0.41f, 0.075f, 0.44f));
            tubes = m.Finish("AH64EnclosedHellfireTubes");
            m = new AH64AttachmentMesh();
            foreach (float x in new[] { -0.18f, 0.18f })
                m.Box(new Vector3(x, -0.02f, 0f), new Vector3(0.06f, 0.07f, 0.53f));
            m.Box(new Vector3(0f, 0.015f, -0.10f), new Vector3(0.42f, 0.07f, 0.07f));
            carrier = m.Finish("AH64ShortBombCarrier");
            m = new AH64AttachmentMesh(); m.Box(Vector3.zero, new Vector3(0.17f, 0.055f, 0.13f)); latch = m.Finish("AH64ReleaseLatch");
            m = new AH64AttachmentMesh(); m.Tube(Vector3.zero, 0.13f, 0.083f, 0.24f);
            m.Tube(new Vector3(0f, 0f, 0.105f), 0.15f, 0.083f, 0.055f);
            nozzle = m.Finish("AH64VectorNozzle");
            m = new AH64AttachmentMesh(); m.Box(Vector3.zero, new Vector3(0.43f, 0.20f, 0.31f));
            for (int i = -1; i <= 1; i++) m.Tube(new Vector3(i * 0.12f, 0f, -0.18f), 0.047f, 0.031f, 0.08f);
            for (int i = -1; i <= 1; i++) m.Box(new Vector3(0f, 0.106f, i * 0.08f), new Vector3(0.34f, 0.025f, 0.025f));
            canister = m.Finish("AH64AftCountermeasures");
            m = new AH64AttachmentMesh(); m.Box(new Vector3(0f, 0f, -0.16f), new Vector3(0.23f, 0.035f, 0.36f));
            m.Box(new Vector3(0f, 0.024f, -0.16f), new Vector3(0.055f, 0.025f, 0.30f));
            brake = m.Finish("AH64AirbrakeFin");
            m = new AH64AttachmentMesh();
            m.Hull(new[] { -0.32f, -0.23f, 0.18f, 0.29f, 0.36f }, new[] { 0f, 0.105f, 0.105f, 0.07f, 0f });
            m.Box(new Vector3(0f, 0f, -0.25f), new Vector3(0.34f, 0.018f, 0.17f));
            m.Box(new Vector3(0f, 0f, -0.25f), new Vector3(0.018f, 0.34f, 0.17f));
            fallbackBomb = m.Finish("AH64FallbackFinnedBomb");
            m = new AH64AttachmentMesh();
            m.Hull(new[] { -0.31f, -0.30f, 0.28f, 0.35f, 0.39f }, new[] { 0f, 0.048f, 0.048f, 0.033f, 0f });
            fallbackMissile = m.Finish("AH64FallbackWingMissile");
        }
    }
}
