using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using AH64.Modules;
using RoR2.Projectile;

namespace AH64.Survivors
{
    public static class AH64Assets
    {
        // particle effects
        public static GameObject hellfireMuzzleFlashEffect;
        public static GameObject hellfireExplosionEffect;
        public static GameObject longbowExplosionEffect;
        public static GameObject hydraMuzzleFlashEffect;
        public static GameObject hydraExplosionEffect;

        public static GameObject chaingunTracerEffect;
        public static GameObject chaingunMuzzleFlashEffect;
        public static GameObject chaingunHitEffect;

        public static GameObject dashThrusterEffect;
        public static GameObject dashDustEffect;
        public static GameObject dashFlareEffect;
        public static GameObject chaingunSplashEffect;
        public static GameObject gatlingSplashEffect;
        public static GameObject cannonSplashEffect;

        /// <summary>
        /// Set by <c>AH64Survivor.AddPrimarySkills</c>. <see cref="Components.AH64GatlingSpin"/>
        /// compares the equipped primary against this to drive its spool; presentation also uses
        /// exact SkillDef identity to select the meshes, so the definitions
        /// has to be reachable from a component that has no reference to the survivor class.
        /// </summary>
        public static RoR2.Skills.SkillDef gatlingSkillDef;
        public static RoR2.Skills.SkillDef cannonSkillDef;

        //projectiles
        public static GameObject hellfireProjectilePrefab;
        public static GameObject hydraRocketProjectilePrefab;

        private static AssetBundle _assetBundle;
        private static GameObject _smokePuffEffect;
        private static GameObject _rotorWashEffect;
        private static GameObject _longbowProjectile;
        private static GameObject _longbowLockIndicatorPrefab;
        private static GameObject _radarPaintIndicatorPrefab;
        //Registered in CreateEffects — the clone strips ShakeEmitter (camera + gamepad rumble) and
        //EffectComponent.soundName so the passive never thumps through the donor VFX.
        public static GameObject radarPaintPingEffect;

        //Threat-red for the radar paint-ping — deliberately not Engi's yellow lock rings.
        public static readonly Color32 RadarEffectColor = new Color32(255, 55, 40, 255);

        /// <summary>
        /// Despite the name, this is <c>OmniExplosionVFX</c>, a stylised explosion flash. There is no
        /// <c>ExplosionLunarGolem</c> prefab in the game's addressables catalog (checked 2026-09-30), so the
        /// first path always misses and the fallback is what ships. Smoke Backflip and Evasive Roll's
        /// thrusters also use it, and that flash is the look they shipped with. Real smoke is <see cref="hydraMuzzleFlashEffect"/>
        /// (<c>MuzzleflashSmokeRing</c>).
        /// </summary>
        public static GameObject SmokePuffEffect
        {
            get
            {
                if (!_smokePuffEffect)
                {
                    _smokePuffEffect = LoadLegacy("Prefabs/Effects/ImpactEffects/ExplosionLunarGolem")
                        ?? LoadLegacy("Prefabs/Effects/OmniExplosionVFX");
                }

                return _smokePuffEffect;
            }
        }

        // Verified against the installed addressables catalog. The Assets/.../MuzzleFlashes
        // string is its internal ID, not its address key; loading that ID prints InvalidKeyException.
        private static readonly string[] SmokeRingPaths =
        {
            "RoR2/Base/Common/VFX/MuzzleflashSmokeRing.prefab"
        };

        /// <summary>
        /// Ground dust, shared by the rotor wash and the dash. Built once in <see cref="Init"/>.
        /// </summary>
        public static GameObject DustEffect => _rotorWashEffect;

        /// <summary>Dust kicked under the rotor disc when hugging the ground.</summary>
        public static GameObject RotorWashEffect => _rotorWashEffect;

        /// <summary>
        /// AH-64 visual clone of Engineer's guided harpoon. Guidance and hit behavior stay vanilla;
        /// CreateLongbowProjectile swaps the ghost, clears the Engi launch cue, and installs our AGM warhead.
        /// </summary>
        public static GameObject LongbowProjectile
        {
            get
            {
                if (!_longbowProjectile)
                {
                    _longbowProjectile = EntityStates.Engi.EngiMissilePainter.Fire.projectilePrefab;
                    if (!_longbowProjectile)
                        _longbowProjectile = LoadLegacy("Prefabs/Projectiles/EngiHarpoon");
                    if (!_longbowProjectile)
                        Log.Error("LongbowProjectile failed to load from Engi Fire.projectilePrefab and LegacyResources. Special will not fire.");
                }

                return _longbowProjectile;
            }
        }

        /// <summary>The ring drawn on a painted Longbow target. Engi ring + dots.</summary>
        public static GameObject LongbowLockIndicatorPrefab
        {
            get
            {
                if (!_longbowLockIndicatorPrefab)
                {
                    //same path Engi's own Paint state uses
                    _longbowLockIndicatorPrefab = LoadLegacy("Prefabs/EngiMissileTrackingIndicator");
                    if (!_longbowLockIndicatorPrefab)
                        _longbowLockIndicatorPrefab = EntityStates.Engi.EngiMissilePainter.Paint.stickyTargetIndicatorPrefab;
                }

                return _longbowLockIndicatorPrefab;
            }
        }

