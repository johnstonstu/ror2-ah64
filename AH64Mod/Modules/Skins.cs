using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AH64.Modules
{
    internal static class Skins
    {
        internal static SkinDef CreateSkinDef(string skinName, Sprite skinIcon, CharacterModel.RendererInfo[] defaultRendererInfos, GameObject root, UnlockableDef unlockableDef = null)
        {
            SkinDefInfo skinDefInfo = new SkinDefInfo
            {
                BaseSkins = Array.Empty<SkinDef>(),
                GameObjectActivations = new SkinDef.GameObjectActivation[0],
                Icon = skinIcon,
                MeshReplacements = new SkinDef.MeshReplacement[0],
                MinionSkinReplacements = new SkinDef.MinionSkinReplacement[0],
                Name = skinName,
                NameToken = skinName,
                ProjectileGhostReplacements = new SkinDef.ProjectileGhostReplacement[0],
                RendererInfos = new CharacterModel.RendererInfo[defaultRendererInfos.Length],
                RootObject = root,
                UnlockableDef = unlockableDef
            };

            On.RoR2.SkinDef.Awake += DoNothing;

            SkinDef skinDef = ScriptableObject.CreateInstance<RoR2.SkinDef>();
            skinDef.baseSkins = skinDefInfo.BaseSkins;
            skinDef.icon = skinDefInfo.Icon;
            skinDef.unlockableDef = skinDefInfo.UnlockableDef;
            skinDef.rootObject = skinDefInfo.RootObject;
            defaultRendererInfos.CopyTo(skinDefInfo.RendererInfos, 0);
            skinDef.rendererInfos = skinDefInfo.RendererInfos;
            skinDef.gameObjectActivations = skinDefInfo.GameObjectActivations;
            skinDef.meshReplacements = skinDefInfo.MeshReplacements;
            skinDef.projectileGhostReplacements = skinDefInfo.ProjectileGhostReplacements;
            skinDef.minionSkinReplacements = skinDefInfo.MinionSkinReplacements;
            skinDef.nameToken = skinDefInfo.NameToken;
            skinDef.name = skinDefInfo.Name;

            //SkinDef.Bake() reads skinDefParams and ignores the legacy top-level fields above (which
            //is what every "obsolete: use skinDefParams instead" build warning is about). Without it
            //Bake bails with "SkinDef X has no SkinDefParams! Cannot bake." and the skin never applies.
            //FromSkinDef converts the fields we just set, so this must stay after them.
            skinDef.skinDefParams = SkinDefParams.FromSkinDef(skinDef);

            On.RoR2.SkinDef.Awake -= DoNothing;

            return skinDef;
        }

        private static void DoNothing(On.RoR2.SkinDef.orig_Awake orig, RoR2.SkinDef self)
        {
        }

        /// <summary>
        /// Give the character-select display prefab its own ModelSkinController, carrying skins rooted at
        /// the *display* rather than the body.
        ///
        /// <para>R2API already papers over a missing controller: on main-menu init it adds one and clones
        /// the body's skins array whenever the two lengths disagree. That silences the warning but leaves
        /// the display holding body-rooted SkinDefs — their rendererInfos reference renderers on the body
        /// model prefab, so applying a skin in the lobby writes to the wrong object's renderers. With a
        /// single default skin nothing visibly breaks, which is why this has gone unnoticed; add a mastery
        /// skin and it would. Building the parallel set here keeps the lobby honest and drops the three
        /// startup warnings.</para>
        /// </summary>
        internal static void CreateDisplaySkinController(GameObject displayPrefab, SkinDef[] bodySkins)
        {
            if (!displayPrefab || bodySkins == null || bodySkins.Length == 0)
                return;

            CharacterModel displayModel = displayPrefab.GetComponent<CharacterModel>();
            if (!displayModel || displayModel.baseRendererInfos == null || displayModel.baseRendererInfos.Length == 0)
            {
                Log.Warning("display prefab has no CharacterModel renderer infos; leaving its skins to R2API's fallback");
                return;
            }

            ModelSkinController controller = displayPrefab.GetComponent<ModelSkinController>();
            if (!controller)
                controller = displayPrefab.AddComponent<ModelSkinController>();

            //the array length is what R2API compares, so this must stay 1:1 with the body's skins
            SkinDef[] displaySkins = new SkinDef[bodySkins.Length];
            for (int i = 0; i < bodySkins.Length; i++)
            {
                SkinDef source = bodySkins[i];
                SkinDef displaySkin = CreateSkinDef(source.name,
                    source.icon,
                    displayModel.baseRendererInfos,
                    displayPrefab,
                    source.unlockableDef);

                //CreateSkinDef sets nameToken from the name; keep the body's token so the lobby label matches
                displaySkin.nameToken = source.nameToken;
                displaySkins[i] = displaySkin;
            }

            controller.skins = displaySkins;
        }

        internal struct SkinDefInfo
        {
            internal SkinDef[] BaseSkins;
            internal Sprite Icon;
            internal string NameToken;
            internal UnlockableDef UnlockableDef;
            internal GameObject RootObject;
            internal CharacterModel.RendererInfo[] RendererInfos;
            internal SkinDef.MeshReplacement[] MeshReplacements;
            internal SkinDef.GameObjectActivation[] GameObjectActivations;
            internal SkinDef.ProjectileGhostReplacement[] ProjectileGhostReplacements;
            internal SkinDef.MinionSkinReplacement[] MinionSkinReplacements;
            internal string Name;
        }

        private static CharacterModel.RendererInfo[] getRendererMaterials(CharacterModel.RendererInfo[] defaultRenderers, params Material[] materials)
        {
            CharacterModel.RendererInfo[] newRendererInfos = new CharacterModel.RendererInfo[defaultRenderers.Length];
            defaultRenderers.CopyTo(newRendererInfos, 0);

            for (int i = 0; i < newRendererInfos.Length; i++)
            {
                try
                {
                    newRendererInfos[i].defaultMaterial = materials[i];
                }
                catch
                {
                    Log.Error("error adding skin rendererinfo material. make sure you're not passing in too many");
                }
            }

            return newRendererInfos;
        }
        /// <summary>
        /// Pass mesh asset names from the bundle in the same order as your renderer infos; use null to
        /// keep a slot's default mesh.
        /// </summary>
        /// <param name="assetBundle">your skindef's rendererinfos to access the renderers</param>
        /// <param name="defaultRendererInfos">your skindef's rendererinfos to access the renderers</param>
        /// <param name="meshes">name of the mesh assets in your project</param>
        /// <returns></returns>
        internal static SkinDef.MeshReplacement[] getMeshReplacements(AssetBundle assetBundle, CharacterModel.RendererInfo[] defaultRendererInfos, params string[] meshes)
        {

            List<SkinDef.MeshReplacement> meshReplacements = new List<SkinDef.MeshReplacement>();

            for (int i = 0; i < defaultRendererInfos.Length; i++)
            {
                if (string.IsNullOrEmpty(meshes[i]))
                    continue;

                meshReplacements.Add(
                new SkinDef.MeshReplacement
                {
                    renderer = defaultRendererInfos[i].renderer,
                    mesh = assetBundle.LoadAsset<Mesh>(meshes[i])
                });
            }

            return meshReplacements.ToArray();
        }
    }
}