using BepInEx.Configuration;
using AH64.Modules;
using AH64.Modules.Characters;
using AH64.Survivors.Components;
using AH64.Survivors.SkillStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AH64.Survivors
{
    public class AH64Survivor : SurvivorBase<AH64Survivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string assetBundleName => "ah64";

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string bodyName => "AH64Body";

        //name of the ai master for vengeance and goobo. must be unique
        public override string masterName => "AH64Monster";

        //prefabs authored by AH64Phase4Builder from the Blender FBX
        public override string modelPrefabName => "mdlAH64";
        public override string displayPrefabName => "AH64Display";

        public const string AH64_PREFIX = "AH64_";

        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => AH64_PREFIX;
        
        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = AH64_PREFIX + "NAME",
            subtitleNameToken = AH64_PREFIX + "SUBTITLE",

            //baked from AH64Display by AH64 -> Bake Character Portrait
            characterPortrait = assetBundle.LoadAsset<Texture>("texAH64Icon"),
            //olive drab — UI accent, not the model
            bodyColor = new Color(0.22f, 0.28f, 0.18f),
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            //Gunship: more hull than a human survivor. The large model and pulled-back camera make
            //human-survivor speeds read slower. 8.5 (1.2.1, down from 10) is still a noticeable cruise.
            //Fire Control Radar can add another 15% while facing its paint. Higher acceleration sharpens
            //cyclic reversals without changing the hover controller or top speed. Armor stays 0 here;
            //the passive owns it.
            maxHealth = 155f,
            healthRegen = 1.5f,
            armor = 0f,

            moveSpeed = AH64PlaytestConfig.BaseMoveSpeed,
            acceleration = AH64PlaytestConfig.Acceleration,

            jumpCount = 1,

            //FBX origin is at ground contact under the main gear. Capsule MUST stay centred on the
            //transform (center = 0): CharacterBody.footPosition is transform.y - height/2 and does NOT
            //read capsuleYOffset. A non-zero center made the hover probe start underground on spawn,
            //miss the floor, and void-descend straight through the map.
            capsuleRadius = 1.0f,
            capsuleHeight = 2.2f,
            capsuleCenter = Vector3.zero,
            modelBasePosition = new Vector3(0f, -1.1f, 0f), // -height/2 so the gear sits on footPosition
            aimOriginPosition = new Vector3(0f, 1.45f, 0.8f),
            cameraPivotPosition = new Vector3(0f, 1.2f, 0f),
            cameraParamsVerticalOffset = 1.6f,
            cameraParamsDepth = -14f,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
            //material left null: Prefabs.SetupCustomRendererInfos hopoo-converts the FBX import material
            //in place. LoadMaterial matches with Contains, so never request the bare "matAH64".
            new CustomRendererInfo { childName = "Airframe" },
            new CustomRendererInfo { childName = "AirframeDark" },
            new CustomRendererInfo { childName = "Canopy", ignoreOverlays = true },
            new CustomRendererInfo { childName = "AirframeMarkings" },
            new CustomRendererInfo { childName = "NoseOptics", ignoreOverlays = true },
            new CustomRendererInfo { childName = "MainRotor" },
            new CustomRendererInfo { childName = "TailRotor" },
            new CustomRendererInfo { childName = "ChinTurret" },
            new CustomRendererInfo { childName = "ChinBarrel" },
            //Alternate primary's barrel cluster. Ships with its renderer disabled; it still
            //needs an entry here so elite/on-fire overlays reach it once the loadout enables it.
            new CustomRendererInfo { childName = "ChinGatling" },
            new CustomRendererInfo { childName = "ChinGatlingHousing" },
            new CustomRendererInfo { childName = "ChinCannon" },
            new CustomRendererInfo { childName = "PodRocketL" },
            new CustomRendererInfo { childName = "PodRocketR" },
            new CustomRendererInfo { childName = "PodMissileL" },
            new CustomRendererInfo { childName = "PodMissileR" },
            //Individual Hellfires. AH64PylonMissiles hides these as the special is
            //spent, but they still need entries here so elite/on-fire overlays reach
            //whichever ones remain on the rail.
            new CustomRendererInfo { childName = "MissileL0" },
            new CustomRendererInfo { childName = "MissileL1" },
            new CustomRendererInfo { childName = "MissileL2" },
            new CustomRendererInfo { childName = "MissileL3" },
            new CustomRendererInfo { childName = "MissileR0" },
            new CustomRendererInfo { childName = "MissileR1" },
            new CustomRendererInfo { childName = "MissileR2" },
            new CustomRendererInfo { childName = "MissileR3" },
            //Teal FCR bubble — ignoreOverlays so elite/cloak never replace the radome glass look.
            new CustomRendererInfo { childName = "RadarDome", ignoreOverlays = true },
            //RotorBlurMain/RotorBlurTail are deliberately absent: they carry a transparent FX
            //material that CharacterModel must never manage (UpdateRendererMaterials would hand it
            //overlays and stomp the fade). AH64FlightVisuals owns them via ChildLocator instead.
        };
        public override UnlockableDef characterUnlockableDef => AH64Unlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new AH64ItemDisplays();

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }

        public override GameObject bodyPrefab { get; protected set; }
        public override CharacterBody prefabCharacterBody { get; protected set; }
        public override GameObject characterModelObject { get; protected set; }
        public override CharacterModel prefabCharacterModel { get; protected set; }
        public override GameObject displayPrefab { get; protected set; }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            AH64Unlockables.Init();

            base.InitializeCharacter();

            AH64States.Init();
            AH64Tokens.Init();

            AH64Assets.Init(assetBundle);
            AH64RiskOfOptions.SetModIcon(assetBundle.LoadAsset<Texture2D>("texAH64Icon"));
            AH64Buffs.Init(assetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            InstallLoadoutAttachments();
            InitializeSkins();
            InitializeCharacterMaster();

            prefabCharacterModel.gameObject.AddComponent<AH64PrimaryWeaponVisuals>();
            displayPrefab.AddComponent<AH64PrimaryWeaponVisuals>();
            displayPrefab.AddComponent<AH64LobbyWeaponPreview>();
            displayPrefab.AddComponent<AH64RotorSpin>();
            displayPrefab.AddComponent<AH64LobbyRotorAudio>();

            AdditionalBodySetup();

            AddHooks();
        }

        private void AdditionalBodySetup()
        {
            //No hitbox group: the kit has no melee. The template's BaseMeleeAttack (default hitbox group
            //"SwordGroup", which never existed on this model) was removed in 1.2 along with its examples.

            //marks this body as ours so the stat hook below only buffs AH64s, and owns the
            //Plasma Reactive Armor charge
            bodyPrefab.AddComponent<AH64PassiveComponent>();
            //Commando's cloned Interactor is 1u — unreachable from hover. See interactionDistance.
            Interactor interactor = bodyPrefab.GetComponent<Interactor>();
            if (interactor)
                interactor.maxInteractionDistance = AH64StaticValues.interactionDistance;
            //Crash instead of Commando's ragdoll death, which left the aircraft hanging in the air.
            CharacterDeathBehavior deathBehavior = bodyPrefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
                deathBehavior.deathState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.AH64Death));
            //the chopper never touches the ground: this switches CharacterMotor into flight + anti-gravity
            //and holds a fixed altitude above terrain. Driven from AH64Main, not from its own FixedUpdate.
            bodyPrefab.AddComponent<AH64HoverController>();
            AH64HoverController.InstallItemHooks();
            AH64AirtimeGauge.Install();
            bodyPrefab.AddComponent<AH64PickupReach>();
            //Luminous Shot: one stack per Hydra ripple, not per rocket
            bodyPrefab.AddComponent<AH64LuminousRipple>();
            //presentation: rotors, chin turret, flight lean/wash, engine audio, reload smoke
            bodyPrefab.AddComponent<AH64RotorSpin>();
            bodyPrefab.AddComponent<AH64ChinTurret>();
            //Harmless while the M230 is equipped — it finds ChinGatling, sees no fire
            //input, and holds at 0 rpm without touching the transform.
            bodyPrefab.AddComponent<AH64GatlingSpin>();
            AH64HellfireOwner.Install(bodyPrefab);
            bodyPrefab.AddComponent<AH64FlightVisuals>();
            bodyPrefab.AddComponent<AH64FlightAudio>();
            bodyPrefab.AddComponent<AH64HeatVisuals>();
            PolishAirframeMaterials(characterModelObject ? characterModelObject.GetComponent<CharacterModel>() : null, lobbyReadability: false);
        }

        //Smoothness/specular don't survive the Standard->HGStandard swap (nothing maps them), so the
        //airframe would render dead flat without this. Values are per-material by name.
        //Preserve authored albedo and emission in-game; only cloned lobby materials get readability tints.
        //Order matters: "matAH64Body" must not be matched by a bare "matAH64" Contains check elsewhere.
        private static void PolishAirframeMaterials(CharacterModel characterModel, bool lobbyReadability)
        {
            if (!characterModel || characterModel.baseRendererInfos == null) return;

            CharacterModel.RendererInfo[] infos = characterModel.baseRendererInfos;
            for (int i = 0; i < infos.Length; i++)
            {
                CharacterModel.RendererInfo info = infos[i];
                Material source = info.defaultMaterial;
                if (!source) continue;

                Material mat = source;
                if (lobbyReadability)
                {
                    mat = UnityEngine.Object.Instantiate(source);
                    mat.name = source.name.Replace(" (Instance)", "");
                    info.defaultMaterial = mat;
                    if (info.renderer)
                        info.renderer.sharedMaterial = mat;
                }

                ApplyAirframeMaterialPolish(mat, lobbyReadability);
                infos[i] = info;
            }

            characterModel.baseRendererInfos = infos;
        }

        private static void ApplyAirframeMaterialPolish(Material mat, bool lobbyReadability)
        {
            string name = mat.name;
            if (name.Contains("matAH64Dark"))
            {
                if (lobbyReadability)
                {
                    // Lobby lighting is weak. Tint only the display clone, leaving each in-game skin's
                    // authored material color intact.
                    Color tint = new Color(0.25f, 0.29f, 0.20f);
                    if (name.Contains("Rotor")) tint = new Color(0.18f, 0.21f, 0.16f);
                    else if (name.Contains("Gun")) tint = new Color(0.30f, 0.33f, 0.25f);
                    else if (name.Contains("RocketPod")) tint = new Color(0.32f, 0.38f, 0.18f);
                    else if (name.Contains("Hellfire")) tint = new Color(0.25f, 0.28f, 0.20f);
                    tint = LiftLobbyColor(tint, 2.35f, 0.32f);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                    SetLobbyFillEmission(mat, tint, 0.70f);
                }
                mat.SetFloat("_Smoothness", lobbyReadability ? 0.38f : 0.30f);
                mat.SetFloat("_SpecularStrength", lobbyReadability ? 0.30f : 0.16f);
                mat.SetFloat("_SpecularExponent", 3f);
            }
            else if (name.Contains("matAH64Markings"))
            {
                if (lobbyReadability)
                {
                    Color tint = LiftLobbyColor(new Color(0.52f, 0.31f, 0.065f), 2.0f, 0.28f);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                }
                mat.SetFloat("_Smoothness", 0.34f);
                mat.SetFloat("_SpecularStrength", lobbyReadability ? 0.32f : 0.16f);
                mat.SetFloat("_SpecularExponent", 4f);
            }
            else if (name.Contains("matAH64Radar"))
            {
                mat.SetFloat("_Smoothness", 0.55f);
                mat.SetFloat("_SpecularStrength", lobbyReadability ? 0.55f : 0.35f);
                mat.SetFloat("_SpecularExponent", 6f);
                if (lobbyReadability)
                {
                    Color tint = LiftLobbyColor(new Color(0.18f, 0.24f, 0.28f), 1.9f, 0.24f);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                    if (mat.HasProperty("_EmColor")) mat.SetColor("_EmColor", new Color(0.10f, 0.16f, 0.18f));
                    if (mat.HasProperty("_EmPower")) mat.SetFloat("_EmPower", 0.85f);
                }
            }
            else if (name.Contains("matAH64Glass"))
            {
                mat.SetFloat("_Smoothness", 0.85f);
                mat.SetFloat("_SpecularStrength", lobbyReadability ? 0.70f : 0.55f);
                mat.SetFloat("_SpecularExponent", 8f);
                if (lobbyReadability && mat.HasProperty("_Color"))
                {
                    Color glass = mat.GetColor("_Color");
                    mat.SetColor("_Color", LiftLobbyColor(glass, 1.7f, 0.16f));
                }
            }
            else if (name.Contains("matAH64Body") || name.Contains("matAH64Optics"))
            {
                mat.SetFloat("_Smoothness", lobbyReadability ? 0.52f : 0.40f);
                mat.SetFloat("_SpecularStrength", lobbyReadability ? 0.50f : 0.28f);
                mat.SetFloat("_SpecularExponent", 5f);
                if (lobbyReadability)
                {
                    Color tint = name.Contains("Optics")
                        ? new Color(0.05f, 0.16f, 0.24f)
                        : new Color(0.48f, 0.56f, 0.32f);
                    tint = LiftLobbyColor(tint, 2.25f, 0.38f);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
                    SetLobbyFillEmission(mat, tint, 0.95f);
                }
            }
        }

        //Character-select lighting is weak; raise albedo floors and add a soft fill so olive paint
        //does not silhouette. Clamped so we stay readable without turning neon.
        private static Color LiftLobbyColor(Color color, float multiply, float floor)
        {
            return new Color(
                Mathf.Clamp01(Mathf.Max(color.r * multiply, floor)),
                Mathf.Clamp01(Mathf.Max(color.g * multiply, floor)),
                Mathf.Clamp01(Mathf.Max(color.b * multiply, floor)),
                color.a);
        }

        private static void SetLobbyFillEmission(Material mat, Color tint, float power)
        {
            if (mat.HasProperty("_EmColor"))
                mat.SetColor("_EmColor", tint * 0.45f);
            if (mat.HasProperty("_EmPower"))
                mat.SetFloat("_EmPower", power);
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(bodyPrefab);

            //the main "Body" state machine has some special properties.
            //AH64Main is GenericCharacterMain plus the hover: it hands the vertical axis to
            //AH64HoverController after the base state has written the horizontal input into moveDirection.
            Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(SkillStates.AH64Main), typeof(EntityStates.SpawnTeleporterState));

            //Three weapon machines so primary / secondary / special never interrupt each other —
            //a gunship fires the chin gun and the pylons at the same time.
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon2");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon3");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(bodyPrefab);
            //add our own
            AddPassiveSkill();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtilitySkills();
            AddSpecialSkills();
        }

        /// <summary>
        /// Durasteel Plating. There is no skill to activate here â€” this is the icon and tooltip only.
        /// The armor itself is applied in RecalculateStatsAPI_GetStatCoefficients below.
        /// </summary>
        private void AddPassiveSkill()
        {
            bodyPrefab.GetComponent<SkillLocator>().passiveSkill = new SkillLocator.PassiveSkill
            {
                enabled = true,
                skillNameToken = AH64_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = AH64_PREFIX + "PASSIVE_DESCRIPTION",
                keywordToken = "",
                icon = assetBundle.LoadAsset<Sprite>("texAH64PassiveIcon"),
            };
        }

        /// <summary>
        /// M230 chain gun. Held-fire hitscan on "Weapon"; pods are "Weapon3" and special is "Weapon2",
        /// so none of the three hardpoints steal each other's fire.
        /// <para>Each round costs a stock, and the drum comes back all at once — that's the reload.
        /// Deliberately NOT built with the "typical primary" SkillDefInfo constructor, which hardcodes
        /// requiredStock/stockToConsume to 0 and would let you fire on an empty drum.</para>
        /// </summary>
        private void AddPrimarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Primary);

            AH64MagazineSkillDef chaingunSkillDef = Skills.CreateSkillDef<AH64MagazineSkillDef>(new SkillDefInfo
            {
                skillName = "AH64Chaingun",
                skillNameToken = AH64_PREFIX + "PRIMARY_CHAINGUN_NAME",
                skillDescriptionToken = AH64_PREFIX + "PRIMARY_CHAINGUN_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64PrimaryIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireChaingun)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Any,

                //the ammo drum. AH64MagazineSkillDef turns a recharge tick into the WHOLE drum rather
                //than a trickle, and resetCooldownTimerOnUse restarts that timer on every shot — so the
                //reload only ever completes once you stop firing (or run dry), and always fills you back
                //to full. That is what makes this read as a reload instead of a cooldown.
                //rechargeStock must stay 1: see AH64MagazineSkillDef for the Eclipse Lite reason.
                baseMaxStock = AH64StaticValues.chaingunMagazineSize,
                rechargeStock = 1,
                baseRechargeInterval = AH64PlaytestConfig.ChaingunReload,

                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = true,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                //hold to fire
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                //agile: keep firing through a sprint
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });
            chaingunSkillDef.attackSpeedBuffsRestockSpeed = true;
            chaingunSkillDef.attackSpeedBuffsRestockSpeed_Multiplier = AH64StaticValues.primaryReloadAttackSpeedMultiplier;
            chaingunSkillDef.barrierRestocks = AH64StaticValues.primaryReloadBarrierRestocks;

            AH64MagazineSkillDef gatlingSkillDef = Skills.CreateSkillDef<AH64MagazineSkillDef>(new SkillDefInfo
            {
                skillName = "AH64Gatling",
                skillNameToken = AH64_PREFIX + "PRIMARY_GATLING_NAME",
                skillDescriptionToken = AH64_PREFIX + "PRIMARY_GATLING_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64GatlingIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireGatling)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Any,

                //Twice the drum of the M230 and a slower refill — same "whole drum at once"
                //reload shape, because a trickle would fight the spool ramp.
                baseMaxStock = AH64StaticValues.gatlingMagazineSize,
                rechargeStock = 1,
                baseRechargeInterval = AH64PlaytestConfig.GatlingReload,

                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = true,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });
            gatlingSkillDef.attackSpeedBuffsRestockSpeed = true;
            gatlingSkillDef.attackSpeedBuffsRestockSpeed_Multiplier = AH64StaticValues.primaryReloadAttackSpeedMultiplier;
            gatlingSkillDef.barrierRestocks = AH64StaticValues.primaryReloadBarrierRestocks;

            //Published before the variant is added so AH64GatlingSpin can identify the
            //equipped primary without holding a reference to this class.
            AH64Assets.gatlingSkillDef = gatlingSkillDef;

            AH64MagazineSkillDef cannonSkillDef = Skills.CreateSkillDef<AH64MagazineSkillDef>(new SkillDefInfo
            {
                skillName = "AH64Cannon",
                skillNameToken = AH64_PREFIX + "PRIMARY_CANNON_NAME",
                skillDescriptionToken = AH64_PREFIX + "PRIMARY_CANNON_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64CannonIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireCannon)),
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Any,

                //Only 8 shells, and the same whole-drum reload shape as the other two.
                baseMaxStock = AH64StaticValues.cannonMagazineSize,
                rechargeStock = 1,
                baseRechargeInterval = AH64PlaytestConfig.CannonReload,

                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = true,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });
            cannonSkillDef.attackSpeedBuffsRestockSpeed = true;
            cannonSkillDef.attackSpeedBuffsRestockSpeed_Multiplier = AH64StaticValues.primaryReloadAttackSpeedMultiplier;
            cannonSkillDef.barrierRestocks = AH64StaticValues.primaryReloadBarrierRestocks;

            AH64Assets.cannonSkillDef = cannonSkillDef;
            Skills.AddPrimarySkills(bodyPrefab, chaingunSkillDef, gatlingSkillDef, cannonSkillDef);
        }

        /// <summary>
        /// One variant for now: the wing-pylon rocket pods. Piston Punch — a melee punch, on a
        /// helicopter — was removed from this slot, and Tri-Blast held it as a placeholder until the
        /// airframe's pylon hardpoints existed.
        /// </summary>
        private void AddSecondarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Secondary);

            Skills.AddSecondarySkills(bodyPrefab, BuildRocketPodsSkillDef());
        }

        /// <summary>
        /// Hydra-70 pods. Hold-to-ripple unguided rockets off alternating wing pylons. Own machine
        /// ("Weapon3") so the chin gun and the pods can fire together. Stock is the pod magazine —
        /// the number above the skill icon — and Backup Magazine adds rockets to that drum.
        /// </summary>
        private SkillDef BuildRocketPodsSkillDef()
        {
            AH64MagazineSkillDef rocketPods = Skills.CreateSkillDef<AH64MagazineSkillDef>(new SkillDefInfo
            {
                skillName = "AH64RocketPods",
                skillNameToken = AH64_PREFIX + "SECONDARY_ROCKETPODS_NAME",
                skillDescriptionToken = AH64_PREFIX + "SECONDARY_ROCKETPODS_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64SecondaryIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireRocketPods)),
                activationStateMachineName = "Weapon3",
                interruptPriority = EntityStates.InterruptPriority.Any,

                //pod magazine. Same reload shape as the M230: full restock on a single recharge tick,
                //timer restarting on every rocket so the reload only finishes once you stop firing or run dry.
                //The full restock includes Backup Magazine rockets, which a fixed rechargeStock of 6 missed.
                baseMaxStock = AH64StaticValues.hydraRocketCount,
                rechargeStock = 1,
                baseRechargeInterval = AH64StaticValues.hydraReloadDuration,

                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = true,
                fullRestockOnAssign = true,
                //false so Backup Magazine adds rockets to the magazine (GenericSkill.RecalculateMaxStock)
                dontAllowPastMaxStocks = false,
                //hold to ripple, same as the chain gun
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });
            //every rocket is an activation; AH64LuminousRipple grants one Luminous Shot stack per ripple instead
            rocketPods.autoHandleLuminousShot = false;
            rocketPods.reloadSecondsPerExtraStock = AH64StaticValues.hydraReloadPerExtraRocket;
            return rocketPods;
        }

        /// <summary>
        /// Default: Evasive Roll (barrel roll + toned flares). Loadout variant: Smoke Backflip
        /// (rearward aerobatic flip + smoke + cloak), then Braking Turn. All use the Body machine.
        /// </summary>
        private void AddUtilitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Utility);

            SkillDef dashSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "AH64EvasiveJink",
                skillNameToken = AH64_PREFIX + "UTILITY_DASH_NAME",
                skillDescriptionToken = AH64_PREFIX + "UTILITY_DASH_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64UtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.ServoDash)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = AH64PlaytestConfig.DashCooldown,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = true,
            });

            SkillDef backflipSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "AH64SmokeBackflip",
                skillNameToken = AH64_PREFIX + "UTILITY_BACKFLIP_NAME",
                skillDescriptionToken = AH64_PREFIX + "UTILITY_BACKFLIP_DESCRIPTION",
                //Placeholder until a dedicated icon ships — keeps the loadout picker readable.
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64UtilityIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.SmokeBackflip)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = AH64StaticValues.backflipCooldown,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            SkillDef brakingSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "AH64BrakingTurn",
                skillNameToken = AH64_PREFIX + "UTILITY_BRAKING_TURN_NAME",
                skillDescriptionToken = AH64_PREFIX + "UTILITY_BRAKING_TURN_DESCRIPTION",
                // Private prototype placeholder; no dedicated braking icon has been authored.
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64UtilityIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.BrakingTurn)),
                activationStateMachineName = "Body",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,
                baseRechargeInterval = AH64BrakingTurnStaticValues.Cooldown,
                baseMaxStock = AH64BrakingTurnStaticValues.Stock,
                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = false,
                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            Skills.AddUtilitySkills(bodyPrefab, dashSkillDef, backflipSkillDef, brakingSkillDef);
        }

        /// <summary>
        /// Default special is custom Longbow (hold to paint, release to fire; primary/secondary stay
        /// free). Hellfire stays as a loadout variant. Both sit on "Weapon2" so they never drop the
        /// chain gun or Hydra pods.
        /// </summary>
        private void AddSpecialSkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Special);

            SkillDef longbowSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "AH64Longbow",
                skillNameToken = AH64_PREFIX + "SPECIAL_LONGBOW_NAME",
                skillDescriptionToken = AH64_PREFIX + "SPECIAL_LONGBOW_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64SpecialIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.PaintLongbow)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = AH64StaticValues.longbowMaxLocks,
                rechargeStock = 1,
                baseRechargeInterval = AH64StaticValues.longbowRechargeInterval,

                requiredStock = 1,
                //PaintLongbow deducts per lock. Consuming here would burn a stock just to open targeting.
                stockToConsume = 0,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                //Must stay false: GenericSkill.RecalculateMaxStock drops bonusStockFromBody when it is
                //true, so Lysate Cell added no missiles. Refund overflow is clamped in PaintLongbow.RefundStock.
                dontAllowPastMaxStocks = false,

                isCombatSkill = true,
                //false so holding special keeps PaintLongbow alive for continuous locking
                mustKeyPress = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
            });

            SkillDef hellfireSkillDef = Skills.CreateSkillDef<AH64HellfireSkillDef>(new SkillDefInfo
            {
                skillName = "AH64Hellfire",
                skillNameToken = AH64_PREFIX + "SPECIAL_HELLFIRE_NAME",
                skillDescriptionToken = AH64_PREFIX + "SPECIAL_HELLFIRE_DESCRIPTION",
                // TODO(Unity): Bake texAH64HellfireIcon into ah64 bundle (single AGM on inboard rail, olive/orange).
                // Interim: Hydra secondary art — keeps Hellfire distinct from Longbow (texAH64SpecialIcon) in the
                // special loadout; texBazookaFireIcon is not shipped in this bundle.
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64SecondaryIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.FireHellfire)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 1,
                baseRechargeInterval = AH64StaticValues.hellfireCooldown,

                isCombatSkill = true,
                mustKeyPress = true,
            });

            SkillDef bombingSkillDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "AH64BombingRun",
                skillNameToken = AH64_PREFIX + "SPECIAL_BOMBING_NAME",
                skillDescriptionToken = AH64_PREFIX + "SPECIAL_BOMBING_DESCRIPTION",
                // Private prototype presentation; a unique bomb icon remains a release gate.
                skillIcon = assetBundle.LoadAsset<Sprite>("texAH64SpecialIcon"),
                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.BombingRun)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Skill,
                baseMaxStock = 1,
                requiredStock = 1,
                stockToConsume = 1,
                rechargeStock = 1,
                baseRechargeInterval = AH64BombingRunStaticValues.Cooldown,
                beginSkillCooldownOnSkillEnd = false,
                resetCooldownTimerOnUse = false,
                dontAllowPastMaxStocks = false,
                fullRestockOnAssign = true,
                mustKeyPress = true,
                isCombatSkill = true,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false
            });
            AH64Assets.bombingSkillDef = bombingSkillDef;
            // Existing defaults and variant order remain intact; AI paint behavior is unchanged.
            Skills.AddSpecialSkills(bodyPrefab, longbowSkillDef, hellfireSkillDef, bombingSkillDef);
        }
        #endregion skills
        
        #region skins
        private void InstallLoadoutAttachments()
        {
            CharacterModel displayModel = displayPrefab.GetComponent<CharacterModel>();
            if (!displayModel || !prefabCharacterModel.GetComponent<ChildLocator>()
                || !displayModel.GetComponent<ChildLocator>())
            {
                Log.Warning("AH64 modular attachments unavailable: body/display model anchors missing.");
                return;
            }
            Material frameMaterial = null;
            foreach (CharacterModel.RendererInfo info in prefabCharacterModel.baseRendererInfos)
                if (info.renderer && info.renderer.name == "AirframeDark")
                    frameMaterial = info.defaultMaterial;
            if (!frameMaterial)
            {
                Log.Warning("AH64 modular attachments unavailable: converted airframe material missing.");
                return;
            }
            // Build both sets before skins; each body renderer has an exact display counterpart.
            AH64LoadoutAttachments bodyAttachments = AH64LoadoutAttachmentBuilder.Build(prefabCharacterModel.transform, frameMaterial);
            AH64LoadoutAttachments displayAttachments = AH64LoadoutAttachmentBuilder.Build(displayModel.transform, frameMaterial);
            if (!MatchingAttachmentRenderers(bodyAttachments, displayAttachments))
            {
                Log.Warning("AH64 modular attachments omitted: body/display renderer sets do not match.");
                return;
            }
            RegisterLoadoutAttachments(prefabCharacterModel, bodyAttachments, false);
            RegisterLoadoutAttachments(displayModel, displayAttachments, true);
        }

        private static bool MatchingAttachmentRenderers(AH64LoadoutAttachments body, AH64LoadoutAttachments display)
        {
            if (!body || !display || body.Renderers == null || display.Renderers == null
                || body.Renderers.Length != display.Renderers.Length) return false;
            var names = new HashSet<string>();
            foreach (Renderer renderer in body.Renderers)
                if (!renderer || !names.Add(renderer.name)) return false;
            foreach (Renderer renderer in display.Renderers)
                if (!renderer || !names.Remove(renderer.name)) return false;
            return names.Count == 0;
        }

        private static void RegisterLoadoutAttachments(CharacterModel model, AH64LoadoutAttachments attachments, bool display)
        {
            if (!attachments) return;
            ChildLocator locator = model.GetComponent<ChildLocator>();
            var infos = new List<CharacterModel.RendererInfo>(model.baseRendererInfos);
            var pairs = new List<ChildLocator.NameTransformPair>(locator.transformPairs);
            foreach (Renderer renderer in attachments.Renderers)
            {
                infos.Add(new CharacterModel.RendererInfo
                {
                    renderer = renderer,
                    defaultMaterial = renderer.sharedMaterial,
                    ignoreOverlays = false,
                    defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                });
                pairs.Add(new ChildLocator.NameTransformPair { name = renderer.name, transform = renderer.transform });
            }
            model.baseRendererInfos = infos.ToArray();
            locator.transformPairs = pairs.ToArray();
            attachments.IsDisplay = display;
            if (attachments.BombRack) attachments.BombRack.BombingSkill = AH64Assets.bombingSkillDef;
            var selection = model.gameObject.AddComponent<AH64LoadoutSelection>();
            selection.IsDisplay = display;
            selection.Attachments = attachments;
            attachments.PresentationReady = true;
        }

        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();
            // Clone alternate paints only after the default shader polish is complete.
            PolishAirframeMaterials(prefabCharacterModel, lobbyReadability: false);
            skinController.skins = AH64Skins.Create(assetBundle, prefabCharacterModel);

            // Bake complete display-rooted overrides for each palette. Runtime lobby boosting used
            // to overwrite every skin with olive and could lose its edits when CharacterModel updated.
            Skins.CreateDisplaySkinController(displayPrefab, skinController.skins, AH64Skins.CreateLobbyMaterial);
            Log.Debug("AH64 visual staging: registered Default, Desert Tan, Arctic, Army Green and Night Stalker (mastery) body/display skins.");
        }
        #endregion skins

        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            //Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            AH64AI.Init(bodyPrefab, masterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {
            //this hook fires for every body in the run, so bail out on anything that isn't ours
            AH64PassiveComponent passive = sender ? sender.GetComponent<AH64PassiveComponent>() : null;
            if (!passive)
                return;

            if (passive.FacingPainted)
                args.moveSpeedMultAdd += AH64PlaytestConfig.RadarFacingSpeedBonus;

            if (passive.CloseToPainted)
                args.armorAdd += AH64StaticValues.radarCloseArmor;

            //Evasive Roll braces the plating for a moment after the move ends
            if (sender.HasBuff(AH64Buffs.platingBuff))
            {
                args.armorAdd += AH64StaticValues.dashArmorBonus;
            }
        }
    }
}
