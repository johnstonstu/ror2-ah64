using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace AH64
{
    internal sealed partial class DevAutopilot
    {
        private string windowMarker;
        private int markerFrame;
        private bool WindowComparisonEnabled { get { return Environment.GetEnvironmentVariable("AH64_AUTOPILOT_WINDOW_COMPARE") == "1"; } }

        private void BeginWindowComparison(string name)
        {
            if (!WindowComparisonEnabled) return;
            windowMarker = Path.GetFileName(output) + " / " + name;
            markerFrame = Time.frameCount;
        }

        private void OnGUI()
        {
            if (!WindowComparisonEnabled || windowMarker == null) return;
            GUI.Label(new Rect(12, 12, 1200, 36), "AH64 DIAGNOSTIC " + windowMarker + " / requested frame " + markerFrame);
        }

        private void WriteWindowComparisonRequest(string name)
        {
            if (!WindowComparisonEnabled) return;
            var request = new WindowComparisonRequest { runId=Path.GetFileName(output), name=name,
                marker=windowMarker, markerFrame=markerFrame, renderFrame=Time.frameCount,
                fixedTime=Time.fixedTime, realtime=Time.realtimeSinceStartup };
            File.WriteAllText(Path.Combine(output, "window-comparison-request.json"), JsonUtility.ToJson(request));
            Event("window-comparison-request", windowMarker + "; markerFrame=" + markerFrame + "; renderFrame=" + Time.frameCount);
        }

        [Serializable]
        private sealed class WindowComparisonRequest
        {
            public string runId, name, marker;
            public int markerFrame, renderFrame;
            public float fixedTime, realtime;
        }

        private static string DescribeCaptureCamera(Camera camera)
        {
            // Read only the installed public field; no private postprocessing state or mutation.
            var layer = camera.GetComponents<Component>().SingleOrDefault(component =>
                component.GetType().FullName == "UnityEngine.Rendering.PostProcessing.PostProcessLayer");
            string postprocess = "absent";
            if (layer) {
                var finalBlit = layer.GetType().GetField("finalBlitToCameraTarget", BindingFlags.Public | BindingFlags.Instance);
                var behaviour = layer as Behaviour;
                postprocess = "enabled=" + (behaviour && behaviour.enabled) + "; finalBlitToCameraTarget=" +
                    (finalBlit == null ? "public-field-unavailable" : finalBlit.GetValue(layer).ToString());
            }
            string buffers = string.Join("|", Enum.GetValues(typeof(CameraEvent)).Cast<CameraEvent>().SelectMany(stage =>
                camera.GetCommandBuffers(stage).Select(buffer => stage + ":" + buffer.name + ":bytes=" + buffer.sizeInBytes)));
            return "camera=" + camera.name + "; frame=" + Time.frameCount + "; position=" + camera.transform.position +
                "; rotation=" + camera.transform.rotation + "; enabled=" + camera.enabled + "; cullingMask=" + camera.cullingMask +
                "; FOV=" + camera.fieldOfView + "; near=" + camera.nearClipPlane + "; far=" + camera.farClipPlane +
                "; postprocess=" + postprocess + "; commandBuffers=" + buffers + "; cached-destinations=not-publicly-readable";
        }
    }
}
