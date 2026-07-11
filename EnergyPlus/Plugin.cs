using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace EnergyPlus
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<int> EnergyMultiplier;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            EnergyMultiplier = Config.Bind(
                "Gameplay",
                "EnergyMultiplier",
                1,
                new ConfigDescription(
                    "Energy drain from 0 to 10. 0 = Infinite energy, 10 = Normal Drain * 10.",
                    new AcceptableValueRange<int>(0, 10)
                )
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.energyplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }


    [HarmonyPatch(typeof(PlayerPersistence), "SubstractEnergy")]
    static class PlayerPersistence_SubstractEnergy_Patch
    {
        static bool Prefix(ref float amount)
        {
            Plugin._logger.LogInfo($"SubstractEnergy called with amount: {amount}");

            amount = amount * Plugin.EnergyMultiplier.Value;

            Plugin._logger.LogInfo($"SubstractEnergy modified amount: {amount}");
            return true;
        }
    }
}
