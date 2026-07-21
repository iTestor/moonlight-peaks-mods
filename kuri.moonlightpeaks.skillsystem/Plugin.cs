using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace kuri.moonlightpeaks.skillsystem
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.skillsystem");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
