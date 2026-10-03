using AH64.Survivors.SkillStates;

namespace AH64.Survivors
{
    public static class AH64States
    {
        public static void Init()
        {
            //the custom main "Body" state - it owns the hover. Registered like any other EntityState.
            Modules.Content.AddEntityState(typeof(AH64Main));

            //the crash. Networked on the Body machine like the main state, so it must be registered too.
            Modules.Content.AddEntityState(typeof(AH64Death));

            //All three primary variants must be registered, not just the default. An unregistered
            //state still runs locally but has no EntityStateIndex, so the network serializer logs
            //"Sending state that resolves to invalid ..." and it never replicates to other clients.
            Modules.Content.AddEntityState(typeof(FireChaingun));

            Modules.Content.AddEntityState(typeof(FireGatling));

            Modules.Content.AddEntityState(typeof(FireCannon));

            Modules.Content.AddEntityState(typeof(FireRocketPods));

            Modules.Content.AddEntityState(typeof(ServoDash));

            Modules.Content.AddEntityState(typeof(SmokeBackflip));

            Modules.Content.AddEntityState(typeof(BrakingTurn));

            Modules.Content.AddEntityState(typeof(FireHellfire));

            Modules.Content.AddEntityState(typeof(BombingRun));

            //both halves of the Longbow special. Fire is only ever entered from Paint, but it still has
            //to be in the catalog — EntityStateCatalog resolves it by type when the machine serializes.
            Modules.Content.AddEntityState(typeof(PaintLongbow));

            Modules.Content.AddEntityState(typeof(FireLongbow));
        }
    }
}
