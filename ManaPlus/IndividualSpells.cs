using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ManaPlus
{
    internal class IndividualSpells
    {
        public static Dictionary<string, ConfigEntry<int>> SpellConfigs = new Dictionary<string, ConfigEntry<int>>();
        public static Dictionary<string, float> defaultCosts = new Dictionary<string, float>();
        public static Dictionary<string, ItemAsset> spells = new Dictionary<string, ItemAsset>();

        private static bool _spellsInitialized = false;

        public static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            InitializeSpellConfigs();
        }

        public static void InitializeSpellConfigs()
        {
            if (_spellsInitialized) return;

            ItemAsset[] allItems = Resources.FindObjectsOfTypeAll<ItemAsset>();
            int registeredCount = 0;

            foreach (var item in allItems)
            {
                if (item != null && (item.name.StartsWith("Item_Spell_") || item.SpellAddon != null))
                {
                    string internalName = item.name.Replace("(Clone)", "").Trim();

                    if (internalName.IndexOf("demo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        continue;
                    }

                    if (!defaultCosts.ContainsKey(internalName))
                    {
                        defaultCosts[internalName] = item.SpellAddon != null ? item.SpellAddon.ManaUsage : 3f;
                    }

                    if(!spells.ContainsKey(internalName))
                    {
                        spells[internalName] = item;
                    }

                    if (!SpellConfigs.ContainsKey(internalName))
                    {
                        string cleanName = FormatSpellName(internalName);
                        string category = internalName.Contains("Ethereal") ? "4. Individual Ethereal Spells" : "3. Individual Spells";

                        int defaultCost = item.SpellAddon != null ? (int)item.SpellAddon.ManaUsage : 3;

                        var configEntry = Plugin._config.Bind(
                            category,
                            cleanName,
                            defaultCost,
                            new ConfigDescription(
                                $"Mana cost for casting {cleanName}.",
                                new AcceptableValueRange<int>(0, 8)
                            )
                        );

                        SpellConfigs[internalName] = configEntry;
                        registeredCount++;
                    }
                }
            }

            if (registeredCount > 0)
            {
                Plugin._logger.LogDebug($"[ManaPlus] {registeredCount} Spells successfully loaded into the config!");
                _spellsInitialized = true;
            }
        }

        private static string FormatSpellName(string internalName)
        {
            string temp = internalName.Replace("Item_Spell_", "");
            temp = temp.Replace("_", " ");

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < temp.Length; i++)
            {
                if (i > 0 && char.IsUpper(temp[i]) && temp[i - 1] != ' ')
                {
                    sb.Append(' ');
                }
                sb.Append(temp[i]);
            }

            return sb.ToString().Trim();
        }

        public static float GetCostForSpell(ItemAsset itemAsset)
        {
            if (itemAsset == null) return 0f;
            string internalName = itemAsset.name.Replace("(Clone)", "").Trim();

            if (SpellConfigs.TryGetValue(internalName, out var configEntry))
            {
                return (float)configEntry.Value;
            }

            return itemAsset.SpellAddon != null ? (float)itemAsset.SpellAddon.ManaUsage : 0f;
        }
    }

}
