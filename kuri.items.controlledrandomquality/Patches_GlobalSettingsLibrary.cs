using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static kuri.items.controlledrandomquality.Plugin;

namespace kuri.items.controlledrandomquality
{
    internal class Patches_GlobalSettingsLibrary
    {
        [HarmonyPatch(typeof(GlobalSettingsLibrary), "GetRandomizedItemQualityLevel")]
        public static class ControlledRandomQualityPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(ItemAsset itemAsset, ref ItemQualityLevel __result)
            {

                ConfigQualityOverride selectedOverride = Plugin.QualityOverrideConfig.Value;

                if (selectedOverride == ConfigQualityOverride.Random)
                {
                    return true;
                }

                // --- LOST ITEM CHECK ---
                if (itemAsset != null && IsActiveLostItem(itemAsset))
                {
                    return true;
                }


                switch (selectedOverride)
                {
                    case ConfigQualityOverride.Regular:
                        __result = ItemQualityLevel.Regular;
                        break;
                    case ConfigQualityOverride.Good:
                        __result = ItemQualityLevel.Good;
                        break;
                    case ConfigQualityOverride.Perfect:
                        __result = ItemQualityLevel.Perfect;
                        break;
                }

                return false;
            }

            private static bool IsActiveLostItem(ItemAsset itemAsset)
            {
                try
                {
                    var gamePersistence = GamePersistence.Instance;
                    if (gamePersistence?.JobBoard?.ActiveJobs == null)
                    {
                        return false;
                    }

                    foreach (var job in gamePersistence.JobBoard.ActiveJobs)
                    {
                        if (job == null) continue;

                        var lostItemSub = job.LostItemJobPersistence;
                        if (lostItemSub != null && lostItemSub.LostItem.ItemAsset != null)
                        {
                            if (lostItemSub.LostItem.ItemAsset == itemAsset)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                }

                return false;
            }
        }
    }
}
