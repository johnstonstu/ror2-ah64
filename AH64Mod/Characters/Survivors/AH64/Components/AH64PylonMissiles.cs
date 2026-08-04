using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Empties the wing pylons as the special is spent, and refills them on reload.
    ///
    /// <para>Eight Hellfires are modelled but the Longbow only carries six charges, so
    /// the mapping is proportional rather than one-missile-per-charge. Keeping the
    /// modelled count at eight was deliberate — four per rail is the silhouette that
    /// was signed off, and bending the art to match a balance number that may still
    /// move is the wrong way round.</para>
    ///
    /// <para>Visibility goes through <see cref="Renderer.forceRenderingOff"/>, never
    /// <c>Renderer.enabled</c>. Every missile is in <c>customRendererInfos</c>, and
    /// <c>CharacterModel.UpdateMaterials</c> sets <c>enabled = true</c> on everything in
    /// <c>baseRendererInfos</c> whenever materials go dirty — verified by decompiling
    /// RoR2.dll — so an <c>enabled</c> write is undone within a frame. This is the same
    /// trap that left the M230 visible next to the gatling.</para>
    ///
    /// <para>Presentation only. It reads skill stock and never writes it, so it cannot
    /// desync clients: each machine renders its own body from its own skill state.</para>
    /// </summary>
    public class AH64PylonMissiles : MonoBehaviour
    {
        //Rail order is outboard-to-inboard within each side, so the pylons empty from
        //the outside in. Firing off the outer stations first is both how it is done and
        //the more readable direction — the gap appears at the wingtip end where the
        //silhouette is cleanest.
        private static readonly string[] MissileNames =
        {
            "MissileL3", "MissileR3",
            "MissileL2", "MissileR2",
            "MissileL1", "MissileR1",
            "MissileL0", "MissileR0",
        };

        private Renderer[] missiles;
        private SkillLocator skillLocator;
        private int lastShown = -1;

        private void Start()
        {
            skillLocator = GetComponent<SkillLocator>();

            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
                return;

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
                return;

            missiles = new Renderer[MissileNames.Length];
            for (int i = 0; i < MissileNames.Length; i++)
            {
                Transform t = childLocator.FindChild(MissileNames[i]);
                if (t)
                    missiles[i] = t.GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            if (missiles == null || skillLocator == null || !skillLocator.special)
                return;

            GenericSkill special = skillLocator.special;
            int max = Mathf.Max(1, special.maxStock);
            int stock = Mathf.Clamp(special.stock, 0, max);

            //Ceil, so any remaining charge always shows at least one missile on the
            //rail. Dropping to an empty pylon while you can still fire would read as
            //a bug rather than as ammunition.
            int shown = Mathf.CeilToInt(missiles.Length * (stock / (float)max));
            if (stock > 0)
                shown = Mathf.Max(1, shown);

            if (shown == lastShown)
                return;

            lastShown = shown;
            for (int i = 0; i < missiles.Length; i++)
            {
                if (missiles[i])
                    missiles[i].forceRenderingOff = i >= shown;
            }
        }
    }
}
