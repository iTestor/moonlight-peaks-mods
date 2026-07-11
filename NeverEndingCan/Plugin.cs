using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace NeverEndingCan
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> UnlimitedEnabled;
        public static float defaultWaterUsePerSecond = -1f;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            UnlimitedEnabled = Config.Bind("General", "Unlimited Enabled", true, "Enable or disable unlimited watering can.");

            Config.ConfigReloaded += (sender, args) =>
            {
                _logger.LogInfo("Config wurde extern geändert und neu geladen!");
            };

            _harmony = new Harmony("dev.kuri.moonlightpeaks.neverendingcan");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(WateringCanToolAsset), "WaterUsePerSecond", MethodType.Getter)]
    public static class InfiniteHandWateringCanPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result)
        {
            if(Plugin.defaultWaterUsePerSecond < 0) Plugin.defaultWaterUsePerSecond = __result;

            __result = Plugin.UnlimitedEnabled.Value ? 0f : Plugin.defaultWaterUsePerSecond;
        }
    }
}
