using System;
using System.Collections;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private NetworkUser startedNetworkUser;
        private BodyIndex expectedBody = BodyIndex.None;

        private void OnNetworkStarted(NetworkUser networkUser)
        {
            if (!networkUser.isLocalPlayer) return;
            startedNetworkUser = networkUser;
            Event("bootstrap-network-started", "completed NetworkUser.Start; preference=" + networkUser.bodyIndexPreference);
        }

        private IEnumerator Bootstrap()
        {
            yield return BootstrapWait(() => {
                string scene = SceneManager.GetActiveScene().name;
                if (gameLoaded && scene != "title" && scene != "splash") IntroCutsceneController.shouldSkip = true;
                return gameLoaded && scene == "title" && LocalUserManager.GetFirstLocalUser() != null;
            }, 90f, "title/local-user", null);
            var user = LocalUserManager.GetFirstLocalUser();
            if (LocalUserManager.readOnlyLocalUsersList.Count != 1 || user.currentNetworkUser || NetworkServer.active || NetworkClient.active)
                throw new InvalidOperationException("Bootstrap requires one offline local user before hosting: " + BootstrapSnapshot(user));
            var profile = user.userProfile;
            if (profile == null) throw new InvalidOperationException("No player profile for save protection.");
            // The mod-manager profile does not isolate Steam saves. Keep this protection
            // through shutdown, before changing the in-memory survivor preference.
            profile.canSave = false;
            profile.saveRequestPending = false;
            Event("save-protection", "loaded player profile canSave=false for this diagnostic process; no disk save changes");
            SurvivorDef survivor = null;
            yield return BootstrapWait(() => {
                expectedBody = BodyCatalog.FindBodyIndex("AH64Body");
                survivor = SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(expectedBody));
                return expectedBody != BodyIndex.None && survivor && survivor.bodyPrefab && BodyCatalog.FindBodyIndex(survivor.bodyPrefab) == expectedBody;
            }, 15f, "AH64-catalog", user);
            var prior = profile.GetSurvivorPreference();
            Event("bootstrap-profile", "prior=" + (prior ? prior.cachedName : "null") + "; selecting=" + survivor.cachedName + "; body=" + expectedBody);
            // Actual RoR2.NetworkUser.Start links the user THEN reads this profile.
            // Selecting after hosting is too late for a null or stale loaded preference.
            profile.SetSurvivorPreference(survivor);
            if (profile.canSave || profile.GetSurvivorPreference() != survivor)
                throw new InvalidOperationException("Protected in-memory AH64 selection was not retained.");
            RoR2.Console.instance.SubmitCmd((NetworkUser)null, "transition_command \"gamemode ClassicRun; host 0;\"", false);
            yield return BootstrapWait(() => PreGameController.instance && user.currentNetworkUser &&
                startedNetworkUser == user.currentNetworkUser && NetworkServer.active && NetworkClient.active &&
                NetworkUser.readOnlyInstancesList.Count == 1 && user.currentNetworkUser.isLocalPlayer && user.currentNetworkUser.hasAuthority,
                45f, "solo-lobby/NetworkUser.Start", user);
            user.currentNetworkUser.SetSurvivorPreferenceClient(survivor);
            yield return BootstrapWait(() => user.currentNetworkUser.bodyIndexPreference == expectedBody,
                10f, "AH64-selection-acknowledged", user);
            if (!PreGameController.instance) throw new InvalidOperationException("Lobby launched before explicit launch.");
            PreGameController.instance.runSeed = 1301UL;
            UnityEngine.Random.InitState(1301);
            Event("bootstrap-launch", BootstrapSnapshot(user));
            PreGameController.instance.StartLaunch();
            yield return BootstrapWait(() => Run.instance && user.cachedBody && BodyProblem(user).Length == 0,
                60f, "first-body/AH64-prerequisites", user);
            RequireBody(user);
            var arena = SceneCatalog.FindSceneDef("golemplains");
            if (!arena) throw new InvalidOperationException("Fixed arena missing.");
            if (SceneManager.GetActiveScene().name != "golemplains") {
                var previousBody = user.cachedBody;
                Run.instance.AdvanceStage(arena);
                // Scene load can precede LocalUser's control-chain rebuild. An old cached
                // body must never satisfy the new stage's readiness gate.
                yield return BootstrapWait(() => SceneManager.GetActiveScene().name == "golemplains" &&
                    user.cachedBody && !ReferenceEquals(user.cachedBody, previousBody) && BodyProblem(user).Length == 0,
                    60f, "fixed-arena/new-body", user);
            }
            yield return new WaitForSecondsRealtime(2f);
            RequireBody(user);
            pilot = user.cachedBody;
            Event("bootstrap-ready", BootstrapSnapshot(user));
        }

        private void RequireBody(LocalUser user)
        {
            string problem = BodyProblem(user);
            if (problem.Length != 0) throw new InvalidOperationException("AH64 prerequisite: " + problem + "; " + BootstrapSnapshot(user));
        }

        private string BodyProblem(LocalUser user)
        {
            var body = user.cachedBody;
            if (!body) return "missing body";
            if (body.bodyIndex != expectedBody) return "wrong body index";
            if (!body.hasAuthority) return "body lacks authority";
            if (!body.characterMotor) return "missing CharacterMotor";
            if (!body.inputBank) return "missing InputBankTest";
            if (!body.characterDirection) return "missing CharacterDirection";
            if (!body.modelLocator) return "missing ModelLocator";
            if (!body.modelLocator.modelTransform) return "missing model transform";
            if (!user.cachedMaster || !user.cachedMasterController || user.cachedMasterController.master != user.cachedMaster || user.cachedMaster.GetBody() != body)
                return "local master/controller/body control-chain mismatch";
            foreach (string name in new[] { "Body", "Weapon2" }) {
                var machines = body.GetComponents<EntityStateMachine>().Where(m => m.customName == name).ToArray();
                if (machines.Length != 1 || machines[0].state == null) return "missing/ambiguous/uninitialized state machine " + name;
            }
            return "";
        }

        private IEnumerator BootstrapWait(Func<bool> predicate, float timeout, string phase, LocalUser user)
        {
            segment = "bootstrap:" + phase;
            float end = Time.realtimeSinceStartup + timeout;
            string previous = null;
            while (true) {
                bool ready = predicate();
                string snapshot = BootstrapSnapshot(user);
                if (snapshot != previous) { Event("bootstrap-wait", phase + "; " + snapshot); previous = snapshot; }
                if (ready) { Event("bootstrap-phase-ready", phase + "; " + snapshot); yield break; }
                if (Time.realtimeSinceStartup > end) throw new TimeoutException("bootstrap " + phase + "; " + snapshot);
                yield return null;
            }
        }

        private string BootstrapSnapshot(LocalUser user)
        {
            var network = user == null ? null : user.currentNetworkUser;
            var body = user == null ? null : user.cachedBody;
            return "scene=" + SceneManager.GetActiveScene().name + "; expectedBody=" + expectedBody +
                "; localUsers=" + LocalUserManager.readOnlyLocalUsersList.Count + "; networkUsers=" + NetworkUser.readOnlyInstancesList.Count +
                "; server=" + NetworkServer.active + "; client=" + NetworkClient.active +
                "; networkStarted=" + (network && startedNetworkUser == network) +
                "; preference=" + (network ? network.bodyIndexPreference.ToString() : "none") +
                "; body=" + (body ? body.gameObject.name + "/" + body.bodyIndex : "none") +
                "; authority=" + (body && body.hasAuthority) +
                "; prerequisite=" + (user == null ? "no local user" : BodyProblem(user));
        }
    }
}
