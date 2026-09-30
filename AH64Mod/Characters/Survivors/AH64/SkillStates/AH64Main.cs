using AH64.Survivors.Components;
using EntityStates;
using UnityEngine;

namespace AH64.Survivors.SkillStates
{
    /// <summary>
    /// The chopper's main "Body" state. Everything a ground survivor does is inherited unchanged; this
    /// only takes over the vertical axis, handing it to <see cref="AH64HoverController"/>.
    /// </summary>
    public class AH64Main : GenericCharacterMain
    {
        private AH64HoverController hoverController;

        public override void OnEnter()
        {
            base.OnEnter();
            hoverController = gameObject.GetComponent<AH64HoverController>();
        }

        public override void HandleMovements()
        {
            base.HandleMovements();

            if (!isAuthority || !hoverController)
                return;

            bool jumpHeld = inputBank && inputBank.jump.down;
            //Descend is its own button (controller B, which RoR2 leaves unbound, or a keyboard key) so
            //pulling the stick back strafes backwards instead of dropping altitude.
            //Classic controls keep 1.1's pull-back-to-descend.
            bool descendHeld = AH64PlaytestConfig.ClassicAltitude
                ? inputBank && inputBank.rawMoveDown.down
                : AH64DescendInput.IsHeld(characterBody);
            hoverController.ApplyHover(jumpHeld, descendHeld, Time.fixedDeltaTime);
        }

        /// <summary>
        /// Jump is the collective, not a vanilla jump — read as a held key in <see cref="HandleMovements"/>.
        /// Extra jump count from items raises the collective ceiling instead. The press itself still
        /// stands in for a jump for items (Wax Quail, onJump listeners) — see AH64HoverController.Items.
        /// </summary>
        public override void ProcessJump()
        {
            if (jumpInputReceived && hoverController)
                hoverController.OnCollectiveTapped();

            jumpInputReceived = false;
        }
    }
}
