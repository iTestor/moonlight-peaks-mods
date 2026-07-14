using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Chicken.UI;
using Chicken.Utilities;
using HarmonyLib;
using UnityEngine;

namespace InstantCrafting
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> InstantCrafting;
        public static ConfigEntry<bool> InstantCooking;
        public static ConfigEntry<bool> InstantBrewing;
        public static ConfigEntry<bool> InstantProduction;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            InstantCrafting = Config.Bind("General", "Instant Crafting", true, "Enable or disable instant crafting.");
            InstantCooking = Config.Bind("General", "Instant Cooking", true, "Enable or disable instant cooking.");
            InstantBrewing = Config.Bind("General", "Instant Brewing", true, "Enable or disable instant brewing.");
            InstantProduction = Config.Bind("General", "Instant Production", true, "Enable or disable instant production. For e.g. Refinery, Press, Seed Sequencer, Chocolatier, etc..");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.instantcrafting");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            field?.SetValue(obj, value);
        }
    }

}