        /// <summary>
        /// Fire Control Radar paint marker. Huntress tracker (arrow/diamond), deliberately different
        /// from Longbow's Engi ring so passive paint and special locks never read as the same VFX.
        /// </summary>
        public static GameObject RadarPaintIndicatorPrefab
        {
            get
            {
                if (!_radarPaintIndicatorPrefab)
                {
                    _radarPaintIndicatorPrefab = LoadLegacy("Prefabs/HuntressTrackingIndicator");
                    if (!_radarPaintIndicatorPrefab)
                        _radarPaintIndicatorPrefab = LongbowLockIndicatorPrefab;
                }

                return _radarPaintIndicatorPrefab;
            }
        }

        /// <summary>
        /// Brief ping on the painted enemy when Fire Control Radar acquires them — deliberately
        /// not the Engi lock ring Longbow uses. Prefer the Init-time clone (no shake / no rumble).
        /// </summary>
        public static GameObject RadarPaintPingEffect
        {
            get
            {
                if (radarPaintPingEffect)
                    return radarPaintPingEffect;

                return LoadLegacy("Prefabs/Effects/OmniImpactVFX");
            }
        }

        /// <summary>
        /// Load a vanilla prefab. <see cref="LegacyResourcesAPI"/> first — it is what Engi itself uses
        /// and what this project's tracers/pods already rely on. Addressables with the
        /// <c>Assets/RoR2/Base/...</c> catalog paths throw <c>InvalidKeyException</c> at runtime in this
        /// profile (verified in BepInEx LogOutput), so they are a last resort only.
        /// </summary>
        private static GameObject LoadLegacy(string legacyPath)
        {
            GameObject loaded = LegacyResourcesAPI.Load<GameObject>(legacyPath);
            return loaded;
        }

        /// <summary>
        /// M230 tracer — a warm-tinted clone of Commando's, never the vanilla prefab itself.
        ///
        /// <para>The stock tracers all read cold blue-white, which is what the "tracer is blue"
        /// playtest note was about. It cannot be fixed by tinting what <see cref="LoadVanilla"/>
        /// returns: that hands back the shared vanilla prefab, so recolouring it would repaint
        /// Commando's tracers for every player in the run.</para>
        ///
        /// <para><see cref="Modules.Asset.CloneTracer"/> gives us our own prefab and registers the
        /// EffectDef. Both the LineRenderer vertex colours <em>and</em> its material tint are set:
        /// which one the streak actually honours depends on the tracer's shader, so setting one
        /// alone silently does nothing on some of them.</para>
        /// </summary>
        private static GameObject CreateChaingunTracer()
        {
            //30mm API burns yellow-orange. Kept bright rather than saturated so it still reads
            //against Verdant Falls' daylight, not just against a night sky.
            //Pulled back twice on playtest 2026-08-03. 0.80/0.57/0.19 was still reported as
            //"way too intense" — and the gatling variant fires at ~18 rounds/sec against the
            //M230's 11, so the same tracer stacks nearly twice as densely. This is roughly
            //half the original brightness and a little over half the alpha.
            Color warm = new Color(0.52f, 0.36f, 0.12f, 0.55f);

            GameObject tracer = Modules.Asset.CloneTracer("TracerCommandoShotgun", "AH64ChaingunTracer")
                ?? Modules.Asset.CloneTracer("TracerCommandoDefault", "AH64ChaingunTracer");

            if (!tracer)
            {
                Log.Warning("Could not clone a vanilla tracer — falling back to the untinted vanilla prefab.");
                return LoadVanilla(
                    "Prefabs/Effects/Tracers/TracerCommandoShotgun",
                    "Prefabs/Effects/Tracers/TracerCommandoDefault");
            }

            foreach (LineRenderer line in tracer.GetComponentsInChildren<LineRenderer>(true))
            {
                line.startColor = warm;
                line.endColor = new Color(warm.r, warm.g * 0.6f, warm.b * 0.35f, 0f);

                //Instantiate before tinting: sharedMaterial here is still the vanilla asset.
                if (line.sharedMaterial)
                {
                    Material tinted = UnityEngine.Object.Instantiate(line.sharedMaterial);
                    if (tinted.HasProperty("_TintColor"))
                        tinted.SetColor("_TintColor", warm);
                    if (tinted.HasProperty("_Color"))
                        tinted.SetColor("_Color", warm);
                    line.material = tinted;
                }
            }

            TintVfxHierarchy(tracer, warm);
            return tracer;
        }

        /// <summary>
        /// Clone the vanilla footstep dust into an AH-64-owned, catalog-registered effect.
        ///
        /// <para><c>EffectManager.SpawnEffect</c> resolves its prefab through
        /// <c>EffectCatalog.FindEffectIndexFromPrefab</c> and, on a miss, logs
        /// "Unable to SpawnEffect from prefab named X" and spawns <em>nothing</em>. Verified in
        /// RoR2.dll. <c>GenericFootstepDust</c> is only ever instantiated directly by
        /// <c>FootstepHandler</c>, so it is not in the catalog — handing it to SpawnEffect was
        /// silently doing nothing while logging once per rotor-wash tick (532 lines in one run).</para>
        ///
        /// <para>Cloning also keeps us off the shared vanilla instance, the same hazard already
        /// documented on the tracer path: mutating it would change the effect for every character
        /// in the run.</para>
        /// </summary>
        private static GameObject CreateRegisteredDustEffect()
        {
            GameObject source = LoadVanilla(
                "Prefabs/GenericFootstepDust",
                "Assets/RoR2/Base/Common/VFX/Footstep/GenericFootstepDust.prefab",
                "Prefabs/GenericLargeFootstepDust");

            if (!source)
                return null;

            GameObject clone = PrefabAPI.InstantiateClone(source, "AH64RotorWash", false);

            if (!clone.GetComponent<UnityEngine.Networking.NetworkIdentity>())
                clone.AddComponent<UnityEngine.Networking.NetworkIdentity>();

            VFXAttributes vfx = clone.GetComponent<VFXAttributes>();
            if (!vfx)
                vfx = clone.AddComponent<VFXAttributes>();
            vfx.vfxPriority = VFXAttributes.VFXPriority.Medium;

            EffectComponent effect = clone.GetComponent<EffectComponent>();
            if (!effect)
                effect = clone.AddComponent<EffectComponent>();
            //the callers pass a scale to size the wash by ground proximity, so this must be on
            effect.applyScale = true;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = false;
            effect.positionAtReferencedTransform = false;

            Content.CreateAndAddEffectDef(clone);

            return clone;
        }

