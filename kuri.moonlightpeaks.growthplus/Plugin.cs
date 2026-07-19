using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace kuri.moonlightpeaks.growthplus
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> GrowthPlusEnabled;
        public static ConfigEntry<int> GrowthSpeedConfig;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            GrowthPlusEnabled = Config.Bind(
                "General",
                "GrowthPlusEnabled",
                true,
                new ConfigDescription("Ob das Wachstum von Pflanzen beschleunigt werden soll.")
            );

            GrowthSpeedConfig = Config.Bind(
                "General",
                "GrowthSpeedPercent",
                50,
                new ConfigDescription("Wie viel Prozent schneller Pflanzen wachsen sollen (0 = Normal, 50 = Doppelt so schnell, 100 = Maximal schnell/1 Tag pro Stufe).", new AcceptableValueRange<int>(0, 100))
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.growthplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
