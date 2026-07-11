using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace BiggerStacks
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<int> StackSize;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            StackSize = Config.Bind(
                "Gameplay",
                "StackSize",
                100,
                new ConfigDescription(
                    "Stack size from 1 to 999.",
                    new AcceptableValueRange<int>(1, 999)
                )
            );

            _harmony = new Harmony("dev.kuri.moonlightpeaks.biggerstacks");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(ItemAsset), "get_MaxStackSize")]
    public static class MaxStackSizePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            __result = Plugin.StackSize.Value;
        }
    }
}
