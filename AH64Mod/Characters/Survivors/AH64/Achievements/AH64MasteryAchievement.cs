using RoR2;
using AH64.Modules.Achievements;

namespace AH64.Survivors.Achievements
{
    //automatically creates language tokens "ACHIEVMENT_{identifier.ToUpper()}_NAME" and "ACHIEVMENT_{identifier.ToUpper()}_DESCRIPTION" 
    [RegisterAchievement(identifier, unlockableIdentifier, null, 10, null)]
    public class AH64MasteryAchievement : BaseMasteryAchievement
    {
        public const string identifier = AH64Survivor.AH64_PREFIX + "masteryAchievement";
        public const string unlockableIdentifier = AH64Survivor.AH64_PREFIX + "masteryUnlockable";

        //Hardcoded body name — never touch AH64Survivor.instance here. Achievements install during
        //profile load; if the plugin failed to Awake (or hasn't yet), instance is null and the NRE
        //aborts RoR2Application.InitializeGameRoutine at the 99% hang.
        public override string RequiredCharacterBody => "AH64Body";

        //difficulty coeff 3 is monsoon. 3.5 is typhoon for grandmastery skins
        public override float RequiredDifficultyCoefficient => 3;
    }
}