using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bakes the alternate primary's skill icon: a six-barrel muzzle seen head-on.
///
/// <para>Head-on rather than the three-quarter view the other icons use, because this one
/// has to survive being drawn at ~64px in the skill bar. A concentric ring of bores is
/// still legible at that size; a foreshortened barrel group is not.</para>
///
/// <para>Rendered at 4x and box-filtered down — there is no antialiasing to lean on when
/// writing pixels directly, and hard circle edges alias badly against the dark plate.</para>
///
/// Menu: AH64 -> Bake Gatling Skill Icon
/// </summary>
public static class AH64GatlingIconBaker
{
    private const string OutputPath =
        "Assets/AH64/Bundle/Icons/texAH64GatlingIcon.png";
    private const string CannonOutputPath =
        "Assets/AH64/Bundle/Icons/texAH64CannonIcon.png";

    private const int Size = 1024;
    private const int Supersample = 4;

    // Sampled from texAH64PrimaryIcon so the set reads as one family.
    private static readonly Color Plate = new Color32(0x1E, 0x1E, 0x1C, 0xFF);
    private static readonly Color PlateStripe = new Color32(0x26, 0x26, 0x23, 0xFF);
    private static readonly Color Frame = new Color32(0x6E, 0x72, 0x52, 0xFF);
    private static readonly Color HousingLit = new Color32(0x6A, 0x6E, 0x6B, 0xFF);
    private static readonly Color HousingMid = new Color32(0x4A, 0x4E, 0x4C, 0xFF);
    private static readonly Color HousingDark = new Color32(0x2C, 0x2F, 0x2E, 0xFF);
    private static readonly Color BoreDark = new Color32(0x12, 0x13, 0x13, 0xFF);
    private static readonly Color MuzzleHot = new Color32(0xFF, 0xC1, 0x4A, 0xFF);
    private static readonly Color MuzzleWarm = new Color32(0xE8, 0x7C, 0x1E, 0xFF);

    [MenuItem("AH64/Bake Alternate Primary Skill Icons")]
    public static void BakeFromMenu()
    {
        BakeAll();
        EditorUtility.DisplayDialog("AH64 Skill Icons",
            "Wrote:\n" + OutputPath + "\n" + CannonOutputPath, "OK");
    }

    /// <summary>Dialog-free entry point for batch/MCP runs.</summary>
    public static string BakeAll()
    {
        // Six small bores on a ring — reads as a rotary cannon at skill-bar size.
        Bake(OutputPath, bores: 6, ringRadiusPx: 210f, boreRadiusPx: 74f, housingRadiusPx: 332f);
        // One big bore, no ring — a single heavy barrel. The silhouette difference is the
        // whole point: at 64px the player distinguishes these by bore count, not detail.
        Bake(CannonOutputPath, bores: 1, ringRadiusPx: 0f, boreRadiusPx: 176f, housingRadiusPx: 332f);
        return null;
    }

    public static string Bake() => BakeAll();

