using System;
using System.Collections;
using System.Reflection;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace kuri.moonlightpeaks.critterplus
{
    /// <summary>
    /// Enforces the configured MaxLimit as a hard cap directly at the spawn call.
    /// Necessary because BaseEntitySpawner first restores the number of critter persisted in the
    /// save game during Setup() (see "for (int i = 0; i < entry.SpawnCount; i++)"), and that
    /// happens BEFORE and INDEPENDENTLY of SpawnStartChance/RespawnIntervalChance.
    /// In other words, SpawnChance=0% only prevents new spawns - it does not remove critter that
    /// are already saved. This patch ensures that no more than MaxLimit critter ever exist at the
    /// same time, no matter which code path the spawn call comes from.
    /// </summary>
    [HarmonyPatch]
    public static class CritterSpawnLimitPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Plugin.LogDebug("[CritterSpawnLimitPatch] Resolving TargetMethod(): EntityBasicCritterSpawner.SpawnSpawnable(SpawnConfig)");
            MethodBase method = AccessTools.Method(typeof(EntityBasicCritterSpawner), "SpawnSpawnable");

            if (method == null)
                Plugin._logger.LogError("[CritterSpawnLimitPatch] Could NOT find SpawnSpawnable()! The limit patch will have no effect.");
            else
                Plugin.LogDebug($"[CritterSpawnLimitPatch] TargetMethod resolved: {method.DeclaringType?.FullName}.{method.Name}");

            return method;
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, object spawnConfig)
        {
            try
            {
                bool allow = ShouldAllowSpawn(__instance, spawnConfig);
                Plugin.LogDebug($"[CritterSpawnLimitPatch] Prefix result: allow={allow}");
                return allow;
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[CritterSpawnLimitPatch] Error while checking limit, allowing spawn to be safe: {ex}");
                return true;
            }
        }

        private static bool ShouldAllowSpawn(object spawnerInstance, object spawnConfig)
        {
            if (spawnConfig == null)
            {
                Plugin.LogDebug("[CritterSpawnLimitPatch] spawnConfig is null, allowing original behaviour.");
                return true;
            }

            object itemAssetObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnItemAsset");
            if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj))
            {
                Plugin.LogDebug("[CritterSpawnLimitPatch] SpawnItemAsset is not a UnityEngine.Object, allowing original behaviour.");
                return true;
            }

            string assetName = itemAssetUnityObj.name;
            Plugin.LogDebug($"[CritterSpawnLimitPatch] Checking limit for '{assetName}'.");

            if (!CritterConfigManager.OverrideCritterConfigs.TryGetValue(assetName, out var overrideEntry))
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}' is not a known critter, no limit check, allowing spawn.");
                return true;
            }

            if (!overrideEntry.Value)
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': override disabled, no limit check, allowing spawn.");
                return true;
            }

            int maxLimit = CritterConfigManager.GetEffectiveMaxLimit(assetName); // 0 if SpawnChance=0

            FieldInfo spawnEntriesField = ReflectionHelpers.FindFieldInHierarchy(spawnerInstance.GetType(), "spawnEntries");
            if (spawnEntriesField == null)
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': field 'spawnEntries' not found, cannot check limit -> allowing spawn.");
                return true;
            }

            if (!(spawnEntriesField.GetValue(spawnerInstance) is IDictionary spawnEntries))
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': 'spawnEntries' is not an IDictionary -> allowing spawn.");
                return true;
            }

            if (!spawnEntries.Contains(spawnConfig))
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': no SpawnEntry exists yet for this spawnConfig -> allowing spawn.");
                return true;
            }

            object spawnEntry = spawnEntries[spawnConfig];
            int instantiatedCount = ReflectionHelpers.GetCollectionCount(spawnEntry, "InstantiatedSpawns");
            int trackedCount = ReflectionHelpers.GetCollectionCount(spawnEntry, "TrackedSpawns"); // logging only, see note below

            // IMPORTANT: according to the original code, TrackedSpawns is only cleared again
            // when the spawner is destroyed (DestroyTracked), NOT after an instance has
            // successfully spawned. If we counted TrackedSpawns, the counter would keep
            // increasing after every spawn attempt and would eventually block every further
            // spawn, even though fewer critter actually exist. That's why the limit check counts
            // only InstantiatedSpawns.
            int currentCount = instantiatedCount;

            Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': instantiated={instantiatedCount} (counted), tracked={trackedCount} (log only, ignored), MaxLimit={maxLimit}.");

            if (currentCount >= maxLimit)
            {
                Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': limit reached ({currentCount}/{maxLimit}) -> spawn BLOCKED.");
                return false;
            }

            Plugin.LogDebug($"[CritterSpawnLimitPatch] '{assetName}': limit not yet reached ({currentCount}/{maxLimit}) -> spawn allowed.");
            return true;
        }
    }
}