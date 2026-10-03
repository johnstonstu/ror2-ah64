using System;
using System.Linq;
using RoR2;
using UnityEngine;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        // Render the verified local gameplay camera at its existing native dimensions.
        private byte[] ReadGameCameraPng()
        {
            var user = LocalUserManager.GetFirstLocalUser();
            var owned = Camera.allCameras.Select(candidate => candidate.GetComponent<SceneCamera>()).Where(scene =>
                scene && scene.cameraRigController && scene.cameraRigController.localUserViewer == user &&
                pilot && scene.cameraRigController.target == pilot.gameObject && scene.camera == scene.cameraRigController.sceneCam).ToArray();
            if (user == null || owned.Length != 1) throw new InvalidOperationException("Missing/ambiguous local AH64 gameplay camera.");
            var camera = owned[0].camera;
            int width = camera.pixelWidth, height = camera.pixelHeight;
            if (width <= 0 || height <= 0) throw new InvalidOperationException("Invalid gameplay camera dimensions.");
            RenderTexture previous = RenderTexture.active, previousCameraTarget = camera.targetTexture;
            string target = previous ? previous.name + "/" + previous.GetInstanceID() + "/" + previous.width + "x" + previous.height : "backbuffer";
            Event("capture-source", "source=owned-gameplay-camera; phase=end-of-frame; width=" + width + "; height=" + height +
                "; frame=" + Time.frameCount + "; realtime=" + Time.realtimeSinceStartup + "; previousTarget=" + target +
                "; camera=" + camera.name + "/" + camera.GetInstanceID() + "; targetBody=" + pilot.gameObject.name + "; UI=not-rendered");
            Texture2D texture = null;
            Event("capture-camera-diagnostics", DescribeCaptureCamera(camera));
            RenderTexture capture = null;
            int errorsBefore = errors;
            try {
                try {
                    capture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                    if (!capture.Create()) throw new InvalidOperationException("Gameplay capture target creation failed.");
                    camera.targetTexture = capture;
                    camera.Render();
                    Event("capture-before-read", DescribeCaptureCamera(camera));
                    RenderTexture.active = capture;
                    texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    texture.Apply(false, false);
                } finally {
                    camera.targetTexture = previousCameraTarget;
                    RenderTexture.active = previous;
                }
                if (errors != errorsBefore) throw new InvalidOperationException("Unity logged an error during gameplay capture.");
                Color32[] pixels = texture.GetPixels32();
                if (pixels.Length == 0 || !pixels.Any(pixel => pixel.r != pixels[0].r || pixel.g != pixels[0].g || pixel.b != pixels[0].b))
                    throw new InvalidOperationException("Gameplay capture has no RGB variation; visual evidence unavailable.");
                byte[] png = ImageConversion.EncodeToPNG(texture);
                if (png == null || png.Length == 0) throw new InvalidOperationException("Gameplay PNG encoding was empty.");
                Event("capture-read", "source=owned-gameplay-camera; width=" + texture.width + "; height=" + texture.height +
                    "; frame=" + Time.frameCount + "; restoredTarget=" + (RenderTexture.active == previous) +
                    "; restoredCameraTarget=" + (camera.targetTexture == previousCameraTarget) + "; pixels=" + pixels.Length + "; bytes=" + png.Length);
                return png;
            } finally {
                if (texture) UnityEngine.Object.Destroy(texture);
                if (capture) { capture.Release(); UnityEngine.Object.Destroy(capture); }
            }
        }
    }
}
