using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    internal static class AH64AudioDiagnostics
    {
        private static Dictionary<uint, string> eventNames;

        //Wwise has no global "what is playing" query, so walk every emitter in the scene. This
        //finds loops owned by enemies, interactables or the stage as well as the aircraft's own.
        [ConCommand(commandName = "ah64_audio_scan", flags = ConVarFlags.None,
            helpText = "List every Wwise emitter in the scene that is playing, with event names and distance.")]
        private static void AudioScan(ConCommandArgs args)
        {
            Vector3 listener = ListenerPosition();
            var lines = new List<KeyValuePair<float, string>>();
            uint[] ids = new uint[32];
            foreach (AkGameObj source in Object.FindObjectsOfType<AkGameObj>())
            {
                uint count = (uint)ids.Length;
                AKRESULT result = AkSoundEngine.GetPlayingIDsFromGameObject(
                    AkSoundEngine.GetAkGameObjectID(source.gameObject), ref count, ids);
                if (result != AKRESULT.AK_Success || count == 0) continue;
                float distance = Vector3.Distance(listener, source.transform.position);
                for (int i = 0; i < count && i < ids.Length; i++)
                {
                    uint eventId = AkSoundEngine.GetEventIDFromPlayingID(ids[i]);
                    lines.Add(new KeyValuePair<float, string>(distance,
                        $"AH-64 scan: {distance,6:F1}m  {EventName(eventId)}  on {Path(source.transform)}"));
                }
            }
            lines.Sort((a, b) => a.Key.CompareTo(b.Key));
            Debug.Log($"AH-64 scan: {lines.Count} playing event(s), nearest first.");
            foreach (var line in lines) Debug.Log(line.Value);
        }

        private static Vector3 ListenerPosition()
        {
            foreach (AkAudioListener listener in AkAudioListener.DefaultListeners.ListenerList)
                if (listener && listener.isActiveAndEnabled) return listener.transform.position;
            return Camera.main ? Camera.main.transform.position : Vector3.zero;
        }

        private static string EventName(uint id)
        {
            if (eventNames == null) LoadEventNames();
            return eventNames.TryGetValue(id, out string name) ? name : $"event {id}";
        }

        private static void LoadEventNames()
        {
            var names = new Dictionary<uint, string>();
            foreach (string own in new[] { "Play_AH64_Rotor", "Stop_AH64_Rotor" })
                names[AkSoundEngine.GetIDFromString(own)] = own;
            string info = System.IO.Path.Combine(Application.streamingAssetsPath,
                "Audio", "GeneratedSoundBanks", "Windows", "SoundbanksInfo.xml");
            try
            {
                if (File.Exists(info))
                    foreach (Match match in Regex.Matches(File.ReadAllText(info), "<Event Id=\"(\\d+)\" Name=\"([^\"]+)\""))
                        if (uint.TryParse(match.Groups[1].Value, out uint id))
                            names[id] = match.Groups[2].Value;
            }
            catch (System.Exception exception)
            {
                Log.Warning($"ah64_audio_scan could not read vanilla event names: {exception.Message}");
            }
            eventNames = names;
        }

        private static string Path(Transform transform)
        {
            string path = transform.name;
            for (Transform parent = transform.parent; parent && path.Length < 120; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }
    }
}
