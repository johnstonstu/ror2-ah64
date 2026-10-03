// Compile the real capture helper against offline doubles. No game pixels are fabricated.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object {
        public string name = "preview";
        public static implicit operator bool(Object value) { return value != null; }
        public int GetInstanceID() { return 17; }
        public static void Destroy(Object value) { Fixture.destroyed++; }
    }
    public class RenderTexture : Object { public static RenderTexture active; public int width = 512, height = 512; }
    public static class Screen { public static int width = 2560, height = 1440; }
    public static class Time { public static int frameCount = 42; public static float realtimeSinceStartup = 1.25f; }
    public class Camera : Object { public static Camera[] allCameras = new Camera[0]; public RenderTexture targetTexture; }
    public enum TextureFormat { RGB24 }
    public struct Rect { public float x, y, width, height; public Rect(float left, float bottom, float w, float h) { x=left; y=bottom; width=w; height=h; } }
    public class Texture2D : Object {
        public int width, height;
        public Texture2D(int w, int h, TextureFormat format, bool mip) { width=w; height=h; Fixture.allocated++; }
        public void ReadPixels(Rect region, int x, int y, bool mip) {
            if (RenderTexture.active != null || region.width != Screen.width || region.height != Screen.height || width != Screen.width || height != Screen.height)
                throw new Exception("Incorrect capture source/dimensions");
            Fixture.reads++;
            if (Fixture.readThrows) throw new InvalidOperationException("read failure");
        }
        public void Apply(bool mip, bool unreadable) { }
    }
    public static class ImageConversion {
        public static byte[] EncodeToPNG(Texture2D texture) {
            if (RenderTexture.active != Fixture.prior) throw new Exception("Render state not restored before encoding");
            if (Fixture.encodeThrows) throw new InvalidOperationException("encode failure");
            return Fixture.empty ? new byte[0] : new byte[] { 1, 2 }; // Fixture bytes never saved as game imagery.
        }
    }
}
internal static class Fixture
{
    internal static int checks, allocated, destroyed, reads;
    internal static bool readThrows, encodeThrows, empty;
    internal static UnityEngine.RenderTexture prior;
    private static void Reset() {
        allocated=destroyed=reads=0; readThrows=encodeThrows=empty=false;
        UnityEngine.Screen.width=2560; UnityEngine.Screen.height=1440;
        prior=new UnityEngine.RenderTexture(); UnityEngine.RenderTexture.active=prior;
    }
    private static void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
    private static bool Reject(AH64.DevAutopilot runner) { try { runner.TestReadBackbuffer(); return false; } catch (InvalidOperationException) { return true; } }
    public static int Main() {
        try {
            Reset(); var runner = new AH64.DevAutopilot(); var bytes=runner.TestReadBackbuffer();
            Check(reads==1 && bytes.Length>0 && runner.events[0].Contains("width=2560; height=1440") && runner.events[0].Contains("512x512"), "mismatched preview target uses full backbuffer dimensions");
            Check(UnityEngine.RenderTexture.active==prior && destroyed==1, "success restores prior render target and destroys temporary texture");
            Check(runner.events[0].Contains("frame=42") && runner.events[1].Contains("restoredTarget=True"), "capture source/frame/restoration evidence");
            Reset(); readThrows=true; Check(Reject(runner) && UnityEngine.RenderTexture.active==prior && destroyed==1, "read exception restores target and destroys texture");
            Reset(); encodeThrows=true; Check(Reject(runner) && UnityEngine.RenderTexture.active==prior && destroyed==1, "encode exception restores target and destroys texture");
            Reset(); UnityEngine.Screen.width=0; Check(Reject(runner) && UnityEngine.RenderTexture.active==prior && allocated==0, "invalid dimensions fail before modifying render state");
            Reset(); empty=true; Check(Reject(runner) && UnityEngine.RenderTexture.active==prior && destroyed==1, "empty PNG rejected with cleanup");
            Reset(); prior=null; UnityEngine.RenderTexture.active=null; Check(runner.TestReadBackbuffer().Length>0 && UnityEngine.RenderTexture.active==null && destroyed==1, "existing backbuffer source preserved");
            Console.WriteLine("CAPTURE_CHECK_PASS checks=" + checks + " game=not-run images=not-produced"); return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
namespace AH64
{
    internal sealed partial class DevAutopilot {
        internal readonly List<string> events = new List<string>();
        private void Event(string name, string reason) { events.Add(name + ":" + reason); }
        internal byte[] TestReadBackbuffer() { return ReadBackbufferPng(); }
    }
}
