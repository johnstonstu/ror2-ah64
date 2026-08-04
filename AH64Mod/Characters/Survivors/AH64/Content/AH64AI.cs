using RoR2;
using RoR2.CharacterAI;
using UnityEngine;

namespace AH64.Survivors
{
    /// <summary>
    /// AI for when the AH-64 is not player-controlled (Artifact of Vengeance, Goobo).
    /// <para>Drivers are evaluated top to bottom, so the most situational skills are registered first and
    /// the plain chase is the fallback. Ranges here mirror the kit — which is now entirely ranged, so no
    /// driver should ever be closing to melee.</para>
    /// </summary>
    public static class AH64AI
    {
        public static void Init(GameObject bodyPrefab, string masterName)
        {
            GameObject master = Modules.Prefabs.CreateBlankMasterPrefab(bodyPrefab, masterName);

            BaseAI baseAI = master.GetComponent<BaseAI>();
            baseAI.aimVectorDampTime = 0.1f;
            baseAI.aimVectorMaxSpeed = 360;

            //mouse over these fields for tooltips

            //close the gap when the target is a medium distance out, using the dash's armor aggressively
            AISkillDriver dashDriver = master.AddComponent<AISkillDriver>();
            //Selection Conditions
            dashDriver.customName = "Use Utility Evasive Roll";
            dashDriver.skillSlot = SkillSlot.Utility;
            dashDriver.requiredSkill = null; //usually used when you have skills that override other skillslots like engi harpoons
            dashDriver.requireSkillReady = true;
            dashDriver.requireEquipmentReady = false;
            dashDriver.minUserHealthFraction = float.NegativeInfinity;
            dashDriver.maxUserHealthFraction = float.PositiveInfinity;
            dashDriver.minTargetHealthFraction = float.NegativeInfinity;
            dashDriver.maxTargetHealthFraction = float.PositiveInfinity;
            dashDriver.minDistance = 12;
            dashDriver.maxDistance = 40;
            dashDriver.selectionRequiresTargetLoS = true;
            dashDriver.selectionRequiresOnGround = false;
            dashDriver.selectionRequiresAimTarget = false;
            dashDriver.maxTimesSelected = -1;

            //Behavior
            dashDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            dashDriver.activationRequiresTargetLoS = false;
            dashDriver.activationRequiresAimTargetLoS = false;
            dashDriver.activationRequiresAimConfirmation = false;
            dashDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            dashDriver.moveInputScale = 1;
            dashDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            dashDriver.ignoreNodeGraph = false; //will chase relentlessly but be kind of stupid
            dashDriver.shouldSprint = true;
            dashDriver.shouldFireEquipment = false;
            dashDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;

            //Transition Behavior
            dashDriver.driverUpdateTimerOverride = -1;
            dashDriver.resetCurrentEnemyOnNextDriverSelection = false;
            dashDriver.noRepeat = false;
            dashDriver.nextHighPriorityOverride = null;

            //some fields omitted that aren't commonly changed. will be set to default values

            //Longbow (variant 0) uses Engi-style primary confirm locks — AI still can't drive that well.
            //Leave the driver pointing at Special for Hellfire loadouts; with Longbow this mostly no-ops.
            AISkillDriver rocketDriver = master.AddComponent<AISkillDriver>();
            //Selection Conditions
            rocketDriver.customName = "Use Special";
            rocketDriver.skillSlot = SkillSlot.Special;
            rocketDriver.requireSkillReady = true;
            rocketDriver.minDistance = 14;
            rocketDriver.maxDistance = 70;
            rocketDriver.selectionRequiresTargetLoS = true;
            rocketDriver.selectionRequiresOnGround = false;
            rocketDriver.selectionRequiresAimTarget = false;
            rocketDriver.maxTimesSelected = -1;

            //Behavior
            rocketDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            rocketDriver.activationRequiresTargetLoS = true;
            rocketDriver.activationRequiresAimTargetLoS = false;
            rocketDriver.activationRequiresAimConfirmation = true;
            rocketDriver.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            rocketDriver.moveInputScale = 1;
            rocketDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            rocketDriver.buttonPressType = AISkillDriver.ButtonPressType.TapContinuous;

            //The secondary is ranged AoE. Drivers select by *slot*, not by SkillDef, and the AI always
            //takes variant 0 of the family — which used to be the melee Piston Punch, so this driver was
            //once capped at 9 units. Leaving a melee range in place would have the AI walk into contact to
            //ripple rockets. Strafes rather than chases, for the same reason, and now carries a minimum:
            //the pods' own 5u blasts hurt at point blank.
            AISkillDriver secondaryDriver = master.AddComponent<AISkillDriver>();
            //Selection Conditions
            secondaryDriver.customName = "Use Secondary Hydra-70 Pods";
            secondaryDriver.skillSlot = SkillSlot.Secondary;
            secondaryDriver.requireSkillReady = true;
            secondaryDriver.minDistance = 8;
            secondaryDriver.maxDistance = 55;
            secondaryDriver.selectionRequiresTargetLoS = true;
            secondaryDriver.selectionRequiresOnGround = false;
            secondaryDriver.selectionRequiresAimTarget = false;
            secondaryDriver.maxTimesSelected = -1;

            //Behavior
            secondaryDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            secondaryDriver.activationRequiresTargetLoS = true;
            secondaryDriver.activationRequiresAimTargetLoS = false;
            secondaryDriver.activationRequiresAimConfirmation = true;
            secondaryDriver.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            secondaryDriver.moveInputScale = 1;
            secondaryDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            secondaryDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;

            //the default behaviour: strafe and hose the target down with the chain gun
            AISkillDriver chaingunDriver = master.AddComponent<AISkillDriver>();
            //Selection Conditions
            chaingunDriver.customName = "Use Primary M230 Chain Gun";
            chaingunDriver.skillSlot = SkillSlot.Primary;
            chaingunDriver.requireSkillReady = false; //usually false for primaries
            chaingunDriver.minDistance = 0;
            chaingunDriver.maxDistance = 60;
            chaingunDriver.selectionRequiresTargetLoS = true;
            chaingunDriver.selectionRequiresOnGround = false;
            chaingunDriver.selectionRequiresAimTarget = false;
            chaingunDriver.maxTimesSelected = -1;

            //Behavior
            chaingunDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            chaingunDriver.activationRequiresTargetLoS = true;
            chaingunDriver.activationRequiresAimTargetLoS = false;
            chaingunDriver.activationRequiresAimConfirmation = true;
            chaingunDriver.movementType = AISkillDriver.MovementType.StrafeMovetarget;
            chaingunDriver.moveInputScale = 1;
            chaingunDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            chaingunDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;

            //fallback for when nothing above is selectable (no line of sight, target too far)
            AISkillDriver chaseDriver = master.AddComponent<AISkillDriver>();
            //Selection Conditions
            chaseDriver.customName = "Chase";
            chaseDriver.skillSlot = SkillSlot.None;
            chaseDriver.requireSkillReady = false;
            chaseDriver.minDistance = 0;
            chaseDriver.maxDistance = float.PositiveInfinity;

            //Behavior
            chaseDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
            chaseDriver.activationRequiresTargetLoS = false;
            chaseDriver.activationRequiresAimTargetLoS = false;
            chaseDriver.activationRequiresAimConfirmation = false;
            chaseDriver.movementType = AISkillDriver.MovementType.ChaseMoveTarget;
            chaseDriver.moveInputScale = 1;
            chaseDriver.aimType = AISkillDriver.AimType.AtMoveTarget;
            chaseDriver.shouldSprint = true;
            chaseDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;

            //recommend taking these for a spin in game, messing with them in runtimeinspector to get a feel for what they should do at certain ranges and such
        }
    }
}
