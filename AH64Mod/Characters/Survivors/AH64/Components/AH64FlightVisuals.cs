using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Presentation-only flight attitude: banks into strafe, pitches with forward/climb speed, barrel
    /// rolls on utility, and kicks up rotor wash near the ground.
    ///
    /// <para><b>We own the model transform.</b> <see cref="ModelLocator"/> normally copies
    /// ModelBase's rotation onto <c>mdlAH64</c> every LateUpdate, which wipes pitch/roll. An
    /// intermediate AttitudePivot child would keep lean, but SkinDef mesh replacement uses
    /// <c>Transform.Find("Airframe")</c> (direct children only) — reparenting made the body
    /// invisible. So we disable <c>autoUpdateModelTransform</c> and sync position + yaw*lean
    /// ourselves, leaving the mesh hierarchy intact.</para>
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class AH64FlightVisuals : MonoBehaviour
    {
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");

        private CharacterMotor motor;
        private CharacterDirection characterDirection;
        private ModelLocator modelLocator;
        private AH64HoverController hover;
        private InputBankTest inputBank;
        private CharacterBody body;
        private CharacterModel characterModel;
        private Transform model;
        private Transform modelBase;
        private float washCooldown;

        //rotor blur discs. Shipped inactive on mdlAH64; this component activates and fades them.
        //They are deliberately not CharacterModel renderers, so writing their alpha through a
        //MaterialPropertyBlock can never be stomped by UpdateRendererMaterials.
        private Renderer blurMain;
        private Renderer blurTail;
        private MaterialPropertyBlock blurBlock;
        private Color blurTint;
        private float blurEffort;

        //procedural barrel roll from Evasive Roll — overrides normal lean while active
        private float barrelRollSign;
        private float barrelRollTimer;
        private float barrelRollDuration;

        //procedural pitch flip from Smoke Backflip — mutually exclusive with barrel roll
        private float backflipTimer;
        private float backflipDuration;
        private Quaternion leanLocal = Quaternion.identity;

        /// <summary>
        /// Full ~360° barrel roll for Evasive Roll. <paramref name="rollSign"/> is ±1 (left / right).
        /// </summary>
        public void PlayBarrelRoll(float rollSign, float duration)
        {
            EnsureModelRefs();

            backflipTimer = 0f;
            barrelRollSign = Mathf.Sign(rollSign) >= 0f ? 1f : -1f;
            barrelRollDuration = Mathf.Max(duration, 0.01f);
            barrelRollTimer = barrelRollDuration;
            leanLocal = Quaternion.identity;
        }

        /// <summary>
        /// Full ~360° pitch backflip for the Smoke Backflip utility variant.
        /// </summary>
        public void PlayBackflip(float duration)
        {
            EnsureModelRefs();

            barrelRollTimer = 0f;
            backflipDuration = Mathf.Max(duration, 0.01f);
            backflipTimer = backflipDuration;
            leanLocal = Quaternion.identity;
        }

        private void Start()
        {
            motor = GetComponent<CharacterMotor>();
            characterDirection = GetComponent<CharacterDirection>();
            modelLocator = GetComponent<ModelLocator>();
            hover = GetComponent<AH64HoverController>();
            inputBank = GetComponent<InputBankTest>();
            body = GetComponent<CharacterBody>();

            EnsureModelRefs();
            if (!model)
                return;

            //stop ModelLocator from wiping pitch/roll every LateUpdate
            modelLocator.autoUpdateModelTransform = false;

            ChildLocator locator = model.GetComponent<ChildLocator>();
            if (locator)
            {
                blurMain = BindBlurDisc(locator, "RotorBlurMain");
                blurTail = BindBlurDisc(locator, "RotorBlurTail");
            }
        }

        private void OnDestroy()
        {
            //hand the model back if we're torn down mid-run
            if (modelLocator)
                modelLocator.autoUpdateModelTransform = true;
        }

        private void EnsureModelRefs()
        {
            if (!modelLocator)
                modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator)
                return;

            model = modelLocator.modelTransform;
            modelBase = modelLocator.modelBaseTransform;
            characterModel = model ? model.GetComponent<CharacterModel>() : null;
        }

        private Renderer BindBlurDisc(ChildLocator locator, string childName)
        {
            Transform disc = locator.FindChild(childName);
            Renderer renderer = disc ? disc.GetComponent<Renderer>() : null;
            if (!renderer)
                return null;

            if (blurBlock == null)
            {
                blurBlock = new MaterialPropertyBlock();
                blurTint = renderer.sharedMaterial && renderer.sharedMaterial.HasProperty(TintColorId)
                    ? renderer.sharedMaterial.GetColor(TintColorId)
                    : new Color(0.24f, 0.25f, 0.27f, AH64StaticValues.rotorBlurMaxAlpha);
            }

            return renderer;
        }

        private void LateUpdate()
        {
            if (!model || !motor)
                return;

            if (backflipTimer > 0f)
                leanLocal = EvaluateBackflipLean();
            else if (barrelRollTimer > 0f)
                leanLocal = EvaluateBarrelRollLean();
            else
                leanLocal = EvaluateFlightLean();

            //replace ModelLocator's UpdateModelTransform: follow ModelBase position/yaw, layer lean
            Quaternion yaw = modelBase
                ? modelBase.rotation
                : YawBasisFallback();
            Vector3 position = modelBase ? modelBase.position : transform.position;
            model.SetPositionAndRotation(position, yaw * leanLocal);

            TryRotorWash();
            UpdateRotorBlur(motor.velocity);
        }

        private Quaternion YawBasisFallback()
        {
            Vector3 facing = characterDirection ? characterDirection.forward : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                facing = transform.forward;
            return Util.QuaternionSafeLookRotation(facing.normalized, Vector3.up);
        }

        private Quaternion EvaluateBarrelRollLean()
        {
            barrelRollTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(barrelRollTimer / barrelRollDuration);
            float roll = barrelRollSign * AH64StaticValues.dashBarrelRollDegrees * t;

            float pitch;
            if (t < 0.25f)
                pitch = -28f * (t / 0.25f);
            else if (t < 0.55f)
                pitch = Mathf.Lerp(-28f, 4f, (t - 0.25f) / 0.3f);
            else if (t < 0.85f)
                pitch = Mathf.Lerp(4f, 12f, (t - 0.55f) / 0.3f);
            else
                pitch = Mathf.Lerp(12f, 0f, (t - 0.85f) / 0.15f);

            return Quaternion.Euler(pitch, 0f, roll);
        }

        private Quaternion EvaluateBackflipLean()
        {
            backflipTimer -= Time.deltaTime;
            float raw = 1f - Mathf.Clamp01(backflipTimer / backflipDuration);
            //Ease in/out so the long flip doesn't look like a linear spin scrub.
            float t = raw * raw * (3f - 2f * raw);
            //Nose-up through the loop so the airframe reads as a helicopter backflip, not a tumble.
            float pitch = AH64StaticValues.backflipPitchDegrees * t;
            float roll = Mathf.Sin(t * Mathf.PI) * 10f;
            return Quaternion.Euler(pitch, 0f, roll);
        }

        private Quaternion EvaluateFlightLean()
        {
            Vector3 facing = characterDirection ? characterDirection.forward : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                facing = transform.forward;
            facing.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, facing).normalized;
            Vector3 vel = motor.velocity;
            float forwardSpeed = Vector3.Dot(vel, facing);
            float strafeSpeed = Vector3.Dot(vel, right);
            float climbSpeed = vel.y;

            //Stick anticipation: arcade helo controllers lean into input before speed builds, which
            //reads as weight. Blend with velocity lean so hard stops still level out.
            float inputForward = 0f;
            float inputStrafe = 0f;
            if (inputBank)
            {
                Vector3 move = inputBank.moveVector;
                inputForward = Vector3.Dot(move, facing);
                inputStrafe = Vector3.Dot(move, right);
            }

            float inputBlend = AH64StaticValues.leanInputBlend;
            float velBlend = 1f - inputBlend;

            //Positive Euler X is nose down (see collectiveAscentNosePitch), so forward speed and
            //forward stick add positive pitch: the airframe noses into the direction of travel.
            float pitch = Mathf.Clamp(
                velBlend * (forwardSpeed * AH64StaticValues.leanPitchPerSpeed
                    + climbSpeed * AH64StaticValues.leanClimbPitchPerSpeed)
                + inputBlend * (inputForward * AH64StaticValues.leanPitchPerInput),
                -AH64StaticValues.leanMaxPitch,
                AH64StaticValues.leanMaxPitch);

            //Collective attitude: SmoothDamp'd climb intent from the hover controller.
            if (hover)
            {
                float w = hover.AscentPitchWeight;
                if (w >= 0f)
                    pitch += w * AH64StaticValues.collectiveAscentNosePitch;
                else
                    pitch += -w * AH64StaticValues.collectiveDescentNosePitch;
            }

            pitch = Mathf.Clamp(pitch,
                AH64StaticValues.collectiveAscentNosePitch - 2f,
                AH64StaticValues.leanMaxPitch + AH64StaticValues.collectiveDescentNosePitch);

            float roll = Mathf.Clamp(
                velBlend * (-strafeSpeed * AH64StaticValues.leanRollPerSpeed)
                + inputBlend * (-inputStrafe * AH64StaticValues.leanRollPerInput),
                -AH64StaticValues.leanMaxRoll,
                AH64StaticValues.leanMaxRoll);

            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            return Quaternion.Slerp(
                leanLocal,
                target,
                1f - Mathf.Exp(-AH64StaticValues.leanSmoothing * Time.deltaTime));
        }

        private void UpdateRotorBlur(Vector3 velocity)
        {
            if (!blurMain && !blurTail)
                return;

            float effort = Mathf.Clamp01(velocity.magnitude / AH64StaticValues.rotorBlurFullSpeed);
            if (inputBank && inputBank.jump.down)
                effort = Mathf.Clamp01(effort + AH64StaticValues.rotorBlurCollectiveBoost);
            else if (hover && hover.IsAscending)
                effort = Mathf.Clamp01(effort + AH64StaticValues.rotorBlurCollectiveBoost * 0.7f);

            if (barrelRollTimer > 0f || backflipTimer > 0f)
                effort = Mathf.Clamp01(effort + 0.45f);

            blurEffort = Mathf.Lerp(blurEffort, effort,
                1f - Mathf.Exp(-AH64StaticValues.rotorBlurSmoothing * Time.deltaTime));

            bool visible = CanShowFlightEffects()
                && blurEffort > AH64StaticValues.rotorBlurMinEffort;
            float alpha = visible
                ? AH64StaticValues.rotorBlurMaxAlpha
                    * Mathf.InverseLerp(AH64StaticValues.rotorBlurMinEffort, 1f, blurEffort)
                : 0f;

            ApplyBlur(blurMain, visible, alpha);
            ApplyBlur(blurTail, visible, alpha);
        }

        private void ApplyBlur(Renderer renderer, bool visible, float alpha)
        {
            if (!renderer)
                return;

            if (renderer.gameObject.activeSelf != visible)
                renderer.gameObject.SetActive(visible);

            if (!visible)
                return;

            blurBlock.SetColor(TintColorId, new Color(blurTint.r, blurTint.g, blurTint.b, alpha));
            renderer.SetPropertyBlock(blurBlock);
        }

        private bool CanShowFlightEffects()
        {
            //These FX bypass CharacterModel's material swaps. Suppress both friendly revealed
            //and enemy cloaked presentations, using the same buff source as GetVisibilityLevel.
            //Check the counter directly: CharacterModel.visibility is updated per camera later.
            if (body && (body.hasCloakBuff || (body.healthComponent && !body.healthComponent.alive)))
                return false;

            return characterModel && characterModel.isActiveAndEnabled
                && characterModel.invisibilityCount <= 0
                && characterModel.visibility != VisibilityLevel.Invisible;
        }

        private void TryRotorWash()
        {
            washCooldown -= Time.deltaTime;
            if (washCooldown > 0f || !AH64PlaytestConfig.RotorWashEnabled
                || !AH64Assets.RotorWashEffect || !CanShowFlightEffects())
                return;

            float effort = Mathf.Clamp01(motor.velocity.magnitude / AH64StaticValues.rotorWashFullSpeed);
            if (motor.velocity.y > 0.1f)
                effort = Mathf.Clamp01(effort + 0.35f);

            washCooldown = Mathf.Lerp(
                AH64StaticValues.rotorWashIdleInterval,
                AH64StaticValues.rotorWashInterval,
                effort);

            //Hover probes run only on the body authority. Probe locally at effect cadence so
            //spectators also get current terrain, and a missed probe does not raycast every frame.
            const float probeLift = 0.1f;
            Vector3 probeOrigin = transform.position + Vector3.up
                * (motor.capsuleYOffset - motor.capsuleHeight * 0.5f + probeLift);
            if (!Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit,
                AH64StaticValues.rotorWashMaxHeight + probeLift,
                LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                return;

            float groundDistance = Mathf.Max(0f, hit.distance - probeLift);
            float proximity = 1f - Mathf.Clamp01(groundDistance / AH64StaticValues.rotorWashMaxHeight);
            float washScale = Mathf.Lerp(0.55f, 1.4f, proximity)
                * Mathf.Lerp(0.8f, 1.15f, effort)
                * AH64StaticValues.rotorWashScale;
            EffectManager.SpawnEffect(AH64Assets.RotorWashEffect, new EffectData
            {
                origin = hit.point + hit.normal * 0.03f,
                rotation = Quaternion.FromToRotation(Vector3.up, hit.normal),
                scale = washScale,
            }, false);
        }
    }
}
