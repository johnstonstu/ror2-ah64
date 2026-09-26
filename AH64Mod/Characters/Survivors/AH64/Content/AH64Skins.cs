using System;
using System.Collections.Generic;
using AH64.Modules;
using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    internal static class AH64Skins
    {
        internal static SkinDef[] Create(AssetBundle bundle, CharacterModel model)
        {
            // The default atlas is already olive. Multiplying it by sand/white still produces green.
            // Use the existing neutral variation for alternate paint, preserving all normal maps.
            Texture neutral = bundle.LoadAsset<Texture2D>("texAH64SurfaceVariation");
            if (!neutral)
                throw new InvalidOperationException("AH64 skins require texAH64SurfaceVariation in the asset bundle.");

            return new[]
            {
                Skins.CreateSkinDef("DEFAULT_SKIN", bundle.LoadAsset<Sprite>("texMainSkin"),
                    model.baseRendererInfos, model.gameObject),
                CreatePaint(model, neutral, "DESERT", new Color(0.78f, 0.65f, 0.43f),
                    new Color(0.31f, 0.25f, 0.19f), new Color(0.87f, 0.81f, 0.64f)),
                CreatePaint(model, neutral, "ARCTIC", new Color(0.81f, 0.85f, 0.87f),
                    new Color(0.24f, 0.28f, 0.31f), new Color(0.67f, 0.30f, 0.13f))
            };
        }

        private static SkinDef CreatePaint(CharacterModel model, Texture neutral, string name,
            Color body, Color mechanical, Color markings)
        {
            var infos = (CharacterModel.RendererInfo[])model.baseRendererInfos.Clone();
            var materials = new Dictionary<Material, Material>();
            for (int i = 0; i < infos.Length; i++)
            {
                Material source = infos[i].defaultMaterial;
                if (!source) continue;
                if (!materials.TryGetValue(source, out Material material))
                {
                    material = new Material(source) { name = source.name + "_" + name };
                    if (source.name.Contains("matAH64Body"))
                    {
                        material.SetTexture("_MainTex", neutral);
                        material.SetColor("_Color", body);
                    }
                    else if (source.name.Contains("matAH64Dark"))
                    {
                        // Preserve the rotor/gun/pod value hierarchy while removing the olive tint.
                        float value = source.HasProperty("_Color") ? source.GetColor("_Color").maxColorComponent : 0.3f;
                        material.SetColor("_Color", mechanical * Mathf.Clamp(value / 0.29f, 0.65f, 1.30f));
                    }
                    else if (source.name.Contains("matAH64Markings"))
                        material.SetColor("_Color", markings);
                    materials.Add(source, material);
                }
                infos[i].defaultMaterial = material;
            }
            return Skins.CreateSkinDef(AH64Survivor.AH64_PREFIX + name + "_SKIN_NAME",
                CreateIcon(name, body, mechanical, markings), infos, model.gameObject);
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

        private static Sprite CreateIcon(string name, Color body, Color mechanical, Color markings)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "texAH64Skin" + name,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool border = x < 5 || y < 5 || x >= size - 5 || y >= size - 5;
                    pixels[y * size + x] = border ? new Color(0.12f, 0.14f, 0.16f) :
                        (x + y < 104 ? mechanical : (x + y < 140 ? markings : body));
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
