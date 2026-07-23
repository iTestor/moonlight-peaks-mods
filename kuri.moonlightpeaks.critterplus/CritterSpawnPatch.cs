using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace kuri.moonlightpeaks.critterplus
{
    [HarmonyPatch]
    public static class CritterSpawnPatch
    {
        // Prevents applying the override twice, in case Setup() runs again or multiple
        // spawners share the same assets (SpawnConfig/ScriptableObjects).
        private static readonly HashSet<object> patchedConfigs =
            new HashSet<object>(ReferenceEqualityComparer.Instance);

        // IMPORTANT: es kann mehrere EntityBasicCritterSpawner gleichzeitig geben (z.B. mehrere
        // Regionen/Räume, die parallel geladen sind). Ein einzelnes statisches Feld, das bei
        // jedem Setup()-Aufruf überschrieben wird, zeigt daher NICHT zuverlässig auf den
        // Spawner, in dessen Bereich der Spieler sich gerade aufhält - sondern nur auf den
        // zuletzt initialisierten. Wir merken uns deshalb ALLE bisher gesehenen Spawner und
        // wählen bei Bedarf (siehe GetNearestActiveSpawner) den räumlich passenden aus.
        private static readonly HashSet<EntityBasicCritterSpawner> activeSpawners =
            new HashSet<EntityBasicCritterSpawner>();

        private static EntityBasicCritterSpawner spawnerInstance;

        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            Plugin.LogDebug("[CritterSpawnPatch] Resolving TargetMethod(): EntityBasicCritterSpawner.Setup()");
            MethodBase method = AccessTools.Method(typeof(EntityBasicCritterSpawner), "Setup");

            if (method == null)
                Plugin._logger.LogError("[CritterSpawnPatch] Could NOT find EntityBasicCritterSpawner.Setup()! This patch will have no effect.");
            else
                Plugin.LogDebug($"[CritterSpawnPatch] TargetMethod resolved: {method.DeclaringType?.FullName}.{method.Name}");

            return method;
        }

        [HarmonyPrefix]
        private static void Prefix(object __instance)
        {
            Plugin.LogDebug($"[CritterSpawnPatch] Prefix() triggered for instance of type '{__instance.GetType().FullName}' (InstanceID: {(__instance as UnityEngine.Object)?.GetInstanceID().ToString() ?? "n/a"}).");

            try
            {
                CritterConfigManager.InitializeAllConfigs();
                spawnerInstance = __instance as EntityBasicCritterSpawner;

                if (spawnerInstance != null)
                {
                    activeSpawners.Add(spawnerInstance);
                    Plugin.LogDebug($"[CritterSpawnPatch] Spawner zur aktiven Liste hinzugefügt (InstanceID: {spawnerInstance.GetInstanceID()}). Aktive Spawner insgesamt: {activeSpawners.Count}.");
                }

                ApplyCritterOverrides(__instance);
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[CritterSpawnPatch] Exception while applying critter overrides: {ex}");
            }
        }

        private static void ApplyCritterOverrides(object spawnerInstance)
        {
            Plugin.LogDebug("[CritterSpawnPatch] Searching for field 'spawnConfigs' in the type hierarchy...");
            FieldInfo spawnConfigsField = ReflectionHelpers.FindFieldInHierarchy(spawnerInstance.GetType(), "spawnConfigs");

            if (spawnConfigsField == null)
            {
                Plugin.LogDebug("[CritterSpawnPatch] Field 'spawnConfigs' not found. Aborting.");
                return;
            }
            Plugin.LogDebug($"[CritterSpawnPatch] Field 'spawnConfigs' found on type '{spawnConfigsField.DeclaringType?.FullName}'.");

            object rawList = spawnConfigsField.GetValue(spawnerInstance);
            if (!(rawList is IEnumerable configList))
            {
                Plugin.LogDebug("[CritterSpawnPatch] 'spawnConfigs' is not an IEnumerable or is null. Aborting.");
                return;
            }

            int totalCount = 0;
            int skippedAlready = 0;
            int skippedUnknown = 0;
            int skippedOverrideOff = 0;
            int appliedCount = 0;

            foreach (object spawnConfig in configList)
            {
                totalCount++;

                if (spawnConfig == null)
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] Entry #{totalCount}: spawnConfig is null, skipping.");
                    continue;
                }

                if (patchedConfigs.Contains(spawnConfig))
                {
                    skippedAlready++;
                    Plugin.LogDebug($"[CritterSpawnPatch] Entry #{totalCount}: already patched, skipping.");
                    continue;
                }

                object itemAssetObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnItemAsset");
                if (itemAssetObj == null)
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] Entry #{totalCount}: SpawnItemAsset is null, skipping.");
                    continue;
                }

                if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj))
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] Entry #{totalCount}: SpawnItemAsset is not a UnityEngine.Object (type: {itemAssetObj.GetType().FullName}), skipping.");
                    continue;
                }

                string assetName = itemAssetUnityObj.name;
                Plugin.LogDebug($"[CritterSpawnPatch] Entry #{totalCount}: SpawnItemAsset.name = '{assetName}'.");

                if (!CritterConfigManager.OverrideCritterConfigs.TryGetValue(assetName, out var overrideEntry))
                {
                    skippedUnknown++;
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}' is not a known critter in CritterConfigManager, skipping (default values stay active).");
                    continue;
                }

                Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': override flag = {overrideEntry.Value}.");

                if (!overrideEntry.Value)
                {
                    skippedOverrideOff++;
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': override disabled, default values stay active.");
                    continue;
                }

                patchedConfigs.Add(spawnConfig);

                int chancePercent = CritterConfigManager.SpawnChanceConfigs[assetName].Value;
                int maxLimit = CritterConfigManager.GetEffectiveMaxLimit(assetName); // 0 if SpawnChance=0
                int intervalMinutes = Plugin.RespawnIntervalMinutes.Value; // global config instead of per-critter
                float chance01 = Mathf.Clamp01(chancePercent / 100f);

                Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': applying override -> SpawnChance={chancePercent}% (={chance01}), MaxLimit(effective)={maxLimit}, IntervalMinutes(global)={intervalMinutes}.");

                // Capture this critter's original, un-overridden values exactly once, before we
                // touch anything below. Without this, disabling the override later (while the
                // room stays loaded) would have nothing to fall back to, and the critter would stay
                // stuck on whatever override values were last applied - see
                // CritterRespawnPopulationMaxSyncPatch, which restores from this snapshot.
                object startBehaviour = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnStartBehaviour");
                float originalRespawnPopulationMax = Convert.ToSingle(ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnPopulationMax") ?? 0f);
                object originalRespawnRarity = ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnRarity");
                float? originalSpawnStartChance = startBehaviour != null
                    ? (float?)Convert.ToSingle(ReflectionHelpers.GetPropertyValue(startBehaviour, "SpawnStartChance") ?? 0f)
                    : null;

                CritterOriginalValueStore.CaptureIfMissing(spawnConfig, originalRespawnPopulationMax, originalRespawnRarity, originalSpawnStartChance);

                // Max. simultaneous population of this critter
                SetPropertyValueLogged(spawnConfig, "RespawnPopulationMax", (float)maxLimit, assetName);

                // Start spawn chance (used if SpawnStartBehaviour.Behaviour == Chance)
                if (startBehaviour != null)
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': SpawnStartBehaviour found (type: {startBehaviour.GetType().FullName}).");
                    SetPropertyValueLogged(startBehaviour, "SpawnStartChance", chance01, assetName);
                }
                else
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': SpawnStartBehaviour is null, skipping SpawnStartChance.");
                }

                // Chance & interval per respawn tick
                object respawnRarity = ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnRarity");
                if (respawnRarity != null)
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': RespawnRarity found (type: {respawnRarity.GetType().FullName}).");

                    // IMPORTANT: RespawnRarity assets (e.g. "Uncommon_RespawnRarity") are
                    // ScriptableObjects that are often shared by multiple critter (rarity-tier
                    // pattern: Common/Uncommon/Rare...). If we mutated the original asset
                    // directly, the last critter processed with that same asset would overwrite
                    // the values of every other critter sharing it (e.g. a critter with
                    // SpawnChance=0 would reset RespawnIntervalChance to 0 for another critter
                    // with SpawnChance=100 that shares the same rarity tier). We therefore
                    // clone the asset once per SpawnConfig and assign the clone exclusively
                    // to this SpawnConfig before writing any values.
                    if (respawnRarity is UnityEngine.Object respawnRarityUnityObj)
                    {
                        UnityEngine.Object clonedRarity = UnityEngine.Object.Instantiate(respawnRarityUnityObj);
                        clonedRarity.name = respawnRarityUnityObj.name + "_Clone_" + assetName;

                        Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': cloned RespawnRarity asset '{respawnRarityUnityObj.name}' -> '{clonedRarity.name}' (prevents cross-talk with other critter sharing the same rarity tier).");

                        SetPropertyValueObjectLogged(spawnConfig, "RespawnRarity", clonedRarity, assetName);

                        respawnRarity = clonedRarity; // from here on, operate exclusively on the clone
                    }
                    else
                    {
                        Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': RespawnRarity is not a UnityEngine.Object and cannot be cloned. Writing to the original asset instead (risk: a shared asset may be overwritten by other critter).");
                    }

                    SetPropertyValueLogged(respawnRarity, "RespawnIntervalChance", chance01, assetName);
                    SetPropertyValueTypedLogged(respawnRarity, "RespawnIntervalMinutes", intervalMinutes, assetName);
                }
                else
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': RespawnRarity is null, skipping interval/chance.");
                }

                object directorCheckEventObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "DirectorCheckEvent");
                if (directorCheckEventObj != null)
                {
                    ReflectionHelpers.SetMemberValue(spawnConfig, "DirectorCheckEvent", null);
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': DirectorCheckEvent set to null (Biom-/Location-Check is skipped entirely for this critter -> the flag remains set to true permanently).");
                } else
                {
                    Plugin.LogDebug($"[CritterSpawnPatch] '{assetName}': DirectorCheckEvent is already null, no change needed.");
                }

                    appliedCount++;
            }

            Plugin.LogDebug($"[CritterSpawnPatch] Done. Total configs={totalCount}, applied={appliedCount}, unknown={skippedUnknown}, override-off={skippedOverrideOff}, already-patched={skippedAlready}.");
        }

        /// <summary>Sets a float property and logs the value before/after.</summary>
        private static void SetPropertyValueLogged(object obj, string propName, float newValue, string context)
        {
            PropertyInfo prop = ReflectionHelpers.GetPropertyInfo(obj, propName);
            if (prop == null)
            {
                Plugin.LogDebug($"[CritterSpawnPatch] ({context}) Property '{propName}' not found on type '{obj.GetType().FullName}'.");
                return;
            }

            object oldValue = prop.GetValue(obj);
            prop.SetValue(obj, newValue);
            Plugin.LogDebug($"[CritterSpawnPatch] ({context}) {obj.GetType().Name}.{propName}: {oldValue} -> {newValue}");
        }

        /// <summary>Sets a property whose actual type (int/float) is determined at runtime, and logs the value before/after.</summary>
        private static void SetPropertyValueTypedLogged(object obj, string propName, int newValue, string context)
        {
            PropertyInfo prop = ReflectionHelpers.GetPropertyInfo(obj, propName);
            if (prop == null)
            {
                Plugin.LogDebug($"[CritterSpawnPatch] ({context}) Property '{propName}' not found on type '{obj.GetType().FullName}'.");
                return;
            }

            object oldValue = prop.GetValue(obj);
            object typedNewValue;

            if (oldValue is float)
            {
                typedNewValue = (float)newValue;
            }
            else if (oldValue is int)
            {
                typedNewValue = newValue;
            }
            else
            {
                Plugin.LogDebug($"[CritterSpawnPatch] ({context}) Unexpected type for '{propName}': {oldValue?.GetType().FullName ?? "null"}. Setting as int anyway.");
                typedNewValue = newValue;
            }

            prop.SetValue(obj, typedNewValue);
            Plugin.LogDebug($"[CritterSpawnPatch] ({context}) {obj.GetType().Name}.{propName}: {oldValue} -> {typedNewValue}");
        }

        /// <summary>Sets a reference property (e.g. a ScriptableObject asset) and logs the value before/after by object name.</summary>
        private static void SetPropertyValueObjectLogged(object obj, string propName, object newValue, string context)
        {
            PropertyInfo prop = ReflectionHelpers.GetPropertyInfo(obj, propName);
            if (prop == null)
            {
                Plugin.LogDebug($"[CritterSpawnPatch] ({context}) Property '{propName}' not found on type '{obj.GetType().FullName}'.");
                return;
            }

            object oldValue = prop.GetValue(obj);
            prop.SetValue(obj, newValue);

            string oldName = (oldValue as UnityEngine.Object)?.name ?? oldValue?.ToString() ?? "null";
            string newName = (newValue as UnityEngine.Object)?.name ?? newValue?.ToString() ?? "null";
            Plugin.LogDebug($"[CritterSpawnPatch] ({context}) {obj.GetType().Name}.{propName}: '{oldName}' -> '{newName}'");
        }

        public static HashSet<object> GetLoadedConfigs()
        {
            return patchedConfigs;
        }

        public static EntityBasicCritterSpawner GetSpawnerInstance()
        {
            return spawnerInstance;
        }

        internal static IEnumerable<EntityBasicCritterSpawner> GetAllActiveSpawners()
        {
            activeSpawners.RemoveWhere(s => s == null);
            return activeSpawners; // die private Liste, die im Log "Aktive Spawner insgesamt: X" befüllt
        }

        /// <summary>
        /// Liefert von allen bisher über Setup() erfassten Spawnern denjenigen, der räumlich am
        /// nächsten an referencePosition liegt (z.B. Kamera-/Spielerposition). Bereinigt dabei
        /// nebenbei zerstörte/entladene Spawner-Referenzen aus der internen Liste (Unity's
        /// überladener ==-Operator erkennt "kaputte" MonoBehaviour-Referenzen zuverlässig als
        /// null, auch wenn das C#-Objekt selbst noch existiert). Gibt null zurück, wenn aktuell
        /// kein Spawner bekannt ist.
        /// </summary>
        public static EntityBasicCritterSpawner GetNearestActiveSpawner(Vector3 referencePosition)
        {
            activeSpawners.RemoveWhere(s => s == null);

            EntityBasicCritterSpawner nearest = null;
            float nearestSqrDist = float.MaxValue;

            foreach (EntityBasicCritterSpawner spawner in activeSpawners)
            {
                float sqrDist = (spawner.transform.position - referencePosition).sqrMagnitude;
                if (sqrDist < nearestSqrDist)
                {
                    nearestSqrDist = sqrDist;
                    nearest = spawner;
                }
            }

            return nearest;
        }
    }
}