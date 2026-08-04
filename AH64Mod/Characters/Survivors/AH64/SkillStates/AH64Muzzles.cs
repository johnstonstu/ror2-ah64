using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// Where each weapon's shots leave the airframe.
    ///
    /// <para>The Blender airframe defines these as empties (see <c>ANCHORS</c> in
    /// <c>Art/Blender/build_ah64.py</c>). Named anchor → <see cref="Gun"/> → ChinBarrel tip →
    /// aim-ray origin. Without that chain a rocket asked for a missing pylon would spawn at the
    /// world origin, and a stale ChildLocator after an FBX reimport would fire the M230 from the
    /// cockpit.</para>
    ///
    /// <para>Takes a <see cref="ChildLocator"/> rather than the state, because EntityState.gameObject and
    /// GetModelChildLocator() are both protected — states pass their own locator in.
    /// <c>ChildLocator.FindChild</c> returns null quietly on a miss, so probing for an absent anchor
    /// every shot is free and silent.</para>
    /// </summary>
    internal static class AH64Muzzles
    {
        /// <summary>Chin turret muzzle.</summary>
        public const string Gun = "Muzzle";
        public const string ChinBarrel = "ChinBarrel";

        //wing pylons - Hydra-70 pods outboard, Hellfire rails inboard
        public const string RocketL = "MuzzleRocketL";
        public const string RocketR = "MuzzleRocketR";
        public const string MissileL = "MuzzleMissileL";
        public const string MissileR = "MuzzleMissileR";

        //Approximate barrel length past ChinBarrel's pivot (Blender BARREL_LEN). Used only when the
        //Muzzle empty is missing from ChildLocator but the barrel mesh is still there.
        private const float BarrelTipLocalY = 1.30f;

        /// <summary>
        /// The M230 has an articulated barrel, so its spawn point must follow the barrel transform
        /// rather than a separately exported empty. The empty remains useful for other content, but
        /// an FBX reimport can leave it visually below the muzzle while the barrel itself is correct.
        /// </summary>
        public static Vector3 GunOrigin(ChildLocator childLocator, Ray aimRay)
        {
            Transform barrel = childLocator
                ? childLocator.FindChild(ChinBarrel) ?? FindDeep(childLocator.transform, ChinBarrel)
                : null;
            return barrel ? barrel.TransformPoint(0f, BarrelTipLocalY, 0f) : Origin(childLocator, Gun, aimRay);
        }

        public static Transform Find(ChildLocator childLocator, string muzzleName)
        {
            if (!childLocator)
                return null;

            Transform muzzle = childLocator.FindChild(muzzleName);
            if (muzzle)
                return muzzle;

            //Stale ChildLocator after an FBX reimport without Phase 4: empties still exist under
            //the model root even when the serialized NameTransformPair is broken.
            Transform root = childLocator.transform;
            muzzle = FindDeep(root, muzzleName);
            if (muzzle)
                return muzzle;

            if (muzzleName != Gun)
            {
                muzzle = childLocator.FindChild(Gun) ?? FindDeep(root, Gun);
                if (muzzle)
                    return muzzle;
            }

            return childLocator.FindChild(ChinBarrel) ?? FindDeep(root, ChinBarrel);
        }

        /// <summary>
        /// The world position to spawn from, falling back to the aim origin so a shot is never lost to a
        /// missing anchor.
        /// </summary>
        public static Vector3 Origin(ChildLocator childLocator, string muzzleName, Ray aimRay)
        {
            Transform muzzle = Find(childLocator, muzzleName);
            if (!muzzle)
                return aimRay.origin;

            //ChinBarrel alone is the breech pivot — nudge to the tip so tracers leave the cannon.
            if (muzzle.name == ChinBarrel)
                return muzzle.TransformPoint(0f, BarrelTipLocalY, 0f);

            return muzzle.position;
        }

        /// <summary>
        /// The muzzle name to hand to effects and <c>BulletAttack.muzzleName</c>, which look the name up
        /// themselves and can't take a Transform. Prefer a ChildLocator-registered name so
        /// <c>SimpleMuzzleFlash</c> can attach; fall back to <see cref="Gun"/> / barrel when the
        /// requested pylon is missing.
        /// </summary>
        public static string ResolveName(ChildLocator childLocator, string muzzleName)
        {
            if (!childLocator)
                return Gun;

            if (childLocator.FindChild(muzzleName))
                return muzzleName;

            if (childLocator.FindChild(Gun))
                return Gun;

            if (childLocator.FindChild(ChinBarrel))
                return ChinBarrel;

            //Deep-found Muzzle still wants the "Muzzle" string for SimpleMuzzleFlash index lookup —
            //if that also fails the flash simply won't spawn; bullets still use Origin().
            return Gun;
        }

        /// <summary>
        /// Direction from a wing/rail muzzle toward the aim point, so Hydra rockets fired from ±1.28u
        /// off centre actually converge on the crosshair instead of sailing past at close range.
        /// Falls back to the aim ray when the muzzle coincides with it.
        /// </summary>
        public static Vector3 AimDirection(ChildLocator childLocator, string muzzleName, Ray aimRay, float convergeDistance = 40f)
        {
            Vector3 origin = Origin(childLocator, muzzleName, aimRay);
            return AimDirection(origin, aimRay, convergeDistance);
        }

        /// <summary>Converge a known world-space muzzle origin on the aim ray.</summary>
        public static Vector3 AimDirection(Vector3 origin, Ray aimRay, float convergeDistance = 40f)
        {
            Vector3 target = aimRay.GetPoint(convergeDistance);
            Vector3 toTarget = target - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
                return aimRay.direction;
            return toTarget.normalized;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (!root || string.IsNullOrEmpty(name))
                return null;

            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found)
                    return found;
            }

            return null;
        }
    }
}
