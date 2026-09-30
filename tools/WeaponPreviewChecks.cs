using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using RoR2;
using RoR2.Skills;
using RoR2.SurvivorMannequins;
using AH64.Survivors;
using AH64.Survivors.Components;

static class WeaponPreviewChecks
{
    static readonly string[] Names = { "ChinBarrel", "ChinGatling", "ChinGatlingHousing", "ChinCannon" };
    static int assertions;
    static void Check(bool condition, string message) { assertions++; if (!condition) throw new Exception(message); }
    static void Call(object o, string method) => o.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, null);
    static Renderer[] Renderers(GameObject model) => Names.Select(n => model.GetComponent<ChildLocator>().children[n].GetComponent<Renderer>()).ToArray();

    static GameObject Model(GameObject parent, bool lobby)
    {
        var model = new GameObject { parent = parent };
        var locator = model.Add<ChildLocator>();
        foreach (string name in Names)
        {
            var part = new GameObject { parent = model };
            part.Add<Renderer>();
            locator.children[name] = part.Add<Transform>();
        }
        model.Add<ModelSkinController>();
        var visual = model.Add<AH64PrimaryWeaponVisuals>();
        Call(visual, "Awake"); Call(visual, "OnEnable");
        if (lobby) { var preview = model.Add<AH64LobbyWeaponPreview>(); Call(preview, "OnEnable"); Call(preview, "Start"); }
        return model;
    }

    // Variant ordering is deliberately Cannon/M230/Gatling, and primary is slot 1.
    // Index-based presentation or always reading slot 0 must fail this fixture.
    static NetworkUser User(SurvivorDef survivor, uint variant)
    {
        var user = new NetworkUser { survivor = survivor };
        user.Change(variant);
        return user;
    }
    static GameObject Slot(NetworkUser user)
    {
        var go = new GameObject(); go.Add<SurvivorMannequinSlotController>().networkUser = user; return go;
    }
    static void Visible(GameObject model, int gun)
    {
        bool[] expected = gun == 0 ? new[] { true, false, false, false } :
                          gun == 1 ? new[] { false, true, true, false } : new[] { false, false, false, true };
        var renderers = Renderers(model);
        for (int i = 0; i < 4; i++)
        {
            Check(!renderers[i].forceRenderingOff == expected[i], "Wrong gun visibility at part " + i);
            Check(renderers[i].gameObject.active, "Selection deactivated an aiming transform");
        }
    }

    public static void Main()
    {
        var prefab = new GameObject();
        prefab.Add<GenericSkill>(); // unrelated skill before primary
        var primary = prefab.Add<GenericSkill>();
        var m230 = new SkillDef();
        primary.skillFamily = new SkillFamily { variants = new[] {
            new SkillFamily.Variant { skillDef = AH64Assets.cannonSkillDef },
            new SkillFamily.Variant { skillDef = m230 },
            new SkillFamily.Variant { skillDef = AH64Assets.gatlingSkillDef } } };
        prefab.Add<SkillLocator>().primary = primary;
        BodyCatalog.prefab = prefab;
        var survivor = new SurvivorDef { bodyPrefab = prefab };
        var a = User(survivor, 2); var b = User(survivor, 0);
        var slotA = Slot(a); var slotB = Slot(b);
        var modelA = Model(slotA, true); var modelB = Model(slotB, true);
        Visible(modelA, 1); Visible(modelB, 2);

        // Lobby swap sound: silent for the first primary shown and for a repeated selection,
        // one clunk per real change, and only on the mannequin whose owner changed.
        Check(Util.plays.Count == 0, "Showing the first primary played the swap sound");
        a.Change(2);
        Check(Util.plays.Count == 0, "Re-selecting the same primary played the swap sound");
        a.Change(1);
        Check(Util.plays.Count == 1 && Util.plays[0] == "Play_railgunner_R_gun_swap", "Changing primary did not play one swap sound");
        a.Change(2);
        Check(Util.plays.Count == 2, "Changing back did not play the swap sound");

        // Immediate events, repeated toggles, and asynchronously completed skin callbacks.
        for (int repeat = 0; repeat < 5; repeat++)
            for (int skin = 0; skin < 5; skin++)
                for (uint variant = 0; variant < 3; variant++)
                {
                    a.Change(variant);
                    int gun = variant == 0 ? 2 : variant == 1 ? 0 : 1;
                    Visible(modelA, gun); Visible(modelB, 2);
                    foreach (var r in Renderers(modelA)) { r.enabled = true; r.skin = skin; }
                    modelA.GetComponent<ModelSkinController>().Applied(skin);
                    Visible(modelA, gun);
                    Check(Renderers(modelA).All(r => r.skin == skin), "Weapon update changed the skin");
                }
        // A delayed old skin apply must not revert the most recent primary.
        a.Change(0); modelA.GetComponent<ModelSkinController>().Applied(0); Visible(modelA, 2);
        // Material flag maintenance must leave cloak/death enabled=false intact.
        foreach (var r in Renderers(modelA)) r.enabled = false;
        Call(modelA.GetComponent<AH64PrimaryWeaponVisuals>(), "LateUpdate");
        Check(Renderers(modelA).All(r => !r.enabled), "Presentation undid cloak visibility");

        // Actual slot swap order: reparent first, then exchange owners.
        modelA.parent = slotB; modelB.parent = slotA;
        slotA.GetComponent<SurvivorMannequinSlotController>().networkUser = b;
        slotB.GetComponent<SurvivorMannequinSlotController>().networkUser = a;
        int soundsBeforeSwap = Util.plays.Count;
        Call(modelA.GetComponent<AH64LobbyWeaponPreview>(), "LateUpdate");
        Check(Util.plays.Count == soundsBeforeSwap, "A mannequin changing owner played the swap sound");
        a.Change(1); Visible(modelA, 0); Visible(modelB, 2);
        // Reuse a slot for a different owner, then remove the owner.
        slotB.GetComponent<SurvivorMannequinSlotController>().networkUser = b;
        Call(modelA.GetComponent<AH64LobbyWeaponPreview>(), "LateUpdate"); Visible(modelA, 2);
        slotB.GetComponent<SurvivorMannequinSlotController>().networkUser = null;
        Call(modelA.GetComponent<AH64LobbyWeaponPreview>(), "LateUpdate"); Visible(modelA, 0);
        // Disable detaches events; re-enable/return reads the latest loadout.
        var previewA = modelA.GetComponent<AH64LobbyWeaponPreview>();
        Call(previewA, "OnDisable"); b.Change(2); Visible(modelA, 0);
        slotB.GetComponent<SurvivorMannequinSlotController>().networkUser = b;
        Call(previewA, "OnEnable"); Visible(modelA, 1);

        // Fresh gameplay body and skill override, independent of lobby code.
        var body = new GameObject(); var skill = body.Add<GenericSkill>();
        skill.skillDef = AH64Assets.cannonSkillDef;
        body.Add<SkillLocator>().primary = skill;
        var bodyModel = Model(body, false); Visible(bodyModel, 2);
        skill.skillDef = AH64Assets.gatlingSkillDef;
        Call(bodyModel.GetComponent<AH64PrimaryWeaponVisuals>(), "LateUpdate"); Visible(bodyModel, 1);
        skill.skillDef = m230;
        Call(bodyModel.GetComponent<AH64PrimaryWeaponVisuals>(), "LateUpdate"); Visible(bodyModel, 0);
        Check(modelA.components.All(c => c is AH64LobbyWeaponPreview || c is AH64PrimaryWeaponVisuals || c is ChildLocator || c is ModelSkinController), "Unexpected lobby component");
        Console.WriteLine("PASS: " + assertions + " assertions; 15 primary/skin callback combinations, 5 repetitions, owner isolation/swap/reuse, lobby swap sound, immediate events, late skin completion, cloak flags, disable/re-enable, body skill changes. Game APIs are doubles; live gameplay remains a playtest.");
    }
}