    private static string Bake(string outputPath, int bores, float ringRadiusPx,
        float boreRadiusPx, float housingRadiusPx)
    {
        int hi = Size * Supersample;
        var hiRes = new Color[hi * hi];

        float c = (hi - 1) * 0.5f;
        float unit = hi / 1024f;

        // Bores on a ring, first one at the top so a cluster reads as deliberate rather
        // than arbitrarily rotated. A single bore lands dead centre (ringR = 0).
        float ringR = ringRadiusPx * unit;
        float boreR = boreRadiusPx * unit;
        var boreCentres = new Vector2[bores];
        for (int i = 0; i < bores; i++)
        {
            float a = Mathf.PI * 0.5f + i * (Mathf.PI * 2f / bores);
            boreCentres[i] = new Vector2(c + Mathf.Cos(a) * ringR, c + Mathf.Sin(a) * ringR);
        }

        for (int y = 0; y < hi; y++)
        {
            for (int x = 0; x < hi; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);

                // Backing plate with the diagonal stripe the other icons carry.
                bool stripe = (((x + y) / (int)(96f * unit)) & 1) == 0;
                Color col = stripe ? PlateStripe : Plate;

                // Muzzle bloom sits OUTSIDE the housing silhouette. Drawn behind it (as the
                // first version did) the housing covers it completely and the icon reads
                // as a cold machine part.
                float glowBand = Mathf.Clamp01(1f - Mathf.Abs(r - 345f * unit) / (140f * unit));
                if (glowBand > 0f)
                    col = Color.Lerp(col, MuzzleWarm, Mathf.Pow(glowBand, 2.4f) * 0.30f);

                float theta = Mathf.Atan2(dy, dx);

                // Faceted housing rather than a smooth radial gradient — the rest of the
                // icon set is flat-shaded low-poly, and a smooth ring reads as a CD next
                // to them. Each facet takes one flat value from a fixed light direction.
                const int facets = 12;
                float facetIndex = Mathf.Floor((theta + Mathf.PI) / (Mathf.PI * 2f / facets));
                float facetAngle = -Mathf.PI + (facetIndex + 0.5f) * (Mathf.PI * 2f / facets);
                // Light from upper-left, the convention the other icons use.
                float lambert = Mathf.Clamp01(
                    0.32f + 0.68f * (Mathf.Cos(facetAngle - Mathf.PI * 0.75f) * 0.5f + 0.5f));

                float housingR = 332f * unit;
                if (r < housingR)
                {
                    Color body = Color.Lerp(HousingDark, HousingLit, lambert);
                    col = body;

                    // Rim bevel: a brighter lip catching the light, dark on the far side.
                    if (r > housingR - 26f * unit)
                        col = Color.Lerp(body, Color.white, 0.18f * lambert);
                    // Recessed rotor face inside the bevel.
                    else if (r > housingR - 44f * unit)
                        col = Color.Lerp(body, HousingDark, 0.55f);
                    else
                        col = Color.Lerp(body, HousingDark, 0.28f);
                }

                // Bores. Lit rim on the light side, warm ember down the tube — a gatling
                // that has just been firing, not a parts-catalogue photo.
                for (int i = 0; i < bores; i++)
                {
                    float bd = Vector2.Distance(new Vector2(x, y), boreCentres[i]);
                    if (bd >= boreR)
                        continue;

                    Vector2 off = (new Vector2(x, y) - boreCentres[i]) / Mathf.Max(boreR, 0.001f);

                    // Raised lip: lit on the upper-left, matching the housing's light.
                    float lip = Mathf.Clamp01(off.x * -0.7071f + off.y * 0.7071f);
                    col = Color.Lerp(HousingMid, HousingLit, lip);

                    if (bd < boreR * 0.84f)
                    {
                        // Inside the hole the lit surface is the FAR wall — lower-right — not
                        // the near one. Shading the near wall (as the first pass did) reads
                        // as a dome, and six domes look like rivets, not barrels.
                        float farWall = Mathf.Clamp01(off.x * 0.7071f - off.y * 0.7071f);
                        float depth = Mathf.Clamp01(bd / (boreR * 0.84f));
                        Color wall = Color.Lerp(BoreDark, HousingMid, farWall * depth * 0.8f);

                        // Small ember at the bottom of the tube only — a whole glowing disc
                        // is what made these read convex.
                        float ember = Mathf.Clamp01(1f - depth / 0.35f);
                        col = Color.Lerp(wall, MuzzleHot, Mathf.Pow(ember, 2f) * 0.5f);
                    }
                }

                // Central rotor hub, same facet light so it sits in the same world.
                // Only on a cluster — a single-barrel cannon has no rotor, and drawing one
                // put a spurious ring in the middle of its bore.
                if (bores > 1 && r < 64f * unit)
                {
                    col = r < 44f * unit
                        ? Color.Lerp(HousingDark, BoreDark, 0.5f)
                        : Color.Lerp(HousingMid, HousingLit, lambert);
                }

                // Inset frame.
                float edge = Mathf.Min(Mathf.Min(x, hi - 1 - x), Mathf.Min(y, hi - 1 - y));
                if (edge < 10f * unit)
                    col = Plate;
                else if (edge < 18f * unit)
                    col = Frame;

                hiRes[y * hi + x] = col;
            }
        }

        // Box filter down.
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        var outPixels = new Color[Size * Size];
        int s = Supersample;
        float inv = 1f / (s * s);
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float rr = 0f, gg = 0f, bb = 0f;
                for (int oy = 0; oy < s; oy++)
                {
                    int sy = y * s + oy;
                    for (int ox = 0; ox < s; ox++)
                    {
                        Color p = hiRes[sy * (Size * s) + x * s + ox];
                        rr += p.r; gg += p.g; bb += p.b;
                    }
                }
                outPixels[y * Size + x] = new Color(rr * inv, gg * inv, bb * inv, 1f);
            }
        }
        tex.SetPixels(outPixels);
        tex.Apply(false, false);

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputPath));
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        File.WriteAllBytes(full, png);
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

        // Must import as a Sprite — SkillDef.skillIcon is a Sprite, and a Default-type
        // texture silently resolves to null there.
        var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        Debug.Log("[AH64GatlingIconBaker] Wrote " + outputPath);
        return null;
    }
}
