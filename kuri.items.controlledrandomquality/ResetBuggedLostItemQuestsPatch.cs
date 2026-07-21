using System;
using System.Collections.Generic;
using Chicken.Utilities;
using HarmonyLib;

namespace kuri.items.controlledrandomquality
{
    [HarmonyPatch(typeof(GameInventory), "HandleDayStartEvent")]
    public static class ResetBuggedLostItemQuestsPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (Plugin.ResetBuggedLostItemQuests.Value)
            {
                ResetBuggedLostItemQuests();
                Plugin.ResetBuggedLostItemQuests.Value = false;
            }
        }

        public static void ResetBuggedLostItemQuests()
        {
            try
            {
                var gamePersistence = GamePersistence.Instance;
                var jobBoard = gamePersistence?.JobBoard;
                if (jobBoard?.ActiveJobs == null) return;

                var gameInventory = MonoBehaviourSingleton<GameInventory>.Instance;
                if (gameInventory == null) return;


                var activeJobsCopy = new List<JobPersistence>(jobBoard.ActiveJobs);

                foreach (var job in activeJobsCopy)
                {
                    if (job == null) continue;

                    var lostItemSub = job.LostItemJobPersistence;
                    if (lostItemSub != null && lostItemSub.LostItem.ItemAsset != null)
                    {
                        var targetAsset = lostItemSub.LostItem.ItemAsset;
                        string itemName = targetAsset.name ?? "Unknown Item";

                        // 1. Alle Instanzen dieses Quest-Items im Inventar suchen
                        List<ItemEntry> foundItems = GetQuestItemsFromInventory(gameInventory, targetAsset);

                        if (foundItems.Count > 0)
                        {
                            Plugin._logger?.LogWarning($"[RetroFix] Found {foundItems.Count} instance(s) of bugged quest item '{itemName}' in inventory. Starting cleanup...");


                            foreach (var item in foundItems)
                            {
                                Plugin._logger?.LogInfo($"[RetroFix] Removing item '{itemName}' (Quality: {item.ItemQualityLevel}) from inventory.");
                                gameInventory.RemoveItem(item);
                            }


                            BaseJobAsset jobAsset = job.JobAssetRef?.Asset;
                            NpcConfigAsset npcConfig = job.SubjectNpcAssetRef?.Asset;


                            lostItemSub.HasSpawnedItem = false;


                            Plugin._logger?.LogInfo($"[RetroFix] Removing active job for '{itemName}'.");
                            jobBoard.RemoveActiveJob(job);


                            if (jobAsset != null && npcConfig != null && JobController.Instance != null)
                            {
                                Plugin._logger?.LogInfo($"[RetroFix] Re-posting fresh job for '{itemName}' via JobController.");
                                JobController.Instance.PostJob(jobAsset, npcConfig);
                            }
                            else
                            {
                                Plugin._logger?.LogWarning($"[RetroFix] JobController unavailable! Re-adding original job back to OpenJobs directly.");
                                jobBoard.AddOpenJob(job);
                            }

                            Plugin._logger?.LogInfo($"[RetroFix] Successfully reset quest and cleared items for '{itemName}'.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin._logger?.LogError($"[RetroFix] Error occurred while resetting bugged lost item quests: {ex}");
            }
        }

        private static List<ItemEntry> GetQuestItemsFromInventory(GameInventory gameInventory, ItemAsset targetAsset)
        {
            var matchedItems = new List<ItemEntry>();

            // Hauptinventar, Quest-Items & GlobalInventory (Kisten/Lager) durchsuchen
            var inventoriesToCheck = new[] { gameInventory.Inventory, gameInventory.QuestItems, gameInventory.GlobalInventory };

            foreach (var inv in inventoriesToCheck)
            {
                if (inv?.Slots == null) continue;

                foreach (var slot in inv.Slots)
                {
                    if (slot != null && !slot.IsEmpty && slot.ItemAsset == targetAsset)
                    {
                        matchedItems.Add(slot.ItemEntry);
                    }
                }
            }
            return matchedItems;
        }
    }
}