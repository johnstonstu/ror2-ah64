using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// The ring that sits on a painted target while the Longbow radar holds it, carrying one dot per
    /// missile committed to that target.
    ///
    /// <para>Wraps the Engineer's own <c>EngiMissileTrackingIndicator</c> prefab, which ships the ring
    /// plus a <c>DotOrigin/DotTemplate</c> child but leaves the dots for code to place — vanilla's
    /// harpoon painter subclasses <see cref="Indicator"/> for exactly this. Handing the prefab to a
    /// plain <c>Indicator</c> instead draws the ring and nothing else, so a second lock on the same
    /// target would be invisible.</para>
    /// </summary>
    internal class LongbowLockIndicator : Indicator
    {
        public int missileCount;

        private readonly List<Transform> dots = new List<Transform>();
        private Transform dotOrigin;
        private Transform dotTemplate;
        //-1, not 0, so the very first missile still triggers a build pass
        private int builtCount = -1;

        public LongbowLockIndicator(GameObject owner, GameObject visualizerPrefab)
            : base(owner, visualizerPrefab)
        {
        }

        public override void UpdateVisualizer()
        {
            base.UpdateVisualizer();

            if (builtCount == missileCount || !visualizerTransform)
                return;

            if (!dotOrigin)
            {
                dotOrigin = visualizerTransform.Find("DotOrigin");
                dotTemplate = visualizerPrefab ? visualizerPrefab.transform.Find("DotOrigin/DotTemplate") : null;
            }

            if (!dotOrigin || !dotTemplate)
                return;

            while (dots.Count < missileCount)
            {
                GameObject dot = Object.Instantiate(dotTemplate.gameObject, dotOrigin);
                //renderers have to be registered with the indicator or they ignore its visibility pass
                //and stay drawn while the ring itself is hidden
                FindRenderers(dot.transform);
                dots.Add(dot.transform);
            }

            //fan the dots evenly around the ring, offset so odd counts sit symmetrically
            float step = 360f / Mathf.Max(missileCount, 1);
            float start = (missileCount - 1) * 90f;

            for (int i = 0; i < dots.Count; i++)
            {
                dots[i].gameObject.SetActive(i < missileCount);

                if (i < missileCount)
                    dots[i].localRotation = Quaternion.Euler(0f, 0f, start + i * step);
            }

            builtCount = missileCount;
        }

        /// <summary>
        /// The visualizer is torn down and rebuilt whenever the camera changes target, which invalidates
        /// every transform cached above. Leaving them stale gives a ring with no dots for the rest of
        /// the lock.
        /// </summary>
        public override void OnDestroyVisualizer()
        {
            base.OnDestroyVisualizer();

            dots.Clear();
            dotOrigin = null;
            dotTemplate = null;
            builtCount = -1;
        }
    }
}