        private static GameObject LoadVanilla(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                GameObject loaded = null;

                if (path.StartsWith("Prefabs/", System.StringComparison.Ordinal)
                    || path.StartsWith("Shaders/", System.StringComparison.Ordinal))
                {
                    loaded = LoadLegacy(path);
                }
                else
                {
                    //Addressables last — log only once per miss via LoadAddressable
                    loaded = LoadAddressable(path);
                }

                if (loaded)
                    return loaded;
            }

            Log.Error($"Failed to load vanilla prefab. Tried: {string.Join(" | ", paths)}");
            return null;
        }

        private static GameObject LoadAddressable(string addressablePath)
        {
            // Optional fallback addresses may not exist in every game build. Check before
            // loading so a normal miss does not make Addressables emit a Unity exception.
            bool located = false;
            foreach (var locator in Addressables.ResourceLocators)
                if (locator.Locate(addressablePath, typeof(GameObject), out var locations)
                    && locations != null && locations.Count > 0)
                {
                    located = true;
                    break;
                }
            if (!located) return null;

            try
            {
                return Addressables.LoadAssetAsync<GameObject>(addressablePath).WaitForCompletion();
            }
            catch (System.Exception error)
            {
                Log.Error($"AH-64 could not load catalog asset {addressablePath}: {error}");
                throw;
            }
        }

        public static void Init(AssetBundle assetBundle)
        {
            _assetBundle = assetBundle;
            //effects must exist before projectiles: the rockets' impact explosions reference them
            CreateEffects();

            CreateProjectiles();
        }

        #region effects
        private static void CreateEffects()
        {
            LoadWeaponEffects();
            CreateRocketExplosionEffect();
            CreateChaingunSplashEffect();
            hydraExplosionEffect = CreateHydraExplosionEffect();
            CreateGatlingSplashEffect();
            CreateCannonSplashEffect();
            CreateLongbowExplosionEffect();
            radarPaintPingEffect = CreateRadarPaintPingEffect();
        }

        /// <summary>
        /// Presentation for every weapon slot. Paths are LegacyResourcesAPI <c>Prefabs/...</c> keys —
        /// the same API Engi and the rest of this mod already use successfully for tracers and pods.
        /// </summary>
        private static void LoadWeaponEffects()
        {
            //Warm tracers only — ClayBruiserMinigun reads purple/pink and was the "purple bloom" on M230.
            chaingunTracerEffect = CreateChaingunTracer();

            //Merc strong impact is energy-coloured; Hitspark reads as hot metal.
            chaingunHitEffect = LoadVanilla(
                "Prefabs/Effects/ImpactEffects/Hitspark1",
                "Prefabs/Effects/OmniImpactVFX");

            chaingunMuzzleFlashEffect = CreateChaingunMuzzleFlash();

            hydraMuzzleFlashEffect = LoadVanilla(SmokeRingPaths);
            //hydraExplosionEffect + chaingunSplashEffect: custom AH64*Explosion prefabs, after Hellfire loads.

            hellfireMuzzleFlashEffect = LoadWarmMuzzleFlash();
            if (!hellfireMuzzleFlashEffect)
                hellfireMuzzleFlashEffect = _assetBundle.LoadEffect("AH64HellfireMuzzleFlash", true);

            dashThrusterEffect = hydraMuzzleFlashEffect;

            //Must be built here, not lazily on first hover: an EffectDef is only picked up while the
            //ContentPack is still being assembled.
            _rotorWashEffect = CreateRegisteredDustEffect();
            dashDustEffect = _rotorWashEffect;

            dashFlareEffect = LoadVanilla(
                "Prefabs/Effects/ImpactEffects/ExplosionFirework",
                "Prefabs/Effects/ImpactEffects/CritsparkHeavy",
                "Prefabs/Effects/OmniExplosionVFX");
        }

        /// <summary>
        /// Commando's muzzle flash only. Catalog address verified the same way as the smoke ring:
        /// <c>RoR2/Base/Common/VFX/Muzzleflash1.prefab</c>. Its point light is pale yellow.
        ///
        /// <para><c>MuzzleflashFMJ</c> is not a fallback. Its light is blue (0.66, 0.75, 1) at
        /// intensity 22. <c>MuzzleflashBandit2</c> is not a casing puff either: the HitFlash
        /// sprite is magenta (1, 0.05, 0.74) and the point light is pink (1, 0.55, 0.97) with
        /// range 10. Spawning that on the chin every round is what washed the belly purple.
        /// <c>MuzzleflashBarrage</c> stays excluded for the same reason.</para>
        /// </summary>
        private static GameObject LoadWarmMuzzleFlash()
        {
            return LoadVanilla(
                "RoR2/Base/Common/VFX/Muzzleflash1.prefab",
                "Prefabs/Effects/MuzzleFlashes/Muzzleflash1");
        }

