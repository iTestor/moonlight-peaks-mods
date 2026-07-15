using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace kuri.nokturna.autowin
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;

        public static ConfigEntry<bool> AutoWin;

        private void Awake()
        {
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            AutoWin = Config.Bind("General", "Auto Win", true, "Enable or disable auto win.");

            AutoWin.SettingChanged += OnConfigChanged;
        }

        private void Update()
        {
            if (TryApplyAutoWin(AutoWin.Value))
            {
                this.enabled = false;
            }
        }

        private void OnConfigChanged(object sender, EventArgs e)
        {
            _logger.LogInfo($"[Nokturna] Config geändert. Setze AutoWinNokturna auf: {AutoWin.Value}");
            TryApplyAutoWin(AutoWin.Value);
        }

        private bool TryApplyAutoWin(bool value)
        {
            try
            {
                Type srOptionsType = AccessTools.TypeByName("SROptionsVampire");
                if (srOptionsType == null) return false;

                object currentInstance = AccessTools.Field(srOptionsType, "Current")?.GetValue(null);

                if (currentInstance != null)
                {
                    ReflectionHelpers.SetMemberValue(currentInstance, "autoWinNokturna", value);

                    _logger.LogInfo($"[Nokturna] autoWinNokturna (Feld) is set on {value}!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Nokturna] Error while writing SROptionsVampire: {ex}");
            }
            return false;
        }
    }
}