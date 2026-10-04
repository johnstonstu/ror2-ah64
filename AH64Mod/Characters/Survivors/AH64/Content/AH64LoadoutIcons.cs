using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>Illustrated attachment icons shipped in the DLL, independently of the model bundle.</summary>
    internal static class AH64LoadoutIcons
    {
        private static readonly Dictionary<string, Sprite> Icons = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> Failed = new HashSet<string>();

        // Skills are registered on Unity's main thread. These shared sprites live for the plugin lifetime.
        internal static Sprite Get(string skillName, Sprite fallback)
        {
            if (string.IsNullOrEmpty(skillName)) return fallback;
            if (Icons.TryGetValue(skillName, out Sprite cached)) return cached;
            if (Failed.Contains(skillName)) return fallback;

            Texture2D texture = null;
            try
            {
                var assembly = typeof(AH64LoadoutIcons).Assembly;
                string suffix = ".Content.LoadoutIcons." + skillName + ".png";
                string resource = Array.Find(assembly.GetManifestResourceNames(),
                    name => name.EndsWith(suffix, StringComparison.Ordinal));
                if (resource == null) throw new InvalidDataException("Embedded icon missing: " + skillName);
                byte[] bytes;
                using (Stream stream = assembly.GetManifestResourceStream(resource))
                using (var buffer = new MemoryStream())
                {
                    if (stream == null) throw new InvalidDataException("Cannot open " + resource);
                    stream.CopyTo(buffer);
                    bytes = buffer.ToArray();
                }

                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                texture.name = skillName + "LoadoutIcon";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                if (!ImageConversion.LoadImage(texture, bytes, true))
                    throw new InvalidDataException("Invalid embedded PNG: " + skillName);
                if (texture.width != texture.height || texture.width < 128)
                    throw new InvalidDataException("Expected square attachment icon: " + skillName);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
                sprite.name = texture.name;
                Icons.Add(skillName, sprite);
                return sprite;
            }
            catch (Exception exception)
            {
                // An optional UI improvement must never prevent survivor registration.
                if (texture) UnityEngine.Object.Destroy(texture);
                Failed.Add(skillName);
                Log.Warning("AH-64 attachment icon " + skillName + " failed; using bundle fallback: " + exception.Message);
                return fallback;
            }
        }
    }
}
