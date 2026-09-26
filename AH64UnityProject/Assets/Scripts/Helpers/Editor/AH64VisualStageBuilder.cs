using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Rebuilds and verifies the visual staging bundle without installing it.</summary>
public static class AH64VisualStageBuilder
{
    private const string BundleRoot = "Assets/AH64/Bundle/";
    private static readonly string[] Anchors =
    {
        "Chest", "Head", "NoseTip", "WingL", "WingR", "TailTip", "MainHurtbox",
        "HeadHurtbox", "AimOrigin", "Muzzle", "MuzzleRocketL", "MuzzleRocketR",
        "MuzzleMissileL", "MuzzleMissileR"
    };

    public static void RunFromCommandLine()
    {
        AssetBundle bundle = null;
        try
        {
            if (AH64BuildSafety.ProfileDeploymentRequested)
                throw new InvalidOperationException("Visual staging forbids profile deployment.");
            var baseline = CaptureAnchors(AssetDatabase.LoadAssetAtPath<GameObject>(BundleRoot + "mdlAH64.prefab"));
            // Approved tail extension moves only this cosmetic item anchor. All gameplay
            // anchors remain unchanged; use an absolute target so repeated builds are safe.
            Matrix4x4 tailTip = baseline["TailTip"];
            tailTip.SetColumn(3, new Vector4(0f, 1.16f, -3.75f, 1f));
            baseline["TailTip"] = tailTip;
            string error = AH64Phase4Builder.RefreshPrefabsFromFbx();
            if (error != null) throw new InvalidOperationException(error);
            VerifyPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BundleRoot + "mdlAH64.prefab"), baseline);
            VerifyPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BundleRoot + "AH64Display.prefab"), baseline);
            AssetDatabase.SaveAssets();
            // Phase4 refresh only prepares model materials/prefabs. Audio assets stay untouched.
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string bundlePath = Path.Combine(projectRoot, "AssetBundles", "ah64");
            // A staging artifact must also satisfy pack.ps1 freshness after prefab regeneration.
            AssetBundleManifest manifest = AH64BundleBuilder.BuildBundlesChecked(forceRebuild: true);
            if (!manifest || !File.Exists(bundlePath))
                throw new InvalidOperationException("Bundle build failed or produced no output: " + bundlePath);
            if (AssetBundle.GetAllLoadedAssetBundles().Any(b => b.name == "ah64"))
                throw new InvalidOperationException("An ah64 bundle is already loaded; use a fresh staging session.");
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (!bundle) throw new InvalidOperationException("Cannot load staged bundle: " + bundlePath);
            VerifyPrefab(bundle.LoadAsset<GameObject>("mdlAH64"), baseline);
            VerifyPrefab(bundle.LoadAsset<GameObject>("AH64Display"), baseline);
            var duplicateNames = bundle.GetAllAssetNames().GroupBy(p => Path.GetFileNameWithoutExtension(p).ToLowerInvariant())
                .Where(g => g.Count() > 1).Select(g => g.Key).ToArray();
            if (duplicateNames.Length > 0)
                throw new InvalidOperationException("Ambiguous bundle names: " + string.Join(", ", duplicateNames));
            Debug.Log("[AH64VisualStageBuilder] PASS: source and disk bundle preserve anchors, colliders, " +
                "25 single-material meshes, permanent racks and gatling alignment. No profile installed.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[AH64VisualStageBuilder] Validation failed: " + exception);
            throw;
        }
        finally
        {
            if (bundle) bundle.Unload(true);
        }
    }

    private static Dictionary<string, Matrix4x4> CaptureAnchors(GameObject prefab)
    {
        if (!prefab) throw new InvalidOperationException("Missing baseline model prefab.");
        ChildLocator locator = prefab.GetComponent<ChildLocator>();
        if (!locator) throw new InvalidOperationException(prefab.name + " has no ChildLocator.");
        var result = new Dictionary<string, Matrix4x4>();
        foreach (string name in Anchors)
        {
            Transform child = FindChild(locator, name);
            if (!child) throw new InvalidOperationException("Missing anchor " + name);
            result.Add(name, prefab.transform.worldToLocalMatrix * child.localToWorldMatrix);
        }
        return result;
    }

    private static void VerifyPrefab(GameObject prefab, Dictionary<string, Matrix4x4> baseline)
    {
        if (!prefab) throw new InvalidOperationException("Missing model/display prefab.");
        var anchors = CaptureAnchors(prefab);
        foreach (var pair in baseline)
            for (int i = 0; i < 16; i++)
                if (Mathf.Abs(anchors[pair.Key][i] - pair.Value[i]) > 0.001f)
                    throw new InvalidOperationException(prefab.name + " changed anchor " + pair.Key);
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length != 25) throw new InvalidOperationException(prefab.name + " expected 25 meshes, got " + filters.Length);
        ChildLocator locator = prefab.GetComponent<ChildLocator>();
        int vertices = 0;
        foreach (MeshFilter filter in filters)
        {
            Mesh mesh = filter.sharedMesh;
            Renderer renderer = filter.GetComponent<Renderer>();
            if (!mesh || mesh.subMeshCount != 1 || !renderer || renderer.sharedMaterials.Length != 1 || !renderer.sharedMaterial)
                throw new InvalidOperationException(prefab.name + "/" + filter.name + " violates the single-material mesh contract.");
            if (FindChild(locator, filter.name) != filter.transform)
                throw new InvalidOperationException("Missing/mismatched renderer locator " + filter.name);
            vertices += mesh.vertexCount;
        }
        if (vertices > 15000) throw new InvalidOperationException("Visual vertex budget exceeded: " + vertices);
        foreach (string rack in new[] { "PodMissileL", "PodMissileR" })
            if (FindChild(locator, rack).GetComponent<MeshFilter>().sharedMesh.vertexCount <= 24)
                throw new InvalidOperationException(rack + " is still only a rectangular plate.");
        CheckCollider(locator, "MainHurtbox", new Vector3(1.6f, 1.4f, 3.2f));
        CheckCollider(locator, "HeadHurtbox", new Vector3(0.7f, 0.55f, 0.9f));
        Transform gatling = FindChild(locator, "ChinGatling");
        if (Quaternion.Angle(gatling.localRotation, Quaternion.identity) > 0.01f)
            throw new InvalidOperationException("Gatling FBX rotation correction is missing.");
        Vector3 bore = gatling.InverseTransformDirection(FindChild(locator, "Muzzle").position - gatling.position).normalized;
        if (Vector3.Dot(bore, Vector3.forward) < 0.99f)
            throw new InvalidOperationException("Gatling spin axis no longer follows the muzzle.");
        Debug.Log("[AH64VisualStageBuilder] " + prefab.name + ": " + filters.Length + " meshes, " + vertices + " vertices, anchors preserved.");
    }

    private static Transform FindChild(ChildLocator locator, string name)
    {
        var matches = locator.transformPairs.Where(pair => pair.name == name).ToArray();
        if (matches.Length != 1 || !matches[0].transform)
            throw new InvalidOperationException("Expected one locator target for " + name);
        return matches[0].transform;
    }
    private static void CheckCollider(ChildLocator locator, string name, Vector3 size)
    {
        BoxCollider collider = FindChild(locator, name).GetComponent<BoxCollider>();
        if (!collider || !collider.isTrigger || collider.center != Vector3.zero || collider.size != size)
            throw new InvalidOperationException("Changed gameplay collider " + name);
    }
}