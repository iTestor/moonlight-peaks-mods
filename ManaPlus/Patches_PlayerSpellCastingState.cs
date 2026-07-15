using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ManaPlus
{
    [HarmonyPatch(typeof(PlayerSpellCastingState), "HandleSpellCast")]
    public static class PlayerSpellCastingState_HandleSpellCast_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ItemAsset itemAsset)
        {
            Plugin.CurrentCastingSpell = itemAsset;
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            Plugin.CurrentCastingSpell = null;
        }
    }
}
