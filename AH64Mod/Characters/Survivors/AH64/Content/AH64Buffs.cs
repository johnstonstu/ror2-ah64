using RoR2;
using UnityEngine;

namespace AH64.Survivors
{
    public static class AH64Buffs
    {
        // the bonus armor granted while Servo Dash's plating is braced
        public static BuffDef platingBuff;

        // Fire Control Radar paint mark — red threat icon on the painted enemy's buff bar
        public static BuffDef radarPaintedBuff;

        // airtime drain pause after a kill; a buff because kills are server-side and airtime is
        // authority-side, and timed buffs already replicate to the owning client
        public static BuffDef killAirtimeBuff;

        public static void Init(AssetBundle assetBundle)
        {
            killAirtimeBuff = Modules.Content.CreateAndAddBuff("AH64KillAirtime", null, Color.white, false, false);
            killAirtimeBuff.isHidden = true;

            platingBuff = Modules.Content.CreateAndAddBuff("AH64PlatingBuff",
                LegacyResourcesAPI.Load<BuffDef>("BuffDefs/HiddenInvincibility").iconSprite,
                Color.white,
                false,
                false);

            Sprite paintIcon = null;
            if (RoR2Content.Equipment.Scanner)
                paintIcon = RoR2Content.Equipment.Scanner.pickupIconSprite;
            if (!paintIcon)
            {
                BuffDef cloak = LegacyResourcesAPI.Load<BuffDef>("BuffDefs/Cloak");
                if (cloak)
                    paintIcon = cloak.iconSprite;
            }

            //isDebuff so it reads as a threat mark on the enemy, not a friendly buff
            radarPaintedBuff = Modules.Content.CreateAndAddBuff("AH64RadarPainted",
                paintIcon,
                new Color(1f, 0.22f, 0.15f),
                false,
                true);
        }
    }
}
