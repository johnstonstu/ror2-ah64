using RoR2;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AH64.Survivors.Components
{
    // Owner-only designation cue, independent of stock/acknowledgement. It does not claim a lock
    // or that the server has accepted this frame's aim. No camera or character renderer ownership.
    public sealed class AH64HellfireLaser : MonoBehaviour
    {
        private AH64HellfireOwner owner;
        private GameObject beamObject;
        private LineRenderer beam;
        private Material material;
        private bool warned;
        internal bool Visible => beam && beam.enabled;

        private void Awake() { owner = GetComponent<AH64HellfireOwner>(); }
        private void OnEnable() { SceneManager.activeSceneChanged += SceneChanged; }
        private void SceneChanged(Scene previous, Scene next) { DisposeBeam(); }

        private void LateUpdate()
        {
            if (!owner || !owner.LocalHeld)
            {
                Hide();
                return;
            }
            Ray ray = owner.AimRay;
            if (!AH64HellfireAim.ValidRay(owner.Body, ray))
            {
                Hide();
                return;
            }
            if (!EnsureBeam())
                return;
            // Start on the exact native aim ray, avoiding a rail-offset beam that would shine
            // through nearby cover while its endpoint claims a different designation.
            Vector3 point = AH64HellfireAim.Resolve(owner.Body, ray);
            beam.SetPosition(0, ray.origin);
            beam.SetPosition(1, point);
            beam.enabled = true;
        }

        private bool EnsureBeam()
        {
            if (beam)
                return true;
            // Existing base-game Commando tracer material is already loaded by AH64Assets.
            // Clone the material only; do not instantiate a transient tracer or require DLC/Unity assets.
            GameObject source = AH64Assets.chaingunTracerEffect;
            LineRenderer donor = source ? source.GetComponentInChildren<LineRenderer>(true) : null;
            if (!donor || !donor.sharedMaterial)
            {
                if (!warned)
                {
                    warned = true;
                    Log.Warning("Hellfire laser: base-game tracer material unavailable; presentation cannot be created.");
                }
                return false;
            }
            material = new Material(donor.sharedMaterial);
            Color red = new Color(1f, 0.16f, 0.05f, 0.85f);
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", red);
            if (material.HasProperty("_Color")) material.SetColor("_Color", red);
            if (material.HasProperty("_ZTest")) material.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            beamObject = new GameObject("AH64HellfireDesignationLaser");
            beamObject.transform.SetParent(transform, false);
            beam = beamObject.AddComponent<LineRenderer>();
            beam.sharedMaterial = material;
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.startWidth = beam.endWidth = AH64HellfireFeedbackValues.LaserWidth;
            beam.startColor = beam.endColor = red;
            beam.numCapVertices = 4;
            beam.shadowCastingMode = ShadowCastingMode.Off;
            beam.receiveShadows = false;
            beam.enabled = false;
            return true;
        }

        private void Hide() { if (beam) beam.enabled = false; }
        private void DisposeBeam()
        {
            Hide();
            if (beamObject) Destroy(beamObject);
            if (material) Destroy(material);
            beam = null;
            beamObject = null;
            material = null;
        }
        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            DisposeBeam();
        }
        private void OnDestroy() { DisposeBeam(); }
    }
}
