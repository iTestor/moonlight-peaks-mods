using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace InstantCrafting
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> InstantEnabled;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            InstantEnabled = Config.Bind("General", "InstantEnabled", true, "Enable or disable instant crafting.");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.instantcrafting");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(MicrogameDifficultyAsset), "Difficulty", MethodType.Getter)]
    public static class MicrogameDifficultyAsset_get_Difficulty_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            __result = 0;
        }
    }
}
