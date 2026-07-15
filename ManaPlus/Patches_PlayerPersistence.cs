using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ManaPlus
{
    [HarmonyPatch(typeof(PlayerPersistence), "SubstractMana")]
    public static class PlayerPersistence_SubstractMana_Patch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float amount)
        {
            if (!Plugin.ManaPlusIsActive.Value)
            {
                return;
            }

            if (Plugin.ManaDrainIndividual.Value && Plugin.CurrentCastingSpell != null)
            {
                float customCost = IndividualSpells.GetCostForSpell(Plugin.CurrentCastingSpell);

                amount = customCost;
            }
            else
            {
                amount = amount * Plugin.ManaDrainMultiplier.Value;
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPersistence), "AddMana")]
    static class PlayerPersistence_AddMana_Patch
    {
        static void Prefix(ref float amount)
        {
            if (!Plugin.ManaPlusIsActive.Value)
            {
                return;
            }
            amount = amount * Plugin.ManaGainMultiplier.Value;
        }
    }
}
