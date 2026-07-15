using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kuri.weather.manipulator
{
    public enum WeatherType
    {
        Clear_Spring,
        Clear_Summer,
        Clear_Autumn,
        Clear_Winter,
        Rain,
        RainStorm,
        Snow,
        SnowStorm,
        RainStorm_With_Thunder,
        SnowStorm_With_Thunder
    }

    public static class WeatherTypeExtensions
    {
        // Wandelt die saubere Enum in den exakten internen Asset-Namen des Spiels um
        public static string GetAssetName(this WeatherType type)
        {
            return type switch
            {
                WeatherType.Clear_Spring => "Weather_Clear_Spring",
                WeatherType.Clear_Summer => "Weather_Clear_Summer",
                WeatherType.Clear_Autumn => "Weather_Clear_Autumn",
                WeatherType.Clear_Winter => "Weather_Clear_Winter",
                WeatherType.Rain => "Weather_Rain",
                WeatherType.RainStorm => "Weather_RainStorm",
                WeatherType.Snow => "Weather_Snow",
                WeatherType.SnowStorm => "Weather_SnowStorm",
                WeatherType.RainStorm_With_Thunder => "Weather_Content_RainStormWithManualThunder",
                WeatherType.SnowStorm_With_Thunder => "Weather_Content_SnowStormWithManualThunder",
                _ => "Weather_Clear_Spring"
            };
        }
    }
}
