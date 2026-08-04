using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Character-select lighting is far darker than stage light. Soft albedo lifts on init were not
    /// enough against the olive body atlas, so this re-applies a hard fill whenever the lobby
    /// display enables. Body/in-run mats are never touched — only materials on this display instance.
    /// </summary>
    public class AH64LobbyDisplayBoost : MonoBehaviour
    {
        private static bool loggedOnce;

        private void OnEnable()
        {
            Apply();
        }

        private void Start()
        {
            //Skins / CharacterModel may assign materials after OnEnable on first spawn.
            Apply();
        }

        private void Apply()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            int boosted = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!renderer || renderer is ParticleSystemRenderer)
                    continue;

                Material[] shared = renderer.sharedMaterials;
                if (shared == null || shared.Length == 0)
                    continue;

                Material[] next = null;
                for (int m = 0; m < shared.Length; m++)
                {
                    Material source = shared[m];
                    if (!source)
                        continue;

                    Material mat = source;
                    //Never mutate a shared bundle/body material — lobby-only instance.
                    if (!mat.name.EndsWith(" (Lobby)"))
                    {
                        mat = Instantiate(source);
                        mat.name = source.name.Replace(" (Instance)", "") + " (Lobby)";
                        if (next == null)
                        {
                            next = new Material[shared.Length];
                            for (int c = 0; c < shared.Length; c++)
                                next[c] = shared[c];
                        }
                        next[m] = mat;
                    }

                    BoostMaterial(mat);
                    boosted++;
                }

                if (next != null)
                    renderer.sharedMaterials = next;
            }

            CharacterModel model = GetComponent<CharacterModel>();
            if (model && model.baseRendererInfos != null)
            {
                CharacterModel.RendererInfo[] infos = model.baseRendererInfos;
                for (int i = 0; i < infos.Length; i++)
                {
                    CharacterModel.RendererInfo info = infos[i];
                    if (!info.renderer)
                        continue;

                    Material live = info.renderer.sharedMaterial;
                    if (!live)
                        continue;

                    info.defaultMaterial = live;
                    infos[i] = info;
                }
                model.baseRendererInfos = infos;
            }

            if (!loggedOnce)
            {
                loggedOnce = true;
                Log.Info($"AH64LobbyDisplayBoost: brightened {boosted} lobby material slot(s).");
            }
        }

        private static void BoostMaterial(Material mat)
        {
            string name = mat.name;
            bool isBody = name.Contains("matAH64Body") || name.Contains("matAH64Markings");
            bool isGlass = name.Contains("Glass") || name.Contains("Optics");
            bool isDark = name.Contains("matAH64Dark") || name.Contains("Radar") || name.Contains("Rotor");

            //0.1.17.1 was readable but blown-out (EmPower ~3). Keep a soft fill so olive still
            //reads in the menu without looking self-lit.
            Color tint;
            float emPower;
            if (isBody)
            {
                tint = new Color(0.70f, 0.76f, 0.52f, 1f);
                emPower = 0.85f;
            }
            else if (isGlass)
            {
                tint = new Color(0.28f, 0.42f, 0.50f, 1f);
                emPower = 0.45f;
            }
            else if (isDark)
            {
                tint = new Color(0.42f, 0.46f, 0.36f, 1f);
                emPower = 0.65f;
            }
            else
            {
                tint = new Color(0.62f, 0.68f, 0.50f, 1f);
                emPower = 0.70f;
            }

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);

            //HGStandard fill light — this is what actually reads under menu lighting.
            if (mat.HasProperty("_EmColor"))
                mat.SetColor("_EmColor", tint * 0.40f);
            if (mat.HasProperty("_EmPower"))
                mat.SetFloat("_EmPower", emPower);

            if (mat.HasProperty("_SpecularStrength"))
                mat.SetFloat("_SpecularStrength", Mathf.Max(mat.GetFloat("_SpecularStrength"), 0.40f));
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", Mathf.Max(mat.GetFloat("_Smoothness"), 0.38f));
        }
    }
}
