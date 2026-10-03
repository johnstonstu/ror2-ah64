using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RoR2;
using UnityEngine;
using Path = System.IO.Path;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private readonly Dictionary<string, EntityStates.EntityState> previousStates = new Dictionary<string, EntityStates.EntityState>();
        private readonly HashSet<int> observedProjectiles = new HashSet<int>();
        private readonly HashSet<int> liveProjectiles = new HashSet<int>();
        private Quaternion previousAttitude;
        private bool hasAttitude;
        private int poseEpoch;
        private int terminalExit;
        private static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var file = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
        }

        private void OpenEvidence()
        {
            jsonl = NewWriter("telemetry.jsonl"); csv = NewWriter("telemetry.csv");
            trace = NewWriter("trace.txt"); runtimeLog = NewWriter("runtime.log");
            csv.WriteLine("tick,time,scenario,px,py,pz,vx,vy,vz,fx,fy,fz,ax,ay,az,qx,qy,qz,qw,authority,bodyState,weapon2State");
            var stage = JsonUtility.FromJson<StageIdentity>(File.ReadAllText(Path.Combine(output, "identity.json")));
            if (stage == null || stage.status != "staged" || stage.sourceSha.Length != 40) throw new InvalidDataException("Bad staged identity.");
            string plugin = Path.GetDirectoryName(typeof(AH64Plugin).Assembly.Location);
            var header = new Header {
                recordType = "header", runId = Path.GetFileName(output), scenario = Suite,
                sourceSha = stage.sourceSha, dirtyWorkspaceFingerprint = stage.dirtyWorkspaceFingerprint,
                pluginVersion = AH64Plugin.MODVERSION, profile = stage.profile, role = "solo-server-authority",
                requestedAssertionCount = ExpectedAssertions,
                dllSha256 = Hash(typeof(AH64Plugin).Assembly.Location),
                bundleSha256 = Hash(Path.Combine(plugin, "AssetBundles", "ah64")),
                bankSha256 = Hash(Path.Combine(plugin, "SoundBanks", "AH64Rotor.bnk")),
                configSha256 = Hash(Path.Combine(stage.profile, "BepInEx", "config", AH64Plugin.MODUID + ".cfg")),
                gameBuild = Hash(Path.Combine(Application.dataPath, "Managed", "RoR2.dll")),
                identityFile = "identity.json", seed = 1301
            };
            Write(header);
            trace.WriteLine("START suite=" + Suite + " sha=" + stage.sourceSha);
            runtimeLog.WriteLine("AH64 autopilot evidence opened; loading errors are counted.");
        }

        private StreamWriter NewWriter(string name)
        {
            return new StreamWriter(new FileStream(Path.Combine(output, name), FileMode.CreateNew, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
        }
        private void Write(object record) { jsonl.WriteLine(JsonUtility.ToJson(record)); }

        private void OnLog(string message, string stack, LogType type)
        {
            if (finished || runtimeLog == null) return;
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++;
            if (type == LogType.Warning) warnings++;
            runtimeLog.WriteLine(type + ": " + message + "\n" + stack);
        }

        private void FixedUpdate()
        {
            if (!scripting || finished || !pilot) return;
            try {
                tick++;
                CheckWindowCapturePhase();
                Vector3 position = pilot.gameObject.transform.position, velocity = pilot.characterMotor.velocity;
                Quaternion attitude = pilot.modelLocator.modelTransform.rotation;
                if (!Finite(position) || !Finite(velocity) || !Finite(new Vector3(attitude.x, attitude.y, attitude.z)) || float.IsNaN(attitude.w) || float.IsInfinity(attitude.w))
                    throw new InvalidOperationException("Nonfinite physics/attitude telemetry.");
                if (hasAttitude && Quaternion.Angle(previousAttitude, attitude) > 60f) {
                    visualFlags++; Event("visual-flag", "attitude delta >60deg/tick; UNTRIAGED (render pose sampled at physics cadence)");
                }
                previousAttitude = attitude; hasAttitude = true;
                Vector3 aim = pilot.inputBank.aimDirection;
                Vector3 forward = pilot.characterDirection.forward;
                if (!Finite(aim) || !Finite(forward)) throw new InvalidOperationException("Nonfinite native aim/facing.");
                ObserveProjectiles();
                var sample = new Sample {
                    recordType = "sample", runId = Path.GetFileName(output), scenario = segment, tick = tick,
                    simulationTime = Time.fixedTime, entityId = pilot.gameObject.GetInstanceID(),
                    ownerId = pilot.master ? pilot.master.gameObject.GetInstanceID() : 0, role = "solo-server-authority",
                    position = position, velocity = velocity, facing = forward, nativeAim = aim,
                    modelAttitude = attitude, authority = pilot.hasAuthority,
                    poseEpoch = poseEpoch,
                    maneuverCapture = ManeuverObservation(), guidanceToken = GuidanceObservation(),
                    attitudeSamplePhase = "FixedUpdate reads most recent LateUpdate model pose"
                };
                Write(sample); samples++;
                ObserveBrakingPhysics();
                var machines = pilot.GetComponents<EntityStateMachine>();
                foreach (var machine in machines) {
                    EntityStates.EntityState previous;
                    previousStates.TryGetValue(machine.customName, out previous);
                    if (previous != machine.state) {
                        if (previous != null) Event("state-exit", machine.customName + ":" + previous.GetType().FullName + "; reason=observed transition");
                        if (machine.state != null) Event("state-enter", machine.customName + ":" + machine.state.GetType().FullName + "; reason=observed transition");
                        previousStates[machine.customName] = machine.state;
                    }
                }
                string values = string.Join(",", new[] { position.x, position.y, position.z, velocity.x, velocity.y, velocity.z,
                    forward.x, forward.y, forward.z, aim.x, aim.y, aim.z, attitude.x, attitude.y, attitude.z, attitude.w }.Select(v => v.ToString("R", Invariant)));
                string body = machines.First(m => m.customName == "Body").state.GetType().Name;
                string weapon = machines.First(m => m.customName == "Weapon2").state.GetType().Name;
                csv.WriteLine(tick + "," + Time.fixedTime.ToString("R", Invariant) + "," + segment + "," + values + "," + pilot.hasAuthority + "," + body + "," + weapon);
            } catch (Exception e) { Finish("failed", "telemetry exception: " + e); }
        }

        private static bool Finite(Vector3 v)
        {
            return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
        }

        private void ObserveProjectiles()
        {
            var current = new HashSet<int>();
            foreach (var projectile in FindObjectsOfType<RoR2.Projectile.ProjectileController>()) {
                if (!projectile || projectile.owner != pilot.gameObject) continue;
                int id = projectile.gameObject.GetInstanceID(); current.Add(id);
                var guidance = projectile.GetComponent<Survivors.Components.AH64HellfireGuidance>();
                if (observedProjectiles.Add(id)) Event("projectile-spawn", "projectileId=" + id + "; prefab=" + projectile.gameObject.name + "; combo="+projectile.combo+"; token="+(guidance ? guidance.Token.ToString() : "not-guided"));
                if (prototypeStarted && guidance) Write(new ProjectileSample {
                    recordType="projectile-sample",runId=Path.GetFileName(output),scenario=segment,tick=tick,
                    projectileId=id,combo=projectile.combo,token=guidance.Token,position=projectile.gameObject.transform.position,
                    heading=projectile.gameObject.transform.forward,lastTurnDegrees=guidance.LastTurnDegrees,
                    remainingTurnDegrees=guidance.RemainingTurnDegrees
                });
            }
            foreach (int id in liveProjectiles) if (!current.Contains(id)) Event("projectile-despawn", "projectileId=" + id + "; reason=observed disappearance; collision/payload=unverified");
            liveProjectiles.Clear(); foreach (int id in current) liveProjectiles.Add(id);
        }

        private string ManeuverObservation()
        {
            var state=Machine("Body").state;
            var roll=state as Survivors.SkillStates.ServoDash;
            if(roll!=null) return JsonUtility.ToJson(roll.EntrySnapshot)+"; progress="+roll.ManeuverProgress.ToString("R",Invariant)+"; yielded="+roll.MotionYielded;
            var flip=state as Survivors.SkillStates.SmokeBackflip;
            if(flip!=null) return JsonUtility.ToJson(flip.EntrySnapshot)+"; progress="+flip.ManeuverProgress.ToString("R",Invariant)+"; yielded="+flip.MotionYielded;
            return "no-active-maneuver";
        }

        private string GuidanceObservation()
        {
            var owner=pilot.GetComponent<Survivors.Components.AH64HellfireOwner>();
            return owner ? "server-active-token="+owner.Policy.ActiveToken : "no-guidance-owner";
        }

        private void Event(string name, string reason)
        {
            Write(new EventRecord { recordType = "event", runId = Path.GetFileName(output), scenario = segment,
                tick = tick, simulationTime = Time.fixedTime, entityId = pilot ? pilot.gameObject.GetInstanceID() : 0,
                ownerId = pilot && pilot.master ? pilot.master.gameObject.GetInstanceID() : 0, role = "solo-server-authority", eventName = name, reason = reason });
            trace.WriteLine("EVENT " + tick + " " + segment + " " + name + " " + reason);
        }

        private void Assert(string id, bool passed, string expected, string actual, float tolerance)
        {
            var check = new Check { id = id, passed = passed, expected = expected, actual = actual, tolerance = tolerance, scenario = segment };
            checks.Add(check);
            Write(new AssertionRecord { recordType = "assertion", runId = Path.GetFileName(output), scenario = segment,
                assertionId = id, expected = expected, actual = actual, tolerance = tolerance, passed = passed });
            trace.WriteLine("ASSERT " + id + " " + (passed ? "PASS" : "FAIL") + " expected=" + expected + " actual=" + actual);
        }

        private void Finish(string status, string reason)
        {
            if (finished) return;
            finished = true; scripting = false; move = Vector3.zero;
            int failures = checks.Count(c => !c.passed);
            bool prototypePassed = FinishPrototypeEvidence();
            bool balancePassed = FinishBalanceEvidence();
            bool brakingPassed = FinishBrakingEvidence();
            if (!prototypePassed || !balancePassed || !brakingPassed) status = "failed";
            if (status == "completed" && (checks.Count != ExpectedAssertions || failures > 0 || errors > 0 || warnings > 0 || visualFlags > 0)) status = "failed";
            var summary = new Summary { recordType = "summary", runId = Path.GetFileName(output), scenario = Suite, suite = Suite,
                status = status, reason = reason, expectedAssertions = ExpectedAssertions, assertions = checks.Count,
                failedAssertions = failures, executed = checks.Count, passed = checks.Count - failures, failed = failures,
                skipped = Math.Max(0, ExpectedAssertions - checks.Count), captures = captures, samples = samples,
                errors = errors, warnings = warnings, visualFlags = visualFlags, checks = checks.ToArray(),
                artifactPaths = new[] { "telemetry.jsonl", "telemetry.csv", "trace.txt", "runtime.log", "identity.json", "LogOutput.log", "Player.log" },
                warningClassification = "all warnings untriaged", errorClassification = "all observed errors blocking",
                limitation = "SOLO state injection; input activation, hit/payload, audio, full revolution, physical controller and multiplayer unverified" };
            Write(summary);
            trace.WriteLine("TERMINAL " + status + " " + reason);
            runtimeLog.WriteLine("AH64 autopilot terminal " + status);
            jsonl.Dispose(); csv.Dispose(); trace.Dispose(); runtimeLog.Dispose();
            string temp = Path.Combine(output, "result.json.tmp");
            File.WriteAllText(temp, JsonUtility.ToJson(summary, true));
            File.Move(temp, Path.Combine(output, "result.json"));
            terminalExit = status == "completed" ? 0 : 1; quitAt = Time.realtimeSinceStartup + 2f;
        }

        [Serializable] private class StageIdentity { public string status = "", sourceSha = "", profile = "", dirtyWorkspaceFingerprint = ""; }
        [Serializable] private class Record { public int schemaVersion = 1; public string recordType, runId, scenario; }
        [Serializable] private class Header : Record {
            public string sourceSha, dirtyWorkspaceFingerprint, pluginVersion, dllSha256, bundleSha256, bankSha256, configSha256, gameBuild, profile, role, identityFile;
            public int requestedAssertionCount, seed;
        }
        [Serializable] private class Sample : Record {
            public int tick, entityId, ownerId, poseEpoch; public float simulationTime;
            public Vector3 position, velocity, facing, nativeAim; public Quaternion modelAttitude;
            public bool authority; public string role, maneuverCapture, guidanceToken, attitudeSamplePhase;
        }
        [Serializable] private class ProjectileSample : Record {
            public int tick,projectileId,combo; public uint token;
            public Vector3 position,heading; public float lastTurnDegrees,remainingTurnDegrees;
        }
        [Serializable] private class EventRecord : Record {
            public int tick, entityId, ownerId; public float simulationTime; public string role, eventName, reason;
        }
        [Serializable] private class AssertionRecord : Record {
            public string assertionId, expected, actual; public float tolerance; public bool passed;
        }
        [Serializable] private class Check { public string id, expected, actual, scenario; public float tolerance; public bool passed; }
        [Serializable] private class Summary : Record {
            public int schema = 1, expectedAssertions, assertions, failedAssertions, executed, passed, failed, skipped, captures, samples, errors, warnings, visualFlags;
            public string suite, status, reason, warningClassification, errorClassification, limitation; public Check[] checks; public string[] artifactPaths;
        }
    }
}
