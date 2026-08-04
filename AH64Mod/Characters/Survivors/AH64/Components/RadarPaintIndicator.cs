using RoR2;
using UnityEngine;
using UnityEngine.UI;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Huntress tracker recolored threat-red so the Fire Control Radar paint never reads as a Longbow
    /// Engi ring (or as vanilla Huntress lock). Tint is applied once per visualizer instance.
    /// </summary>
    internal class RadarPaintIndicator : Indicator
    {
        public static readonly Color PaintTint = new Color(1f, 0.18f, 0.12f, 1f);

        private bool tinted;

        public RadarPaintIndicator(GameObject owner, GameObject visualizerPrefab)
            : base(owner, visualizerPrefab)
        {
        }

        public override void UpdateVisualizer()
        {
            base.UpdateVisualizer();

            if (tinted || !visualizerInstance)
                return;

            TintHierarchy(visualizerInstance.transform);
            tinted = true;
        }

        public override void OnDestroyVisualizer()
        {
            base.OnDestroyVisualizer();
            tinted = false;
        }

        private static void TintHierarchy(Transform root)
        {
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                sr.color = PaintTint;

            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
                g.color = PaintTint;

            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is SpriteRenderer)
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                if (r.sharedMaterial && r.sharedMaterial.HasProperty("_Color"))
                    block.SetColor("_Color", PaintTint);
                if (r.sharedMaterial && r.sharedMaterial.HasProperty("_TintColor"))
                    block.SetColor("_TintColor", PaintTint);
                r.SetPropertyBlock(block);
            }
        }
    }
}
