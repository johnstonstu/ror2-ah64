using BepInEx;
using Rewired;
using RoR2;

namespace AH64.Survivors.Components
{
    /// <summary>
    /// The collective's "down" input. RoR2 has no action for it, so it is read directly: a keyboard key
    /// from config, and gamepad B (Circle on PlayStation) through Rewired's gamepad template, which is
    /// unbound in RoR2's default controller layout. Holding jump already uses the right thumb, so the
    /// neighbouring face button keeps both altitude controls under it.
    ///
    /// <para>Local authority only. Anything a remote body needs follows from its motor, not from this.</para>
    /// </summary>
    internal static class AH64DescendInput
    {
        public static bool IsHeld(CharacterBody body)
        {
            LocalUser user = FindLocalUser(body);
            if (user == null || user.isUIFocused)
                return false;

            if (UnityInput.Current.GetKey(AH64PlaytestConfig.DescendKey.MainKey))
                return true;

            Player player = user.inputPlayer;
            if (!AH64PlaytestConfig.ControllerBDescends || player == null)
                return false;

            foreach (Joystick joystick in player.controllers.Joysticks)
            {
                IGamepadTemplate gamepad = joystick.GetTemplate<IGamepadTemplate>();
                if (gamepad != null && gamepad.actionBottomRow2.value)
                    return true;
            }

            return false;
        }

        private static LocalUser FindLocalUser(CharacterBody body)
        {
            if (!body)
                return null;

            foreach (LocalUser user in LocalUserManager.readOnlyLocalUsersList)
            {
                if (user.cachedBody == body)
                    return user;
            }

            return null;
        }
    }
}
