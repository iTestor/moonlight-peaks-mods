using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace InstantCrafting
{
    public static class CraftingInstantState
    {
        public static bool Active;
    }

    // Flag setzen, sobald das Microgame startet
    [HarmonyPatch(typeof(CraftingMicrogame), "Initialize")]
    public static class Patch_CraftingMicrogame_Initialize
    {
        static void Prefix()
        {
            CraftingInstantState.Active = Plugin.InstantCrafting.Value;
        }
    }

    // Flag wieder zurücksetzen, sobald das Microgame fertig ist
    [HarmonyPatch(typeof(CraftingMicrogame), "OnDestroy")]
    public static class Patch_CraftingMicrogame_OnDestroy
    {
        static void Postfix()
        {
            CraftingInstantState.Active = false;
        }
    }

    // Difficulty nur überschreiben, wenn das Crafting-Microgame gerade aktiv ist
    [HarmonyPatch(typeof(MicrogameDifficultyAsset), "Difficulty", MethodType.Getter)]
    public static class Patch_Difficulty_OnlyDuringCrafting
    {
        static void Postfix(ref int __result)
        {
            if (CraftingInstantState.Active)
            {
                __result = 0;
            }
        }
    }
}
