using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using static kuri.items.controlledrandomquality.Plugin;

namespace kuri.items.controlledrandomquality
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public enum ConfigQualityOverride
        {
            Random,
            Regular,
            Good,
            Perfect
        }

        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<ConfigQualityOverride> QualityOverrideConfig;

        public static ConfigEntry<bool> ResetBuggedLostItemQuests;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            QualityOverrideConfig = Config.Bind(
                "1. General",
                "Quality Override",
                ConfigQualityOverride.Random,
                "Choose which quality level should be forced when an item is generated. Set to 'Random' to keep normal game randomness."
            );

            ResetBuggedLostItemQuests = Config.Bind(
                "2. Fixes",
                "Reset Bugged Lost Item Quests",
                true,
                "If enabled, it will clear the lost item quests and their associated items. You can get this quest new on the JobBoard."
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.controlledrandomquality");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