        /// <summary>
        /// The chain gun's muzzle flash with a tiny per-shot camera shake riding on it.
        ///
        /// <para>Cloned, not modified in place: adding a ShakeEmitter to a shared vanilla asset would
        /// hand camera shake to every user of that flash. A cloned prefab is not in the EffectCatalog and
        /// <c>EffectManager.SpawnEffect</c> refuses unregistered prefabs outright, hence the explicit
        /// <c>CreateAndAddEffectDef</c>.</para>
        /// </summary>
        private static GameObject CreateChaingunMuzzleFlash()
        {
            GameObject vanilla = LoadWarmMuzzleFlash();
            if (!vanilla)
            {
                Log.Error("AH64ChaingunMuzzleFlash: Muzzleflash1 did not load. Primary will fire without a barrel flash.");
                return null;
            }

            GameObject flash = PrefabAPI.InstantiateClone(vanilla, "AH64ChaingunMuzzleFlash", false);
            //Own the light colour on the clone. Muzzleflash1's authored light is pale yellow;
            //a warm orange reads as a gun flash against the olive belly and cannot go pink if
            //the donor prefab is ever swapped. Particle sprites stay on the Hopoo ramp —
            //white start colours are what that shader expects. Only a cool/magenta start
            //colour is rewritten, which is the Bandit2 HitFlash failure mode.
            WarmMuzzleFlashInstance(flash);

            ShakeEmitter shake = flash.AddComponent<ShakeEmitter>();
            shake.amplitudeTimeDecay = true;
            shake.duration = AH64StaticValues.chaingunShakeDuration;
            shake.radius = AH64StaticValues.chaingunShakeRadius;
            shake.scaleShakeRadiusWithLocalScale = false;
            shake.wave = new Wave
            {
                amplitude = AH64StaticValues.chaingunShakeAmplitude,
                frequency = AH64StaticValues.chaingunShakeFrequency,
                cycleOffset = 0f
            };

            Content.CreateAndAddEffectDef(flash);
            return flash;
        }

        //White-hot core through orange. Applied only to the AH-64 clone, never the shared vanilla prefab.
        private static readonly Color MuzzleFlashLight = new Color(1f, 0.62f, 0.18f, 1f);
        private static readonly Color MuzzleFlashHot = new Color(1f, 0.78f, 0.28f, 1f);

        /// <summary>
        /// Force the clone's point light to a warm muzzle colour, and replace a start colour
        /// that is itself magenta or blue. White start colours are left alone: Hopoo's flash
        /// shader uses them as "no tint" over the authored ramp.
        /// </summary>
        private static void WarmMuzzleFlashInstance(GameObject flash)
        {
            if (!flash)
                return;

            foreach (Light light in flash.GetComponentsInChildren<Light>(true))
                light.color = MuzzleFlashLight;

            foreach (ParticleSystem ps in flash.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                ParticleSystem.MinMaxGradient start = main.startColor;
                if (start.mode == ParticleSystemGradientMode.Color && IsCoolOrMagenta(start.color))
                    main.startColor = MuzzleFlashHot;
                else if (start.mode == ParticleSystemGradientMode.TwoColors
                    && (IsCoolOrMagenta(start.colorMin) || IsCoolOrMagenta(start.colorMax)))
                    main.startColor = new ParticleSystem.MinMaxGradient(Color.white, MuzzleFlashHot);
            }
        }

        private static bool IsCoolOrMagenta(Color color)
        {
            bool magenta = color.r > 0.45f && color.b > 0.45f && color.g < color.r * 0.75f && color.g < color.b;
            bool cool = color.b > color.r && color.b > color.g * 0.85f;
            return magenta || cool;
        }

        /// <summary>
        /// Soft acquire flash on the painted enemy. Avoid OmniImpactVFXLightning — its ShakeEmitter
        /// is what made lock feel like a combat hit.
        /// </summary>
        private static GameObject CreateRadarPaintPingEffect()
        {
            GameObject vanilla = LoadLegacy("Prefabs/Effects/OmniImpactVFX")
                ?? LoadLegacy("Prefabs/Effects/ImpactEffects/OmniImpactVFX")
                ?? LoadLegacy("Prefabs/Effects/ImpactEffects/BootShockwave");
            if (!vanilla)
            {
                Log.Error("AH64RadarPaintPing: no donor impact VFX found.");
                return null;
            }

            GameObject ping = PrefabAPI.InstantiateClone(vanilla, "AH64RadarPaintPing", false);
            StripEffectFeedback(ping);

            EffectComponent effect = ping.GetComponent<EffectComponent>();
            if (effect)
                effect.applyScale = true;

            Content.CreateAndAddEffectDef(ping);
            return ping;
        }

        /// <summary>
        /// ShakeEmitter drives both camera shake and <see cref="ShakeEmitter.ApplySpacialRumble"/>
        /// gamepad vibration. EffectComponent.soundName fires even when we play our own UI beep.
        /// </summary>
        private static void StripEffectFeedback(GameObject root)
        {
            if (!root)
                return;

            foreach (ShakeEmitter shake in root.GetComponentsInChildren<ShakeEmitter>(true))
                UnityEngine.Object.Destroy(shake);

            EffectComponent effect = root.GetComponent<EffectComponent>();
            if (effect)
                effect.soundName = string.Empty;
        }

