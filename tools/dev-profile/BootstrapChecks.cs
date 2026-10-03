// Offline lifecycle doubles compile the real DevAutopilot.Bootstrap.cs. No game assembly
// is loaded or executed; the Release build and installed-game API scan are separate gates.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RoR2;

namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value) { return value != null; } }
    public class GameObject : Object { public string name = "AH64Body(Clone)"; }
    public class Transform : Object { }
    public static class Time { public static float realtimeSinceStartup; }
    public class WaitForSecondsRealtime { public float delay; public WaitForSecondsRealtime(float value) { delay = value; } }
    public static class Random { public static void InitState(int seed) { Fixture.randomSeed = seed; } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public static class SceneManager { public static string Name; public static Scene GetActiveScene() { return new Scene { name = Name }; } }
}
namespace UnityEngine.Networking
{
    public static class NetworkServer { public static bool active; }
    public static class NetworkClient { public static bool active; }
}
namespace RoR2
{
    public class CombatDirector : UnityEngine.Object
    {
        public static readonly List<CombatDirector> instancesList = new List<CombatDirector>();
        private bool active = true;
        public bool enabled {
            get { return active; }
            set { active = value; if (!value) instancesList.Remove(this); }
        }
    }
    public enum BodyIndex { None = -1, Commando, AH64 }
    public enum SurvivorIndex { None = -1, AH64 }
    public class SurvivorDef : UnityEngine.Object { public string cachedName = "AH64"; public UnityEngine.GameObject bodyPrefab = new UnityEngine.GameObject(); }
    public static class BodyCatalog
    {
        public static BodyIndex FindBodyIndex(string name) { return Fixture.catalogReady ? BodyIndex.AH64 : BodyIndex.None; }
        public static BodyIndex FindBodyIndex(UnityEngine.GameObject prefab) { return Fixture.catalogReady && prefab == Fixture.survivor.bodyPrefab ? BodyIndex.AH64 : BodyIndex.None; }
    }
    public static class SurvivorCatalog
    {
        public static SurvivorIndex GetSurvivorIndexFromBodyIndex(BodyIndex body) { return body == BodyIndex.AH64 ? SurvivorIndex.AH64 : SurvivorIndex.None; }
        public static SurvivorDef GetSurvivorDef(SurvivorIndex survivor) { return survivor == SurvivorIndex.AH64 ? Fixture.survivor : null; }
    }
    public class UserProfile
    {
        public bool canSave = true, saveRequestPending = true;
        public SurvivorDef preference;
        public SurvivorDef GetSurvivorPreference() { return preference; }
        public void SetSurvivorPreference(SurvivorDef value) { preference = value; }
    }
    public class LocalUser { public UserProfile userProfile = new UserProfile(); public NetworkUser currentNetworkUser; public CharacterBody cachedBody; public CharacterMaster cachedMaster; public PlayerCharacterMasterController cachedMasterController; }
    public static class LocalUserManager
    {
        public static List<LocalUser> readOnlyLocalUsersList = new List<LocalUser>();
        public static LocalUser GetFirstLocalUser() { return readOnlyLocalUsersList.FirstOrDefault(); }
    }
    public class NetworkUser : UnityEngine.Object
    {
        public bool isLocalPlayer = true, hasAuthority = true;
        public BodyIndex bodyIndexPreference = BodyIndex.None;
        public static List<NetworkUser> readOnlyInstancesList = new List<NetworkUser>();
        public static event Action<NetworkUser> onPostNetworkUserStart;
        public static void Started(NetworkUser user) { onPostNetworkUserStart?.Invoke(user); }
        public static void Clear() { onPostNetworkUserStart = null; readOnlyInstancesList.Clear(); }
        public void SetSurvivorPreferenceClient(SurvivorDef survivor) { if (!survivor) throw new ArgumentException("survivorDef"); if (Fixture.ack) bodyIndexPreference = BodyIndex.AH64; }
    }
    public static class IntroCutsceneController { public static bool shouldSkip; }
    public class Console
    {
        public static Console instance = new Console();
        public void SubmitCmd(NetworkUser user, string command, bool flag)
        {
            Fixture.hosted = true;
            var local = LocalUserManager.GetFirstLocalUser();
            if (local.userProfile.canSave || local.userProfile.saveRequestPending || local.userProfile.GetSurvivorPreference() != Fixture.survivor)
                throw new Exception("Host reached before protected profile selection");
            UnityEngine.Networking.NetworkServer.active = UnityEngine.Networking.NetworkClient.active = true;
            local.currentNetworkUser = new NetworkUser();
            NetworkUser.readOnlyInstancesList.Add(local.currentNetworkUser);
            PreGameController.instance = new PreGameController();
            // Reproduce installed NetworkUser.Start: link first, consume profile, then event.
            local.currentNetworkUser.SetSurvivorPreferenceClient(local.userProfile.GetSurvivorPreference());
            if (Fixture.startCompletes) NetworkUser.Started(local.currentNetworkUser);
        }
    }
    public class PreGameController : UnityEngine.Object
    {
        public static PreGameController instance; public ulong runSeed;
        public void StartLaunch()
        {
            if (runSeed != 1301UL || Fixture.randomSeed != 1301 || LocalUserManager.GetFirstLocalUser().currentNetworkUser.bodyIndexPreference != BodyIndex.AH64)
                throw new Exception("Launch before acknowledged deterministic selection");
            Fixture.launched = true; instance = null; Run.instance = new Run();
            UnityEngine.SceneManagement.SceneManager.Name = "other-stage";
            Fixture.Body(); Fixture.mutateBody?.Invoke(LocalUserManager.GetFirstLocalUser());
        }
    }
    public class SceneDef : UnityEngine.Object { }
    public static class SceneCatalog { public static SceneDef FindSceneDef(string name) { return new SceneDef(); } }
    public class Run : UnityEngine.Object
    {
        public static Run instance;
        public void AdvanceStage(SceneDef scene) { Fixture.advanced = true; UnityEngine.SceneManagement.SceneManager.Name = "golemplains"; if (Fixture.replaceBody) Fixture.Body(); Fixture.mutateStageBody?.Invoke(LocalUserManager.GetFirstLocalUser()); }
    }
    public class CharacterBody : UnityEngine.Object
    {
        public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); public BodyIndex bodyIndex = BodyIndex.AH64; public bool hasAuthority = true;
        public UnityEngine.Object characterMotor = new UnityEngine.Object(), inputBank = new UnityEngine.Object(), characterDirection = new UnityEngine.Object();
        public ModelLocator modelLocator = new ModelLocator(); public EntityStateMachine[] machines = { new EntityStateMachine("Body"), new EntityStateMachine("Weapon2") };
        public T[] GetComponents<T>() { return machines.Cast<T>().ToArray(); }
    }
    public class ModelLocator : UnityEngine.Object { public UnityEngine.Transform modelTransform = new UnityEngine.Transform(); }
    public class CharacterMaster : UnityEngine.Object { public CharacterBody body; public CharacterBody GetBody() { return body; } }
    public class PlayerCharacterMasterController : UnityEngine.Object { public CharacterMaster master; }
}
public class EntityStateMachine : UnityEngine.Object { public string customName; public object state = new object(); public EntityStateMachine(string name) { customName = name; } }

