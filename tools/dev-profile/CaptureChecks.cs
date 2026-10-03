// Real capture helper with offline render/ownership doubles. No game images are produced.
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
    public class GameObject : Object { }
    public enum RenderTextureFormat { ARGB32 }
    public class RenderTexture : Object {
        public static RenderTexture active; public int width,height;
        public RenderTexture(int w,int h,int depth,RenderTextureFormat format) { width=w; height=h; }
        public bool Create() { return !Fixture.createFails; }
        public void Release() { Fixture.released++; }
    }
    public static class Time { public static int frameCount=42; public static float realtimeSinceStartup=1.25f; }
    public class Camera : Object {
        public static Camera[] allCameras; public RenderTexture targetTexture;
        public int pixelWidth=2560,pixelHeight=1440; public RoR2.SceneCamera scene;
        public T GetComponent<T>() { return (T)(object)scene; }
        public void Render() {
            if (targetTexture==null || targetTexture.width!=pixelWidth || targetTexture.height!=pixelHeight) throw new Exception("Wrong render dimensions");
            Fixture.rendered++; if (Fixture.renderThrows) throw new InvalidOperationException("render failure");
            if (Fixture.renderLogsError) Fixture.runner.TestLogError();
        }
    }
    public enum TextureFormat { RGB24 }
    public struct Color32 { public byte r,g,b; }
    public struct Rect { public float width,height; public Rect(float x,float y,float w,float h) { width=w; height=h; } }
    public class Texture2D : Object {
        public int width,height;
        public Texture2D(int w,int h,TextureFormat format,bool mip) { width=w; height=h; }
        public void ReadPixels(Rect region,int x,int y,bool mip) {
            if (RenderTexture.active==null || region.width!=RenderTexture.active.width || region.height!=RenderTexture.active.height) throw new Exception("Wrong active source/dimensions");
            Fixture.reads++; if (Fixture.readThrows) throw new InvalidOperationException("read failure");
        }
        public void Apply(bool mip,bool unreadable) { }
        public Color32[] GetPixels32() { return new[] {new Color32(),new Color32 {r=(byte)(Fixture.flat ? 0 : 1)}}; }
    }
    public static class ImageConversion {
        public static byte[] EncodeToPNG(Texture2D texture) {
            if (RenderTexture.active!=Fixture.prior || Fixture.camera.targetTexture!=Fixture.cameraPrior) throw new Exception("State not restored before encoding");
            if (Fixture.encodeThrows) throw new InvalidOperationException("encode failure");
            return Fixture.empty ? new byte[0] : new byte[] {1,2};
        }
    }
}
namespace RoR2
{
    public class LocalUser { }
    public static class LocalUserManager { public static LocalUser user; public static LocalUser GetFirstLocalUser() { return user; } }
    public class CharacterBody : UnityEngine.Object { public UnityEngine.GameObject gameObject=new UnityEngine.GameObject {name="AH64Body(Clone)"}; }
    public class CameraRigController : UnityEngine.Object { public LocalUser localUserViewer; public UnityEngine.GameObject target; public UnityEngine.Camera sceneCam; }
    public class SceneCamera : UnityEngine.Object { public CameraRigController cameraRigController; public UnityEngine.Camera camera; }
}
internal static class Fixture
{
    internal static int checks,destroyed,released,rendered,reads;
    internal static bool createFails,renderThrows,renderLogsError,readThrows,encodeThrows,empty,flat;
    internal static AH64.DevAutopilot runner;
    internal static UnityEngine.RenderTexture prior,cameraPrior;
    internal static UnityEngine.Camera camera;
    private static AH64.DevAutopilot Reset() {
        destroyed=released=rendered=reads=0; createFails=renderThrows=renderLogsError=readThrows=encodeThrows=empty=flat=false;
        prior=new UnityEngine.RenderTexture(512,512,0,UnityEngine.RenderTextureFormat.ARGB32); UnityEngine.RenderTexture.active=prior;
        cameraPrior=null; camera=new UnityEngine.Camera {targetTexture=cameraPrior};
        runner=new AH64.DevAutopilot(); var user=new RoR2.LocalUser(); RoR2.LocalUserManager.user=user;
        camera.scene=new RoR2.SceneCamera {camera=camera,cameraRigController=new RoR2.CameraRigController {target=runner.Body.gameObject,localUserViewer=user,sceneCam=camera}};
        UnityEngine.Camera.allCameras=new[] {camera}; return runner;
    }
    private static void Check(bool value,string reason) { if (!value) throw new Exception(reason); checks++; }
    private static bool Reject(AH64.DevAutopilot runner) { try { runner.TestReadCamera(); return false; } catch (InvalidOperationException) { return true; } }
    private static bool Clean() { return UnityEngine.RenderTexture.active==prior && camera.targetTexture==cameraPrior && released==1 && destroyed>=1; }
    public static int Main() {
        try {
            var runner=Reset(); Check(runner.TestReadCamera().Length>0 && rendered==1 && reads==1 && Clean(),"native camera renders despite mismatched prior target");
            Check(runner.events[0].Contains("width=2560; height=1440") && runner.events[0].Contains("512x512") && runner.events[0].Contains("frame=42"),"source dimensions and frame diagnostics");
            runner=Reset(); renderThrows=true; Check(Reject(runner) && Clean(),"render exception cleanup");
            runner=Reset(); renderLogsError=true; Check(Reject(runner) && Clean(),"native logged error rejects capture with cleanup");
            runner=Reset(); readThrows=true; Check(Reject(runner) && Clean(),"read exception cleanup");
            runner=Reset(); encodeThrows=true; Check(Reject(runner) && Clean(),"encode exception cleanup");
            runner=Reset(); createFails=true; Check(Reject(runner) && Clean() && rendered==0,"failed target creation cleanup");
            runner=Reset(); flat=true; Check(Reject(runner) && Clean(),"flat black image rejected");
            runner=Reset(); empty=true; Check(Reject(runner) && Clean(),"empty PNG rejected");
            runner=Reset(); camera.pixelWidth=0; Check(Reject(runner) && rendered==0 && destroyed==0 && UnityEngine.RenderTexture.active==prior,"invalid dimensions reject before state changes");
            runner=Reset(); camera.scene.cameraRigController.target=new UnityEngine.GameObject(); Check(Reject(runner) && rendered==0,"wrong target body rejected");
            runner=Reset(); camera.scene.cameraRigController.localUserViewer=new RoR2.LocalUser(); Check(Reject(runner) && rendered==0,"wrong local viewer rejected");
            runner=Reset(); UnityEngine.Camera.allCameras=new[] {camera,camera}; Check(Reject(runner) && rendered==0,"ambiguous gameplay camera rejected");
            Console.WriteLine("CAPTURE_CHECK_PASS checks="+checks+" game=not-run images=not-produced"); return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
namespace AH64
{
    internal sealed partial class DevAutopilot {
        private RoR2.CharacterBody pilot=new RoR2.CharacterBody(); private int errors;
        internal RoR2.CharacterBody Body {get {return pilot;}}
        internal readonly List<string> events=new List<string>();
        private void Event(string name,string reason) {events.Add(name+":"+reason);}
        internal byte[] TestReadCamera() {return ReadGameCameraPng();}
        internal void TestLogError() {errors++;}
    }
}
