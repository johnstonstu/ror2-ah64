using RoR2;
using UnityEngine;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// Luminous Shot gains a stack on every secondary activation, and every Hydra rocket is one, so a
    /// single ripple filled the whole buff where Commando's Phase Round earns one stack. The pods opt
    /// out of the automatic handling (<c>autoHandleLuminousShot = false</c>) and this grants one stack
    /// per ripple instead. Server only: <c>onSkillActivatedServer</c> never fires on clients.
    /// </summary>
    public class AH64LuminousRipple : MonoBehaviour
    {
        private CharacterBody body;
        private float lastSecondaryTime = float.NegativeInfinity;

        private void Awake()
        {
            body = GetComponent<CharacterBody>();
        }

        private void OnEnable()
        {
            if (body)
                body.onSkillActivatedServer += OnSkillActivatedServer;
        }

        private void OnDisable()
        {
            if (body)
                body.onSkillActivatedServer -= OnSkillActivatedServer;
        }

        private void OnSkillActivatedServer(GenericSkill skill)
        {
            if (!body.skillLocator || skill != body.skillLocator.secondary
                || !skill.skillDef || skill.skillDef.autoHandleLuminousShot)
                return;

            float now = Time.fixedTime;
            float interval = AH64StaticValues.hydraFireInterval / Mathf.Max(body.attackSpeed, 0.01f);
            bool newRipple = now - lastSecondaryTime > Mathf.Max(AH64StaticValues.luminousRippleGap, 2.5f * interval);
            lastSecondaryTime = now;

            if (newRipple
                && body.inventory
                && body.inventory.GetItemCountEffective(DLC2Content.Items.IncreasePrimaryDamage) > 0)
                body.AddIncreasePrimaryDamageStack();
        }
    }
}
