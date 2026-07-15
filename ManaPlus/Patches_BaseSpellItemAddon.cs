using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ManaPlus
{
    [HarmonyPatch(typeof(BaseSpellItemAddon), "PlayerHasEnoughCurrentMana", MethodType.Getter)]
    public static class BaseSpellItemAddon_PlayerHasEnoughCurrentMana_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(BaseSpellItemAddon __instance, ref bool __result)
        {
            if (!Plugin.ManaPlusIsActive.Value)
            {
                return true;
            }

            ItemAsset currentSpell = Plugin.CurrentCastingSpell;

            if (currentSpell != null)
            {
                float requiredMana;

                if (Plugin.ManaDrainIndividual.Value)
                {
                    requiredMana = IndividualSpells.GetCostForSpell(currentSpell);
                }
                else
                {
                    requiredMana = __instance.ManaUsage * Plugin.ManaDrainMultiplier.Value;
                }

                __result = GamePersistence.Instance.Player.Mana >= requiredMana;

                return false;
            }

            return true;
        }
    }
}
