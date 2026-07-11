using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ManaPlus
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<int> ManaDrainMultiplier;
        public static ConfigEntry<int> ManaGainMultiplier;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            ManaDrainMultiplier = Config.Bind(
                "Gameplay",
                "ManaDrainMultiplier",
                1,
                new ConfigDescription(
                    "Mana drain from 0 to 10. 0 = Infinite mana, 10 = Normal Drain * 10.",
                    new AcceptableValueRange<int>(0, 10)
                )
            );

            ManaGainMultiplier = Config.Bind(
                "Gameplay",
                "ManaGainMultiplier",
                1,
                new ConfigDescription(
                    "Mana gain from 0 to 10. 0 = No mana gain, 10 = Normal Gain * 10.",
                    new AcceptableValueRange<int>(0, 10)
                )
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.manaplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(PlayerPersistence), "SubstractMana")]
    static class PlayerPersistence_SubstractMana_Patch
    {
        static bool Prefix(ref float amount)
        {
            amount = amount * Plugin.ManaDrainMultiplier.Value;

            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPersistence), "AddMana")]
    static class PlayerPersistence_AddMana_Patch
    {
        static bool Prefix(ref float amount)
        {
            amount = amount * Plugin.ManaGainMultiplier.Value;

            return true;
        }
    }
}
