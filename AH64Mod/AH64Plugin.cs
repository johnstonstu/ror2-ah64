using BepInEx;
using AH64.Survivors;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AH64
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class AH64Plugin : BaseUnityPlugin
    {
        //BepInEx plugin GUID. Also the ContentPack identifier and the Risk of Options mod key, and
        //it names the config file (BepInEx/config/com.JohnstonStu.AH64.cfg). Changing it after
        //publication would orphan every user's settings, so it is fixed from 1.0.0 onward.
        public const string MODUID = "com.JohnstonStu.AH64";
        public const string MODNAME = "AH64";
        //Must stay in step with Build/manifest.json — NetworkCompatibility is
        //EveryoneNeedSameModVersion, so a mismatch is a lobby rejection. tools/pack.ps1 enforces it.
        //BepInEx 5 requires System.Version format: major.minor[.build[.revision]] — NOT semver
        //letters. A suffix like "1.0.0-rc1" silently skips the plugin while still registering
        //achievements, which presents as a hang at 99%.
        public const string MODVERSION = "1.2.0";

        public static AH64Plugin instance;

        void Awake()
        {
            instance = this;

            //easy to use logger
            Log.Init(Logger);
            AH64PlaytestConfig.Init(Config);

            // used when you want to properly set up language folders
            Modules.Language.Init();

            // character initialization
            new AH64Survivor().Initialize();

            // make a content pack and add it. this has to be last
            new Modules.ContentPacks().Initialize();
        }
    }
}
