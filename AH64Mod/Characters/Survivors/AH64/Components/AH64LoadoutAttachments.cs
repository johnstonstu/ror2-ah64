using AH64.Survivors.SkillStates;
using EntityStates;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // Sole visibility owner for all six variants and retired wing stores. State reads pose
    // hardware only; they never create a projectile, consume stock or send a message.
    [DefaultExecutionOrder(215)]
    public sealed class AH64LoadoutAttachments : MonoBehaviour
    {
        public bool PresentationReady;
        public bool IsDisplay;
        public Renderer[] Renderers;
        public Renderer[] LegacyRenderers;
        public Renderer[] LongbowStores;
        private Renderer[][] specialGroups, utilityGroups;
        public Transform[] SpecialRoots;
        public Transform[] UtilityRoots;
        public Transform[] RecoilPivots;
        public Transform[] UtilityPivots;
        public AH64BombingRunRack BombRack;
        private int specialIndex = -1, utilityIndex = -1;
        private CharacterBody body;
        private EntityStateMachine[] machines;
        private EntityState lastFire;
        private Vector3[] recoilRest;
        private Quaternion[] utilityRest;
        private float recoilAge = 1f;

        public void Select(int special, int utility)
        {
            specialIndex = special >= 0 && special < 3 ? special : -1;
            utilityIndex = utility >= 0 && utility < 3 ? utility : -1;
            ApplyVisibility();
        }

        private void Start()
        {
            body = GetComponentInParent<CharacterBody>();
            machines = body ? body.GetComponents<EntityStateMachine>() : new EntityStateMachine[0];
            recoilRest = new Vector3[RecoilPivots == null ? 0 : RecoilPivots.Length];
            for (int i = 0; i < recoilRest.Length; i++) if (RecoilPivots[i]) recoilRest[i] = RecoilPivots[i].localPosition;
            utilityRest = new Quaternion[UtilityPivots == null ? 0 : UtilityPivots.Length];
            for (int i = 0; i < utilityRest.Length; i++) if (UtilityPivots[i]) utilityRest[i] = UtilityPivots[i].localRotation;
        }

        private bool Visible => PresentationReady && (IsDisplay || (body && body.healthComponent && body.healthComponent.alive
            && body.modelLocator && body.modelLocator.modelTransform && transform.IsChildOf(body.modelLocator.modelTransform)));

        private void LateUpdate()
        {
            if (BombRack) BombRack.PresentationReady = PresentationReady && !IsDisplay;
            Pose();
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            if (!PresentationReady) { Hide(Renderers); return; }
            Hide(LegacyRenderers);
            bool visible = Visible;
            if (specialGroups == null) specialGroups = Groups(SpecialRoots);
            if (utilityGroups == null) utilityGroups = Groups(UtilityRoots);
            if (SpecialRoots != null)
                for (int i = 0; i < SpecialRoots.Length; i++) SetGroup(specialGroups[i], visible && specialIndex == i);
            if (UtilityRoots != null)
                for (int i = 0; i < UtilityRoots.Length; i++) SetGroup(utilityGroups[i], visible && utilityIndex == i);
            if (visible && specialIndex == 0 && LongbowStores != null)
            {
                SkillLocator skills = body ? body.GetComponent<SkillLocator>() : null;
                float fraction = IsDisplay ? 1f : skills && skills.special ? skills.special.stock / (float)Mathf.Max(1, skills.special.maxStock) : 0f;
                int shown = Mathf.CeilToInt(LongbowStores.Length * fraction);
                for (int i = 0; i < LongbowStores.Length; i++) if (LongbowStores[i]) LongbowStores[i].forceRenderingOff = i >= shown;
            }
            if (visible && specialIndex == 2 && BombRack && !IsDisplay && BombRack.FeedBombs != null)
                for (int i = 0; i < BombRack.FeedBombs.Length; i++)
                    if (BombRack.FeedBombs[i]) BombRack.FeedBombs[i].forceRenderingOff = !BombRack.FeedVisible(i);
        }

        private void Pose()
        {
            if (recoilRest == null || utilityRest == null) return;
            EntityState firing = null;
            bool jink = false, smoke = false, banked = false;
            if (Visible && !IsDisplay && machines != null)
                foreach (EntityStateMachine machine in machines)
                {
                    if (!machine) continue;
                    EntityState state = machine.state;
                    if (state is FireHellfire || state is FireLongbow) firing = state;
                    jink |= state is ServoDash; smoke |= state is SmokeBackflip; banked |= state is BrakingTurn;
                }
            if (firing != null && firing != lastFire) recoilAge = 0f;
            lastFire = firing;
            recoilAge += Time.deltaTime;
            float recoil = Mathf.Max(0f, 1f - recoilAge / 0.20f) * 0.035f;
            for (int i = 0; i < recoilRest.Length; i++)
                if (RecoilPivots[i]) RecoilPivots[i].localPosition = recoilRest[i] - Vector3.forward * recoil;
            for (int i = 0; i < utilityRest.Length; i++)
            {
                if (!UtilityPivots[i]) continue;
                float angle = i < 2 ? (jink ? 18f : 0f) : i == 2 ? (smoke ? 12f : 0f) : (banked ? 42f : 0f);
                Quaternion target = utilityRest[i] * Quaternion.Euler(angle, 0f, 0f);
                UtilityPivots[i].localRotation = Quaternion.Slerp(UtilityPivots[i].localRotation, target, 1f - Mathf.Exp(-18f * Time.deltaTime));
            }
        }

        private static Renderer[][] Groups(Transform[] roots)
        {
            var result = new Renderer[roots == null ? 0 : roots.Length][];
            for (int i = 0; i < result.Length; i++) result[i] = roots[i] ? roots[i].GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            return result;
        }

        private static void SetGroup(Renderer[] group, bool visible)
        {
            foreach (Renderer renderer in group) if (renderer) renderer.forceRenderingOff = !visible;
        }
        private static void Hide(Renderer[] renderers)
        {
            if (renderers != null) foreach (Renderer renderer in renderers) if (renderer) renderer.forceRenderingOff = true;
        }

        private void OnDisable()
        {
            Hide(Renderers);
            if (recoilRest != null) for (int i = 0; i < recoilRest.Length; i++) if (RecoilPivots[i]) RecoilPivots[i].localPosition = recoilRest[i];
            if (utilityRest != null) for (int i = 0; i < utilityRest.Length; i++) if (UtilityPivots[i]) UtilityPivots[i].localRotation = utilityRest[i];
            lastFire = null; recoilAge = 1f;
        }
    }
}
