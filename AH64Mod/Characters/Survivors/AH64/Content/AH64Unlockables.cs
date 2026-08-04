using AH64.Survivors.Achievements;
using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    public static class AH64Unlockables
    {
        public static UnlockableDef characterUnlockableDef = null;
        public static UnlockableDef masterySkinUnlockableDef = null;

        public static void Init()
        {
            masterySkinUnlockableDef = Modules.Content.CreateAndAddUnlockbleDef(
                AH64MasteryAchievement.unlockableIdentifier,
                Modules.Tokens.GetAchievementNameToken(AH64MasteryAchievement.identifier),
                AH64Survivor.instance.assetBundle.LoadAsset<Sprite>("texMasteryAchievement"));
        }
    }
}
