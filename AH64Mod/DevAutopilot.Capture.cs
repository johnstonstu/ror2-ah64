using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private WindowCaptureRequest pendingCapture;
        private bool capturePhaseInvalid;
        private int capturePhaseChecks;

        // Observe frame and physics cadence; never pause or alter ability timing.
        private void CheckWindowCapturePhase()
        {
            if (pendingCapture == null) return;
            capturePhaseChecks++;
            if (!pilot || pilot.gameObject.GetInstanceID() != pendingCapture.bodyId || segment != pendingCapture.phase ||
                Machine("Body").state.GetType().FullName != pendingCapture.bodyState ||
                Machine("Weapon2").state.GetType().FullName != pendingCapture.weaponState ||
                (pendingCapture.requiresProjectile && OwnedProjectiles() == 0)) capturePhaseInvalid = true;
        }

        private IEnumerator Capture(string name)
        {
            if (Environment.GetEnvironmentVariable("AH64_AUTOPILOT_WINDOW_CAPTURE") != "1")
                throw new InvalidOperationException("Baseline requires its explicitly configured owned-window helper.");
            yield return new WaitForEndOfFrame();
            var stage = JsonUtility.FromJson<WindowCaptureStage>(File.ReadAllText(Path.Combine(output, "identity.json")));
            string directory = Path.Combine(output, "window-captures");
            Directory.CreateDirectory(directory);
            var request = new WindowCaptureRequest {
                runId=Path.GetFileName(output), token=Guid.NewGuid().ToString("N"), name=name,
                phase=segment, bodyId=pilot.gameObject.GetInstanceID(), bodyName=pilot.gameObject.name,
                bodyState=Machine("Body").state.GetType().FullName, weaponState=Machine("Weapon2").state.GetType().FullName,
                requiresProjectile=name=="hellfire-launched", pid=Process.GetCurrentProcess().Id,
                executable=Path.Combine(stage.gameDirectory, "Risk of Rain 2.exe"), sourceSha=stage.sourceSha,
                owner=stage.owner, reservation=stage.reservation, lockPath=stage.runtimeLockPath,
                requestFrame=Time.frameCount, requestFixedTime=Time.fixedTime, requestRealtime=Time.realtimeSinceStartup,
                requestedUtc=DateTime.UtcNow.ToString("o")
            };
            pendingCapture=request; capturePhaseInvalid=false; capturePhaseChecks=0;
            string requestPath=Path.Combine(directory, request.token+".request.json");
            string ackPath=Path.Combine(directory, request.token+".ack.json");
            try {
                CheckWindowCapturePhase();
                File.WriteAllText(requestPath+".tmp", JsonUtility.ToJson(request, true));
                File.Move(requestPath+".tmp", requestPath);
                Event("capture-request", name+"; token="+request.token+"; frame="+request.requestFrame+"; utc="+request.requestedUtc);
                float deadline=Time.realtimeSinceStartup+10f;
                while (!File.Exists(ackPath)) {
                    CheckWindowCapturePhase();
                    if (capturePhaseInvalid) throw new InvalidOperationException("Requested capture phase expired: "+name);
                    if (Time.realtimeSinceStartup>deadline) throw new TimeoutException("Owned-window acknowledgement: "+name);
                    yield return null;
                }
                CheckWindowCapturePhase();
                var ack=JsonUtility.FromJson<WindowCaptureAck>(File.ReadAllText(ackPath));
                string png=Path.Combine(output, name+".png");
                WindowCaptureContract.Validate(request, ack, capturePhaseInvalid, Hash(png), File.ReadAllBytes(png), DateTime.UtcNow);
                File.WriteAllText(Path.Combine(directory, request.token+".phase.json"), JsonUtility.ToJson(new WindowPhaseEvidence {
                    runId=request.runId, token=request.token, phaseValid=true, phaseChecks=capturePhaseChecks,
                    confirmedFrame=Time.frameCount, confirmedFixedTime=Time.fixedTime, confirmedUtc=DateTime.UtcNow.ToString("o"),
                    limitation="State/body/phase observed at frame and physics cadence through acknowledgement; request frame is not the captured rendered frame; simulation never paused"
                }, true));
                captures++;
                Event("capture", name+".png; token="+request.token+"; source=owned-game-window; interval="+ack.startedUtc+".."+ack.finishedUtc+
                    "; state/phase retained; no exact rendered-frame claim; controller/audio unverified");
            } finally { pendingCapture=null; }
        }

#pragma warning disable CS0649 // Populated by Unity JSON deserialization.
        [Serializable] private sealed class WindowCaptureStage { public string sourceSha, gameDirectory, owner, reservation, runtimeLockPath; }
#pragma warning restore CS0649
        [Serializable] private sealed class WindowPhaseEvidence {
            public string runId, token, confirmedUtc, limitation; public bool phaseValid;
            public int phaseChecks, confirmedFrame; public float confirmedFixedTime;
        }
    }
}
