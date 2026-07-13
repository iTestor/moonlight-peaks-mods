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
            "Item_Fish_Brickle",
            "Item_Fish_Daybreaker",
            "Item_Fish_Fortipod",
            "Item_Fish_Furybud",
            "Item_Fish_Glammer",
            "Item_Fish_Glibby",
            "Item_Fish_GloyGloy",
            "Item_Fish_Gnasher",
            "Item_Fish_Goldy",
            "Item_Fish_Goliath",
            "Item_Fish_Leftsee",
            "Item_Fish_Missing",
            "Item_Fish_Moonflutter",
            "Item_Fish_Mouthout",
            "Item_Fish_Orbis",
            "Item_Fish_Skullfin",
            "Item_Fish_Snuffy",
            "Item_Fish_Splotch",
            "Item_Fish_Twilight",
            "Item_Fish_Violet",
            "Item_Fish_Whisper"

        };

        public static void InitializeAllFishConfigs()
        {
            if (ConfigsCreated)
            {
                Plugin.LogDebug("[FishConfigManager] InitializeAllFishConfigs() erneut aufgerufen, aber Configs existieren bereits -> übersprungen.");
                return;
            }

            Plugin._logger.LogInfo("=== [GENERATE INITIAL FISH CONFIGS] ===");
            Plugin.LogDebug($"[FishConfigManager] Anzahl bekannter Fische: {fishAssetNames.Length}");

            foreach (string assetName in fishAssetNames)
            {
                string cleanName = assetName.Replace("Item_Fish_", "").Replace("_", " ");
                Plugin.LogDebug($"[FishConfigManager] Erzeuge Config-Sektion für '{assetName}' (Anzeige: '{cleanName}').");

                OverrideFishConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Fish - {cleanName}",
                    $"1. Override",
                    false,
                    $"Override for {cleanName}. (true = Custom Spawn-Chance & MaxLimit | false = Default Values)"
                );
                Plugin.LogDebug($"[FishConfigManager]   -> Override-Entry erstellt, Startwert={OverrideFishConfigs[assetName].Value}");

                SpawnChanceConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Fish - {cleanName}",
                    $"2. SpawnChance",
                    50,
                    new ConfigDescription(
                        $"Spawn-Chance for {cleanName}.",
                        new AcceptableValueRange<int>(0, 100)
                    )
                );
                Plugin.LogDebug($"[FishConfigManager]   -> SpawnChance-Entry erstellt, Startwert={SpawnChanceConfigs[assetName].Value}");

                SpawnMaxConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Fish - {cleanName}",
                    $"3. MaxLimit",
                    2,
                    $"Max limit for {cleanName}. (0, 1, 2... = Own Limit)"
                );
                Plugin.LogDebug($"[FishConfigManager]   -> MaxLimit-Entry erstellt, Startwert={SpawnMaxConfigs[assetName].Value}");

                Plugin._logger.LogInfo($"Config entry for {assetName} successfully created.");
            }

            ConfigsCreated = true;
            Plugin._logger.LogInfo("=== [CONFIG CREATION COMPLETE] ===");
            Plugin.LogDebug($"[FishConfigManager] Dictionaries befüllt: Override={OverrideFishConfigs.Count}, Chance={SpawnChanceConfigs.Count}, Max={SpawnMaxConfigs.Count}");
        }

        /// <summary>
        /// Liefert das tatsächlich zu verwendende MaxLimit für einen Fisch.
        /// Ist SpawnChance auf 0 gesetzt, wird MaxLimit automatisch auf 0 erzwungen,
        /// damit wirklich keine Instanz mehr gespawnt/wiederhergestellt wird,
        /// unabhängig vom konfigurierten MaxLimit-Wert.
        /// </summary>
        public static int GetEffectiveMaxLimit(string assetName)
        {
            int chancePercent = SpawnChanceConfigs[assetName].Value;
            int configuredMax = SpawnMaxConfigs[assetName].Value;
            int effectiveMax = chancePercent <= 0 ? 0 : configuredMax;

            Plugin.LogDebug(
                $"[FishConfigManager] GetEffectiveMaxLimit('{assetName}'): SpawnChance={chancePercent}%, konfiguriertes MaxLimit={configuredMax} -> effektives MaxLimit={effectiveMax}"
                + (effectiveMax != configuredMax ? " (durch SpawnChance=0 erzwungen)" : ""));

            return effectiveMax;
        }
    }
}