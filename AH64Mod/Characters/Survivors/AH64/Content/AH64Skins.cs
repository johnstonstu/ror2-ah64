using System;
using System.Collections.Generic;
using AH64.Modules;
using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    internal static class AH64Skins
    {
        private sealed class Paint
        {
            public string Name;
            public string Icon;
            public Color Body;
            public Color Mechanical;
            public Color Markings;
            //CARC is a flat finish; the stylised paints keep the default sheen.
            public bool Matte;
            //Locked behind the Mastery achievement (beat the game on Monsoon as the AH-64).
            public bool Mastery;
        }

        //Colours are tints over the neutral atlas, whose median panel value is 0.80, so the paint
        //players see is roughly 0.8x these. Order is the saved loadout index: append, never reorder.
        private static readonly Paint[] Paints =
        {
            //Army desert CARC 686A (FS 33446) with black low-visibility markings.
            new Paint { Name = "DESERT", Icon = "texAH64SkinDesert", Body = new Color(0.82f, 0.71f, 0.54f),
                Mechanical = new Color(0.27f, 0.25f, 0.21f), Markings = new Color(0.10f, 0.09f, 0.08f), Matte = true },
            new Paint { Name = "ARCTIC", Icon = "texAH64SkinArctic", Body = new Color(0.89f, 0.93f, 0.95f),
                Mechanical = new Color(0.24f, 0.28f, 0.31f), Markings = new Color(0.67f, 0.30f, 0.13f) },
            //US Army CARC Aircraft Green (FS 34031), greyer than the stylised default olive. Lifted above
            //the real chip: at true value the matte airframe and near-black mechanicals lost all shading
            //under stage lighting. Mechanical parts land at the default skin's brightness.
            new Paint { Name = "ARMY", Icon = "texAH64SkinArmy", Body = new Color(0.31f, 0.345f, 0.25f),
                Mechanical = new Color(0.25f, 0.27f, 0.22f), Markings = new Color(0.07f, 0.07f, 0.065f), Matte = true },
            //Mastery reward. Night Stalker black after the Army's special operations aviation regiment,
            //with dull red markings. Semi-gloss rather than matte: a flat black airframe loses its shape
            //under stage lighting. Lifted off true black for the same reason.
            new Paint { Name = "NIGHT", Icon = "texAH64SkinNight", Body = new Color(0.20f, 0.21f, 0.22f),
                Mechanical = new Color(0.14f, 0.15f, 0.16f), Markings = new Color(0.50f, 0.11f, 0.08f), Mastery = true },
        };

        internal static SkinDef[] Create(AssetBundle bundle, CharacterModel model)
        {
            //The default atlas is olive, and tinting it always reads green. Alternate paints tint a
            //greyscale copy that keeps the panel lines, rivets and grime.
            Texture neutral = bundle.LoadAsset<Texture2D>("texAH64BodyPanelAtlasNeutral");
            if (!neutral)
                throw new InvalidOperationException("AH64 skins require texAH64BodyPanelAtlasNeutral in the asset bundle.");

            var skins = new SkinDef[Paints.Length + 1];
            skins[0] = Skins.CreateSkinDef("DEFAULT_SKIN", LoadIcon(bundle, "texMainSkin"),
                model.baseRendererInfos, model.gameObject);
            for (int i = 0; i < Paints.Length; i++)
                skins[i + 1] = CreatePaint(model, neutral, Paints[i], LoadIcon(bundle, Paints[i].Icon));
            return skins;
        }

        private static Sprite LoadIcon(AssetBundle bundle, string name)
        {
            Sprite icon = bundle.LoadAsset<Sprite>(name);
            if (!icon)
                throw new InvalidOperationException("AH64 skins require the " + name + " sprite in the asset bundle.");
            return icon;
        }

        private static SkinDef CreatePaint(CharacterModel model, Texture neutral, Paint paint, Sprite icon)
        {
            var infos = (CharacterModel.RendererInfo[])model.baseRendererInfos.Clone();
            var materials = new Dictionary<Material, Material>();
            for (int i = 0; i < infos.Length; i++)
            {
                Material source = infos[i].defaultMaterial;
                if (!source) continue;
                if (!materials.TryGetValue(source, out Material material))
                {
                    material = new Material(source) { name = source.name + "_" + paint.Name };
                    if (source.name.Contains("matAH64Body"))
                    {
                        material.SetTexture("_MainTex", neutral);
                        material.SetColor("_Color", paint.Body);
                        if (paint.Matte) ApplyMatte(material, 0.30f, 0.18f);
                    }
                    else if (source.name.Contains("matAH64Dark"))
                    {
                        // Preserve the rotor/gun/pod value hierarchy while removing the olive tint.
                        float value = source.HasProperty("_Color") ? source.GetColor("_Color").maxColorComponent : 0.3f;
                        material.SetColor("_Color", paint.Mechanical * Mathf.Clamp(value / 0.29f, 0.65f, 1.30f));
                        if (paint.Matte) ApplyMatte(material, 0.22f, 0.11f);
                    }
                    else if (source.name.Contains("matAH64Markings"))
                    {
                        material.SetColor("_Color", paint.Markings);
                        if (paint.Matte) ApplyMatte(material, 0.24f, 0.10f);
                    }
                    materials.Add(source, material);
                }
                infos[i].defaultMaterial = material;
            }
            UnlockableDef unlockable = paint.Mastery ? AH64Unlockables.masterySkinUnlockableDef : null;
            if (paint.Mastery && !unlockable)
                Log.Warning("AH64 mastery unlockable is missing; the " + paint.Name + " skin is unlocked for everyone.");
            return Skins.CreateSkinDef(AH64Survivor.AH64_PREFIX + paint.Name + "_SKIN_NAME",
                icon, infos, model.gameObject, unlockable);
        }

        private static void ApplyMatte(Material material, float smoothness, float specular)
        {
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_SpecularStrength", specular);
        }

        internal static Material CreateLobbyMaterial(Material source)
        {
            var material = new Material(source) { name = source.name + " (Lobby)" };
            if (material.HasProperty("_Color"))
            {
                Color tint = material.GetColor("_Color");
                // A capped value lift preserves paint hue instead of resetting every skin to olive.
                float peak = Mathf.Max(tint.r, Mathf.Max(tint.g, tint.b));
                float lift = peak > 0f ? Mathf.Min(1.65f, 0.90f / peak) : 1f;
                tint *= Mathf.Max(1f, lift);
                tint.a = material.GetColor("_Color").a;
                material.SetColor("_Color", tint);
                if (material.HasProperty("_EmColor")) material.SetColor("_EmColor", tint * 0.40f);
                if (material.HasProperty("_EmPower")) material.SetFloat("_EmPower", 0.70f);
            }
            return material;
        }
    }
}
