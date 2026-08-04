using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Spins <c>MainRotor</c> and <c>TailRotor</c> about their local Y axes.
    ///
    /// <para>Local Y, not Z: Blender's Z-up becomes Unity's Y-up on import, so the thin axis of each
    /// rotor disc is local Y in the game. Spinning about local Z wobbles instead of spinning — the same
    /// failure mode as an off-centre origin, and just as invisible until it's in game.</para>
    /// </summary>
    public class AH64RotorSpin : MonoBehaviour
    {
        //real Apache main rotor is ~225 rpm; this is stylistic — readable at a glance without looking like a fan
        private const float MainRotorRpm = 280f;
        private const float TailRotorRpm = 1200f;

        private Transform mainRotor;
        private Transform tailRotor;

        private void Start()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
                return;

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
                return;

            mainRotor = childLocator.FindChild("MainRotor");
            tailRotor = childLocator.FindChild("TailRotor");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (mainRotor)
                mainRotor.Rotate(Vector3.up, MainRotorRpm * 6f * dt, Space.Self);
            if (tailRotor)
                tailRotor.Rotate(Vector3.up, TailRotorRpm * 6f * dt, Space.Self);
        }
    }
}
