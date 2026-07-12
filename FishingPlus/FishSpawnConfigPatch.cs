using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace FishingPlus
{
    [HarmonyPatch]
    public static class FishSpawnConfigPatch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EntitySpawner.BaseEntitySpawner<EntitySpawner.EntityFishSpawner>), "UpdateSpawns");
        }

        [HarmonyPrefix]
        public static void Prefix(object __instance)
        {
            try
            {
                var spawnConfigs = Traverse.Create(__instance).Field("spawnConfigs").GetValue() as IList;
                if (spawnConfigs == null) return;

                foreach (var config in spawnConfigs)
                {
                    var traverseConfig = Traverse.Create(config);
                    var itemAsset = traverseConfig.Property("SpawnItemAsset").GetValue();

                    if (itemAsset == null) continue;
                    string assetName = Traverse.Create(itemAsset).Property("name").GetValue<string>() ?? itemAsset.ToString();

                    // Prüfen, ob wir Config-Einträge für diesen Fisch haben
                    if (FishConfigManager.OverrideFishConfigs.TryGetValue(assetName, out var overrideConfig))
                    {
                        var currentRarity = traverseConfig.Property("RespawnRarity").GetValue();

                        // 1. WENN DER OVERRIDE FALSE IST: Echte Werte auslesen und in die Config füttern!
                        if (!overrideConfig.Value)
                        {
                            if (currentRarity != null)
                            {
                                // Wert sauber als float auslesen
                                float vanillaChance = Traverse.Create(currentRarity).Property("RespawnIntervalChance").GetValue<float>();
                                // Umrechnen in 0-100% für den User
                                int vanillaChancePercent = Mathf.RoundToInt(vanillaChance * 100f);

                                if (FishConfigManager.SpawnChanceConfigs.TryGetValue(assetName, out var chanceConfig))
                                {
                                    chanceConfig.Value = vanillaChancePercent;
                                }
                            }

                            // Hier lesen wir es als int aus (oder float, falls das Spiel dort float nutzt – mit GetValue<float>() sind wir flexibel)
                            float vanillaMax = Traverse.Create(config).Property("RespawnPopulationMax").GetValue<float>();

                            if (FishConfigManager.SpawnMaxConfigs.TryGetValue(assetName, out var maxConfig))
                            {
                                // Da maxConfig.Value ein int ist, runden wir den ausgelesenen Wert sicherheitshalber in ein int
                                maxConfig.Value = Mathf.RoundToInt(vanillaMax);
                            }
                        }
                        // 2. WENN DER OVERRIDE TRUE IST: Eigene Werte aufzwingen!
                        else
                        {
                            if (FishConfigManager.SpawnChanceConfigs.TryGetValue(assetName, out var chanceConfig) && currentRarity != null)
                            {
                                float unityChance = Mathf.Clamp(chanceConfig.Value / 100f, 0f, 1f);
                                Traverse.Create(currentRarity).Property("RespawnIntervalChance").SetValue(unityChance);
                                Traverse.Create(currentRarity).Property("RespawnIntervalMinutes").SetValue(1);
                            }

                            if (FishConfigManager.SpawnMaxConfigs.TryGetValue(assetName, out var maxConfig))
                            {
                                traverseConfig.Property("RespawnPopulationMax").SetValue(maxConfig.Value);
                            }
                        }

                        Plugin._logger.LogInfo($"[FishSpawnConfigPatch] Processed fish '{assetName}': Override={overrideConfig.Value}, SpawnChance={FishConfigManager.SpawnChanceConfigs[assetName].Value}, SpawnMax={FishConfigManager.SpawnMaxConfigs[assetName].Value}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin._logger.LogError($"Error while live reading/writing fish config: {ex.Message}");
            }
        }
    }
}