        /// <summary>
        /// Hydra impact — custom <c>AH64HydraExplosion</c> from the bundle (orange blob burst at 0.55×).
        /// Falls back to a scaled Hellfire clone (never OmniImpact — that vanished in playtest).
        /// </summary>
        private static GameObject CreateHydraExplosionEffect()
        {
            GameObject fx = _assetBundle.LoadEffect("AH64HydraExplosion", "Play_engi_M1_explo");
            if (fx)
            {
                EffectComponent effect = fx.GetComponent<EffectComponent>();
                if (effect)
                    effect.applyScale = false;

                DestroyOnTimer timer = fx.GetComponent<DestroyOnTimer>();
                if (timer)
                    timer.duration = 1.4f;

                return fx;
            }

            Log.Warning("AH64HydraExplosion missing from bundle — falling back to scaled Hellfire warhead.");
            if (!hellfireExplosionEffect)
                return null;

            fx = PrefabAPI.InstantiateClone(hellfireExplosionEffect, "AH64HydraExplosionFallback", false);
            ScaleVfxHierarchy(fx, 0.28f);
            TintVfxHierarchy(fx, new Color(1f, 0.75f, 0.35f, 1f));

            ShakeEmitter[] shakes = fx.GetComponents<ShakeEmitter>();
            for (int i = 0; i < shakes.Length; i++)
                UnityEngine.Object.DestroyImmediate(shakes[i]);

            EffectComponent fallback = fx.GetComponent<EffectComponent>();
            if (fallback)
            {
                fallback.soundName = "Play_engi_M1_explo";
                fallback.applyScale = false;
            }

            fx.transform.localScale = Vector3.one * 0.55f;
            Content.CreateAndAddEffectDef(fx);
            return fx;
        }

        /// <summary>
        /// M789 cannon impact — the M230's composition at 1.15x, and the only primary impact
        /// allowed to linger. Silent here: the report is played by the skill state, and the
        /// blast timer is longer than the other two because the effect itself runs longer.
        /// </summary>
        private static void CreateCannonSplashEffect()
        {
            GameObject splash = _assetBundle.LoadEffect("AH64CannonExplosion", string.Empty);
            if (!splash)
            {
                Log.Warning("AH64CannonExplosion missing from bundle — reusing the M230 splash.");
                cannonSplashEffect = chaingunSplashEffect;
                return;
            }

            EffectComponent effect = splash.GetComponent<EffectComponent>();
            if (effect)
            {
                effect.soundName = string.Empty;
                effect.applyScale = true;
            }

            DestroyOnTimer timer = splash.GetComponent<DestroyOnTimer>();
            if (timer)
                timer.duration = 1.8f;

            cannonSplashEffect = splash;
        }

        /// <summary>
        /// XM301 gatling impact — the M230's splash composition at half size. Silent: the gun
        /// audio is a spool/loop set owned by <see cref="Components.AH64GatlingSpin"/>, and a
        /// per-impact one-shot on top of it at 18 rps would be mush.
        /// </summary>
        private static void CreateGatlingSplashEffect()
        {
            GameObject splash = _assetBundle.LoadEffect("AH64GatlingExplosion", string.Empty);
            if (!splash)
            {
                //Fall back to the M230's rather than dropping impact feedback entirely; it is
                //oversized for this gun but visible, which is the property that matters.
                Log.Warning("AH64GatlingExplosion missing from bundle — reusing the M230 splash.");
                gatlingSplashEffect = chaingunSplashEffect;
                return;
            }

            EffectComponent effect = splash.GetComponent<EffectComponent>();
            if (effect)
            {
                effect.soundName = string.Empty;
                effect.applyScale = true;
            }

            DestroyOnTimer timer = splash.GetComponent<DestroyOnTimer>();
            if (timer)
                timer.duration = 1.0f;

            gatlingSplashEffect = splash;
        }

        /// <summary>
        /// M230 HE tip — custom <c>AH64HeExplosion</c> (orange/yellow blob burst). Silent at 10 rps.
        /// </summary>
        private static void CreateChaingunSplashEffect()
        {
            GameObject splash = _assetBundle.LoadEffect("AH64HeExplosion", string.Empty);
            if (splash)
            {
                EffectComponent effect = splash.GetComponent<EffectComponent>();
                if (effect)
                {
                    effect.soundName = string.Empty;
                    effect.applyScale = true;
                }

                DestroyOnTimer timer = splash.GetComponent<DestroyOnTimer>();
                if (timer)
                    timer.duration = 1.2f;

                chaingunSplashEffect = splash;
                return;
            }

            Log.Warning("AH64HeExplosion missing from bundle — falling back to tinted Hellfire clone.");
            if (hellfireExplosionEffect)
            {
                splash = PrefabAPI.InstantiateClone(
                    hellfireExplosionEffect,
                    "AH64ChaingunSplash",
                    false);

                ScaleVfxHierarchy(splash, AH64StaticValues.chaingunSplashParticleMult);
                TintVfxHierarchy(splash, new Color(1f, 0.72f, 0.28f, 1f));

                ShakeEmitter[] shakes = splash.GetComponents<ShakeEmitter>();
                for (int i = 0; i < shakes.Length; i++)
                    UnityEngine.Object.DestroyImmediate(shakes[i]);

                EffectComponent effect = splash.GetComponent<EffectComponent>();
                if (effect)
                {
                    effect.soundName = string.Empty;
                    effect.applyScale = true;
                }

                Content.CreateAndAddEffectDef(splash);
                chaingunSplashEffect = splash;
                return;
            }

            chaingunSplashEffect = hydraExplosionEffect;
        }

