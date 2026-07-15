using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ManaPlus
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> ManaPlusIsActive;
        public static ConfigEntry<bool> ManaDrainIndividual;
        public static ConfigEntry<int> ManaDrainMultiplier;
        public static ConfigEntry<int> ManaGainMultiplier;

        public static ConfigFile _config;

        public static ItemAsset CurrentCastingSpell = null;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            _config = Config;

            ManaPlusIsActive = Config.Bind(
                "1. Global",
                "Enable",
                true,
                new ConfigDescription(
                    "If true, the Mana Gain/Drain Multiplier mod is active. If false, the mod is disabled."
                )
            );

            ManaDrainIndividual = Config.Bind(
                "1. Global",
                "Mana Drain Individual",
                false,
                new ConfigDescription(
                    "If true, each spell will have its own mana drain multiplier. If false, the global multiplier will be used."
                )
            );

            ManaDrainMultiplier = Config.Bind(
                "2. Global Multiplier",
                "Mana Drain Multiplier",
                1,
                new ConfigDescription(
                    "Mana drain from 0 to 8. 0 = Infinite mana, 8 = Normal Drain * 8.",
                    new AcceptableValueRange<int>(0, 8)
                )
            );

            ManaGainMultiplier = Config.Bind(
                "2. Global Multiplier",
                "Mana Gain Multiplier",
                1,
                new ConfigDescription(
                    "Mana gain from 0 to 8. 0 = No mana gain, 8 = Normal Gain * 8.",
                    new AcceptableValueRange<int>(0, 8)
                )
            );

            SceneManager.sceneLoaded += IndividualSpells.OnSceneLoaded;

            _harmony = new Harmony("dev.kuri.moonlightpeaks.manaplus");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}