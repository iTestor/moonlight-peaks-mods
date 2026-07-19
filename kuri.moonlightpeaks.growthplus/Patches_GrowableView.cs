using System;
using System.Reflection;
using HarmonyLib;

namespace kuri.moonlightpeaks.growthplus
{
    [HarmonyPatch(typeof(GrowableView))]
    [HarmonyPatch("ProcessDay")]
    public static class GrowableView_ProcessDay_Patch
    {
        [HarmonyPrefix]
        private static bool Prefix(GrowableView __instance, int day)
        {
            if (!Plugin.GrowthPlusEnabled.Value)
            {
                return true;
            }

            if (__instance.GrowablePersistence == null || __instance.GrowablePersistence.HasBeenReplaced)
            {
                return false;
            }

            string itemName = __instance.ItemAsset?.name ?? "Unknown_Plant";
            int advanceAttempts = GetCustomAdvanceAttempts(__instance);

            Plugin._logger.LogDebug($"[GrowthPlus] Processing {itemName} on Day {day}. Allowed loops: {advanceAttempts}");

            bool hasGrown = false;

            for (int i = 0; i < advanceAttempts; i++)
            {
                GrowStage currentStage = __instance.ItemAsset?.GrowableAddon?.GrowStageContainer?.GetGrowStage(__instance.GrowablePersistence.GrowStageGuid);
                if (currentStage == null) break;

                GrowStage nextStage = null;

                foreach (GrowPath growPath in currentStage.GrowPaths)
                {
                    if (growPath != null && growPath.TargetGrowStage != null && !growPath.GetComponent<DamageTakenRequirement>())
                    {
                        nextStage = growPath.TargetGrowStage;
                        break;
                    }
                }

                if (nextStage == null)
                {
                    Plugin._logger.LogDebug($"[GrowthPlus] {itemName} reached its maximum stage.");
                    break;
                }

                // Serenas Ansatz direkt als kontrollierte Schleife genutzt
                //__instance.SpawnGrowStage(nextStage, true);
                DesiredGrowStageProperty.SetValue(__instance, nextStage);
                __instance.GrowablePersistence.GrowStageGuid = nextStage.Guid;
                hasGrown = true;

                if ((bool)IsDeletedField.GetValue(__instance) || __instance.GrowablePersistence.HasBeenReplaced)
                {
                    return false;
                }
            }

            if (hasGrown)
            {
                __instance.GrowablePersistence.DayGrowStageChanged = day;
                __instance.GrowablePersistence.DayProcessed = day;
            }

            if (!(bool)IsDeletedField.GetValue(__instance) && !__instance.GrowablePersistence.HasBeenReplaced)
            {
                SpreadWithRandomChance.Invoke(__instance, null);
            }

            return false;
        }

        private static int GetCustomAdvanceAttempts(GrowableView growableView)
        {
            int speedPercent = Plugin.GrowthSpeedConfig.Value;

            if (speedPercent <= 0) return 1;
            if (speedPercent >= 100) return 15; // Absolutes Maximum, um jede Pflanze fertigwachsen zu lassen

            if (growableView?.ItemAsset?.GrowableAddon == null) return 1;

            // Holt die echten, unmanipulierten Stufen, da der andere Patch weg ist
            growableView.ItemAsset.GrowableAddon.GetTotalGrowthTime(out int totalStages);
            if (totalStages <= 0) totalStages = 4;

            float percentMultiplier = speedPercent / 100f;
            int calculatedAttempts = (int)Math.Ceiling(totalStages * percentMultiplier);

            return Math.Max(1, calculatedAttempts);
        }

        private static readonly MethodInfo SpreadWithRandomChance = AccessTools.Method(typeof(GrowableView), "SpreadWithRandomChance");
        private static readonly PropertyInfo DesiredGrowStageProperty = AccessTools.Property(typeof(GrowableView), "DesiredGrowStage");
        private static readonly FieldInfo IsDeletedField = AccessTools.Field(typeof(GrowableView), "isDeleted");
    }
}