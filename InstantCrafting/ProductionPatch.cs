using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace InstantCrafting
{
    //-- Mana Extractor Patch
    [HarmonyPatch(typeof(ManaExtractorPersistence.ExtractEntry), nameof(ManaExtractorPersistence.ExtractEntry.IsDone))]
    public static class ManaExtractor_IsDone_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result)
        {
            // Erzwingt, dass die Extraktion immer sofort als fertig (true) gilt
            if (Plugin.InstantProduction.Value) __result = true;
        }
    }

    //-- BaseResourceConverterView Patch
    [HarmonyPatch]
    public static class BaseResourceConverterViewPatch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BaseResourceConverterView), "UpdateProductionProgress");
        }

        [HarmonyPrefix]
        public static bool Prefix(BaseResourceConverterView __instance)
        {
            if (!Plugin.InstantProduction.Value) return true;
            if (!__instance.IsProducingItems) return true;

            __instance.Inventory.UpdateProgress(999999f, 9999, out bool flag);

            if (flag) __instance.ResourceConverterPersistence.UpdateActiveProducingItem();

            if (__instance.IsProducingItems)
            {
                __instance.Invoke("HandleProductionUpdate", 0f);
            }
            else
            {
                AccessTools.Method(typeof(BaseResourceConverterView), "HandleProductionEnd")
                           .Invoke(__instance, null);
            }

            return false;
        }
    }
}
