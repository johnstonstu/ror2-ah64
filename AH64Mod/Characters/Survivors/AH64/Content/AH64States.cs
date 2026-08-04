using AH64.Survivors.SkillStates;

namespace AH64.Survivors
{
    public static class AH64States
    {
        public static void Init()
        {
            //the custom main "Body" state - it owns the hover. Registered like any other EntityState.
            Modules.Content.AddEntityState(typeof(AH64Main));

            Modules.Content.AddEntityState(typeof(FireChaingun));

            Modules.Content.AddEntityState(typeof(FireRocketPods));

            Modules.Content.AddEntityState(typeof(ServoDash));

            Modules.Content.AddEntityState(typeof(SmokeBackflip));

            Modules.Content.AddEntityState(typeof(FireHellfire));

            //both halves of the Longbow special. Fire is only ever entered from Paint, but it still has
            //to be in the catalog — EntityStateCatalog resolves it by type when the machine serializes.
            Modules.Content.AddEntityState(typeof(PaintLongbow));

            Modules.Content.AddEntityState(typeof(FireLongbow));
        }
    }
}
