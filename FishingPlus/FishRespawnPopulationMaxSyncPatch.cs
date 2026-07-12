using System;
using System.Collections;
using System.Reflection;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace FishingPlus
{
    /// <summary>
    /// IMPORTANT FIX for a problem independent of FishSpawnPatch/FishSpawnLimitPatch:
    ///
    /// The original code in UpdateSpawns() does NOT check spawnConfig.RespawnPopulationMax
    /// (the value FishSpawnPatch overrides) for the respawn branch, but instead:
    ///
    ///     entry.SpawnCount < entry.RespawnPopulationMax
    ///
    /// entry.RespawnPopulationMax is the value PERSISTED in the save file
    /// (EntitySpawnerPersistence.Entry), NOT the SpawnConfig field. In the original code this
    /// persisted value is only ever set in a single place:
    ///
    ///     if (!entry.WasAllowedToSpawn && flag)
    ///     {
    ///         entry.WasAllowedToSpawn = true;
    ///         ...
    ///         entry.RespawnPopulationMax = Mathf.FloorToInt(spawnConfig.RespawnPopulationMax);
    ///     }
    ///
    /// -- i.e. only on the ONE-TIME transition from WasAllowedToSpawn false -> true. For fish
    /// without a DirectorCheckEvent (i.e. almost all of them), that happens exactly once in the
    /// lifetime of a save (during the very first Setup()). After that, WasAllowedToSpawn stays
    /// true forever, and this branch never fires again. That means: no matter how often
    /// FishSpawnPatch changes spawnConfig.RespawnPopulationMax afterwards (e.g. because the
    /// player raises a fish's SpawnChance in the config menu), the entry.RespawnPopulationMax
    /// value that actually matters for the respawn roll stays frozen at whatever it was back then.
    ///
    /// Concretely observed: a fish that used to run with MaxLimit=0 (SpawnChance=0) has
    /// entry.RespawnPopulationMax=0 permanently stored in the save file. If SpawnChance is later
    /// raised to e.g. 100% with MaxLimit=10, the check "SpawnCount(0) < RespawnPopulationMax(0)"
    /// stays false forever -> no respawn ever happens again, even though the config and the
    /// SpawnConfig field are both correct.
    ///
    /// This patch fixes that by forcing the persisted entry.RespawnPopulationMax value to the
    /// current effective MaxLimit for every fish with an active override, before EVERY
    /// UpdateSpawns() call. That way the config can change at any time without requiring a save
    /// reload or a lucky WasAllowedToSpawn transition.
    ///
    /// SECOND FIX (live value sync): in addition to the persisted entry.RespawnPopulationMax,
    /// the LIVE values on the SpawnConfig/RespawnRarity object also need to be kept up to date
    /// on every tick. FishSpawnPatch only sets those values ONCE, in the Prefix of Setup() -
    /// i.e. only when the room is first entered. If the player changes the config while the room
    /// is already loaded (so Setup() doesn't run again), spawnConfig.RespawnPopulationMax,
    /// RespawnRarity.RespawnIntervalChance and RespawnRarity.RespawnIntervalMinutes stay frozen
    /// at their old values - but the tick roll in UpdateSpawns() reads those values directly from
    /// the spawnConfig object, not from the config. Result: a freshly enabled fish (e.g. Gnasher
    /// after Goldy) won't spawn despite a correct persisted limit, because the rarity chance is
    /// still at its old (possibly 0%) value. This patch therefore also re-syncs these live values
    /// on EVERY tick - on the RespawnRarity asset already cloned per-fish by FishSpawnPatch, so
    /// there is still no cross-talk between fish sharing the same rarity tier.
    /// </summary>
    [HarmonyPatch]
    public static class FishRespawnPopulationMaxSyncPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Plugin.LogDebug("[FishRespawnPopulationMaxSyncPatch] Resolving TargetMethod(): EntityFishSpawner.UpdateSpawns()");
            MethodBase method = AccessTools.Method(typeof(EntityFishSpawner), "UpdateSpawns");

            if (method == null)
                Plugin._logger.LogError("[FishRespawnPopulationMaxSyncPatch] Could NOT find UpdateSpawns()! The sync patch will have no effect.");
            else
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] TargetMethod resolved: {method.DeclaringType?.FullName}.{method.Name}");

            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            try
            {
                SyncPersistedRespawnPopulationMax(__instance);
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[FishRespawnPopulationMaxSyncPatch] Error while syncing entry.RespawnPopulationMax: {ex}");
            }
        }

        private static void SyncPersistedRespawnPopulationMax(object spawnerInstance)
        {
            FieldInfo persistenceField = ReflectionHelpers.FindFieldInHierarchy(spawnerInstance.GetType(), "persistence");
            if (persistenceField == null)
            {
                Plugin.LogDebug("[FishRespawnPopulationMaxSyncPatch] Field 'persistence' not found, skipping sync.");
                return;
            }

            object persistence = persistenceField.GetValue(spawnerInstance);
            if (persistence == null)
            {
                // During the very first Setup() pass (Prefix of FishSpawnPatch), 'persistence'
                // is still null because it's only assigned inside the original Setup(). However
                // UpdateSpawns() always runs AFTER Room.CallOnLoaded, so persistence should
                // normally always be set by then. Guarding against it anyway, just in case.
                Plugin.LogDebug("[FishRespawnPopulationMaxSyncPatch] 'persistence' is null, skipping sync.");
                return;
            }

            object entries = ReflectionHelpers.GetMemberValue(persistence, "Entries");
            if (entries == null)
            {
                Plugin.LogDebug("[FishRespawnPopulationMaxSyncPatch] 'persistence.Entries' is null, skipping sync.");
                return;
            }

            // IMPORTANT: 'FindOrCreate' is overloaded (e.g. FindOrCreate(ItemAsset) plus other
            // signatures), so GetMethod(string) would throw an AmbiguousMatchException. We
            // collect all single-argument overloads up front and later pick, per spawnConfig,
            // whichever overload's parameter type matches the runtime type of the ItemAsset object.
            MethodInfo[] findOrCreateCandidates = ReflectionHelpers.FindSingleArgMethodOverloads(entries.GetType(), "FindOrCreate");

            if (findOrCreateCandidates.Length == 0)
            {
                Plugin._logger.LogWarning("[FishRespawnPopulationMaxSyncPatch] No matching 'FindOrCreate' method found on 'Entries', skipping sync.");
                return;
            }

            FieldInfo spawnConfigsField = ReflectionHelpers.FindFieldInHierarchy(spawnerInstance.GetType(), "spawnConfigs");
            if (spawnConfigsField == null)
            {
                Plugin.LogDebug("[FishRespawnPopulationMaxSyncPatch] Field 'spawnConfigs' not found, skipping sync.");
                return;
            }

            if (!(spawnConfigsField.GetValue(spawnerInstance) is IEnumerable configList))
            {
                return;
            }

            // Needed for FIX 3 below (entry.SpawnCount desync) - looked up once here instead of
            // once per fish, since it doesn't change during the loop.
            FieldInfo spawnEntriesField = ReflectionHelpers.FindFieldInHierarchy(spawnerInstance.GetType(), "spawnEntries");
            IDictionary spawnEntries = spawnEntriesField?.GetValue(spawnerInstance) as IDictionary;

            foreach (object spawnConfig in configList)
            {
                if (spawnConfig == null) continue;

                object itemAssetObj = ReflectionHelpers.GetMemberValue(spawnConfig, "SpawnItemAsset");
                if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj)) continue;

                string assetName = itemAssetUnityObj.name;

                bool overrideActive = FishConfigManager.OverrideFishConfigs.TryGetValue(assetName, out var overrideEntry) && overrideEntry.Value;

                if (!overrideActive)
                {
                    // Not a known fish, or override disabled. If this SpawnConfig was never
                    // overridden in the first place, there's nothing to do - leave the original
                    // persistence behaviour untouched. But if it WAS overridden before (the
                    // player just turned the override off while the room stayed loaded), its
                    // values are stuck on whatever was last written and need to be restored,
                    // otherwise the fish can never respawn again with its real defaults.
                    if (FishOriginalValueStore.HasSnapshot(spawnConfig))
                    {
                        RestoreOriginalValues(spawnConfig, assetName, entries, findOrCreateCandidates, itemAssetObj, spawnEntries);
                    }
                    continue;
                }

                int maxLimit = FishConfigManager.GetEffectiveMaxLimit(assetName); // 0 if SpawnChance=0

                MethodInfo findOrCreate = ReflectionHelpers.FindMatchingOverload(findOrCreateCandidates, itemAssetObj);

                if (findOrCreate == null)
                {
                    Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': no matching 'FindOrCreate' overload found for parameter type '{itemAssetObj.GetType().FullName}', skipping.");
                    continue;
                }

                object entry = findOrCreate.Invoke(entries, new object[] { itemAssetObj });
                if (entry == null) continue;

                // --- FIX 1: persisted value (entry.RespawnPopulationMax) ---
                int currentPersisted = Convert.ToInt32(ReflectionHelpers.GetMemberValue(entry, "RespawnPopulationMax") ?? 0);
                if (currentPersisted != maxLimit)
                {
                    ReflectionHelpers.SetMemberValue(entry, "RespawnPopulationMax", maxLimit);
                    Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': synced entry.RespawnPopulationMax (persisted): {currentPersisted} -> {maxLimit}.");
                }

                // --- FIX 3: persisted entry.SpawnCount desync ---
                // The persisted entry.SpawnCount is meant to track how many fish of this type
                // currently exist, but it is never decremented when FishSpawnLimitPatch blocks a
                // physical spawn (e.g. while MaxLimit was temporarily 0 due to SpawnChance=0).
                // If a fish that was already counted gets removed/never re-instantiated under
                // that blocked state, SpawnCount stays stuck above the real, physically
                // instantiated count. Since UpdateSpawns() gates every further respawn attempt on
                // "SpawnCount < RespawnPopulationMax", this creates a silent, permanent deadlock
                // that persists even after RespawnPopulationMax above is correctly re-synced. We
                // fix it by clamping SpawnCount down to the actual InstantiatedSpawns count
                // whenever it's too high - never upward, so we only correct the "phantom fish"
                // case without touching legitimate counts.
                SyncSpawnCountToInstantiated(spawnEntries, spawnConfig, entry, assetName);

                // --- FIX 2: live values on spawnConfig / RespawnRarity ---
                // These values are only set once by FishSpawnPatch, during Setup(). If the
                // player changes the config while the room is already loaded, we need to
                // re-sync them here on every tick so UpdateSpawns() doesn't use stale
                // chance/interval values.
                SyncLiveSpawnConfigValues(spawnConfig, assetName, maxLimit);
            }
        }

        /// <summary>
        /// Reverts a previously-overridden fish back to its true original values: the persisted
        /// entry.RespawnPopulationMax, the live spawnConfig.RespawnPopulationMax, the original
        /// (un-cloned) RespawnRarity asset reference, and the original SpawnStartChance. Also
        /// re-applies the same entry.SpawnCount desync correction as the override-active branch
        /// (see FIX 3 there), since disabling the override alone does not fix a SpawnCount that
        /// is already stuck above the real InstantiatedSpawns count. Runs on every tick like the
        /// override branch above, but each individual write is skipped once the value already
        /// matches the original, so this settles down to a no-op after the first tick following
        /// a toggle-off.
        /// </summary>
        private static void RestoreOriginalValues(object spawnConfig, string assetName, object entries, MethodInfo[] findOrCreateCandidates, object itemAssetObj, IDictionary spawnEntries)
        {
            if (!FishOriginalValueStore.TryGet(spawnConfig, out var original))
                return;

            // --- Persisted value (entry.RespawnPopulationMax) ---
            MethodInfo findOrCreate = ReflectionHelpers.FindMatchingOverload(findOrCreateCandidates, itemAssetObj);
            if (findOrCreate != null)
            {
                object entry = findOrCreate.Invoke(entries, new object[] { itemAssetObj });
                if (entry != null)
                {
                    int originalPersisted = Mathf.FloorToInt(original.RespawnPopulationMax);
                    int currentPersisted = Convert.ToInt32(ReflectionHelpers.GetMemberValue(entry, "RespawnPopulationMax") ?? 0);
                    if (currentPersisted != originalPersisted)
                    {
                        ReflectionHelpers.SetMemberValue(entry, "RespawnPopulationMax", originalPersisted);
                        Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': override disabled, restored entry.RespawnPopulationMax (persisted): {currentPersisted} -> {originalPersisted}.");
                    }

                    // --- FIX 3 (same as in the override-active branch): persisted entry.SpawnCount desync ---
                    // Restoring RespawnPopulationMax alone is NOT enough if entry.SpawnCount is
                    // still stuck above the real InstantiatedSpawns count from the time the
                    // override was active (e.g. MaxLimit was temporarily 0 and blocked
                    // instantiation while SpawnCount kept counting). Without this, the same
                    // deadlock ("SpawnCount < RespawnPopulationMax" never true) persists even
                    // after disabling the override and restoring the original limit.
                    SyncSpawnCountToInstantiated(spawnEntries, spawnConfig, entry, assetName);
                }
            }

            // --- Live value on the SpawnConfig itself ---
            SetLiveFloatProperty(spawnConfig, "RespawnPopulationMax", original.RespawnPopulationMax, assetName);

            // --- Original RespawnRarity asset reference (undoes the per-fish clone) ---
            // We only ever wrote to the CLONE, never to the original asset, so simply pointing
            // the SpawnConfig back at the original reference restores its real chance/interval
            // values automatically - no separate restore needed for those.
            object currentRarity = ReflectionHelpers.GetMemberValue(spawnConfig, "RespawnRarity");
            if (original.RespawnRarity != null && !ReferenceEquals(currentRarity, original.RespawnRarity))
            {
                ReflectionHelpers.SetMemberValue(spawnConfig, "RespawnRarity", original.RespawnRarity);
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': override disabled, restored original RespawnRarity asset ('{(currentRarity as UnityEngine.Object)?.name ?? "null"}' -> '{(original.RespawnRarity as UnityEngine.Object)?.name ?? "null"}').");
            }

            // --- Original SpawnStartChance ---
            if (original.SpawnStartChance.HasValue)
            {
                object startBehaviour = ReflectionHelpers.GetMemberValue(spawnConfig, "SpawnStartBehaviour");
                if (startBehaviour != null)
                {
                    SetLiveFloatProperty(startBehaviour, "SpawnStartChance", original.SpawnStartChance.Value, assetName);
                }
            }
        }

        /// <summary>
        /// Corrects a desynced entry.SpawnCount downward to match the number of fish actually
        /// instantiated right now (InstantiatedSpawns) - see FIX 3 above for the background. Only
        /// ever lowers the value; if SpawnCount is already at or below the instantiated count
        /// (the normal case), this is a no-op.
        /// </summary>
        private static void SyncSpawnCountToInstantiated(IDictionary spawnEntries, object spawnConfig, object entry, string assetName)
        {
            if (spawnEntries == null)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({assetName}) 'spawnEntries' not found, skipping SpawnCount desync check.");
                return;
            }

            if (!spawnEntries.Contains(spawnConfig))
            {
                // No SpawnEntry exists yet for this spawnConfig this session (nothing has
                // attempted to spawn it yet) - nothing to correct against.
                return;
            }

            object spawnEntry = spawnEntries[spawnConfig];
            int instantiatedCount = ReflectionHelpers.GetCollectionCount(spawnEntry, "InstantiatedSpawns");
            int currentSpawnCount = Convert.ToInt32(ReflectionHelpers.GetMemberValue(entry, "SpawnCount") ?? 0);

            if (currentSpawnCount > instantiatedCount)
            {
                ReflectionHelpers.SetMemberValue(entry, "SpawnCount", instantiatedCount);
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': corrected desynced entry.SpawnCount (persisted): {currentSpawnCount} -> {instantiatedCount} (matches actual InstantiatedSpawns).");
            }
        }

        /// <summary>
        /// Syncs the live values (spawnConfig.RespawnPopulationMax, as well as
        /// RespawnRarity.RespawnIntervalChance and RespawnRarity.RespawnIntervalMinutes) with the
        /// current config. Operates on the RespawnRarity asset already cloned per-fish by
        /// FishSpawnPatch, so there is no cross-talk between fish sharing the same rarity tier.
        /// </summary>
        private static void SyncLiveSpawnConfigValues(object spawnConfig, string assetName, int maxLimit)
        {
            int chancePercent = FishConfigManager.SpawnChanceConfigs.TryGetValue(assetName, out var chanceCfg)
                ? chanceCfg.Value
                : 0;
            float chance01 = Mathf.Clamp01(chancePercent / 100f);
            int intervalMinutes = Plugin.RespawnIntervalMinutes.Value;

            // Live value on the SpawnConfig itself.
            SetLiveFloatProperty(spawnConfig, "RespawnPopulationMax", (float)maxLimit, assetName);

            // Live values on the (per-fish cloned) RespawnRarity asset.
            object respawnRarity = ReflectionHelpers.GetMemberValue(spawnConfig, "RespawnRarity");
            if (respawnRarity == null)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] '{assetName}': RespawnRarity is null, skipping live sync of chance/interval.");
                return;
            }

            SetLiveFloatProperty(respawnRarity, "RespawnIntervalChance", chance01, assetName);
            SetLiveTypedIntervalMinutes(respawnRarity, intervalMinutes, assetName);
        }

        /// <summary>Sets a float property on any object and logs the value before/after (only if it actually changed).</summary>
        private static void SetLiveFloatProperty(object obj, string propName, float newValue, string context)
        {
            PropertyInfo prop = ReflectionHelpers.GetPropertyInfo(obj, propName);
            if (prop == null)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Property '{propName}' not found on type '{obj.GetType().FullName}'.");
                return;
            }

            if (!prop.CanWrite)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Property '{propName}' on type '{obj.GetType().FullName}' is not writable.");
                return;
            }

            object oldValueObj = prop.GetValue(obj);
            float oldValue = Convert.ToSingle(oldValueObj ?? 0f);

            // Only write if the value actually changes -> avoids unnecessary log noise on
            // every tick once the config has settled.
            if (Mathf.Approximately(oldValue, newValue))
                return;

            prop.SetValue(obj, newValue);
            Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Live {obj.GetType().Name}.{propName}: {oldValue} -> {newValue}");
        }

        /// <summary>
        /// Sets RespawnRarity.RespawnIntervalMinutes while accounting for the property's actual
        /// runtime type (int or float), analogous to SetPropertyValueTypedLogged in FishSpawnPatch.
        /// </summary>
        private static void SetLiveTypedIntervalMinutes(object respawnRarity, int newValue, string context)
        {
            PropertyInfo prop = ReflectionHelpers.GetPropertyInfo(respawnRarity, "RespawnIntervalMinutes");
            if (prop == null)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Property 'RespawnIntervalMinutes' not found on type '{respawnRarity.GetType().FullName}'.");
                return;
            }

            if (!prop.CanWrite)
            {
                Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Property 'RespawnIntervalMinutes' on type '{respawnRarity.GetType().FullName}' is not writable.");
                return;
            }

            object oldValueObj = prop.GetValue(respawnRarity);
            object typedNewValue;

            if (oldValueObj is float oldFloat)
            {
                if (Mathf.Approximately(oldFloat, newValue))
                    return;
                typedNewValue = (float)newValue;
            }
            else if (oldValueObj is int oldInt)
            {
                if (oldInt == newValue)
                    return;
                typedNewValue = newValue;
            }
            else
            {
                Plugin._logger.LogWarning($"[FishRespawnPopulationMaxSyncPatch] ({context}) Unexpected type for 'RespawnIntervalMinutes': {oldValueObj?.GetType().FullName ?? "null"}. Setting as int anyway.");
                typedNewValue = newValue;
            }

            prop.SetValue(respawnRarity, typedNewValue);
            Plugin.LogDebug($"[FishRespawnPopulationMaxSyncPatch] ({context}) Live RespawnRarity.RespawnIntervalMinutes: {oldValueObj} -> {typedNewValue}");
        }
    }
}