internal static class Fixture
{
    internal static bool catalogReady, hosted, launched, advanced, startCompletes, ack, replaceBody;
    internal static int randomSeed, checks;
    internal static SurvivorDef survivor;
    internal static Action<LocalUser> mutateBody, mutateStageBody;
    internal static void Reset()
    {
        catalogReady = startCompletes = ack = replaceBody = true; hosted = launched = advanced = false; randomSeed = 0;
        mutateBody = mutateStageBody = null; survivor = new SurvivorDef(); UnityEngine.Time.realtimeSinceStartup = 0;
        UnityEngine.SceneManagement.SceneManager.Name = "title";
        UnityEngine.Networking.NetworkServer.active = UnityEngine.Networking.NetworkClient.active = false;
        NetworkUser.Clear(); LocalUserManager.readOnlyLocalUsersList.Clear(); LocalUserManager.readOnlyLocalUsersList.Add(new LocalUser());
        Run.instance = null; PreGameController.instance = null;
    }
    internal static void Body()
    {
        var local = LocalUserManager.GetFirstLocalUser(); local.cachedBody = new CharacterBody();
        local.cachedMaster = new CharacterMaster { body = local.cachedBody };
        local.cachedMasterController = new PlayerCharacterMasterController { master = local.cachedMaster };
    }
    internal static void Check(bool value, string name) { if (!value) throw new Exception("BOOTSTRAP_CHECK_FAIL: " + name); checks++; }
    internal static Exception Drive(AH64.DevAutopilot runner, Action tick = null)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(runner.TestBootstrap());
        try {
            for (int step = 0; step < 600; step++) {
                if (!stack.Peek().MoveNext()) { stack.Pop(); if (stack.Count == 0) return null; continue; }
                var nested = stack.Peek().Current as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                var wait = stack.Peek().Current as UnityEngine.WaitForSecondsRealtime;
                UnityEngine.Time.realtimeSinceStartup += wait == null ? 0.5f : wait.delay;
                tick?.Invoke();
            }
            return new Exception("Offline fixture exceeded its bound");
        } catch (Exception error) { return error; }
    }
    internal static void Reject(Action<LocalUser> mutate, string reason)
    {
        Reset(); mutateBody = mutate; var runner = new AH64.DevAutopilot(); var error = Drive(runner);
        Check(error != null && error.Message.Contains(reason) && !runner.Ready, reason);
    }
    public static int Main()
    {
        try {
            CombatDirector.instancesList.Clear();
            var firstDirector = new CombatDirector(); var secondDirector = new CombatDirector();
            CombatDirector.instancesList.Add(firstDirector); CombatDirector.instancesList.Add(secondDirector);
            AH64.DevAutopilot.TestDisableDirectors();
            Check(!firstDirector.enabled && !secondDirector.enabled && CombatDirector.instancesList.Count == 0,
                "director snapshot disables all entries despite synchronous OnDisable removal");
            Reset(); var runner = new AH64.DevAutopilot(); Check(Drive(runner) == null && runner.Ready, "null profile preference replaced before NetworkUser.Start");
            Check(!LocalUserManager.GetFirstLocalUser().userProfile.canSave && !LocalUserManager.GetFirstLocalUser().userProfile.saveRequestPending, "save protection retained through bootstrap");
            Check(runner.events.Any(e => e.Contains("prior=null")) && runner.events.Any(e => e.Contains("bootstrap-ready")), "selection and readiness evidence");
            Reset(); LocalUserManager.GetFirstLocalUser().userProfile.preference = new SurvivorDef { cachedName = "RemovedMod" };
            runner = new AH64.DevAutopilot(); Check(Drive(runner) == null && runner.events.Any(e => e.Contains("prior=RemovedMod")), "stale profile preference replaced");
            Reset(); catalogReady = false; runner = new AH64.DevAutopilot();
            Check(Drive(runner, () => { if (UnityEngine.Time.realtimeSinceStartup >= 2) catalogReady = true; }) == null && runner.Ready, "delayed catalog waits before host");
            Reset(); catalogReady = false; runner = new AH64.DevAutopilot(); var failure = Drive(runner);
            Check(failure is TimeoutException && failure.Message.Contains("AH64-catalog") && !hosted, "missing catalog rejects before host");
            Reset(); startCompletes = false; runner = new AH64.DevAutopilot(); failure = Drive(runner);
            Check(failure is TimeoutException && failure.Message.Contains("networkStarted=False") && !launched, "linked but incomplete NetworkUser.Start cannot launch");
            Reset(); startCompletes = false; runner = new AH64.DevAutopilot(); bool seenWait = false;
            Check(Drive(runner, () => { if (hosted && !launched) { seenWait = true; NetworkUser.Started(LocalUserManager.GetFirstLocalUser().currentNetworkUser); } }) == null && seenWait, "post-start event permits launch");
            Reset(); ack = false; runner = new AH64.DevAutopilot(); failure = Drive(runner);
            Check(failure is TimeoutException && failure.Message.Contains("selection-acknowledged") && !launched, "unacknowledged selection cannot launch");
            Reset(); ack = false; runner = new AH64.DevAutopilot();
            Check(Drive(runner, () => { if (hosted && !launched && UnityEngine.Time.realtimeSinceStartup >= 2) LocalUserManager.GetFirstLocalUser().currentNetworkUser.bodyIndexPreference = BodyIndex.AH64; }) == null, "delayed selection acknowledgment waits");
            Reject(u => { u.cachedBody.bodyIndex = BodyIndex.Commando; u.cachedBody.gameObject.name = "CommandoBody(Clone)"; }, "wrong body index");
            Reject(u => u.cachedBody.hasAuthority = false, "body lacks authority");
            Reject(u => u.cachedBody.characterMotor = null, "missing CharacterMotor");
            Reject(u => u.cachedBody.inputBank = null, "missing InputBankTest");
            Reject(u => u.cachedBody.characterDirection = null, "missing CharacterDirection");
            Reject(u => u.cachedBody.modelLocator = null, "missing ModelLocator");
            Reject(u => u.cachedBody.modelLocator.modelTransform = null, "missing model transform");
            Reject(u => u.cachedMasterController.master = new CharacterMaster(), "control-chain mismatch");
            Reject(u => u.cachedBody.machines[1].state = null, "state machine Weapon2");
            Reset(); replaceBody = false; runner = new AH64.DevAutopilot(); failure = Drive(runner);
            Check(failure is TimeoutException && failure.Message.Contains("fixed-arena/new-body") && !runner.Ready, "old body cannot satisfy new-stage gate");
            Reset(); replaceBody = false; runner = new AH64.DevAutopilot(); bool replaced = false;
            Check(Drive(runner, () => { if (advanced && !replaced) { Body(); replaced = true; } }) == null && runner.Ready, "delayed control-chain rebuild accepts fresh body");
            Reset(); LocalUserManager.GetFirstLocalUser().userProfile = null; runner = new AH64.DevAutopilot(); failure = Drive(runner);
            Check(failure is InvalidOperationException && failure.Message.Contains("save protection") && !hosted, "missing profile rejects before host");
            Reset(); LocalUserManager.readOnlyLocalUsersList.Add(new LocalUser()); runner = new AH64.DevAutopilot(); failure = Drive(runner);
            Check(failure is InvalidOperationException && failure.Message.Contains("one offline local user") && !hosted, "multiple local users reject before host");
            Reset(); startCompletes = false; runner = new AH64.DevAutopilot(); failure = Drive(runner, () => {
                if (hosted && !launched) NetworkUser.Started(new NetworkUser { isLocalPlayer = false });
            });
            Check(failure is TimeoutException && !launched, "remote post-start event cannot satisfy solo local-user gate");
            Reset(); mutateBody = u => u.cachedBody.characterMotor = null; runner = new AH64.DevAutopilot(); bool motorAdded = false;
            Check(Drive(runner, () => { if (launched && !advanced && !motorAdded) { LocalUserManager.GetFirstLocalUser().cachedBody.characterMotor = new UnityEngine.Object(); motorAdded = true; } }) == null && runner.Ready,
                "transient body initialization waits without weakening prerequisites");
            Reset(); mutateStageBody = u => u.cachedBody.characterMotor = null; runner = new AH64.DevAutopilot();
            Check(Drive(runner, () => { if (advanced && UnityEngine.Time.realtimeSinceStartup >= 4) LocalUserManager.GetFirstLocalUser().cachedBody.characterMotor = new UnityEngine.Object(); }) == null && runner.Ready,
                "fresh-stage body also waits for complete prerequisites");
            System.Console.WriteLine("BOOTSTRAP_CHECK_PASS checks=" + checks + " game=not-run profiles=untouched"); return 0;
        } catch (Exception error) { System.Console.Error.WriteLine(error); return 1; }
    }
}
namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private bool gameLoaded = true; private string segment; private CharacterBody pilot;
        internal readonly List<string> events = new List<string>();
        internal DevAutopilot() { NetworkUser.onPostNetworkUserStart += OnNetworkStarted; }
        private void Event(string name, string detail) { events.Add(name + ":" + detail); }
        internal IEnumerator TestBootstrap() { return Bootstrap(); }
        internal static void TestDisableDirectors() { DisableCombatDirectors(); }
        internal bool Ready { get { return pilot != null; } }
    }
}
