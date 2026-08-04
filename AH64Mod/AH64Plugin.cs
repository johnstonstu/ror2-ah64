using BepInEx;
using AH64.Survivors;
using AH64.Survivors.Components;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AH64
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class AH64Plugin : BaseUnityPlugin
    {
        public const string MODUID = "com.stu.AH64";
        public const string MODNAME = "AH64";
        //Playtest iteration scheme (keep in step with Build/manifest.json — EveryoneNeedSameModVersion).
        //BepInEx 5 requires System.Version format: major.minor[.build[.revision]] — NOT semver letters.
        //  small DLL/polish ships → 0.1.16.1, 0.1.16.2, …  (fourth digit ≈ a, b, c)
        //  bundled feature/focus ship  → 0.1.17, 0.1.18, …
        //Using "0.1.16-a" silently skips the plugin and still registers achievements → hang at 99%.
        public const string MODVERSION = "1.0.0";

        // a prefix for name tokens to prevent conflicts- please capitalize all name tokens for convention
        public const string DEVELOPER_PREFIX = "STU";

        public static AH64Plugin instance;

        void Awake()
        {
            instance = this;

            //easy to use logger
            Log.Init(Logger);
            AH64PlaytestConfig.Init(Config);
            AH64BuildStampController.Init();

            // used when you want to properly set up language folders
            Modules.Language.Init();

            // character initialization
            new AH64Survivor().Initialize();

            // make a content pack and add it. this has to be last
            new Modules.ContentPacks().Initialize();
        }
    }
}
