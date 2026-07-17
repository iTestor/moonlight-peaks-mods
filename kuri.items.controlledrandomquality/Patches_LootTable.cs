using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using static kuri.items.controlledrandomquality.Plugin;

namespace kuri.items.controlledrandomquality
{
    [HarmonyPatch(typeof(LootTable), "GetEntriesWithRedistributedWeights")]
    public static class LootTable_GetEntriesWithRedistributedWeights_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref List<LootEntry> __result)
        {
            if (__result == null || __result.Count == 0) return;

            if (Plugin.QualityOverrideConfig.Value == ConfigQualityOverride.Random)
            {
                return;
            }

            ConfigQualityOverride targetConfigQuality = Plugin.QualityOverrideConfig.Value;

            var groupedByItem = __result.GroupBy(e => e.ItemEntry.ItemAsset).ToList();

            List<LootEntry> filteredList = new List<LootEntry>();

            foreach (var group in groupedByItem)
            {
                if (group.Key == null) continue;

                if (group.Count() > 1)
                {
                    LootEntry targetEntry = group.FirstOrDefault(e =>
                        MatchesConfigQuality(e.ItemEntry.ItemQualityLevel, targetConfigQuality)
                    );

                    if (targetEntry.ItemEntry.ItemAsset != null)
                    {
                        filteredList.Add(targetEntry);
                        Plugin._logger.LogDebug($"[ControlledQuality] More Qualities for '{group.Key.name}' found. Just keep: {targetConfigQuality}");
                    }
                    else
                    {
                        LootEntry fallback = group.FirstOrDefault(e => e.ItemEntry.ItemQualityLevel == ItemQualityLevel.Regular);
                        if (fallback.ItemEntry.ItemAsset == null)
                        {
                            fallback = group.First();
                        }
                        filteredList.Add(fallback);
                    }
                }
                else
                {
                    filteredList.AddRange(group);
                }
            }

            __result = filteredList;
        }
        private static bool MatchesConfigQuality(ItemQualityLevel gameQuality, ConfigQualityOverride configQuality)
        {
            return gameQuality.ToString().Equals(configQuality.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
