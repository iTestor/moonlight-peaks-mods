using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace Kuri.SRDebugger
{

    public static class DumperItems
    {
        public static void DumpItemsToWiki()
        {
            // 1. Alle geladenen ItemAssets aus dem Unity-Speicher abrufen
            ItemAsset[] allItems = Resources.FindObjectsOfTypeAll<ItemAsset>();

            if (allItems == null || allItems.Length == 0)
            {
                Debug.LogError("[Dumper] Keine ItemAssets im Speicher gefunden. Stelle sicher, dass du im Spiel oder Hauptmenü bist!");
                return;
            }

            // Zielordner definieren (Plugin-Verzeichnis)
            string pluginFolder = Path.Combine(Paths.PluginPath, MyPluginInfo.PLUGIN_GUID, "wiki");
            if (!Directory.Exists(pluginFolder))
            {
                Directory.CreateDirectory(pluginFolder);
            }

            // Duplikate bereinigen
            var uniqueItems = allItems
                .Where(item => item != null && !string.IsNullOrEmpty(item.name))
                .GroupBy(item => item.name)
                .Select(g => g.First())
                .ToList();

            // Zuordnungs-Wörterbuch (Dateiname -> Liste von Items)
            var fileCategories = new Dictionary<string, List<ItemAsset>>();

            // Hilfsfunktion zum sicheren Hinzufügen zu den Listen
            void AddToCategory(string fileName, ItemAsset item)
            {
                if (!fileCategories.ContainsKey(fileName))
                {
                    fileCategories[fileName] = new List<ItemAsset>();
                }
                fileCategories[fileName].Add(item);
            }

            // 2. Dynamische Aufteilung aller Items
            foreach (var item in uniqueItems)
            {
                string id = item.name;

                // Regel A: Alle DEPRECATED oder DUMMY Einträge strikt aussortieren und in eigene Datei packen
                if (id.ToUpper().Contains("DEPRECATED") || id.ToUpper().Contains("DUMMY") || id.Contains("_Dummy_"))
                {
                    AddToCategory("Deprecated_and_Dummy_Entries.md", item);
                    continue;
                }

                // Regel B: Nur Items verarbeiten, die man auch regulär bekommen kann
                if (!item.IsAcquirable)
                {
                    continue;
                }

                // Regel C: Dynamische Erkennung anhand des Namensmusters (z.B. "Item_Fish" oder "Item_Critter")
                string[] parts = id.Split('_');
                string targetFileName = "General_Items.md"; // Fallback

                if (parts.Length >= 2)
                {
                    // Erstellt den Namen basierend auf den ersten beiden Teilen, z.B. "Item_Fish" -> "Item_Fish.md"
                    targetFileName = $"{parts[0]}_{parts[1]}.md";
                }

                AddToCategory(targetFileName, item);
            }

            // 3. Generierung und Schreiben der einzelnen Markdown-Dateien
            foreach (var category in fileCategories)
            {
                string fileName = category.Key;
                var items = category.Value.OrderBy(i => i.name).ToList();

                StringBuilder md = new StringBuilder();

                // Titel für die Datei hübsch machen (z.B. Item_Fish.md -> Item Fish)
                string cleanTitle = fileName.Replace(".md", "").Replace("_", " ");
                md.AppendLine($"# {cleanTitle}");
                md.AppendLine();
                md.AppendLine("| Internal Name (ID) | Display Name | Max Stack | Acquirable | Droppable | Giftable | Type Addons |");
                md.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");

                foreach (var item in items)
                {
                    List<string> addons = new List<string>();
                    if (item.ToolAddon != null) addons.Add("Tool");
                    if (item.ConsumableAddon != null) addons.Add("Consumable");
                    if (item.SpellAddon != null) addons.Add("Spell");
                    if (item.PlantableAddon != null) addons.Add("Plantable");
                    if (item.CraftableAddon != null) addons.Add("Craftable");
                    if (item.FishAddon != null) addons.Add("Fish");
                    if (item.TradableAddon != null) addons.Add("Tradable");

                    string addonString = addons.Count > 0 ? string.Join(", ", addons) : "Standard";
                    string displayName = string.IsNullOrEmpty(item.Name) ? "Unknown" : item.Name;

                    md.AppendLine($"| `{item.name}` | **{displayName}** | {item.MaxStackSize} | {item.IsAcquirable} | {item.IsDroppable} | {item.IsGiftable} | {addonString} |");
                }

                string fullPath = Path.Combine(pluginFolder, fileName);
                try
                {
                    File.WriteAllText(fullPath, md.ToString(), Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Dumper] Fehler beim Schreiben der Datei {fileName}: {ex.Message}");
                }
            }

            Debug.Log($"[Dumper] Alle verfgbaren Items erfolgreich dynamisch aufgeteilt in: {pluginFolder}");
        }
    }
}
