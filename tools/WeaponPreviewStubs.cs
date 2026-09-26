// Minimal game API doubles for lifecycle checks. Production presentation sources are compiled unchanged.
using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object o) => o != null; }
    public class GameObject : Object
    {
        public readonly List<Component> components = new List<Component>();
        public GameObject parent;
        public bool active = true;
        public T Add<T>() where T : Component, new() { var c = new T { gameObject = this }; components.Add(c); return c; }
        public T GetComponent<T>() where T : Component => components.OfType<T>().FirstOrDefault();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T : Component
        { for (var p = gameObject; p != null; p = p.parent) { var c = p.GetComponent<T>(); if (c != null) return c; } return null; }
    }
    public class MonoBehaviour : Component { }
    public class Transform : Component { }
    public class Renderer : Component { public bool enabled; public bool forceRenderingOff; public int skin; }
}
public class ChildLocator : UnityEngine.Component
{
    public readonly Dictionary<string,UnityEngine.Transform> children = new Dictionary<string,UnityEngine.Transform>();
    public UnityEngine.Transform FindChild(string name) => children.TryGetValue(name, out var t) ? t : null;
}
namespace RoR2.Skills
{
    public class SkillDef : UnityEngine.Object { }
    public class SkillFamily : UnityEngine.Object
    {
        public struct Variant { public SkillDef skillDef; }
        public Variant[] variants;
    }
}
namespace RoR2
{
    public enum BodyIndex { None = -1, AH64 = 0 }
    public class GenericSkill : UnityEngine.Component { public Skills.SkillDef skillDef; public Skills.SkillFamily skillFamily; }
    public class SkillLocator : UnityEngine.Component { public GenericSkill primary; }
    public class ModelSkinController : UnityEngine.Component
    {
        public event Action<int> onSkinApplied;
        public void Applied(int index) => onSkinApplied?.Invoke(index);
    }
    public class SurvivorDef : UnityEngine.Object { public UnityEngine.GameObject bodyPrefab; }
    public static class BodyCatalog
    {
        public static UnityEngine.GameObject prefab;
        public static BodyIndex FindBodyIndex(UnityEngine.GameObject body) => body == prefab ? BodyIndex.AH64 : BodyIndex.None;
        public static GenericSkill[] GetBodyPrefabSkillSlots(BodyIndex index) => prefab.components.OfType<GenericSkill>().ToArray();
    }
    public class Loadout
    {
        public readonly Manager bodyLoadoutManager = new Manager();
        public class Manager
        {
            public readonly Dictionary<int,uint> variants = new Dictionary<int,uint>();
            public uint GetSkillVariant(BodyIndex body, int slot) => variants.TryGetValue(slot, out var v) ? v : 0;
        }
    }
    public class NetworkLoadout
    {
        public readonly Loadout data = new Loadout();
        public void CopyLoadout(Loadout target)
        { target.bodyLoadoutManager.variants.Clear(); foreach (var v in data.bodyLoadoutManager.variants) target.bodyLoadoutManager.variants[v.Key] = v.Value; }
    }
    public class NetworkUser : UnityEngine.Object
    {
        public static event Action<NetworkUser> onLoadoutChangedGlobal;
        public readonly NetworkLoadout networkLoadout = new NetworkLoadout();
        public SurvivorDef survivor;
        public SurvivorDef GetSurvivorPreference() => survivor;
        public void Change(uint variant) { networkLoadout.data.bodyLoadoutManager.variants[1] = variant; onLoadoutChangedGlobal?.Invoke(this); }
    }
}
namespace RoR2.SurvivorMannequins
{
    public class SurvivorMannequinSlotController : UnityEngine.Component { public RoR2.NetworkUser networkUser; }
}
namespace AH64
{
    public static class Log { public static void Error(string text) => throw new Exception(text); }
}
namespace AH64.Survivors
{
    public static class AH64Assets
    {
        public static RoR2.Skills.SkillDef gatlingSkillDef = new RoR2.Skills.SkillDef();
        public static RoR2.Skills.SkillDef cannonSkillDef = new RoR2.Skills.SkillDef();
    }
}
