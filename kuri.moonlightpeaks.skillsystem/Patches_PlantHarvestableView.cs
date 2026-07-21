using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace kuri.moonlightpeaks.skillsystem
{
    internal class Patches_PlantHarvestableView
    {

        [HarmonyPatch(typeof(PlantHarvestableView), "HarvestAndDestroyIfNeeded")]
        public class PlantHarvestableView_HarvestAndDestroyIfNeeded_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(PlantHarvestableView __instance) // Direkt den echten Typen nutzen!
            {
                // Sicherheits-Check, falls __instance unerwartet null ist
                if (__instance == null) return;

                // Da wir nun den echten Typen haben, können wir direkt auf die Properties zugreifen
                if (__instance.IsHarvestable)
                {
                    string cropName = __instance.ItemAsset != null ? __instance.ItemAsset.name : "UnknownCrop";

                    Plugin._logger.LogDebug($"Harvested {cropName}! Granting Farming EXP.");

                    // XpManager.AddFarmingXp(10); 
                }
            }
        }

    }
}