        /// <summary>
        /// Clone a vanilla explosion. When <paramref name="ignoreBlastRadiusScale"/> is set,
        /// <c>EffectComponent.applyScale</c> is cleared and a fixed local scale is baked in — otherwise
        /// <c>ProjectileExplosion</c> would stretch the FX to <c>blastRadius</c> (Hydra = 5 → debris storm).
        /// Particle start sizes are always multiplied: many RoR2 explosion systems simulate in world
        /// space, so transform scale alone does almost nothing.
        /// </summary>
        private static GameObject CreateScaledExplosionEffect(
            string cloneName,
            float particleMult,
            bool ignoreBlastRadiusScale,
            float fixedScale,
            params string[] legacyPaths)
        {
            GameObject vanilla = LoadVanilla(legacyPaths);
            if (!vanilla)
                return null;

            GameObject clone = PrefabAPI.InstantiateClone(vanilla, cloneName, false);
            ScaleVfxHierarchy(clone, particleMult);
            StripDebrisChildren(clone);

            EffectComponent effect = clone.GetComponent<EffectComponent>();
            if (!effect)
                effect = clone.AddComponent<EffectComponent>();

            if (ignoreBlastRadiusScale)
            {
                effect.applyScale = false;
                clone.transform.localScale = Vector3.one * Mathf.Max(fixedScale, 0.01f);
            }
            else
            {
                //Chaingun path: EffectData.scale from the hit callback must apply.
                effect.applyScale = true;
            }

            Content.CreateAndAddEffectDef(clone);
            return clone;
        }

        private static void ScaleVfxHierarchy(GameObject root, float mult)
        {
            if (!root || Mathf.Approximately(mult, 1f))
                return;

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                main.startSizeMultiplier *= mult;
                main.startSpeedMultiplier *= mult;
            }

