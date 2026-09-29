using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Plays the AtG missile launch cue for Hydra and Longbow shots.
    /// </summary>
    internal static class AH64LaunchSound
    {
        //Play_item_proc_missile_fire is DurationType="Infinite": it starts the missile fly loop too.
        //Vanilla posts it on the missile prefab, so the loop dies with the missile. Posted on the
        //body it never stops and every volley stacks another loop under the rotor for the rest of
        //the run. A throwaway emitter reproduces the vanilla lifetime: destroying its AkGameObj
        //unregisters it from Wwise, which stops whatever it is still playing.
        private const string LaunchSound = "Play_item_proc_missile_fire";
        private const float EmitterLifetime = 1.5f;

        internal static void Play(GameObject owner)
        {
            if (!owner)
                return;

            GameObject emitter = new GameObject("AH64LaunchAudio");
            emitter.transform.SetParent(owner.transform, false);
            emitter.AddComponent<AkGameObj>();
            Util.PlaySound(LaunchSound, emitter);
            Object.Destroy(emitter, EmitterLifetime);
        }
    }
}
