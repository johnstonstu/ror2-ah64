using RoR2;
using RoR2.SurvivorMannequins;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// A quiet rotor idle under the character-select model, following <see cref="AH64RotorSpin"/> so picking
    /// the AH-64 is heard as its engine starting. Same Wwise loop and RTPCs as <see cref="AH64FlightAudio"/>,
    /// played flat (2D) at a fraction of flight volume.
    ///
    /// <para>Only on the local player's own mannequin: several AH-64s in a lobby would otherwise stack
    /// flat loops, and a display with no owner (anywhere outside character select) stays silent.</para>
    /// </summary>
    public sealed class AH64LobbyRotorAudio : MonoBehaviour
    {
        private AH64RotorSpin rotorSpin;
        private GameObject emitter;
        private uint playingId;
        private bool failed;

        private void Awake()
        {
            rotorSpin = GetComponent<AH64RotorSpin>();
        }

        private void Update()
        {
            if (failed || Application.isBatchMode)
                return;
            if (!IsLocalMannequin())
            {
                Stop();
                return;
            }
            if (!TryLoadBank())
                return;

            if (!emitter)
            {
                emitter = new GameObject("AH64LobbyRotorWwise");
                emitter.transform.SetParent(transform, false);
                emitter.AddComponent<AkGameObj>();
            }
            if (playingId == 0)
            {
                playingId = AkSoundEngine.PostEvent("Play_AH64_Rotor", emitter);
                if (playingId == 0)
                {
                    //Presentation only: give up quietly rather than retrying every frame in the menu.
                    failed = true;
                    Log.Warning("AH-64 lobby rotor idle failed to start.");
                    return;
                }
            }

            float spool = rotorSpin ? rotorSpin.Spool : 1f;
            float gain = Mathf.Clamp01(AH64PlaytestConfig.RotorHoverVolume * AH64StaticValues.rotorMixTrim
                * AH64StaticValues.rotorLobbyAudioGain * spool);
            //Same authored RTPC range as the flight mix (-700..600 cents).
            float pitch = Mathf.Clamp(AH64PlaytestConfig.RotorPitch
                    * Mathf.Lerp(AH64StaticValues.rotorAudioSpoolPitchFloor, 1f, spool),
                Mathf.Pow(2f, -700f / 1200f), Mathf.Pow(2f, 600f / 1200f));
            float lowpass = Mathf.Clamp(Mathf.Log(20000f / AH64PlaytestConfig.RotorToneCutoff)
                / Mathf.Log(20000f / 600f) * 60f, 0f, 60f);

            AkSoundEngine.SetGameObjectOutputBusVolume(AkSoundEngine.GetAkGameObjectID(emitter), ulong.MaxValue, gain);
            AkSoundEngine.SetRTPCValue("AH64_RotorPitch", 1200f * Mathf.Log(pitch, 2f), emitter);
            AkSoundEngine.SetRTPCValue("AH64_RotorSpatial", 0f, emitter);
            AkSoundEngine.SetRTPCValue("AH64_RotorLowpass", lowpass, emitter);
        }

        private bool IsLocalMannequin()
        {
            SurvivorMannequinSlotController slot = GetComponentInParent<SurvivorMannequinSlotController>();
            NetworkUser user = slot ? slot.networkUser : null;
            return user && user.isLocalPlayer;
        }

        private bool TryLoadBank()
        {
            try
            {
                return AH64RotorBank.EnsureLoaded();
            }
            catch (System.Exception)
            {
                //EnsureLoaded has already logged the cause. Never let menu audio break character select.
                failed = true;
                return false;
            }
        }

        private void Stop()
        {
            uint stopped = playingId;
            playingId = 0;
            if (stopped != 0)
                AkSoundEngine.StopPlayingID(stopped);
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            Stop();
            if (emitter)
                Destroy(emitter);
        }
    }
}
