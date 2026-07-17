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

        public static ConfigEntry<bool> EnergyPlusIsActive;
        public static ConfigEntry<int> EnergyDrainMultiplier;
        public static ConfigEntry<int> EnergyGainMultiplier;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            EnergyPlusIsActive = Config.Bind(
                "1. Global",
                "Enable",
                true,
                new ConfigDescription(
                    "If true, the EnergyPlus mod is active. If false, the mod is disabled."
                )
            );

            EnergyDrainMultiplier = Config.Bind(
                "2. General",
                "Energy Drain (in %)",
                100,
                new ConfigDescription(
                    "Set the energy drain in percent. 0% is infinite energy, 50% is half consumption, 100% is vanilla, 200% is double energy drain.",
                    new AcceptableValueRange<int>(0, 1000)
                )
            );

            EnergyGainMultiplier = Config.Bind(
                "2. General",
                "Energy Gain (in %)",
                100,
                new ConfigDescription(
                    "Set the energy gain in percent. 100% is vanilla, 200% is double energy gain, 500% is five times the energy gain.",
                    new AcceptableValueRange<int>(0, 1000)
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
            if(!Plugin.EnergyPlusIsActive.Value)
            {
                return true;
            }

            amount = amount * (Plugin.EnergyDrainMultiplier.Value / 100f);

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPersistence), "AddEnergy")]
    static class PlayerPersistence_AddEnergy_Patch
    {
        static bool Prefix(ref float amount)
        {
            if (!Plugin.EnergyPlusIsActive.Value)
            {
                return true;
            }

            amount = amount * (Plugin.EnergyGainMultiplier.Value / 100f);

            return true;
        }
    }
}
