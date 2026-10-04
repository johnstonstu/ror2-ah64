using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Security.Cryptography;
using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using System.Security;
using System.Security.Permissions;
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ReadmeCapture
{
    [BepInPlugin("com.JohnstonStu.AH64.ReadmeCapture", "AH64 optional README capture", "1.0.0")]
    [BepInDependency("com.JohnstonStu.AH64")]
    public sealed partial class CapturePlugin : BaseUnityPlugin, ICameraStateProvider
    {
        private const string FrozenHash = "DEB8659CB3D1DAE028B8525D4E6C3EADA5C88DE5DD9FA57E1AA98846BD02D266";
        private CharacterBody pilot;
        private string output, segment = "startup";
        private bool gameLoaded, scripting, finished, held, primaryHeld, collective;
        private float started, quitAt;
        private Vector3 mark, facing = Vector3.forward, move, aim, cameraOffset;
        private Vector3 cameraFocus;
        private bool fixedCamera;
        private int hits, sequence, exitCode;
        private readonly HashSet<int> bombsSeen = new HashSet<int>();
        private StreamWriter markers;
        private void Awake()
        {
            output = Environment.GetEnvironmentVariable("AH64_README_CAPTURE_DIR");
            if (Environment.GetEnvironmentVariable("AH64_README_CAPTURE_MODE") != "showcase-v1" || string.IsNullOrEmpty(output)) { enabled=false; return; }
            if (!Path.IsPathRooted(output) || !Directory.Exists(output) || File.Exists(Path.Combine(output,"markers.jsonl"))) throw new InvalidOperationException("Fresh absolute existing capture directory required");
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AH64_AUTOPILOT_MODE"))) throw new InvalidOperationException("Old autopilot must be disabled");
            var assembly = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AH64");
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(assembly.Location))
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") != FrozenHash) throw new InvalidOperationException("AH64 frozen DLL mismatch");
            started = Time.realtimeSinceStartup;
            markers = new StreamWriter(Path.Combine(output,"markers.jsonl")) { AutoFlush = true };
            RoR2Application.onLoad += Loaded;
            NetworkUser.onPostNetworkUserStart += OnNetworkStarted;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += AfterInput;
            On.RoR2.PlayerCharacterMasterController.Update += AfterAim;
            GlobalEventManager.onServerDamageDealt += Damage;
            StartCoroutine(Guarded(Script()));
        }
        private void Loaded() { gameLoaded=true; }
        private void Update()
        {
            if (markers == null) return;
            if (finished) { if (Time.realtimeSinceStartup >= quitAt) Application.Quit(exitCode); return; }
            if (Time.realtimeSinceStartup-started >= 238f) Finish(false,"240 second budget");
            if (LocalUserManager.readOnlyLocalUsersList.Count>1 || NetworkUser.readOnlyInstancesList.Count>1) Finish(false,"solo guard");
            if (scripting && pilot && segment == "path-bombing") foreach(var projectile in FindObjectsOfType<RoR2.Projectile.ProjectileController>())
                if(projectile.owner == pilot.gameObject && projectile.GetComponents<MonoBehaviour>().Any(c=>c && c.GetType().Name=="AH64BombingRunProjectile") && bombsSeen.Add(projectile.GetInstanceID())) Event("bomb-observed","instance="+projectile.GetInstanceID()+" ordinal="+bombsSeen.Count);
            if (scripting && pilot) foreach (var rig in CameraRigController.readOnlyInstancesList)
                if (rig.targetBody == pilot && !rig.IsOverrideCam(this)) rig.SetOverrideCam(this,0f);
        }
        private IEnumerator Guarded(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>(); stack.Push(root);
            while(stack.Count>0 && !finished) {
                object current=null; bool next=false;
                try { next=stack.Peek().MoveNext(); if(next) current=stack.Peek().Current; }
                catch(Exception e) { Logger.LogError(e); Finish(false,e.ToString()); }
                if(finished) yield break;
                if(!next) { stack.Pop(); continue; }
                var child=current as IEnumerator; if(child!=null) { stack.Push(child); continue; }
                yield return current;
            }
            if(!finished) Finish(true,"showcase finished; manual visual review required");
        }
        private void Finish(bool success,string detail)
        {
            if(finished) return;
            if(clipOpen) Clip(false);
            Event(success?"complete":"failed",detail); finished=true; scripting=false; exitCode=success?0:1;
            File.WriteAllText(Path.Combine(output,"capture-result.json"),JsonUtility.ToJson(new Result { success=success,detail=detail,dllSha256=FrozenHash,hits=hits,utc=DateTime.UtcNow.ToString("o") },true));
            foreach(var rig in CameraRigController.readOnlyInstancesList) if(rig.IsOverrideCam(this)) rig.SetOverrideCam(null,0f);
            // Profile protection deliberately persists through orderly process shutdown.
            quitAt=Time.realtimeSinceStartup+1f;
        }
        private void AfterInput(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig,PlayerCharacterMasterController self)
        { orig(self); if(scripting && !finished && pilot && self.master && self.master.GetBody()==pilot) Input(); }
        private void AfterAim(On.RoR2.PlayerCharacterMasterController.orig_Update orig,PlayerCharacterMasterController self)
        { orig(self); if(scripting && !finished && pilot && self.master && self.master.GetBody()==pilot) pilot.inputBank.aimDirection=aim; }
        private void Input()
        {
            var b=pilot.inputBank; b.moveVector=move; b.aimDirection=aim;
            b.skill1.down=primaryHeld; b.skill2.down=false; b.skill3.down=false; b.skill4.down=held; b.jump.down=collective; b.sprint.down=false;
        }
        private void Damage(DamageReport report)
        { if(pilot && report.attackerBody==pilot && targets.Contains(report.victimBody)) { hits++; Event("hit","target="+report.victimBody.name+" damage="+report.damageDealt); } }
        public void GetCameraState(CameraRigController rig,ref CameraState state)
        {
            if(!pilot) return;
            Vector3 focus=fixedCamera?cameraFocus:pilot.corePosition;
            state.position=focus+cameraOffset;
            state.rotation=Quaternion.LookRotation((fixedCamera?cameraFocus:pilot.corePosition+facing*9f)-state.position);
            state.fov=55f;
        }
        public bool IsUserLookAllowed(CameraRigController rig) { return false; }
        public bool IsUserControlAllowed(CameraRigController rig) { return true; }
        public bool IsHudAllowed(CameraRigController rig) { return true; }
        private bool clipOpen;
        private void Clip(bool start)
        {
            clipOpen=start; var record=Event(start?"clip-start":"clip-end","owned-window recording boundary");
            string path=Path.Combine(output,segment+(start?".start.json":".end.json"));
            File.WriteAllText(path+".tmp",JsonUtility.ToJson(record,true)); File.Move(path+".tmp",path);
        }
        private Marker Event(string kind,string detail)
        {
            var record=new Marker { kind=kind,scene=segment,detail=detail,utc=DateTime.UtcNow.ToString("o"),realtime=Time.realtimeSinceStartup,frame=Time.frameCount,pid=Process.GetCurrentProcess().Id,sequence=sequence++ };
            if(markers!=null) markers.WriteLine(JsonUtility.ToJson(record)); return record;
        }
        private void OnDestroy()
        {
            RoR2Application.onLoad-=Loaded; NetworkUser.onPostNetworkUserStart-=OnNetworkStarted;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate-=AfterInput; On.RoR2.PlayerCharacterMasterController.Update-=AfterAim;
            GlobalEventManager.onServerDamageDealt-=Damage; if(markers!=null) markers.Dispose();
        }
        [Serializable] private sealed class Marker { public string kind,scene,detail,utc; public float realtime; public int frame,pid,sequence; }
        [Serializable] private sealed class Result { public bool success; public string detail,dllSha256,utc; public int hits; }
    }
}
