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
            //Keep sprint available for horizontal travel. rawMoveDown is RoR2's existing bound
            //downward-movement input and works for controller/alternate bindings without coupling
            //altitude loss to the sprint speed modifier.
            bool descendHeld = inputBank && inputBank.rawMoveDown.down;
            hoverController.ApplyHover(jumpHeld, descendHeld, Time.fixedDeltaTime);
        }

        /// <summary>
        /// Jump is the collective, not a vanilla jump — read as a held key in <see cref="HandleMovements"/>.
        /// Extra jump count from items raises the collective ceiling instead.
        /// </summary>
        public override void ProcessJump()
        {
            jumpInputReceived = false;
        }
    }
}
