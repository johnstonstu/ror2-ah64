using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    // RoR2 disables Unity audio. Use the game's Wwise SFX bus for volume and pause.
    public class AH64FlightAudio : MonoBehaviour
    {
        private InputBankTest inputBank;
        private CharacterMotor motor;
        private CharacterBody body;
        private AH64HoverController hoverController;
        private GameObject emitter;
        private uint playingId;
        private int startAttempts;
        private float retryAt;
        private float load;
        private float loadVelocity;
        private float gain;
        private float pitch;
        private float directionalLoad;
        private float directionalVelocity;
        private bool mixErrorLogged;
        private float diagnosticAt;
        private int diagnosticCount;
        private int previousPosition = -1;

        private void Start()
        {
            inputBank = GetComponent<InputBankTest>();
            motor = GetComponent<CharacterMotor>();
            body = GetComponent<CharacterBody>();
            hoverController = GetComponent<AH64HoverController>();
        }

        private void OnEnable()
        {
            load = loadVelocity = 0f;
            startAttempts = 0;
            retryAt = 0f;
            directionalLoad = directionalVelocity = 0f;
        }

        private void Update()
        {
            if (Application.isBatchMode) return;
            if (body && body.healthComponent && !body.healthComponent.alive)
            {
                StopRotor();
                return;
            }
            if (PauseManager.isPaused || !AH64RotorBank.EnsureLoaded()) return;
            if (!emitter)
            {
                emitter = new GameObject("AH64RotorWwise");
                emitter.transform.SetParent(transform, false);
                emitter.AddComponent<AkGameObj>();
            }
            UpdateMix();
            EnsurePlaying();
            CheckPlayback();
            //Collective feedback now comes from this rotor's pitch response. The previous
            //Captain drone quick-move one-shot layered another engine sound over every press.
        }

        private void UpdateMix()
        {
            float speed = 0f;
            if (motor && body && body.moveSpeed > 0.01f)
            {
                Vector3 velocity = motor.velocity;
                velocity.y = 0f;
                speed = Mathf.Clamp01(velocity.magnitude / body.moveSpeed);
            }
            float climb = hoverController ? Mathf.Clamp01(hoverController.AscentPitchWeight) : 0f;
            load = Mathf.SmoothDamp(load, Mathf.Max(speed, climb), ref loadVelocity,
                AH64PlaytestConfig.RotorResponse, Mathf.Infinity, Time.deltaTime);
            directionalLoad = Mathf.SmoothDamp(directionalLoad, GetDirectionalLoad(),
                ref directionalVelocity, AH64PlaytestConfig.RotorResponse,
                Mathf.Infinity, Time.deltaTime);
            float pitchDepth = Mathf.Min(AH64PlaytestConfig.RotorLoadPitch
                * AH64StaticValues.rotorDirectionalPitchScale, 0.12f);
            //Stay within the authored Wwise pitch RTPC range (-700..600 cents).
            pitch = Mathf.Clamp(AH64PlaytestConfig.RotorPitch + pitchDepth * directionalLoad,
                Mathf.Pow(2f, -700f / 1200f), Mathf.Pow(2f, 600f / 1200f));
            bool local = IsLocalPilot();
            gain = Mathf.Clamp01(AH64PlaytestConfig.RotorHoverVolume
                * AH64StaticValues.rotorMixTrim
                * Mathf.Pow(10f, AH64PlaytestConfig.RotorLoadGain * load / 20f));
            if (!local) gain *= DistanceGain();
            CheckResult(AkSoundEngine.SetGameObjectOutputBusVolume(
                AkSoundEngine.GetAkGameObjectID(emitter), ulong.MaxValue, gain), "gain");
            CheckResult(AkSoundEngine.SetRTPCValue("AH64_RotorPitch", 1200f * Mathf.Log(pitch, 2f), emitter), "pitch");
            CheckResult(AkSoundEngine.SetRTPCValue("AH64_RotorSpatial", local ? 0f : 100f, emitter), "spatial mix");
            // Wwise's lowpass scale is perceptual, not a frequency in Hz. Preserve the saved
            // slider as an approximate tonal target; 20 kHz means no extra filtering.
            float lowpass = Mathf.Clamp(Mathf.Log(20000f / AH64PlaytestConfig.RotorToneCutoff)
                / Mathf.Log(20000f / 600f) * 60f, 0f, 60f);
            CheckResult(AkSoundEngine.SetRTPCValue("AH64_RotorLowpass", lowpass, emitter), "tone");
        }

        private float GetDirectionalLoad()
        {
            if (!motor || !body || body.moveSpeed <= 0.01f) return 0f;
            Vector3 forward = inputBank ? inputBank.aimDirection : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                CharacterDirection direction = GetComponent<CharacterDirection>();
                forward = direction ? direction.forward : transform.forward;
                forward.y = 0f;
            }
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float forwardSpeed = Mathf.Clamp(Vector3.Dot(motor.velocity, forward) / body.moveSpeed, -1f, 1f);
            float strafeSpeed = Mathf.Clamp(Vector3.Dot(motor.velocity, right) / body.moveSpeed, -1f, 1f);
            float verticalSpeed = Mathf.Clamp(motor.velocity.y / AH64StaticValues.collectiveClimbRate, -1f, 1f);
            //Anticipate collective input before velocity builds. Descent remains a lower pitch,
            //not another positive speed boost; opposite strafe directions carry equal rotor load.
            float collective = inputBank && inputBank.jump.down ? 1f
                : inputBank && inputBank.rawMoveDown.down ? -1f : 0f;
            float climb = Mathf.Clamp(verticalSpeed * 0.65f + collective * 0.35f, -1f, 1f);
            return Mathf.Clamp(forwardSpeed * (forwardSpeed >= 0f ? 1f : 0.65f)
                + Mathf.Abs(strafeSpeed) * 0.45f + climb * 0.8f, -1f, 1f);
        }

        private float DistanceGain()
        {
            float nearest = float.PositiveInfinity;
            foreach (AkAudioListener listener in AkAudioListener.DefaultListeners.ListenerList)
                if (listener && listener.isActiveAndEnabled)
                    nearest = Mathf.Min(nearest, Vector3.Distance(transform.position, listener.transform.position));
            // Native attenuation is disabled in the bank to avoid applying distance twice.
            float fade = 1f - Mathf.InverseLerp(AH64StaticValues.rotorHoverMinDistance,
                AH64StaticValues.rotorHoverMaxDistance, nearest);
            return fade * fade;
        }

        private bool IsLocalPilot()
        {
            if (!body) return false;
            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
                if (user.cachedBody == body) return true;
            return false;
        }

        private void CheckResult(AKRESULT result, string operation)
        {
            if (result == AKRESULT.AK_Success || mixErrorLogged) return;
            mixErrorLogged = true;
            Log.Error($"AH-64 rotor {operation} failed: {result}.");
        }

        private void EnsurePlaying()
        {
            if (playingId != 0 || startAttempts >= 3 || Time.unscaledTime < retryAt) return;
            startAttempts++;
            retryAt = Time.unscaledTime + 3f;
            playingId = AkSoundEngine.PostEvent("Play_AH64_Rotor", emitter,
                (uint)(AkCallbackType.AK_EndOfEvent | AkCallbackType.AK_EnableGetSourcePlayPosition),
                OnAudioEvent, null);
            diagnosticCount = 0;
            previousPosition = -1;
            diagnosticAt = Time.unscaledTime + 0.75f;
            if (playingId == 0) Log.Error($"AH-64 rotor event failed to start (attempt {startAttempts}).");
            else Log.Info($"AH-64 rotor Wwise candidate C started: id={playingId}, local={IsLocalPilot()}, gain={gain:F3}.");
        }

        private void OnAudioEvent(object cookie, AkCallbackType type, AkCallbackInfo info)
        {
            AkEventCallbackInfo ended = info as AkEventCallbackInfo;
            if (type != AkCallbackType.AK_EndOfEvent || ended == null || ended.playingID != playingId) return;
            Log.Warning($"AH-64 rotor loop ended unexpectedly: id={playingId}.");
            playingId = 0;
        }

        private void CheckPlayback()
        {
            if (playingId == 0 || diagnosticCount >= 3 || Time.unscaledTime < diagnosticAt
                || !Application.isFocused || gain <= 0f) return;
            diagnosticAt = Time.unscaledTime + 0.75f;
            diagnosticCount++;
            AKRESULT result = AkSoundEngine.GetSourcePlayPosition(playingId, out int position);
            Log.Info($"AH-64 rotor playback: id={playingId}, query={result}, positionMs={position}, "
                + $"previousMs={previousPosition}, gain={gain:F3}, pitch={pitch:F3}.");
            if (result != AKRESULT.AK_Success || position == previousPosition)
                Log.Warning("AH-64 rotor playback time is unavailable or stationary; capture ah64_audio_status while playing.");
            previousPosition = position;
        }

        [ConCommand(commandName = "ah64_audio_status", flags = ConVarFlags.None,
            helpText = "Print AH-64 rotor Wwise playback position and current mix.")]
        private static void AudioStatus(ConCommandArgs args)
        {
            AH64FlightAudio[] rotors = FindObjectsOfType<AH64FlightAudio>();
            Debug.Log($"AH-64 audio: rotors={rotors.Length}, paused={PauseManager.isPaused}.");
            foreach (AH64FlightAudio rotor in rotors)
            {
                AKRESULT result = AkSoundEngine.GetSourcePlayPosition(rotor.playingId, out int position);
                Debug.Log($"AH-64 rotor: id={rotor.playingId}, query={result}, positionMs={position}, "
                    + $"gain={rotor.gain:F3}, pitch={rotor.pitch:F3}, direction={rotor.directionalLoad:F3}, "
                    + $"local={rotor.IsLocalPilot()}.");
                rotor.PrintEmitterStatus();
            }
        }

        private void PrintEmitterStatus()
        {
            //Inspect only this aircraft. Do not stop unknown body/weapon/utility events merely
            //because a listener reports overlapping audio; event IDs identify the real source.
            foreach (AkGameObj source in GetComponentsInChildren<AkGameObj>(true))
            {
                uint count = 32;
                uint[] ids = new uint[count];
                AKRESULT result = AkSoundEngine.GetPlayingIDsFromGameObject(
                    AkSoundEngine.GetAkGameObjectID(source.gameObject), ref count, ids);
                Debug.Log($"AH-64 emitter: {source.name}, query={result}, playing={count}.");
                if (result != AKRESULT.AK_Success) continue;
                for (int i = 0; i < ids.Length && i < count; i++)
                    Debug.Log($"AH-64 event: emitter={source.name}, playingId={ids[i]}, "
                        + $"eventId={AkSoundEngine.GetEventIDFromPlayingID(ids[i])}.");
            }
            foreach (AudioSource source in GetComponentsInChildren<AudioSource>(true))
                Debug.Log($"AH-64 Unity audio: emitter={source.name}, playing={source.isPlaying}, "
                    + $"loop={source.loop}, clip={(source.clip ? source.clip.name : "none")}.");
        }

        private void OnDisable() { StopRotor(); }
        private void OnDestroy()
        {
            StopRotor();
            if (emitter) Destroy(emitter);
        }

        private void StopRotor()
        {
            uint stopped = playingId;
            playingId = 0; // End callback must not classify an intentional stop as a failure.
            if (stopped != 0) AkSoundEngine.StopPlayingID(stopped);
        }
    }
}
