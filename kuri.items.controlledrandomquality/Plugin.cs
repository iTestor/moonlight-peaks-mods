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

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            QualityOverrideConfig = Config.Bind(
                "General",
                "Quality Override",
                ConfigQualityOverride.Random,
                "Choose which quality level should be forced when an item is generated. Set to 'Random' to keep normal game randomness."
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.controlledrandomquality");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GlobalSettingsLibrary), "GetRandomizedItemQualityLevel")]
    public static class ControlledRandomQualityPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref ItemQualityLevel __result)
        {
            ConfigQualityOverride selectedOverride = Plugin.QualityOverrideConfig.Value;

            if (selectedOverride == ConfigQualityOverride.Random)
            {
                return true;
            }

            switch (selectedOverride)
            {
                case ConfigQualityOverride.Regular:
                    __result = ItemQualityLevel.Regular;
                    break;
                case ConfigQualityOverride.Good:
                    __result = ItemQualityLevel.Good;
                    break;
                case ConfigQualityOverride.Perfect:
                    __result = ItemQualityLevel.Perfect;
                    break;
            }

            return false;
        }
    }
}
