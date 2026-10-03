using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using RoR2;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Path = System.IO.Path;

namespace AH64
{
    // The only integration hook is TryStart() after content initialization. No hooks, files,
    // input overrides or gameplay changes occur unless BOTH explicit environment gates match.
    [DefaultExecutionOrder(10000)]
    internal sealed partial class DevAutopilot : MonoBehaviour
    {
        internal const string Suite = "baseline-v1";
        internal const int ExpectedAssertions = 12;
        private string output, segment = "startup";
        private CharacterBody pilot;
        private Vector3 mark, facing = Vector3.forward, move;
        private float started, quitAt;
        private bool gameLoaded, scripting, finished;
        private int tick, samples, errors, warnings, visualFlags, launches, captures;
        private readonly List<Check> checks = new List<Check>();
        private StreamWriter jsonl, csv, trace, runtimeLog;

        internal static void TryStart()
        {
            string directory = Environment.GetEnvironmentVariable("AH64_AUTOPILOT_DIR");
            if (Environment.GetEnvironmentVariable("AH64_AUTOPILOT_MODE") != "solo-baseline-v1" || string.IsNullOrEmpty(directory)) return;
            if (!Path.IsPathRooted(directory) || !Directory.Exists(directory) || !File.Exists(Path.Combine(directory, "identity.json")))
                throw new InvalidOperationException("AH64 autopilot requires an existing absolute run directory and staged identity.");
            if (File.Exists(Path.Combine(directory, "telemetry.jsonl"))) throw new InvalidOperationException("AH64 autopilot refuses reused output.");
            var go = new GameObject("AH64DevAutopilot");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<DevAutopilot>();
            runner.output = directory;
            runner.started = Time.realtimeSinceStartup;
            runner.OpenEvidence();
            runner.InstallHooks();
            runner.StartCoroutine(runner.Guarded(runner.Script()));
        }

        private void InstallHooks()
        {
            RoR2Application.onLoad += OnLoaded;
            NetworkUser.onPostNetworkUserStart += OnNetworkStarted;
            Application.logMessageReceived += OnLog;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate += AfterInput;
            On.RoR2.PlayerCharacterMasterController.Update += AfterAim;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += OnProjectile;
        }

        private void OnDestroy()
        {
            RoR2Application.onLoad -= OnLoaded;
            NetworkUser.onPostNetworkUserStart -= OnNetworkStarted;
            Application.logMessageReceived -= OnLog;
            On.RoR2.PlayerCharacterMasterController.FixedUpdate -= AfterInput;
            On.RoR2.PlayerCharacterMasterController.Update -= AfterAim;
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo -= OnProjectile;
            if (output != null && !finished) Finish("incomplete", "runner destroyed");
        }

        private void OnLoaded() { gameLoaded = true; }

        private void Update()
        {
            CheckWindowCapturePhase();
            if (finished) { if (Time.realtimeSinceStartup >= quitAt) Application.Quit(terminalExit); return; }
            if (Time.realtimeSinceStartup - started > 240f) Finish("incomplete", "runtime deadline");
            if (NetworkUser.readOnlyInstancesList.Count > 1 || LocalUserManager.readOnlyLocalUsersList.Count > 1)
                Finish("failed", "solo guard: multiple users");
        }

        // Flatten nested enumerators so a coroutine exception produces a terminal failed record.
        private IEnumerator Guarded(IEnumerator root)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(root);
            while (stack.Count > 0 && !finished)
            {
                object yielded = null; bool next = false;
                try { next = stack.Peek().MoveNext(); if (next) yielded = stack.Peek().Current; }
                catch (Exception e) { Finish("failed", "script exception: " + e); }
                if (finished) yield break;
                if (!next) { stack.Pop(); continue; }
                if (yielded is IEnumerator nested) { stack.Push(nested); continue; }
                yield return yielded;
            }
            if (!finished) Finish("completed", "script ended");
        }

        private IEnumerator Until(Func<bool> predicate, float timeout, string reason)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while (!predicate())
            {
                if (Time.realtimeSinceStartup > end) throw new TimeoutException(reason);
                yield return null;
            }
        }

        private IEnumerator Script()
        {
            yield return Bootstrap();
            DisableCombatDirectors();
            pilot.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
            // Fixed world mark borrowed as terrain coordinates only; no humanoid poses/camera work.
            RaycastHit floor;
            if (!Physics.Raycast(new Vector3(-112.88f, -129.02f, -374.76f), Vector3.down, out floor, 30f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("Fixed mark has no terrain.");
            mark = floor.point + Vector3.up * 2f;
            float best = -1f;
            for (int i = 0; i < 24; i++) {
                var direction = Quaternion.Euler(0f, i * 15f, 0f) * Vector3.forward;
                RaycastHit wall;
                float clear = Physics.Raycast(mark + Vector3.up, direction, out wall, 50f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore) ? wall.distance : 50f;
                if (clear > best) { best = clear; facing = direction; }
            }
            if (best < 15f) throw new InvalidOperationException("Insufficient open travel at fixed mark.");
            scripting = true;
            Event("arena", "fixed seed=1301 mark=" + mark + " facing=" + facing);
            yield return MovementCase("roll", new Survivors.SkillStates.ServoDash(), facing);
            yield return MovementCase("backflip", new Survivors.SkillStates.SmokeBackflip(), -facing);
            yield return HellfireCase();
        }

        private void AfterInput(On.RoR2.PlayerCharacterMasterController.orig_FixedUpdate orig, PlayerCharacterMasterController self)
        {
            orig(self);
            if (!scripting || finished || !pilot || !self.master || self.master.GetBody() != pilot) return;
            var bank = pilot.inputBank;
            bank.moveVector = move; bank.aimDirection = facing;
            bank.skill1.down = bank.skill2.down = bank.skill3.down = bank.skill4.down = bank.jump.down = bank.sprint.down = false;
        }

        private void AfterAim(On.RoR2.PlayerCharacterMasterController.orig_Update orig, PlayerCharacterMasterController self)
        {
            orig(self);
            if (scripting && !finished && pilot && self.master && self.master.GetBody() == pilot) pilot.inputBank.aimDirection = facing;
        }
    }
}
