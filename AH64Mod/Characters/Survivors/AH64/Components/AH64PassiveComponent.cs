using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Lives on the AH64 body prefab. Two jobs:
    ///
    /// <para>1. Marks a CharacterBody as ours so <c>RecalculateStatsAPI</c> / damage hooks can early-out.</para>
    ///
    /// <para>2. Owns the <b>Fire Control Radar</b> passive: periodically paints the strongest enemy in
    /// range (highest maxHealth; elite then nearest on ties), grants facing move speed and close-range
    /// armor via the survivor stat hook, and amplifies damage we deal to the painted target. Scan
    /// pulses emit from the <c>RadarDome</c> mesh on every retarget; the painted enemy gets a red
    /// Huntress-style marker, a buff icon, a HUD "TARGET ACQUIRED" toast, and a ping VFX distinct
    /// from Longbow's Engi rings.</para>
    /// </summary>
    internal class AH64PassiveComponent : MonoBehaviour
    {
        private static readonly int EmColorId = Shader.PropertyToID("_EmColor");
        private static readonly int EmPowerId = Shader.PropertyToID("_EmPower");

        public CharacterBody PaintedBody { get; private set; }

        public bool FacingPainted { get; private set; }
        public bool CloseToPainted { get; private set; }

        private CharacterBody body;
        private TeamComponent teamComponent;
        private InputBankTest inputBank;
        private CharacterDirection characterDirection;
        private BullseyeSearch search;
        private Indicator paintIndicator;
        private EntityStateMachine weapon2Machine;
        private Transform radarDome;
        private Renderer radarDomeRenderer;
        private MaterialPropertyBlock domeBlock;
        private Color domeEmColor = new Color(0.03f, 0.06f, 0.07f);
        private float retargetStopwatch;
        private bool lastFacing;
        private bool lastClose;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
            teamComponent = GetComponent<TeamComponent>();
            inputBank = GetComponent<InputBankTest>();
            characterDirection = GetComponent<CharacterDirection>();
            search = new BullseyeSearch();
            weapon2Machine = EntityStateMachine.FindByCustomName(gameObject, "Weapon2");
            domeBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            ResolveRadarDome();

            //Red Huntress tracker — not the Engi ring Longbow uses
            if (AH64Assets.RadarPaintIndicatorPrefab)
                paintIndicator = new RadarPaintIndicator(gameObject, AH64Assets.RadarPaintIndicatorPrefab);

            //scan immediately so the passive is visible without waiting a full retarget cycle
            retargetStopwatch = AH64StaticValues.radarRetargetInterval * 0.85f;
        }

        private void OnEnable()
        {
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        private void OnDisable()
        {
            On.RoR2.HealthComponent.TakeDamage -= HealthComponent_TakeDamage;
            ClearPaint();
        }

        private void OnDestroy()
        {
            ClearPaint();
        }

        private void Update()
        {
            UpdateDomeVisuals();
        }

        private void FixedUpdate()
        {
            if (!body)
                return;

            retargetStopwatch += Time.fixedDeltaTime;
            if (retargetStopwatch >= AH64StaticValues.radarRetargetInterval)
            {
                retargetStopwatch = 0f;
                Retarget();
            }
            else if (PaintedBody && (!PaintedBody.healthComponent || !PaintedBody.healthComponent.alive))
            {
                ClearPaint();
            }

            UpdateFacingAndClose();
            UpdatePaintVisibility();
        }

        private void ResolveRadarDome()
        {
            ModelLocator modelLocator = GetComponent<ModelLocator>();
            Transform model = modelLocator ? modelLocator.modelTransform : null;
            ChildLocator locator = model ? model.GetComponent<ChildLocator>() : null;
            if (!locator)
                return;

            radarDome = locator.FindChild("RadarDome");
            if (radarDome)
            {
                radarDomeRenderer = radarDome.GetComponent<Renderer>();
                return;
            }

            //fallback until the FBX/ChildLocator ships RadarDome: sit on Head / mast-ish Head empty
            Transform head = locator.FindChild("Head");
            if (!head)
                head = model;

            GameObject dome = new GameObject("RadarDome");
            dome.transform.SetParent(head ? head : model, false);
            //mast sits above the cabin; Head is already high — nudge up a touch
            dome.transform.localPosition = head ? new Vector3(0f, 0.35f, 0f) : new Vector3(0f, 1.6f, 0.2f);
            radarDome = dome.transform;
        }

        private void UpdateDomeVisuals()
        {
            if (!radarDome)
                return;

            bool painted = PaintedBody;
            float spin = painted
                ? AH64StaticValues.radarDomePaintedSpinSpeed
                : AH64StaticValues.radarDomeIdleSpinSpeed;
            radarDome.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);

            if (!radarDomeRenderer)
                return;

            float glow = painted
                ? AH64StaticValues.radarDomeGlowPainted
                : AH64StaticValues.radarDomeGlowIdle;
            //subtle breathe so the dome never looks static even between scans
            glow *= 0.85f + 0.15f * (0.5f + 0.5f * Mathf.Sin(Time.time * (painted ? 8f : 3f)));

            radarDomeRenderer.GetPropertyBlock(domeBlock);
            domeBlock.SetColor(EmColorId, domeEmColor * glow);
            domeBlock.SetFloat(EmPowerId, glow);
            radarDomeRenderer.SetPropertyBlock(domeBlock);
        }

        private void Retarget()
        {
            TeamIndex team = teamComponent ? teamComponent.teamIndex : TeamIndex.None;

            search.filterByDistinctEntity = true;
            search.filterByLoS = false;
            search.minDistanceFilter = 0f;
            search.maxDistanceFilter = AH64StaticValues.radarSearchRadius;
            search.minAngleFilter = 0f;
            search.maxAngleFilter = 180f;
            search.viewer = body;
            search.searchOrigin = body.corePosition;
            search.searchDirection = Vector3.forward;
            search.sortMode = BullseyeSearch.SortMode.Distance;
            search.teamMaskFilter = TeamMask.GetUnprotectedTeams(team);
            search.RefreshCandidates();
            search.FilterOutGameObject(gameObject);

            HurtBox best = null;
            float bestMaxHealth = -1f;
            bool bestElite = false;
            float bestDistSq = float.MaxValue;

            foreach (HurtBox hurtBox in search.GetResults())
            {
                if (!hurtBox || !hurtBox.healthComponent || !hurtBox.healthComponent.alive)
                    continue;

                CharacterBody candidate = hurtBox.healthComponent.body;
                if (!candidate)
                    continue;

                float maxHealth = candidate.healthComponent.fullCombinedHealth;
                bool elite = candidate.isElite || candidate.isBoss;
                float distSq = (candidate.corePosition - body.corePosition).sqrMagnitude;

                bool better = false;
                if (maxHealth > bestMaxHealth + 0.5f)
                    better = true;
                else if (Mathf.Abs(maxHealth - bestMaxHealth) <= 0.5f)
                {
                    if (elite && !bestElite)
                        better = true;
                    else if (elite == bestElite && distSq < bestDistSq)
                        better = true;
                }

                if (!better)
                    continue;

                best = hurtBox;
                bestMaxHealth = maxHealth;
                bestElite = elite;
                bestDistSq = distSq;
            }

            //always pulse from the dome on a scan tick — even if nothing is found
            PlayScanPulse();

            if (!best)
            {
                ClearPaint();
                return;
            }

            CharacterBody next = best.healthComponent.body;
            bool changed = next != PaintedBody;
            if (changed)
            {
                RemovePaintBuff(PaintedBody);
                PaintedBody = next;
                ApplyPaintBuff(PaintedBody);
                PlayPaintPing(PaintedBody);
                ShowTargetAcquiredNotification(PaintedBody);
                body.MarkAllStatsDirty();
            }

            RefreshIndicator();
        }

        private void RefreshIndicator()
        {
            if (paintIndicator == null)
                return;

            if (PaintedBody && !IsLongbowTargeting())
            {
                paintIndicator.targetTransform = PaintedBody.mainHurtBox
                    ? PaintedBody.mainHurtBox.transform
                    : PaintedBody.transform;
                paintIndicator.active = true;
            }
            else
            {
                paintIndicator.active = false;
            }
        }

        /// <summary>
        /// Hide the Huntress-style radar paint while Longbow's Engi rings are up so the two systems
        /// never stack on the same enemy.
        /// </summary>
        private void UpdatePaintVisibility()
        {
            if (paintIndicator == null)
                return;

            if (IsLongbowTargeting())
            {
                paintIndicator.active = false;
                return;
            }

            if (PaintedBody)
                RefreshIndicator();
        }

        private bool IsLongbowTargeting()
        {
            if (!weapon2Machine || weapon2Machine.state == null)
                return false;

            System.Type stateType = weapon2Machine.state.GetType();
            return stateType == typeof(SkillStates.PaintLongbow)
                || stateType == typeof(SkillStates.FireLongbow);
        }

        private void ClearPaint()
        {
            if (PaintedBody != null)
            {
                RemovePaintBuff(PaintedBody);
                PaintedBody = null;
                if (body)
                    body.MarkAllStatsDirty();
            }

            if (paintIndicator != null)
                paintIndicator.active = false;

            FacingPainted = false;
            CloseToPainted = false;
            lastFacing = false;
            lastClose = false;
        }

        private void ApplyPaintBuff(CharacterBody target)
        {
            if (!NetworkServer.active || !target || !AH64Buffs.radarPaintedBuff)
                return;
            if (!target.HasBuff(AH64Buffs.radarPaintedBuff))
                target.AddBuff(AH64Buffs.radarPaintedBuff);
        }

        private void RemovePaintBuff(CharacterBody target)
        {
            if (!NetworkServer.active || !target || !AH64Buffs.radarPaintedBuff)
                return;
            if (target.HasBuff(AH64Buffs.radarPaintedBuff))
                target.RemoveBuff(AH64Buffs.radarPaintedBuff);
        }

        private void UpdateFacingAndClose()
        {
            bool facing = false;
            bool close = false;

            if (PaintedBody && body)
            {
                Vector3 toTarget = PaintedBody.corePosition - body.corePosition;
                float distSq = toTarget.sqrMagnitude;
                close = distSq <= AH64StaticValues.radarCloseRange * AH64StaticValues.radarCloseRange;

                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    toTarget.Normalize();
                    Vector3 aim = inputBank ? inputBank.aimDirection : (characterDirection ? characterDirection.forward : transform.forward);
                    aim.y = 0f;
                    if (aim.sqrMagnitude > 0.0001f)
                    {
                        aim.Normalize();
                        facing = Vector3.Dot(aim, toTarget) >= AH64StaticValues.radarFacingDotMin;
                    }
                }
            }

            FacingPainted = facing;
            CloseToPainted = close;

            if (facing != lastFacing || close != lastClose)
            {
                lastFacing = facing;
                lastClose = close;
                if (body)
                    body.MarkAllStatsDirty();
            }
        }

        private void PlayScanPulse()
        {
            GameObject pulse = AH64Assets.RadarPulseEffect;
            if (!pulse)
                return;

            Vector3 origin = radarDome
                ? radarDome.position
                : (body ? body.corePosition + Vector3.up * 1.5f : transform.position);

            EffectData data = new EffectData
            {
                origin = origin,
                scale = AH64StaticValues.radarPulseScale,
                color = AH64Assets.RadarEffectColor,
            };

            //AH64RadarPulse has parentToReferencedTransform — attach so the ring rides with the airframe
            //instead of freezing where the scan started.
            if (radarDome)
            {
                ModelLocator modelLocator = GetComponent<ModelLocator>();
                ChildLocator locator = modelLocator && modelLocator.modelTransform
                    ? modelLocator.modelTransform.GetComponent<ChildLocator>()
                    : null;
                int domeIndex = locator ? locator.FindChildIndex("RadarDome") : -1;
                if (domeIndex >= 0)
                    data.SetChildLocatorTransformReference(gameObject, domeIndex);
                else
                    data.SetNetworkedObjectReference(radarDome.gameObject);
            }

            EffectManager.SpawnEffect(pulse, data, false);

            //Faintest vanilla UI tick (~30ms). Scan runs every retarget — anything louder fatigues.
            Util.PlaySound("Play_UI_menuHover", gameObject);
        }

        private void PlayPaintPing(CharacterBody target)
        {
            if (!target)
                return;

            GameObject ping = AH64Assets.RadarPaintPingEffect;
            if (!ping)
                return;

            EffectManager.SpawnEffect(ping, new EffectData
            {
                origin = target.corePosition,
                scale = AH64StaticValues.radarPaintPingScale,
                color = AH64Assets.RadarEffectColor,
            }, false);

            //Soft XP chime — not extractorUnit_lockon (~1s + combat feel) or Engi's seeker lock.
            Util.PlaySound("Play_UI_xp_gain", gameObject);
        }

        private void ShowTargetAcquiredNotification(CharacterBody target)
        {
            //Local-only chat line — deliberately not CharacterMasterNotificationQueue (that splash
            //is the item-pickup banner with a big Scanner icon). Chat stays readable without owning HUD.
            if (!body || !body.master || !body.master.hasAuthority || !target)
                return;

            string title = Language.GetString(AH64Survivor.AH64_PREFIX + "RADAR_TARGET_ACQUIRED");
            string enemyName = Util.GetBestBodyName(target.gameObject);
            if (string.IsNullOrEmpty(enemyName))
                enemyName = Language.GetString(AH64Survivor.AH64_PREFIX + "RADAR_TARGET_ACQUIRED_DESC");

            Chat.AddMessage(
                $"<color=#{ColorUtility.ToHtmlStringRGB(RadarPaintIndicator.PaintTint)}>{title}</color> — {enemyName}");
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            //early-out before any AH64 work — this hook runs for every damage instance in the run
            if (NetworkServer.active
                && damageInfo != null
                && damageInfo.attacker
                && damageInfo.attacker == gameObject
                && PaintedBody
                && self
                && self.body == PaintedBody
                && damageInfo.damage > 0f)
            {
                damageInfo.damage *= AH64StaticValues.radarPaintedDamageMult;
            }

            orig(self, damageInfo);
        }
    }
}