            foreach (TrailRenderer trail in root.GetComponentsInChildren<TrailRenderer>(true))
            {
                trail.startWidth *= mult;
                trail.endWidth *= mult;
            }
        }

        private static void TintVfxHierarchy(GameObject root, Color tint)
        {
            if (!root)
                return;

            foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint);
            }
        }

        /// <summary>
        /// ExplosionMissile (and cousins) ship mesh-debris / spark / flare children. Disable by name
        /// so Hydra keeps flash+smoke without a lingering rubble / firework field.
        /// </summary>
        private static void StripDebrisChildren(GameObject root)
        {
            if (!root)
                return;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (!child || child == root.transform)
                    continue;

                string name = child.name;
                if (name.IndexOf("debris", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("rubble", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("rock", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("chunk", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("shard", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("flare", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("spark", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("ember", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("firework", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("shrapnel", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("fragment", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("gravel", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                child.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hellfire-sized bazooka ghost, scaled down for Hydra so the smoke trail doesn't read as a
        /// special-grade missile.
        /// </summary>
        private static GameObject CreateHydraRocketGhost()
        {
            if (_assetBundle.LoadAsset<GameObject>("AH64HellfireGhost") == null)
                return null;

            //CreateProjectileGhostPrefab mutates the bundle asset — clone first so Hellfire keeps full size.
            GameObject source = _assetBundle.CreateProjectileGhostPrefab("AH64HellfireGhost");
            if (!source)
                return null;

            GameObject ghost = PrefabAPI.InstantiateClone(source, "AH64HydraRocketGhost", false);
            float scale = AH64StaticValues.hydraGhostScale;
            ghost.transform.localScale = Vector3.one * scale;
            ScaleVfxHierarchy(ghost, scale);
            return ghost;
        }

        private static void CreateRocketExplosionEffect()
        {
            //Clay Dunestrider heavy blast — Global bank, always loaded (no custom soundbank).
            hellfireExplosionEffect = _assetBundle.LoadEffect("AH64HellfireExplosion", "Play_clayboss_M1_explo");

            if (!hellfireExplosionEffect)
                return;

            ShakeEmitter shakeEmitter = hellfireExplosionEffect.AddComponent<ShakeEmitter>();
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.duration = 0.5f;
            shakeEmitter.radius = 200f;
            shakeEmitter.scaleShakeRadiusWithLocalScale = false;

            shakeEmitter.wave = new Wave
            {
                amplitude = 1f,
                frequency = 40f,
                cycleOffset = 0f
            };
        }

        /// <summary>
        /// Longbow is the default special but inherited EngiHarpoon's tiny seeker pop — no readable boom
        /// or thump from hover distance. Clone the Hellfire warhead VFX and pin a loud Global-bank
        /// grenade boom onto the EffectDef (lifetimeExpiredSound only plays near lifetime end, not on hit).
        /// </summary>
        private static void CreateLongbowExplosionEffect()
        {
            const string longbowBoomSound = "Play_commando_M2_grenade_explo";

            if (hellfireExplosionEffect)
            {
                longbowExplosionEffect = PrefabAPI.InstantiateClone(
                    hellfireExplosionEffect,
                    "AH64LongbowExplosion",
                    false);

                EffectComponent effect = longbowExplosionEffect.GetComponent<EffectComponent>();
                if (effect)
                    effect.soundName = longbowBoomSound;

                ShakeEmitter shake = longbowExplosionEffect.GetComponent<ShakeEmitter>();
                if (shake)
                {
                    shake.duration = 0.40f;
                    shake.wave = new Wave
                    {
                        amplitude = 0.75f,
                        frequency = 36f,
                        cycleOffset = 0f
                    };
                }

                Content.CreateAndAddEffectDef(longbowExplosionEffect);
                return;
            }

            longbowExplosionEffect = CreateScaledExplosionEffect(
                "AH64LongbowExplosion",
                2.4f,
                ignoreBlastRadiusScale: true,
                1.35f,
                "Prefabs/Effects/ImpactEffects/ExplosionFirework",
                "Prefabs/Effects/OmniExplosionVFX",
                "Prefabs/Effects/OmniImpactVFX");
            if (!longbowExplosionEffect)
                return;

            EffectComponent fallback = longbowExplosionEffect.GetComponent<EffectComponent>();
            if (fallback)
                fallback.soundName = longbowBoomSound;

            ShakeEmitter fallbackShake = longbowExplosionEffect.AddComponent<ShakeEmitter>();
            fallbackShake.amplitudeTimeDecay = true;
            fallbackShake.duration = 0.40f;
            fallbackShake.radius = 160f;
            fallbackShake.scaleShakeRadiusWithLocalScale = false;
            fallbackShake.wave = new Wave
            {
                amplitude = 0.75f,
                frequency = 36f,
                cycleOffset = 0f
            };
        }
        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateHellfireProjectile();

            if (hellfireProjectilePrefab)
                Content.AddProjectilePrefab(hellfireProjectilePrefab);

            CreateLongbowProjectile();

            if (_longbowProjectile)
                Content.AddProjectilePrefab(_longbowProjectile);

            CreateHydraRocketProjectile();

            if (hydraRocketProjectilePrefab)
                Content.AddProjectilePrefab(hydraRocketProjectilePrefab);
        }

        /// <summary>
        /// Preserve Engineer harpoon guidance while giving Longbow the AH-64 missile silhouette and a
        /// real AGM warhead boom. Cloning avoids changing Engineer's shared projectile for every player.
        /// </summary>
        private static void CreateLongbowProjectile()
        {
            GameObject source = LoadLegacy("Prefabs/Projectiles/EngiHarpoon");
            if (!source)
                source = EntityStates.Engi.EngiMissilePainter.Fire.projectilePrefab;
            if (!source)
            {
                Log.Error("AH64LongbowProjectile: EngiHarpoon source was not available.");
                return;
            }

            _longbowProjectile = PrefabAPI.InstantiateClone(
                source,
                "AH64LongbowProjectile",
                false);

            ProjectileController controller = _longbowProjectile
                ? _longbowProjectile.GetComponent<ProjectileController>()
                : null;
            ProjectileController hellfireController = hellfireProjectilePrefab
                ? hellfireProjectilePrefab.GetComponent<ProjectileController>()
                : null;

            if (controller && hellfireController && hellfireController.ghostPrefab)
                controller.ghostPrefab = hellfireController.ghostPrefab;
            if (controller)
                controller.startSound = string.Empty;

            //MissileUtils.FireMissile takes no damage type, so the special tag the Hellfire gets from
            //damageTypeOverride has to live on the prefab. Only the source changes; flags stay Engi's.
            ProjectileDamage projectileDamage = _longbowProjectile
                ? _longbowProjectile.GetComponent<ProjectileDamage>()
                : null;
            if (projectileDamage)
                projectileDamage.damageType.damageSource = DamageSource.Special;

            //EngiHarpoon's stock impact is a quiet seeker pop — invisible/inaudible from hover cam.
            //DestroyImmediate: deferred Destroy left Engi's ProjectileImpactExplosion alive on the
            //prefab beside ours, so hits kept the tiny silent Engi detonation.
            ProjectileImpactExplosion[] oldExplosions =
                _longbowProjectile.GetComponents<ProjectileImpactExplosion>();
            for (int i = 0; i < oldExplosions.Length; i++)
            {
                if (oldExplosions[i])
                    UnityEngine.Object.DestroyImmediate(oldExplosions[i]);
            }

            ProjectileSingleTargetImpact singleTarget =
                _longbowProjectile.GetComponent<ProjectileSingleTargetImpact>();
            if (singleTarget)
                UnityEngine.Object.DestroyImmediate(singleTarget);

            ProjectileImpactExplosion explosion = _longbowProjectile.AddComponent<ProjectileImpactExplosion>();
            explosion.blastRadius = AH64StaticValues.longbowBlastRadius;
            explosion.blastDamageCoefficient = 1f;
            explosion.blastProcCoefficient = 1f;
            explosion.falloffModel = BlastAttack.FalloffModel.Linear;
            explosion.destroyOnEnemy = true;
            explosion.destroyOnWorld = true;
            explosion.timerAfterImpact = false;
            explosion.fireChildren = false;
            explosion.explodeOnLifeTimeExpiration = true;
            explosion.impactEffect = longbowExplosionEffect
                ? longbowExplosionEffect
                : hellfireExplosionEffect;
            explosion.blastImpactEffect = null;
            //lifetimeExpiredSound only fires near lifetime end — impact boom is EffectComponent.soundName
            //on longbowExplosionEffect (Play_commando_M2_grenade_explo). Keep this as a backup thump.
            explosion.lifetimeExpiredSound =
                Content.CreateAndAddNetworkSoundEventDef("Play_commando_M2_grenade_explo");
            explosion.offsetForLifetimeExpiredSound = 0f;

            if (explosion.lifetime <= 0f)
                explosion.lifetime = 15f;
        }

        /// <summary>
        /// Shared setup for both rockets: strip the cloned grenade's arc and tumble and drive it forwards
        /// at a constant speed instead. Returns null if the clone failed, so callers can bail loudly.
        /// </summary>
        private static GameObject CreateFlatFlyingRocket(string newPrefabName, float speed, float lifetime)
        {
            GameObject prefab = Asset.CloneProjectilePrefab("CommandoGrenadeProjectile", newPrefabName);

            if (!prefab)
            {
                Log.Error($"Failed to clone CommandoGrenadeProjectile for {newPrefabName}. That skill will not fire.");
                return null;
            }

            //the grenade we cloned arcs and tumbles. a rocket flies flat and fast, so strip the gravity
            //and the spin, and drive it forwards at a constant speed instead
            Rigidbody rigidbody = prefab.GetComponent<Rigidbody>();
            if (rigidbody)
            {
                //useGravity is serialized so it sticks to the clone. don't bother zeroing angularVelocity
                //here — that's runtime state on a prefab we never instantiate, so it wouldn't carry over.
                //ApplyTorqueOnStart below is what actually makes the grenade tumble.
                rigidbody.useGravity = false;
            }

            ApplyTorqueOnStart tumble = prefab.GetComponent<ApplyTorqueOnStart>();
            if (tumble)
                UnityEngine.Object.Destroy(tumble);

            ProjectileSimple flight = prefab.GetComponent<ProjectileSimple>();
            if (!flight)
                flight = prefab.AddComponent<ProjectileSimple>();

            flight.desiredForwardSpeed = speed;
            flight.lifetime = lifetime;
            flight.updateAfterFiring = true;

            return prefab;
        }

        /// <summary>
        /// The special: one heavy anti-armour missile. Same warhead the wrist rocket carried — the shot
        /// was already the right weight, it was just called the wrong thing and left from the wrong place.
        /// </summary>
        private static void CreateHellfireProjectile()
        {
            hellfireProjectilePrefab = CreateFlatFlyingRocket(
                "AH64HellfireProjectile",
                AH64StaticValues.hellfireSpeed,
                AH64StaticValues.hellfireLifetime);

            if (!hellfireProjectilePrefab)
                return;

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(hellfireProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion explosion = hellfireProjectilePrefab.AddComponent<ProjectileImpactExplosion>();

            explosion.blastRadius = AH64StaticValues.hellfireBlastRadius;
            explosion.blastDamageCoefficient = 1f;
            explosion.falloffModel = BlastAttack.FalloffModel.None;
            explosion.destroyOnEnemy = true;
            explosion.destroyOnWorld = true;
            explosion.lifetime = AH64StaticValues.hellfireLifetime;
            explosion.impactEffect = hellfireExplosionEffect;
            explosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("Play_clayboss_M1_explo");
            //detonate on contact rather than sitting as a live charge
            explosion.timerAfterImpact = false;

            ProjectileController controller = hellfireProjectilePrefab.GetComponent<ProjectileController>();

            if (_assetBundle.LoadAsset<GameObject>("AH64HellfireGhost") != null)
                controller.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("AH64HellfireGhost");

            controller.startSound = "";
            //same hazard as the Hydra rocket: the Commando grenade clone's procCoefficient must not
            //be left inherited. Set explicitly so on-hit items see the special's intended weight.
            controller.procCoefficient = AH64StaticValues.hellfireProcCoefficient;
        }

        /// <summary>
        /// The secondary: one rocket out of the Hydra-70 pods. Six of these go out per activation, so it
        /// is deliberately a much smaller warhead than the Hellfire — six overlapping 12u blasts would
        /// fill the screen and delete the distinction between the two skills.
        /// </summary>
        private static void CreateHydraRocketProjectile()
        {
            hydraRocketProjectilePrefab = CreateFlatFlyingRocket(
                "AH64HydraRocketProjectile",
                AH64StaticValues.hydraSpeed,
                AH64StaticValues.hydraLifetime);

            if (!hydraRocketProjectilePrefab)
                return;

            UnityEngine.Object.Destroy(hydraRocketProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion explosion = hydraRocketProjectilePrefab.AddComponent<ProjectileImpactExplosion>();

            explosion.blastRadius = AH64StaticValues.hydraBlastRadius;
            explosion.blastDamageCoefficient = 1f;
            explosion.falloffModel = BlastAttack.FalloffModel.None;
            explosion.destroyOnEnemy = true;
            explosion.destroyOnWorld = true;
            explosion.lifetime = AH64StaticValues.hydraLifetime;
            //own smaller missile blast — sharing the Hellfire's artillery explosion made every Hydra
            //hit read as another special
            //Never fall back to Hellfire's artillery boom — that was the "Hydra still huge" report.
            if (!hydraExplosionEffect)
                Log.Error("AH64HydraExplosion failed to build; Hydra impacts will have no VFX.");
            explosion.impactEffect = hydraExplosionEffect;
            explosion.blastImpactEffect = null;
            explosion.fireChildren = false;
            explosion.timerAfterImpact = false;

            ProjectileController controller = hydraRocketProjectilePrefab.GetComponent<ProjectileController>();

            GameObject hydraGhost = CreateHydraRocketGhost();
            if (hydraGhost)
                controller.ghostPrefab = hydraGhost;
            else if (_assetBundle.LoadAsset<GameObject>("AH64HellfireGhost") != null)
                controller.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("AH64HellfireGhost");

            controller.startSound = "";
            //carried over explicitly rather than inherited from the grenade — six rockets land per
            //activation, so leaving this at the clone's value would quietly change how every on-hit item
            //behaves around the secondary
            controller.procCoefficient = AH64StaticValues.hydraProcCoefficient;

            //Server-side distance ramp. Added in code so the projectile prefab, which is cloned at
            //runtime, does not need a Unity asset rebuild.
            if (!hydraRocketProjectilePrefab.GetComponent<Components.AH64HydraRangeRamp>())
                hydraRocketProjectilePrefab.AddComponent<Components.AH64HydraRangeRamp>();
        }
        #endregion projectiles
    }
}
