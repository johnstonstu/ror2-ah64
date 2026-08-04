using AH64.Survivors;
using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Aims the chin turret on two axes: the housing yaws, the barrel pitches within a limited cone.
    /// Also applies a short spring-back kick when the chain gun fires so the long barrel reads as a gun.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public class AH64ChinTurret : MonoBehaviour
    {
        private const float MinAimSqr = 0.0001f;

        private InputBankTest inputBank;
        private Transform chinTurret;
        private Transform chinBarrel;
        private Quaternion turretRestLocalRotation;
        private Quaternion barrelRestLocalRotation;
        private Vector3 barrelRestLocalPosition;
        private float kickOffset;

        private void Start()
        {
            inputBank = GetComponent<InputBankTest>();

            ModelLocator modelLocator = GetComponent<ModelLocator>();
            if (!modelLocator || !modelLocator.modelTransform)
                return;

            ChildLocator childLocator = modelLocator.modelTransform.GetComponent<ChildLocator>();
            if (!childLocator)
                return;

            chinTurret = childLocator.FindChild("ChinTurret");
            chinBarrel = childLocator.FindChild("ChinBarrel");

            if (chinTurret)
                turretRestLocalRotation = chinTurret.localRotation;

            if (chinBarrel)
            {
                barrelRestLocalRotation = chinBarrel.localRotation;
                barrelRestLocalPosition = chinBarrel.localPosition;
            }
            else if (chinTurret)
            {
                //Pre-split models: pitch falls back onto the single ChinTurret transform.
                chinBarrel = chinTurret;
                barrelRestLocalRotation = turretRestLocalRotation;
                barrelRestLocalPosition = chinTurret.localPosition;
            }
        }

        /// <summary>
        /// Called by <see cref="SkillStates.FireChaingun"/> on each round so the barrel visibly kicks.
        /// </summary>
        public void NotifyFired()
        {
            kickOffset = AH64StaticValues.chinTurretKickDistance;
        }

        private void LateUpdate()
        {
            if (!chinTurret || !inputBank)
                return;

            Vector3 aim = inputBank.aimDirection;
            if (aim.sqrMagnitude >= MinAimSqr)
            {
                //aim relative to the airframe so banking the hull doesn't yank the gun sideways
                Transform hull = chinTurret.parent ? chinTurret.parent : transform;
                Vector3 localAim = hull.InverseTransformDirection(aim.normalized);

                float yaw = Mathf.Atan2(localAim.x, localAim.z) * Mathf.Rad2Deg;
                yaw = Mathf.Clamp(yaw,
                    AH64StaticValues.chinTurretMinYaw,
                    AH64StaticValues.chinTurretMaxYaw);
                float pitch = -Mathf.Asin(Mathf.Clamp(localAim.y, -1f, 1f)) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(pitch,
                    AH64StaticValues.chinTurretMinPitch,
                    AH64StaticValues.chinTurretMaxPitch);

                float turnSpeed = AH64StaticValues.chinTurretTurnSpeed * Time.deltaTime;

                bool split = chinBarrel && chinBarrel != chinTurret;

                if (split)
                {
                    Quaternion yawTarget = turretRestLocalRotation * Quaternion.Euler(0f, yaw, 0f);
                    chinTurret.localRotation = Quaternion.RotateTowards(
                        chinTurret.localRotation,
                        yawTarget,
                        turnSpeed);

                    Quaternion pitchTarget = barrelRestLocalRotation * Quaternion.Euler(pitch, 0f, 0f);
                    chinBarrel.localRotation = Quaternion.RotateTowards(
                        chinBarrel.localRotation,
                        pitchTarget,
                        turnSpeed);
                }
                else
                {
                    Quaternion target = turretRestLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
                    chinTurret.localRotation = Quaternion.RotateTowards(
                        chinTurret.localRotation,
                        target,
                        turnSpeed);
                }
            }

            if (chinBarrel)
            {
                kickOffset = Mathf.MoveTowards(kickOffset, 0f,
                    AH64StaticValues.chinTurretKickRecoverPerSecond * Time.deltaTime);
                //Unity forward after the baked FBX is local +Z along the barrel; recoil shoves aft.
                chinBarrel.localPosition = barrelRestLocalPosition + Vector3.back * kickOffset;
            }
        }
    }
}
