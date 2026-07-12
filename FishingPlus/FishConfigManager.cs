using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace FishingPlus
{
    public static class FishConfigManager
    {
        public static Dictionary<string, ConfigEntry<bool>> OverrideFishConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static Dictionary<string, ConfigEntry<int>> SpawnChanceConfigs = new Dictionary<string, ConfigEntry<int>>();
        public static Dictionary<string, ConfigEntry<int>> SpawnMaxConfigs = new Dictionary<string, ConfigEntry<int>>();
        public static bool ConfigsCreated = false;

        private static string[] fishAssetNames = new string[]
        {
            "Item_Fish_Amour",
            "Item_Fish_Daybreaker",
            "Item_Fish_Brickle",
            "Item_Fish_Furybud",
            "Item_Fish_Fortipod",
            "Item_Fish_Goldy",
            "Item_Fish_Gnasher",
            "Item_Fish_GloyGloy",
            "Item_Fish_Glibby",
            "Item_Fish_Glammer",
            "Item_Fish_Orbis",
            "Item_Fish_Mouthout",
            "Item_Fish_Moonflutter",
            "Item_Fish_Leftsee",
            "Item_Fish_Goliath",
            "Item_Fish_Whisper",
            "Item_Fish_Violet",
            "Item_Fish_Twilight",
            "Item_Fish_Splotch",
            "Item_Fish_Snuffy",
            "Item_Fish_Skullfin"
        };


        public static void InitializeAllFishConfigs()
        {
            if (ConfigsCreated) return;

            Plugin._logger.LogInfo("=== [GENERATE INITIAL FISH CONFIGS] ===");

            foreach (string assetName in fishAssetNames)
            {
                string cleanName = assetName.Replace("Item_Fish_", "").Replace("_", " ");

                // Wir erstellen den Config-Eintrag mit -1.0f als Standard ("Ignorieren")

                OverrideFishConfigs[assetName] = Plugin.Instance.Config.Bind(
                    cleanName,
                    $"1. Override",
                    false,
                    $"Override for {cleanName}. (true = Custom Spawn-Chance & MaxLimit | false = Default Values)"
                );

                SpawnChanceConfigs[assetName] = Plugin.Instance.Config.Bind(
                    cleanName,
                    $"2. SpawnChance",
                    50,
                    new ConfigDescription(
                        $"Spawn-Chance for {cleanName}.",
                        new AcceptableValueRange<int>(0, 100)
                    )
                );

                SpawnMaxConfigs[assetName] = Plugin.Instance.Config.Bind(
                    cleanName,
                    $"3. MaxLimit",
                    2,
                    $"Max limit for {cleanName}. (0, 1, 2... = Own Limit)"
                );

                Plugin._logger.LogInfo($"Config entry for {assetName} successfully created.");
            }

            ConfigsCreated = true;
            Plugin._logger.LogInfo("=== [CONFIG CREATION COMPLETE] ===");
        }
    }
}
