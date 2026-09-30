using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Tiny crosshair-white airtime tick just under the reticle. Shows while the AH-64 is above resting
    /// height or refilling,
    /// and fades out once the tank is full at rest. Parented to <c>HUD.mainUIPanel</c>, so it hides with
    /// the rest of the HUD in cinematics and when the HUD is toggled off.
    ///
    /// <para>Airtime only runs on the body's authority, so the bar is drawn for the local pilot only.</para>
    /// </summary>
    internal class AH64AirtimeGauge : MonoBehaviour
    {
        // Sits just outside the widest vanilla reticle spread so it never overlaps the crosshair itself.
        private const float Width = 36f;
        private const float Height = 2f;
        private const float OffsetBelowCrosshair = -34f;
        private const float LingerSeconds = 0.8f;
        private const float FadeRate = 6f;
        private const float MaxAlpha = 0.85f;
        private const float LowFraction = 0.3f;

        private static readonly Color Normal = new Color(1f, 1f, 1f);
        private static readonly Color Low = new Color(1f, 0.78f, 0.45f);
        private static readonly Color Empty = new Color(1f, 0.42f, 0.36f);

        private static bool installed;

        private HUD hud;
        private CanvasGroup group;
        private RectTransform fill;
        private Image fillImage;
        private float linger;

        internal static void Install()
        {
            if (installed)
                return;

            installed = true;
            On.RoR2.UI.HUD.Awake += (orig, self) =>
            {
                orig(self);
                self.gameObject.AddComponent<AH64AirtimeGauge>();
            };
        }

        private void Awake()
        {
            hud = GetComponent<HUD>();
        }

        private void Update()
        {
            AH64HoverController hover = FindLocalHover();
            bool show = false;
            float fraction = 1f;

            if (hover && !AH64PlaytestConfig.ClassicAltitude)
            {
                float budget = hover.GetAirtimeBudget();
                fraction = budget > 0f ? Mathf.Clamp01(hover.Airtime / budget) : 0f;
                if (!hover.IsAtRestAltitude || fraction < 0.999f)
                    linger = LingerSeconds;
                else
                    linger -= Time.unscaledDeltaTime;
                show = linger > 0f;
            }
            else
            {
                linger = 0f;
            }

            if (!group)
            {
                if (!show || !Build())
                    return;
            }

            group.alpha = Mathf.MoveTowards(group.alpha, show ? MaxAlpha : 0f, FadeRate * Time.unscaledDeltaTime);
            fill.anchorMin = new Vector2(0.5f - fraction * 0.5f, 0f);
            fill.anchorMax = new Vector2(0.5f + fraction * 0.5f, 1f);
            fillImage.color = hover && hover.IsAirtimeLocked ? Empty
                : fraction < LowFraction ? Low
                : Normal;
        }

        private AH64HoverController FindLocalHover()
        {
            GameObject target = hud ? hud.targetBodyObject : null;
            if (!target)
                return null;

            CharacterBody body = target.GetComponent<CharacterBody>();
            if (!body || !body.hasEffectiveAuthority)
                return null;

            return target.GetComponent<AH64HoverController>();
        }

        private bool Build()
        {
            if (!hud || !hud.mainUIPanel)
                return false;

            var root = new GameObject("AH64AirtimeGauge", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(hud.mainUIPanel.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Width, Height);
            rect.anchoredPosition = new Vector2(0f, OffsetBelowCrosshair);

            group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            Image backing = root.AddComponent<Image>();
            backing.color = new Color(0f, 0f, 0f, 0.3f);
            backing.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform));
            fill = (RectTransform)fillObject.transform;
            fill.SetParent(rect, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            fillImage = fillObject.AddComponent<Image>();
            fillImage.raycastTarget = false;
            return true;
        }
    }
}
