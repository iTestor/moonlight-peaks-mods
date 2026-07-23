using System;
using System.Collections;
using System.Reflection;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace kuri.moonlightpeaks.critterplus
{
    /// <summary>
    /// PURE DIAGNOSTIC PATCH - changes no behaviour, only logs additional information.
    ///
    /// Goal: find out why critter with an active override (e.g. Glibby: SpawnChance=100%,
    /// MaxLimit=10) never respawn via HandleTimeTick() -> UpdateSpawns() after the initial
    /// start spawn (SpawnStartBehaviour), even though RespawnPopulationMax and
    /// RespawnIntervalChance/-Minutes should actually allow it.
    ///
    /// Patch 1 (Prefix on HandleTimeTick): shows WHETHER the tick even arrives for this
    /// spawner, and at what interval.
    ///
    /// Patch 2 (Postfix on UpdateSpawns): shows the complete persisted state (SpawnCount,
    /// WasAllowedToSpawn, RespawnPopulationMax, LastIntervalHour/Minute) AFTER every run, for
    /// every critter with an active override - making it visible whether e.g. WasAllowedToSpawn=false
    /// gets stuck (a Director/biome check fails), whether SpawnCount has already reached
    /// RespawnPopulationMax, or whether the interval clock (LastIntervalHour/Minute) isn't advancing.
    /// </summary>
    [HarmonyPatch]
    public static class CritterHandleTimeTickDebugPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Plugin.LogDebug("[CritterHandleTimeTickDebugPatch] Resolving TargetMethod(): EntityBasicCritterSpawner.HandleTimeTick(int,int)");
            MethodBase method = AccessTools.Method(typeof(EntityBasicCritterSpawner), "HandleTimeTick");

            if (method == null)
                Plugin._logger.LogError("[CritterHandleTimeTickDebugPatch] Could NOT find HandleTimeTick()! The debug patch will have no effect.");
            else
                Plugin.LogDebug($"[CritterHandleTimeTickDebugPatch] TargetMethod resolved: {method.DeclaringType?.FullName}.{method.Name}");

            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance, int hour, int minute)
        {
            try
            {
                // Only log for EntityBasicCritterSpawner instances, so the log isn't flooded with
                // critter spawners.
                if (!(__instance is EntityBasicCritterSpawner))
                    return;

                Plugin.LogDebug($"[CritterHandleTimeTickDebugPatch] HandleTimeTick() FIRING for EntityBasicCritterSpawner (InstanceID: {(__instance as UnityEngine.Object)?.GetInstanceID().ToString() ?? "n/a"}) -> hour={hour}, minute={minute}.");
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[CritterHandleTimeTickDebugPatch] Exception in debug prefix: {ex}");
            }
        }
    }

    [HarmonyPatch]
    public static class CritterUpdateSpawnsDebugPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Plugin.LogDebug("[CritterUpdateSpawnsDebugPatch] Resolving TargetMethod(): EntityBasicCritterSpawner.UpdateSpawns()");
            MethodBase method = AccessTools.Method(typeof(EntityBasicCritterSpawner), "UpdateSpawns");

            if (method == null)
                Plugin._logger.LogError("[CritterUpdateSpawnsDebugPatch] Could NOT find UpdateSpawns()! The debug patch will have no effect.");
            else
                Plugin.LogDebug($"[CritterUpdateSpawnsDebugPatch] TargetMethod resolved: {method.DeclaringType?.FullName}.{method.Name}");

            return method;
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                if (!(__instance is EntityBasicCritterSpawner))
                    return;

                string instanceId = (__instance as UnityEngine.Object)?.GetInstanceID().ToString() ?? "n/a";

                FieldInfo spawnConfigsField = ReflectionHelpers.FindFieldInHierarchy(__instance.GetType(), "spawnConfigs");
                FieldInfo persistenceField = ReflectionHelpers.FindFieldInHierarchy(__instance.GetType(), "persistence");

                if (spawnConfigsField == null || persistenceField == null)
                {
                    Plugin.LogDebug("[CritterUpdateSpawnsDebugPatch] Field 'spawnConfigs' or 'persistence' not found -> cannot log state.");
                    return;
                }

                object rawList = spawnConfigsField.GetValue(__instance);
                object persistenceObj = persistenceField.GetValue(__instance);

                if (!(rawList is IEnumerable configList))
                {
                    Plugin.LogDebug("[CritterUpdateSpawnsDebugPatch] 'spawnConfigs' is not an IEnumerable or is null.");
                    return;
                }

                if (persistenceObj == null)
                {
                    Plugin.LogDebug("[CritterUpdateSpawnsDebugPatch] 'persistence' is null.");
                    return;
                }

                object entriesObj = ReflectionHelpers.GetMemberValue(persistenceObj, "Entries");
                if (entriesObj == null)
                {
                    Plugin.LogDebug("[CritterUpdateSpawnsDebugPatch] 'persistence.Entries' is null or not found.");
                    return;
                }

                MethodInfo findOrCreateMethod = ReflectionHelpers.FindSingleArgMethod(entriesObj.GetType(), "FindOrCreate");
                if (findOrCreateMethod == null)
                {
                    Plugin.LogDebug($"[CritterUpdateSpawnsDebugPatch] Method 'FindOrCreate' not found on type '{entriesObj.GetType().FullName}'.");
                    return;
                }

                Plugin.LogDebug($"[CritterUpdateSpawnsDebugPatch] === UpdateSpawns() completed for EntityBasicCritterSpawner (InstanceID: {instanceId}) ===");

                foreach (object spawnConfig in configList)
                {
                    if (spawnConfig == null)
                        continue;

                    object itemAssetObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnItemAsset");
                    if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj))
                        continue;

                    string assetName = itemAssetUnityObj.name;

                    if (!CritterConfigManager.OverrideCritterConfigs.TryGetValue(assetName, out var overrideEntry) || !overrideEntry.Value)
                        continue; // only log critter with an active override

                    object entryObj;
                    try
                    {
                        entryObj = findOrCreateMethod.Invoke(entriesObj, new object[] { itemAssetObj });
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogDebug($"[CritterUpdateSpawnsDebugPatch] '{assetName}': FindOrCreate() could not be invoked: {ex.Message}");
                        continue;
                    }

                    if (entryObj == null)
                    {
                        Plugin.LogDebug($"[CritterUpdateSpawnsDebugPatch] '{assetName}': FindOrCreate() returned null.");
                        continue;
                    }

                    object spawnCount = ReflectionHelpers.GetMemberValue(entryObj, "SpawnCount");
                    object wasAllowed = ReflectionHelpers.GetMemberValue(entryObj, "WasAllowedToSpawn");
                    object respawnPopMax = ReflectionHelpers.GetMemberValue(entryObj, "RespawnPopulationMax");
                    object lastHour = ReflectionHelpers.GetMemberValue(entryObj, "LastIntervalHour");
                    object lastMinute = ReflectionHelpers.GetMemberValue(entryObj, "LastIntervalMinute");

                    int configuredMax = CritterConfigManager.GetEffectiveMaxLimit(assetName);
                    int chancePercent = CritterConfigManager.SpawnChanceConfigs.TryGetValue(assetName, out var chanceCfg) ? chanceCfg.Value : -1;

                    Plugin.LogDebug(
                        $"[CritterUpdateSpawnsDebugPatch] '{assetName}': " +
                        $"SpawnCount={spawnCount}, RespawnPopulationMax(persisted)={respawnPopMax}, " +
                        $"MaxLimit(config/effective)={configuredMax}, SpawnChance(config)={chancePercent}%, " +
                        $"WasAllowedToSpawn={wasAllowed}, LastIntervalHour={lastHour}, LastIntervalMinute={lastMinute}."
                    );
                }
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[CritterUpdateSpawnsDebugPatch] Exception in debug postfix: {ex}");
            }
        }
    }
}