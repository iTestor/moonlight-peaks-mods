using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace kuri.weather.manipulator
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> IsWeatherOverrideEnabled;
        public static ConfigEntry<bool> LockWeather;
        public static ConfigEntry<WeatherType> TargetWeather;

        private static bool _alreadyWarnedMissing = false;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.weathermanipulator");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            IsWeatherOverrideEnabled = Config.Bind(
                "Weather Settings",
                "Enable Weather Override",
                false,
                "Enable or disable the weather override."
            );

            LockWeather = Config.Bind(
                "Weather Settings",
                "Lock Weather (Always)",
                false,
                "If active, the game can no longer change the weather on its own (e.g. through events or room changes)."
            );

            TargetWeather = Config.Bind(
                "Weather Settings",
                "Selected Weather",
                WeatherType.Clear_Spring,
                "The weather to enforce."
            );

            TargetWeather.SettingChanged += (sender, args) => ApplyConfigWeather();
            IsWeatherOverrideEnabled.SettingChanged += (sender, args) => ApplyConfigWeather();

        }

        private void Update()
        {
            if (IsWeatherControllerActive() && LockWeather.Value)
            {
                var current = WeatherController.Instance.CachedCurrentWeather;

                if (current != null)
                {
                    string targetName = TargetWeather.Value.GetAssetName();

                    string currentName = current.name.Replace("(Clone)", "").Trim();

                    if (currentName != targetName)
                    {
                        ApplyConfigWeather();
                    }
                }
            }
        }

        private bool IsWeatherControllerActive()
        {
            return UnityEngine.Object.FindAnyObjectByType<WeatherController>() != null;
        }

        private static void ApplyConfigWeather()
        {
            if (WeatherController.Instance == null) return;

            if (!IsWeatherOverrideEnabled.Value)
            {
                WeatherController.Instance.StartWeatherIfNeeded();
                _alreadyWarnedMissing = false; // Reset
                return;
            }

            string assetName = TargetWeather.Value.GetAssetName();
            WeatherAsset[] allWeathers = Resources.FindObjectsOfTypeAll<WeatherAsset>();
            WeatherAsset targetAsset = null;

            foreach (var weather in allWeathers)
            {
                if (weather.name == assetName)
                {
                    targetAsset = weather;
                    break;
                }
            }

            if (targetAsset != null)
            {
                _alreadyWarnedMissing = false;
                Debug.Log($"[WeatherManipulator] Force Weather from config: {assetName}");
                WeatherController.Instance.SetWeather(targetAsset);
                WeatherController.Instance.StartWeatherIfNeeded();
            }
            else if (!_alreadyWarnedMissing)
            {
                Debug.LogWarning($"[WeatherManipulator] Weather asset '{assetName}' was not found in memory.");
                _alreadyWarnedMissing = true;
            }
        }
    }
}

/*
 [Info   : Unity Log] [VampireCheat] Weather: Weather_Clear_Spring(Clone), Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Clear_Summer, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Rain, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_RainStorm, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Clear_Winter, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Clear_Spring, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Content_SnowStormWithManualThunder, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Clear_Autumn, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_SnowStorm, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Content_RainStormWithManualThunder, Type: WeatherAsset
[Info   : Unity Log] [VampireCheat] Weather: Weather_Snow, Type: WeatherAsset
 */