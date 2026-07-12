using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
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

        private void Awake()
        {
            Instance = this;

            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            FishConfigManager.InitializeAllFishConfigs();

            _harmony = new Harmony("dev.kuri.moonlightpeaks.fishingplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
