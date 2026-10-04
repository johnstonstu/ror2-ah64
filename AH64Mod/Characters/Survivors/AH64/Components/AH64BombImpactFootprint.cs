using UnityEngine;
using UnityEngine.Rendering;

namespace AH64.Survivors.Components
{
    // A short blast cross-section, not a persistent damage zone. Never changes the projectile
    // or performs gameplay queries. The outer edge stops at the same radius as BlastAttack.
    public sealed class AH64BombImpactFootprint : MonoBehaviour
    {
        private const float ExpansionTime = 0.18f;
        private const float FadeStart = 0.22f;
        private const float Lifetime = 0.58f;
        private static Mesh ringMesh;
        private static Material ringMaterial;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private MeshRenderer ringRenderer;
        private MaterialPropertyBlock properties;
        private float age;

        public static void Build(Transform parent, Material fire)
        {
            // The shipped matFire uses Unity's unlit particle shader, additive blending,
            // _Color and _MainTex. Clone only its material, never the explosion hierarchy.
            // Unknown/future donor shaders are optional cosmetics, not startup failures.
            if (!fire || !fire.HasProperty("_Color") || !fire.HasProperty("_MainTex"))
            {
                Log.Warning("Bomb footprint omitted: fire donor lacks _Color/_MainTex.");
                return;
            }
            if (!ringMaterial)
            {
                ringMaterial = new Material(fire) { name = "AH64BombFootprint" };
                ringMaterial.SetTexture("_MainTex", Texture2D.whiteTexture);
                ringMaterial.SetTextureScale("_MainTex", Vector2.one);
                ringMaterial.SetTextureOffset("_MainTex", Vector2.zero);
                ringMaterial.DisableKeyword("_EMISSION");
                if (ringMaterial.HasProperty("_EmissionColor"))
                    ringMaterial.SetColor("_EmissionColor", Color.black);
                if (ringMaterial.HasProperty("_Cull")) ringMaterial.SetFloat("_Cull", 0f);
                if (ringMaterial.HasProperty("_ZWrite")) ringMaterial.SetFloat("_ZWrite", 0f);
            }
            if (!ringMesh) ringMesh = CreateRing();
            var child = new GameObject("BlastFootprint");
            child.transform.SetParent(parent, false);
            // Slightly above the blast centre to reduce coplanar flicker on flat ground.
            child.transform.localPosition = Vector3.up * 0.08f;
            child.AddComponent<MeshFilter>().sharedMesh = ringMesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ringMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            child.AddComponent<AH64BombImpactFootprint>();
        }

        private void OnEnable()
        {
            age = 0f;
            ringRenderer = GetComponent<MeshRenderer>();
            if (properties == null) properties = new MaterialPropertyBlock();
            Animate();
        }

        private void Update()
        {
            age += Time.deltaTime;
            Animate();
        }

        private void Animate()
        {
            float expansion = Mathf.Clamp01(age / ExpansionTime);
            // Ease out, then hold at exactly 6 m while fading rather than travelling past it.
            float scale = Mathf.Lerp(0.16f, 1f, 1f - (1f - expansion) * (1f - expansion));
            transform.localScale = new Vector3(scale, 1f, scale);
            float alpha = 0.65f * (1f - Mathf.Clamp01((age - FadeStart) / (Lifetime - FadeStart)));
            properties.SetColor(ColorProperty, new Color(1f, 0.65f, 0.23f, alpha));
            ringRenderer.SetPropertyBlock(properties);
            ringRenderer.enabled = age < Lifetime;
        }

        private static Mesh CreateRing()
        {
            const int segments = 80;
            var vertices = new Vector3[segments * 3];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 12];
            float radius = AH64BombingRunStaticValues.BlastRadius;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                for (int band = 0; band < 3; band++)
                {
                    int vertex = i * 3 + band;
                    vertices[vertex] = direction * (radius - 0.42f + band * 0.21f);
                    colors[vertex] = new Color(1f, 1f, 1f, band == 1 ? 1f : 0f);
                    uv[vertex] = new Vector2(0.5f, 0.5f);
                }
                int next = ((i + 1) % segments) * 3;
                for (int band = 0; band < 2; band++)
                {
                    int offset = i * 12 + band * 6;
                    int a = i * 3 + band, b = next + band;
                    triangles[offset] = a; triangles[offset + 1] = b; triangles[offset + 2] = a + 1;
                    triangles[offset + 3] = a + 1; triangles[offset + 4] = b; triangles[offset + 5] = b + 1;
                }
            }
            var mesh = new Mesh { name = "AH64BombSixMetreFootprint" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
