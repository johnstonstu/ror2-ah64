using System;
using System.Linq;
using UnityEngine;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        // Called only after WaitForEndOfFrame, after game cameras and GUI render.
        private byte[] ReadBackbufferPng()
        {
            int width = Screen.width, height = Screen.height;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid backbuffer dimensions.");
            RenderTexture previous = RenderTexture.active;
            string target = previous ? previous.name + "/" + previous.GetInstanceID() + "/" + previous.width + "x" + previous.height : "backbuffer";
            string cameras = string.Join("|", Camera.allCameras.Select(camera => camera.name + ":" +
                (camera.targetTexture ? camera.targetTexture.name + "/" + camera.targetTexture.width + "x" + camera.targetTexture.height : "backbuffer")));
            Event("capture-source", "source=main-window-backbuffer; phase=end-of-frame; width=" + width + "; height=" + height +
                "; frame=" + Time.frameCount + "; realtime=" + Time.realtimeSinceStartup + "; previousTarget=" + target + "; cameras=" + cameras);
            Texture2D texture = null;
            try {
                try {
                    // Null explicitly binds the window backbuffer, never a portrait/preview texture.
                    RenderTexture.active = null;
                    texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    texture.Apply(false, false);
                } finally {
                    RenderTexture.active = previous;
                }
                byte[] png = ImageConversion.EncodeToPNG(texture);
                if (png == null || png.Length == 0) throw new InvalidOperationException("Backbuffer PNG encoding was empty.");
                Event("capture-read", "source=main-window-backbuffer; width=" + texture.width + "; height=" + texture.height +
                    "; frame=" + Time.frameCount + "; restoredTarget=" + (RenderTexture.active == previous) + "; bytes=" + png.Length);
                return png;
            } finally {
                if (texture) UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
