using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ReadmeCapture
{
    public sealed partial class CapturePlugin
    {
        // Read Unity's rendered framebuffer, not an OS window backing surface.
        // This excludes external chat overlays and retains the native game HUD.
        private IEnumerator RecordFrames()
        {
            string directory = Path.Combine(output,"frames");
            Directory.CreateDirectory(directory);
            using (var manifest = new StreamWriter(Path.Combine(output,"frames.jsonl"))) {
                manifest.AutoFlush = true;
                int frame = 0;
                float next = Time.realtimeSinceStartup;
                while (!finished) {
                    yield return new WaitForEndOfFrame();
                    if (finished) yield break;
                    if (Time.realtimeSinceStartup < next) continue;
                    string utc = DateTime.UtcNow.ToString("o");
                    Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
                    if (!texture || texture.width != 1280 || texture.height != 720)
                        throw new InvalidOperationException("Unexpected rendered capture dimensions");
                    string name = frame.ToString("D6") + ".jpg";
                    try { File.WriteAllBytes(Path.Combine(directory,name),texture.EncodeToJPG(96)); }
                    finally { Destroy(texture); }
                    manifest.WriteLine("{\"file\":\"" + name + "\",\"utc\":\"" + utc + "\"}");
                    if (frame++ == 0) {
                        File.WriteAllText(Path.Combine(output,"recorder.json"),"{\"source\":\"Unity ScreenCapture.CaptureScreenshotAsTexture; rendered end-of-frame\",\"width\":1280,\"height\":720,\"fpsTarget\":16}");
                        File.WriteAllText(Path.Combine(output,"recording.ready"),"verified engine framebuffer");
                    }
                    next = Math.Max(next + 1f/16f,Time.realtimeSinceStartup);
                }
            }
        }
    }
}
