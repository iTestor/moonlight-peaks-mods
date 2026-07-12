using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace FishingPlus
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<int> RespawnIntervalMinutes;
        public static ConfigEntry<bool> EnableDebugLogging;

        private void Awake()
        {
            Instance = this;

            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
            
            RespawnIntervalMinutes = Config.Bind(
                "1. Global",
                "RespawnIntervalMinutes",
                30,
                new ConfigDescription(
                    "Respawn interval in minutes, applies to ALL fish with an active override. (lower = spawns more often)",
                    new AcceptableValueRange<int>(1, 1440)
                )
            );

            EnableDebugLogging = Config.Bind(
                "2. Debug",
                "EnableDebugLogging",
                false,
                new ConfigDescription(
                    "Enables verbose debug logging for troubleshooting. Keep this disabled during normal play - " +
                    "enabling it produces a lot of log output and can spam the debug console."
                )
            );

            FishConfigManager.InitializeAllFishConfigs();

            _harmony = new Harmony("dev.kuri.moonlightpeaks.fishingplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            LogAppliedPatches();
        }

        internal static void LogDebug(string message)
        {
            if (EnableDebugLogging != null && EnableDebugLogging.Value)
                _logger.LogInfo(message);
        }

        /// <summary>
        /// Debug-Hilfe: listet alle über diese Harmony-Instanz aktiven Patches
        /// samt Ziel-Methode im Log auf, damit man sofort sieht, ob FishSpawnPatch gegriffen hat.
        /// </summary>
        private void LogAppliedPatches()
        {
            foreach (var method in _harmony.GetPatchedMethods())
            {
                var info = Harmony.GetPatchInfo(method);
                LogDebug($"[Plugin] Patched method: {method.DeclaringType?.FullName}.{method.Name} " +
                         $"(Prefixes={info.Prefixes.Count}, Postfixes={info.Postfixes.Count}, Transpilers={info.Transpilers.Count})");
            }
        }

        private void OnDestroy()
        {
            LogDebug("[Plugin] OnDestroy() -> removing all patches.");
            _harmony?.UnpatchSelf();
        }
    }
}
