using RoR2.UI;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Small local-only development stamp on HUDs currently targeting an AH-64 body.
    /// The semantic version identifies network compatibility, the intentional focus label
    /// tells testers what changed, and the revision distinguishes source builds.
    /// </summary>
    internal sealed class AH64BuildStampController : MonoBehaviour
    {
        // Update deliberately when the focus of a playtest build changes.
        private const string ReleaseFocus = "Release";
        private const float FontSize = 13f;
        private static readonly Color StampColor = new Color(0.72f, 0.78f, 0.68f, 0.62f);

        private static bool initialized;
        private static string buildLabel;

        private HUD hud;
        private GameObject stampObject;

        public static void Init()
        {
            if (initialized)
                return;

            initialized = true;
            buildLabel = CreateBuildLabel();
            On.RoR2.UI.HUD.Awake += HUD_Awake;
        }

        private static void HUD_Awake(On.RoR2.UI.HUD.orig_Awake orig, HUD self)
        {
            orig(self);

            if (self && !self.GetComponent<AH64BuildStampController>())
                self.gameObject.AddComponent<AH64BuildStampController>();
        }

        private static string CreateBuildLabel()
        {
            Assembly assembly = typeof(AH64Plugin).Assembly;
            string revision = "local";

            AssemblyInformationalVersionAttribute info =
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            string informationalVersion = info != null ? info.InformationalVersion : string.Empty;
            int plusIndex = informationalVersion.IndexOf('+');
            if (plusIndex >= 0 && plusIndex + 1 < informationalVersion.Length)
            {
                revision = informationalVersion.Substring(plusIndex + 1);
                if (revision.Length > 7)
                    revision = revision.Substring(0, 7);
            }

            return $"AH-64 v{AH64Plugin.MODVERSION} • {ReleaseFocus} • {revision}";
        }

        private void Awake()
        {
            hud = GetComponent<HUD>();
            CreateStamp();
        }

        private void OnEnable()
        {
            HUD.onHudTargetChangedGlobal += OnHudTargetChanged;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            HUD.onHudTargetChangedGlobal -= OnHudTargetChanged;
        }

        private void OnDestroy()
        {
            HUD.onHudTargetChangedGlobal -= OnHudTargetChanged;
        }

        private void CreateStamp()
        {
            if (!hud || !hud.mainContainer || stampObject)
                return;

            stampObject = new GameObject(
                "AH64BuildStamp",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(HGTextMeshProUGUI));
            stampObject.transform.SetParent(hud.mainContainer.transform, false);

            RectTransform rect = (RectTransform)stampObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -12f);
            rect.sizeDelta = new Vector2(360f, 22f);

            HGTextMeshProUGUI text = stampObject.GetComponent<HGTextMeshProUGUI>();
            text.text = buildLabel;
            text.fontSize = FontSize;
            text.color = StampColor;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = false;
            text.raycastTarget = false;

            RefreshVisibility();
        }

        private void OnHudTargetChanged(HUD changedHud)
        {
            if (changedHud == hud)
                RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (!stampObject || !hud)
                return;

            GameObject target = hud.targetBodyObject;
            bool isAH64 = target && target.GetComponent<AH64PassiveComponent>();
            stampObject.SetActive(isAH64);
        }
    }
}
