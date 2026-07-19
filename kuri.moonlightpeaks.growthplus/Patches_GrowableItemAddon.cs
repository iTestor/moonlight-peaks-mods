using System;
using HarmonyLib;

namespace kuri.moonlightpeaks.growthplus
{
    [HarmonyPatch(typeof(GrowableItemAddon))]
    [HarmonyPatch("GetRegrowthTime")]
    public static class GrowableItemAddon_GetRegrowthTime_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(ref int __result)
        {
            if (!Plugin.GrowthPlusEnabled.Value) return;

            int speedPercent = Plugin.GrowthSpeedConfig.Value;
            if (speedPercent <= 0) return;

            if (speedPercent >= 100)
            {
                __result = 0;
            }
            else
            {
                float reductionMultiplier = 1f - (speedPercent / 100f);
                __result = Math.Max(1, (int)Math.Round(__result * reductionMultiplier));
            }
        }
    }

    /*[HarmonyPatch(typeof(GrowableItemAddon), "GetTotalGrowthTime")]
    public static class GrowableItemAddon_GetTotalGrowthTime_Patch_Prefix
    {
        [HarmonyPrefix]
        public static bool Prefix(GrowableItemAddon __instance, out int stages, ref int __result)
        {
            if (!Plugin.GrowthPlusEnabled.Value)
            {
                stages = 0;
                return true;
            }

            int totalDuration = 0;
            stages = 0;
            __result = 0;

            if (__instance.Item.ParametersAddon == null)
            {
                return false;
            }

            int speedPercent = Plugin.GrowthSpeedConfig.Value;
            var category = AddressableLibrary<ItemParameterCategoryLibrary>.Instance.GrowthTime;

            // 1. Zuerst zählen wir, wie viele Stufen es überhaupt gibt
            var parameters = __instance.Item.ParametersAddon.GetParametersInCategory(category);
            foreach (var itemParameter in parameters)
            {
                if (itemParameter.Name.ToLower() != "regrowth")
                {
                    stages++;
                }
            }

            // 2. Jetzt verteilen wir die manipulierte Zeit
            foreach (var itemParameter in parameters)
            {
                if (itemParameter.Name.ToLower() != "regrowth" && itemParameter.TryReadValue<int>(out int originalTime))
                {
                    int newTime;

                    if (speedPercent >= 100)
                    {
                        // Wenn 100% eingestellt ist, versuchen wir absolute Lichtgeschwindigkeit:
                        // Jede Stufe bekommt 0 Tage. Wenn das Spiel damit skaliert, 
                        // wächst es sofort bzw. am nächsten Morgen.
                        newTime = 0;
                    }
                    else
                    {
                        float reductionMultiplier = 1f - (speedPercent / 100f);
                        // Erlaubt Werte bis 0 runter, wenn der Prozentwert hoch genug ist!
                        newTime = Math.Max(0, (int)Math.Round(originalTime * reductionMultiplier));
                    }

                    totalDuration += newTime;
                }
            }

            // Falls totalDuration 0 ist (bei 100%), manche Spiele brauchen mindestens 1 im Gesamtergebnis
            __result = totalDuration;
            return false;
        }

        [HarmonyPatch(typeof(GrowableItemAddon), "GetTotalGrowthTime")]
        public static class GrowableItemAddon_GetTotalGrowthTime_Patch_Postfix
        {
            [HarmonyPostfix]
            public static void Postfix(GrowableItemAddon __instance, int stages, ref int __result)
            {
                // Wichtig: Bei einem Postfix wird aus 'out int' ein normales 'int', 
                // da der Wert zu diesem Zeitpunkt bereits vom Spiel generiert wurde.
                Plugin._logger.LogDebug($"[GetTotalGrowthTime][{__instance.GetProduceItemAsset().AssetName}] Total Days: {__result}, Total Stages: {stages}");
            }
        }

        [HarmonyPatch(typeof(GrowableItemAddon), "GetRegrowthTime")]
        public static class GrowableItemAddon_GetRegrowthTime_Patch_Prefix
        {
            [HarmonyPrefix]
            public static bool Prefix(GrowableItemAddon __instance, ref int __result)
            {
                if (!Plugin.GrowthPlusEnabled.Value)
                {
                    return true;
                }

                int totalRegrowthDuration = 0;
                __result = 0;

                if (__instance.Item.ParametersAddon == null)
                {
                    return false;
                }

                int speedPercent = Plugin.GrowthSpeedConfig.Value;
                var category = AddressableLibrary<ItemParameterCategoryLibrary>.Instance.GrowthTime;

                foreach (var itemParameter in __instance.Item.ParametersAddon.GetParametersInCategory(category))
                {
                    if (itemParameter.Name.ToLower() == "regrowth" && itemParameter.TryReadValue<int>(out int originalTime))
                    {
                        int newTime;

                        if (speedPercent >= 100)
                        {
                            newTime = 1;
                        }
                        else if (speedPercent <= 0)
                        {
                            newTime = originalTime;
                        }
                        else
                        {
                            float reductionMultiplier = 1f - (speedPercent / 100f);
                            newTime = Math.Max(1, (int)Math.Round(originalTime * reductionMultiplier));
                        }

                        totalRegrowthDuration += newTime;
                    }
                }

                __result = totalRegrowthDuration;
                return false; // Originalmethode überspringen
            }
        }

        [HarmonyPatch(typeof(GrowableItemAddon), "GetRegrowthTime")]
        public static class GrowableItemAddon_GetRegrowthTime_Patch_Postfix
        {
            [HarmonyPostfix]
            public static void Postfix(GrowableItemAddon __instance, ref int __result)
            {
                Plugin._logger.LogDebug($"[GrowableItemAddon_GetRegrowthTime_Patch][{__instance.GetProduceItemAsset().AssetName}] Postfix called. Original stages: {__result}");
            }
        }
    }*/
}