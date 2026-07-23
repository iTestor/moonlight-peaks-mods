using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace kuri.moonlightpeaks.critterplus
{
    public static class CritterConfigManager
    {
        public static Dictionary<string, ConfigEntry<bool>> OverrideCritterConfigs = new Dictionary<string, ConfigEntry<bool>>();
        public static Dictionary<string, ConfigEntry<int>> SpawnChanceConfigs = new Dictionary<string, ConfigEntry<int>>();
        public static Dictionary<string, ConfigEntry<int>> SpawnMaxConfigs = new Dictionary<string, ConfigEntry<int>>();
        public static bool ConfigsCreated = false;

        private static string[] critterAssetNames = new string[]
        {
            "Item_Critter_Bee_Firefly",
            "Item_Critter_Bee_Honeyflutter",
            "Item_Critter_Beetle_Bluey",
            "Item_Critter_Beetle_Blushbug",
            "Item_Critter_Beetle_Buzzer",
            "Item_Critter_Beetle_Inferwing",
            "Item_Critter_Beetle_Loomer",
            "Item_Critter_Beetle_Lovepuff",
            "Item_Critter_Beetle_Midnight",
            "Item_Critter_Beetle_Mossback",
            "Item_Critter_Beetle_Murky",
            "Item_Critter_Beetle_Pinky",
            "Item_Critter_Beetle_Psyclops",
            "Item_Critter_Beetle_Purpy",
            "Item_Critter_Beetle_Skullglint",
            "Item_Critter_Beetle_Twizzler",
            "Item_Critter_Butterfly_AmethystWing",
            "Item_Critter_Butterfly_Aura",
            "Item_Critter_Butterfly_Autumnwing",
            "Item_Critter_Butterfly_Batterfly",
            "Item_Critter_Butterfly_Begoniflap",
            "Item_Critter_Butterfly_BeigeBreezer",
            "Item_Critter_Butterfly_BlueGulf",
            "Item_Critter_Butterfly_Blueleaf",
            "Item_Critter_Butterfly_BlueSwallow",
            "Item_Critter_Butterfly_Citrineflutter",
            "Item_Critter_Butterfly_Diamontra",
            "Item_Critter_Butterfly_Fader",
            "Item_Critter_Butterfly_Flamboyance",
            "Item_Critter_Butterfly_FrostFlutter",
            "Item_Critter_Butterfly_Frostleaf",
            "Item_Critter_Butterfly_GreenLady",
            "Item_Critter_Butterfly_GreenPetal",
            "Item_Critter_Butterfly_HauntedPage",
            "Item_Critter_Butterfly_Indigowhisp",
            "Item_Critter_Butterfly_Jader",
            "Item_Critter_Butterfly_Kamehameha",
            "Item_Critter_Butterfly_Lakeflutter",
            "Item_Critter_Butterfly_Limer",
            "Item_Critter_Butterfly_MoonGlimmer",
            "Item_Critter_Butterfly_OrangeFlutter",
            "Item_Critter_Butterfly_OrangePetal",
            "Item_Critter_Butterfly_Peachbliss",
            "Item_Critter_Butterfly_PetalFlap",
            "Item_Critter_Butterfly_Phlox",
            "Item_Critter_Butterfly_PinkBreeze",
            "Item_Critter_Butterfly_PinkGlow",
            "Item_Critter_Butterfly_PinkGulf",
            "Item_Critter_Butterfly_PinkHandyflap",
            "Item_Critter_Butterfly_PinkKite",
            "Item_Critter_Butterfly_Pinkleaf",
            "Item_Critter_Butterfly_PinkPetal",
            "Item_Critter_Butterfly_PinkSliver",
            "Item_Critter_Butterfly_PinkSwallow",
            "Item_Critter_Butterfly_PurpleFlap",
            "Item_Critter_Butterfly_PurpleGulf",
            "Item_Critter_Butterfly_PurpleHandyflap",
            "Item_Critter_Butterfly_PurpleMorpho",
            "Item_Critter_Butterfly_PurpleSliver",
            "Item_Critter_Butterfly_Redflutter",
            "Item_Critter_Butterfly_RedLady",
            "Item_Critter_Butterfly_RedMorpho",
            "Item_Critter_Butterfly_Roseleaf",
            "Item_Critter_Butterfly_Rosetide",
            "Item_Critter_Butterfly_RougePetal",
            "Item_Critter_Butterfly_RubyWing",
            "Item_Critter_Butterfly_SilverSister",
            "Item_Critter_Butterfly_SkinDuster",
            "Item_Critter_Butterfly_SpottedWings",
            "Item_Critter_Butterfly_Springwing",
            "Item_Critter_Butterfly_Summerwing",
            "Item_Critter_Butterfly_TurquoiseSliver",
            "Item_Critter_Butterfly_VioletDuster",
            "Item_Critter_Butterfly_Vitrawing",
            "Item_Critter_Butterfly_WhiteBat",
            "Item_Critter_Butterfly_Winterwing",
            "Item_Critter_Butterfly_Yabberwing",
            "Item_Critter_Crawler_Peeky",
            "Item_Critter_Crawler_Pumkrawler",
            "Item_Critter_Crawler_Skulley",
            "Item_Critter_Crawler_Sourbug",
            "Item_Critter_Crawler_Sprouty",
            "Item_Critter_Crawler_Sweetbug",
            "Item_Critter_Crawler_Turdy",
            "Item_Critter_Glorph",
            "Item_Critter_Mice_Harry",
            "Item_Critter_Mice_Maud",
            "Item_Critter_Snail_Dream",
            "Item_Critter_Snail_Shellycone",
            "Item_Critter_Snail_Skull",
            "Item_Critter_Snail_Sluggy",
            "Item_Critter_Snail_Spiral",
            "Item_Critter_Sunny_Deviled",
            "Item_Critter_Sunny_Normal",
            "Item_Critter_Twigster"
        };

        public static void InitializeAllConfigs()
        {
            if (ConfigsCreated)
            {
                Plugin.LogDebug("[CritterConfigManager] InitializeAllConfigs() erneut aufgerufen, aber Configs existieren bereits -> übersprungen.");
                return;
            }

            Plugin._logger.LogInfo("=== [GENERATE INITIAL CRITTER CONFIGS] ===");
            Plugin.LogDebug($"[CritterConfigManager] Anzahl bekannter Fische: {critterAssetNames.Length}");

            foreach (string assetName in critterAssetNames)
            {
                string cleanName = assetName.Replace("Item_Critter_", "").Replace("_", " ");
                Plugin.LogDebug($"[CritterConfigManager] Erzeuge Config-Sektion für '{assetName}' (Anzeige: '{cleanName}').");

                OverrideCritterConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Critter - {cleanName}",
                    $"1. Override",
                    false,
                    $"Override for {cleanName}. (true = Custom Spawn-Chance & MaxLimit | false = Default Values)"
                );
                Plugin.LogDebug($"[CritterConfigManager]   -> Override-Entry erstellt, Startwert={OverrideCritterConfigs[assetName].Value}");

                SpawnChanceConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Critter - {cleanName}",
                    $"2. SpawnChance",
                    50,
                    new ConfigDescription(
                        $"Spawn-Chance for {cleanName}.",
                        new AcceptableValueRange<int>(0, 100)
                    )
                );
                Plugin.LogDebug($"[CritterConfigManager]   -> SpawnChance-Entry erstellt, Startwert={SpawnChanceConfigs[assetName].Value}");

                SpawnMaxConfigs[assetName] = Plugin.Instance.Config.Bind(
                    $"3. Critter - {cleanName}",
                    $"3. MaxLimit",
                    2,
                    $"Max limit for {cleanName}. (0, 1, 2... = Own Limit)"
                );
                Plugin.LogDebug($"[CritterConfigManager]   -> MaxLimit-Entry erstellt, Startwert={SpawnMaxConfigs[assetName].Value}");

                Plugin._logger.LogInfo($"Config entry for {assetName} successfully created.");
            }

            ConfigsCreated = true;
            Plugin._logger.LogInfo("=== [CONFIG CREATION COMPLETE] ===");
            Plugin.LogDebug($"[CritterConfigManager] Dictionaries befüllt: Override={OverrideCritterConfigs.Count}, Chance={SpawnChanceConfigs.Count}, Max={SpawnMaxConfigs.Count}");
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
                $"[CritterConfigManager] GetEffectiveMaxLimit('{assetName}'): SpawnChance={chancePercent}%, konfiguriertes MaxLimit={configuredMax} -> effektives MaxLimit={effectiveMax}"
                + (effectiveMax != configuredMax ? " (durch SpawnChance=0 erzwungen)" : ""));

            return effectiveMax;
        }
    }
}