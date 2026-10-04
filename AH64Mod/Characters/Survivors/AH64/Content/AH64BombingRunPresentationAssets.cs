using System;
using System.Collections.Generic;
using AH64.Survivors.Components;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace AH64.Survivors
{
    // Returns inert presentation assets. The coordinator registers the impact EffectDef and
    // attached rack renderers; this class never registers a network/gameplay prefab.
    public static class AH64BombingRunPresentationAssets
    {
        public static GameObject Ghost { get; private set; }
        public static GameObject Impact { get; private set; }
        private static Mesh bombMesh;
        private static Mesh boxMesh;
        private static Material bombMaterial;

        public static void Build(GameObject missileGhost, GameObject explosion)
        {
            if (Ghost && Impact) return;
            MeshRenderer donor = missileGhost ? missileGhost.GetComponentInChildren<MeshRenderer>(true) : null;
            if (!donor || !donor.sharedMaterial)
                throw new InvalidOperationException("Bomb presentation requires the existing missile mesh material.");
            Material fire = ParticleMaterial(explosion, "Fire");
            // The donor's Flash child is a Light, not a particle renderer.
            Material flash = fire;
            Material smoke = ParticleMaterial(explosion, "DebrisSmoke");
            bombMaterial = donor.sharedMaterial;
            EnsureMeshes();
            Ghost = BuildGhost();
            Impact = BuildImpact(flash, fire, smoke);
        }

        private static Material ParticleMaterial(GameObject source, string name)
        {
            Material fallback = null;
            if (source)
                foreach (ParticleSystemRenderer renderer in source.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (renderer.name == name && renderer.sharedMaterial) return renderer.sharedMaterial;
                    if (!fallback && renderer.sharedMaterial
                        && renderer.renderMode != ParticleSystemRenderMode.Mesh)
                        fallback = renderer.sharedMaterial;
                }
            // Bundle variants can omit a named emitter or its material. Cosmetic material
            // selection must not abort the entire survivor's registration in Awake.
            Log.Warning("Bomb impact donor material unavailable: " + name
                + (fallback ? "; reusing another particle material." : "; omitting this cosmetic puff."));
            return fallback;
        }

        private static GameObject BuildGhost()
        {
            var seed = new GameObject("AH64BombGhostSeed");
            seed.SetActive(false);
            seed.AddComponent<ProjectileGhostController>();
            Part(seed.transform, "BombVisual", bombMesh, bombMaterial, Vector3.zero, Vector3.one);
            GameObject prefab = PrefabAPI.InstantiateClone(seed, "AH64BombGhost", false);
            UnityEngine.Object.Destroy(seed);
            prefab.SetActive(true);
            return prefab;
        }

        private static GameObject BuildImpact(Material flash, Material fire, Material smoke)
        {
            var seed = new GameObject("AH64BombImpactSeed");
            seed.SetActive(false);
            EffectComponent effect = seed.AddComponent<EffectComponent>();
            // Geometry and particles are authored in metres. EffectComponent.Reset would
            // replace the root scale with EffectData.scale if applyScale were enabled.
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;
            // Global-bank proc explosion: short (0.75-1.06 s), intended for repeated impacts.
            // Unlike the Engineer event this does not depend on a selected survivor's bank.
            effect.soundName = "Play_item_proc_behemoth";
            seed.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Medium;
            seed.AddComponent<DestroyOnTimer>().duration = AH64BombingRunVisualValues.ImpactLifetime;
            // Material reuse only: no donor debris, subemitters, light or shake survives.
            // The contained core reads from hover height; the ring supplies the full blast footprint.
            Puff(seed.transform, "Flash", flash, 1, 4.2f, 0.09f, 0f, new Color(1f, 0.8f, 0.4f, 0.7f));
            Puff(seed.transform, "Fire", fire, 4, 3.2f, 0.34f, 1.1f, new Color(1f, 0.55f, 0.18f, 0.85f));
            Puff(seed.transform, "Smoke", smoke, 3, 2.2f, 0.65f, 0.8f, new Color(0.3f, 0.28f, 0.25f, 0.28f));
            AH64BombImpactFootprint.Build(seed.transform, fire);
            GameObject prefab = PrefabAPI.InstantiateClone(seed, "AH64BombImpact", false);
            UnityEngine.Object.Destroy(seed);
            prefab.SetActive(true);
            return prefab;
        }

        private static void Puff(Transform parent, string name, Material material, short count,
            float size, float life, float speed, Color color)
        {
            if (!material) return;
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            ParticleSystem particles = child.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = false;
            main.duration = life;
            main.startLifetime = life;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size);
            main.startSpeed = speed;
            main.startColor = color;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        }

        // Install on the model prefab before it is spawned. Returned renderers must be registered
        // with CharacterModel (and skins) by the coordinator before enabling this presentation.
        public static AH64BombingRunRack BuildRack(Transform model, Material frameMaterial)
        {
            if (!model || !frameMaterial || !Ghost) throw new InvalidOperationException("Bomb rack prerequisites missing.");
            ChildLocator locator = model.GetComponent<ChildLocator>();
            Transform chest = locator ? locator.FindChild("Chest") : null;
            if (!chest) throw new InvalidOperationException("Bomb rack requires the model Chest anchor.");
            var root = new GameObject("AH64BombRack");
            root.transform.SetParent(model, false);
            root.transform.localPosition = model.InverseTransformPoint(chest.position)
                + new Vector3(0f, -AH64BombingRunVisualValues.RackBelowChest, -AH64BombingRunVisualValues.RackBehindChest);
            var rack = root.AddComponent<AH64BombingRunRack>();
            var renderers = new List<Renderer>();
            renderers.Add(Part(root.transform, "BombRailL", boxMesh, frameMaterial, new Vector3(-0.25f, 0.04f, 0f), new Vector3(0.09f, 0.12f, 0.85f)));
            renderers.Add(Part(root.transform, "BombRailR", boxMesh, frameMaterial, new Vector3(0.25f, 0.04f, 0f), new Vector3(0.09f, 0.12f, 0.85f)));
            renderers.Add(Part(root.transform, "BombBrace", boxMesh, frameMaterial, new Vector3(0f, 0.11f, -0.15f), new Vector3(0.62f, 0.10f, 0.13f)));
            rack.LeftLatch = Latch(root.transform, "BombLatchL", -1f, frameMaterial, renderers);
            rack.RightLatch = Latch(root.transform, "BombLatchR", 1f, frameMaterial, renderers);
            rack.FeedBomb = Part(root.transform, "BombFeed", bombMesh, bombMaterial, new Vector3(0f, -0.13f, 0f), Vector3.one);
            renderers.Add(rack.FeedBomb);
            rack.Renderers = renderers.ToArray();
            foreach (Renderer renderer in rack.Renderers) renderer.forceRenderingOff = true;
            return rack;
        }

        private static Transform Latch(Transform parent, string name, float side, Material material, List<Renderer> renderers)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(side * 0.25f, 0f, 0.1f);
            renderers.Add(Part(pivot, name + "Mesh", boxMesh, material, new Vector3(-side * 0.08f, -0.08f, 0f), new Vector3(0.21f, 0.06f, 0.16f)));
            return pivot;
        }

        private static MeshRenderer Part(Transform parent, string name, Mesh mesh, Material material, Vector3 position, Vector3 scale)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localScale = scale;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void EnsureMeshes()
        {
            if (bombMesh && boxMesh) return;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            AddBox(vertices, triangles, Vector3.zero, Vector3.one);
            boxMesh = Mesh("AH64BombRackBox", vertices, triangles);
            vertices.Clear(); triangles.Clear();
            // Rounded nose points +Z; four tail fins distinguish the unpowered bomb from a missile.
            float[] stations = { -0.32f, -0.23f, 0.18f, 0.29f, 0.36f };
            float[] radii = { 0.045f, 0.105f, 0.105f, 0.07f, 0f };
            const int sides = 12;
            for (int ring = 0; ring < stations.Length; ring++)
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radii[ring], Mathf.Sin(angle) * radii[ring], stations[ring]));
                    if (ring == 0) continue;
                    int a = (ring - 1) * sides + side, b = (ring - 1) * sides + (side + 1) % sides;
                    int c = ring * sides + side, d = ring * sides + (side + 1) % sides;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            AddBox(vertices, triangles, new Vector3(0f, 0f, -0.25f), new Vector3(0.34f, 0.018f, 0.17f));
            AddBox(vertices, triangles, new Vector3(0f, 0f, -0.25f), new Vector3(0.018f, 0.34f, 0.17f));
            bombMesh = Mesh("AH64FinnedBomb", vertices, triangles);
        }

        private static void AddBox(List<Vector3> vertices, List<int> triangles, Vector3 center, Vector3 size)
        {
            int first = vertices.Count;
            for (int i = 0; i < 8; i++) vertices.Add(center + Vector3.Scale(size, new Vector3((i & 1) == 0 ? -0.5f : 0.5f,
                (i & 2) == 0 ? -0.5f : 0.5f, (i & 4) == 0 ? -0.5f : 0.5f)));
            int[] faces = { 0,2,1, 1,2,3, 4,5,6, 5,7,6, 0,1,4, 1,5,4, 2,6,3, 3,6,7, 0,4,2, 2,4,6, 1,3,5, 3,7,5 };
            foreach (int index in faces) triangles.Add(first + index);
        }

        private static Mesh Mesh(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
