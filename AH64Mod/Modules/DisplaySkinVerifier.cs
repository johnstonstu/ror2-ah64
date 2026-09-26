using RoR2;
using UnityEngine;

namespace AH64.Modules
{
    /// <summary>Verifies the live mannequin after skin application, not just the source prefab.</summary>
    public sealed class DisplaySkinVerifier : MonoBehaviour
    {
        private ModelSkinController controller;
        private CharacterModel characterModel;
        private bool failureReported;

        private void OnEnable()
        {
            controller = GetComponent<ModelSkinController>();
            characterModel = GetComponent<CharacterModel>();
            if (controller) controller.onSkinApplied += VerifyAppliedSkin;
        }

        private void OnDisable()
        {
            if (controller) controller.onSkinApplied -= VerifyAppliedSkin;
        }

        private void VerifyAppliedSkin(int index)
        {
            if (failureReported || !controller || !characterModel || index < 0 || index >= controller.skins.Length) return;
            SkinDef skin = controller.skins[index];
            CharacterModel.RendererInfo[] actual = characterModel.baseRendererInfos;
            CharacterModel.RendererInfo[] expected = skin.skinDefParams.rendererInfos;
            if (actual.Length != expected.Length)
            {
                failureReported = true;
                Log.Error($"AH64 lobby skin {skin.name}: renderer count {actual.Length}, expected {expected.Length}.");
                return;
            }
            for (int i = 0; i < actual.Length; i++)
            {
                Renderer renderer = actual[i].renderer;
                if (!renderer || !renderer.transform.IsChildOf(transform)
                    || renderer.name != expected[i].renderer.name
                    || actual[i].defaultMaterial != expected[i].defaultMaterial)
                {
                    failureReported = true;
                    Log.Error($"AH64 lobby skin {skin.name}: renderer/material mismatch at slot {i}.");
                    return;
                }
            }
        }
    }
}
