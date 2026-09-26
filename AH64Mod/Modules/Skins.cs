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
        internal static void CreateDisplaySkinController(GameObject displayPrefab, SkinDef[] bodySkins, Func<Material, Material> prepareMaterial = null)
        {
            if (!displayPrefab || bodySkins == null || bodySkins.Length == 0)
                return;

            CharacterModel displayModel = displayPrefab.GetComponent<CharacterModel>();
            if (!displayModel)
            {
                Log.Warning("display prefab has no CharacterModel; leaving its skins to R2API's fallback");
                return;
            }

            ModelSkinController controller = displayPrefab.GetComponent<ModelSkinController>();
            if (!controller)
                controller = displayPrefab.AddComponent<ModelSkinController>();

            // Never trust displayModel.baseRendererInfos here: CreateDisplayPrefab can fall back
            // to the body's array when the bundled CharacterModel data is unavailable at runtime.
            // Material-name matching alone leaves body Renderer references in a display-rooted skin.
            Renderer[] displayRenderers = displayPrefab.GetComponentsInChildren<Renderer>(true);
            SkinDef[] displaySkins = new SkinDef[bodySkins.Length];
            for (int i = 0; i < bodySkins.Length; i++)
            {
                SkinDef source = bodySkins[i];
                var displayInfos = MapDisplayRenderers(source, displayPrefab, displayRenderers, prepareMaterial);
                SkinDef displaySkin = CreateSkinDef(source.name + "_Display",
                    source.icon, displayInfos, displayPrefab, source.unlockableDef);
                displaySkin.nameToken = source.nameToken;
                displaySkins[i] = displaySkin;
                Log.Debug($"AH64 lobby skin {source.name}: verified {displayInfos.Length} display-owned renderers.");
            }

            controller.skins = displaySkins;
            if (!displayPrefab.GetComponent<DisplaySkinVerifier>()) displayPrefab.AddComponent<DisplaySkinVerifier>();
            // Make the default preview readable before the mannequin's first asynchronous apply,
            // and replace the borrowed body array rather than mutating its entries in place.
            displayModel.baseRendererInfos = (CharacterModel.RendererInfo[])displaySkins[0].skinDefParams.rendererInfos.Clone();
            foreach (CharacterModel.RendererInfo info in displayModel.baseRendererInfos)
                info.renderer.sharedMaterial = info.defaultMaterial;
        }

        private static CharacterModel.RendererInfo[] MapDisplayRenderers(SkinDef source, GameObject root,
            Renderer[] renderers, Func<Material, Material> prepareMaterial)
        {
            var infos = (CharacterModel.RendererInfo[])source.skinDefParams.rendererInfos.Clone();
            var prepared = new Dictionary<Material, Material>();
            for (int i = 0; i < infos.Length; i++)
            {
                Renderer bodyRenderer = infos[i].renderer;
                Renderer match = null;
                foreach (Renderer candidate in renderers)
                {
                    if (!bodyRenderer || candidate.name != bodyRenderer.name) continue;
                    if (match) throw new InvalidOperationException("Duplicate display renderer " + candidate.name);
                    match = candidate;
                }
                if (!match || !match.transform.IsChildOf(root.transform))
                    throw new InvalidOperationException("Skin " + source.name + " cannot map display renderer "
                        + (bodyRenderer ? bodyRenderer.name : "<null>"));
                infos[i].renderer = match;
                Material material = infos[i].defaultMaterial;
                if (material && prepareMaterial != null)
                {
                    if (!prepared.TryGetValue(material, out Material lobbyMaterial))
                    {
                        lobbyMaterial = prepareMaterial(material);
                        prepared.Add(material, lobbyMaterial);
                    }
                    material = lobbyMaterial;
                }
                infos[i].defaultMaterial = material;
            }
            return infos;
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