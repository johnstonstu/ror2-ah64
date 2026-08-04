using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//The real class is RoR2.CharacterModel — the namespace is part of the script identity a built
//assetbundle uses to resolve components in-game. Without it the component deserializes as a
//missing script and SetupCharacterModel silently falls back to customRendererInfos (Route A).
namespace RoR2
{
    public class CharacterModel : MonoBehaviour
    {
        public CharacterModel.RendererInfo[] baseRendererInfos = Array.Empty<CharacterModel.RendererInfo>();

        public CharacterModel.LightInfo[] baseLightInfos = Array.Empty<CharacterModel.LightInfo>();

        [Serializable]
        public struct RendererInfo
        {

            public Renderer renderer;
            public Material defaultMaterial;

            public ShadowCastingMode defaultShadowCastingMode;

            public bool ignoreOverlays;

            public bool hideOnDeath;
        }

        // Token: 0x0200063F RID: 1599
        [Serializable]
        public struct LightInfo
        {
            public Light light;

            public Color defaultColor;
        }
    }
}
