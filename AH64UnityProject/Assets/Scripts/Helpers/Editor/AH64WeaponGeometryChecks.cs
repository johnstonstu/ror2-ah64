using System;
using System.Linq;
using UnityEngine;

/// <summary>Geometry success criteria: original pitch frame, forward bores and tip-aligned VFX.</summary>
public static class AH64WeaponGeometryChecks
{
    public static void Verify(GameObject prefab)
    {
        ChildLocator locator = prefab.GetComponent<ChildLocator>();
        Transform barrel = Find(locator, "ChinBarrel");
        Vector3 forward = prefab.transform.forward;
        foreach (string name in new[] { "ChinGatling", "ChinGatlingHousing", "ChinCannon" })
        {
            Transform part = Find(locator, name);
            if (part.parent != barrel || part.localPosition.sqrMagnitude > 0.000001f
                || Quaternion.Angle(part.localRotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException("Weapon pitch/pivot contract failed: " + name);
        }
        // Do not infer the mesh axes from transform Euler angles. Measure the vertices.
        CheckTip(locator, "ChinBarrel", barrel.TransformPoint(0f, 1.30f, 0f), forward);
        CheckTip(locator, "ChinGatling", barrel.TransformPoint(0f, 1.30f, 0f), forward);
        Transform cannonMuzzle = Find(locator, "MuzzleCannon");
        if (cannonMuzzle.parent != barrel ||
            Vector3.Distance(cannonMuzzle.position, barrel.TransformPoint(0f, .98f, 0f)) > .001f)
            throw new InvalidOperationException("Cannon cosmetic muzzle detached from pitch.");
        CheckTip(locator, "ChinCannon", cannonMuzzle.position, forward);
        Vector3 measured = (barrel.TransformPoint(0f, 1.30f, 0f) - barrel.position).normalized;
        if (Vector3.Dot(measured, forward) < .999f)
            throw new InvalidOperationException("GunOrigin no longer points forward in the preserved pitch frame.");
        Debug.Log("[AH64WeaponGeometryChecks] PASS " + prefab.name + ": all bores, parents, pivots and cosmetic muzzle tips.");
    }

    private static void CheckTip(ChildLocator locator, string name, Vector3 tip, Vector3 forward)
    {
        Transform part = Find(locator, name);
        Mesh mesh = part.GetComponent<MeshFilter>().sharedMesh;
        // Bundled meshes deliberately discard CPU vertices. All bores are axis-aligned
        // after correction, so the projected mesh bounds give the exact front plane.
        Vector3 localForward = part.InverseTransformDirection(forward).normalized;
        if (Mathf.Abs(localForward.y) < .999f)
            throw new InvalidOperationException(name + " imported bore no longer follows its local Y axis.");
        Bounds bounds = mesh.bounds;
        float actual = Vector3.Dot(part.TransformPoint(bounds.center), forward)
            + Mathf.Abs(Vector3.Dot(part.TransformVector(Vector3.right * bounds.extents.x), forward))
            + Mathf.Abs(Vector3.Dot(part.TransformVector(Vector3.up * bounds.extents.y), forward))
            + Mathf.Abs(Vector3.Dot(part.TransformVector(Vector3.forward * bounds.extents.z), forward));
        if (Mathf.Abs(actual - Vector3.Dot(tip, forward)) > .003f)
            throw new InvalidOperationException(name + " mesh tip disagrees with muzzle: " + actual + " vs " + Vector3.Dot(tip, forward));
    }

    private static Transform Find(ChildLocator locator, string name)
    {
        return locator.transformPairs.Single(p => p.name == name).transform;
    }
}
