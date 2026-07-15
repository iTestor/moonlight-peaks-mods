using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chicken.Utilities;
using DG.Tweening;
using HarmonyLib;
using UnityEngine;

namespace FishingPlus
{
    public static class CatchFishInstantState
    {
        public static bool Active;
    }

    [HarmonyPatch(typeof(FishingCatchState), "CatchFish")]
    public static class FishingCatchStatePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(FishingCatchState __instance, ref IEnumerator __result)
        {
            __result = InstantCatchCoroutine(__instance);
            return false;
        }

        private static IEnumerator InstantCatchCoroutine(FishingCatchState state)
        {
            CatchFishInstantState.Active = true;

            // Protected Properties von FishingBaseState sind von hier aus nicht direkt
            // erreichbar (Patch sitzt nicht in der Klassenhierarchie) -> per Reflection lesen.
            FishingBobber bobber = (FishingBobber)ReflectionHelpers.GetMemberValue(state, "bobber");
            FishingData fishingData = (FishingData)ReflectionHelpers.GetMemberValue(state, "fishingData");
            FishingRodItemAddon fishingRod = (FishingRodItemAddon)ReflectionHelpers.GetMemberValue(state, "fishingRod");
            FishStateMachine targetFish = (FishStateMachine)ReflectionHelpers.GetMemberValue(state, "targetFish");
            PlayerView player = MonoBehaviourSingleton<PlayerView>.Instance;

            MoonlightPeaksAudio.PlaySound(
                AddressableLibrary<AudioLibrary>.Instance.FishingCatchAudio,
                new Vector3?(bobber.WorldPosition));

            ItemEntry itemEntry = default(ItemEntry);
            ItemEntryType itemEntryType = ItemEntryType.Normal;

            // 1. Loot bestimmen (identisch zum Original)
            if (fishingData.HasCaughtTrash)
            {
                List<ItemEntry> list;
                List<ItemEntry> list2;
                fishingRod.TrashLootTable.GenerateLoot(out list, out list2);
                List<ItemEntry> list3 = (list.Count > 0) ? list : list2;
                itemEntry = list3.FirstOrDefault();
                itemEntryType = (list3 == list) ? ItemEntryType.Normal : ItemEntryType.Recipe;
            }
            else
            {
                GamePersistence.Instance.GameStats.UniqueCaughtFish.AddDistinct(targetFish.CatchItem.SerializedGuid);
                AddressableLibrary<VariableLibrary>.Instance.UniqueFishCaught.SetValue(
                    GamePersistence.Instance.GameStats.UniqueCaughtFish.Count);
            }

            bool isRealFishCatch = itemEntry.ItemAsset == null; // kein Trash-Item gezogen -> echter Fisch

            if (isRealFishCatch)
            {
                itemEntry = new ItemEntry(targetFish.CatchItem)
                {
                    ItemQualityLevel = AddressableLibrary<GlobalSettingsLibrary>.Instance
                        .GetRandomizedItemQualityLevel(targetFish.CatchItem, fishingRod.QualityImprovement)
                };
                itemEntryType = ItemEntryType.Normal;
                targetFish.IsCaught = true;
                targetFish.FireCaught();

                // 2. Fisch SOFORT unsichtbar machen, aus dem Spawner austragen und terminieren.
                // WICHTIG: Das darf NUR hier passieren (echter Fang), nicht im Trash-Fall!
                // targetFish.HideAndDestroy() allein reicht nicht zuverlässig aus - der Fisch kann
                // unsichtbar aber noch registriert im Spawner hängen bleiben. Deshalb zusätzlich
                // die Renderer hart deaktivieren und manuell beim Spawner austragen.
                foreach (var renderer in targetFish.GetComponentsInChildren<Renderer>())
                {
                    renderer.enabled = false;
                }

                var nearestSpawner = FishSpawnPatch.GetNearestActiveSpawner(player.Character.transform.position);
                if (nearestSpawner != null)
                {
                    try
                    {
                        MethodInfo unregisterMethod = AccessTools.Method(nearestSpawner.GetType().BaseType, "UnregisterSpawnable");
                        if (unregisterMethod != null)
                        {
                            unregisterMethod.Invoke(nearestSpawner, new object[] { targetFish });
                        }
                        else
                        {
                            Plugin._logger.LogWarning("[InstantCatch] UnregisterSpawnable-Methode wurde nicht gefunden.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin._logger.LogError($"[InstantCatch] Fehler beim manuellen Austragen aus dem Spawner: {ex}");
                    }
                }

                targetFish.StopAllCoroutines();
                targetFish.HideAndDestroy();
            }
            else
            {
                // Trash-Fall: Fisch wird freigelassen und bleibt im Teich am Leben - exakt wie
                // im Original. ReleaseFish() initialisiert seine Nibble/Idle-State-Machine neu.
                // Er darf danach NICHT mehr angefasst werden (kein StopAllCoroutines, kein
                // HideAndDestroy) - das würde den frischen State-Reset zerreißen und genau die
                // NullReferenceException in FishBiteState.OnActivate() verursachen, die vorher
                // aufgetreten ist.
                targetFish.ReleaseFish();
            }

            itemEntry.Amount = 1;

            if (itemEntry.ItemAsset != null)
            {
                itemEntry.ItemAsset.ItemPersistence.TodaysStats.Caught++;

                bool hasSpace = MonoBehaviourSingleton<GameInventory>.Instance.HasSpaceForItem(itemEntry);

                if (hasSpace)
                {
                    // 3a. Platz vorhanden -> DIREKT ins Inventar, ohne PickupView/Interact-Flow.
                    // Wichtig: Der normale PickupView-Weg (spawnen + vom Spieler "einsammeln
                    // lassen") triggert offenbar ein spielweites Auto-Pickup/Interact-System,
                    // an dem die Wink/Zwinker-Animation hängt - unabhängig von FishingCatchState.
                    // Deshalb hier bewusst am Interact-System vorbei direkt ins Inventar schieben.
                    Inventory inventory = (itemEntryType == ItemEntryType.Recipe)
                        ? MonoBehaviourSingleton<GameInventory>.Instance.Recipes
                        : MonoBehaviourSingleton<GameInventory>.Instance.GetInventoryForItem(itemEntry.ItemAsset);

                    if (itemEntryType == ItemEntryType.Recipe)
                    {
                        MonoBehaviourSingleton<GameInventory>.Instance.UnlockRecipe(itemEntry.ItemAsset, false, false);
                    }

                    inventory.AddTransitItem(itemEntry);
                    inventory.TransferTransitItem(itemEntry);

                    MoonlightPeaksAudio.PlaySound(
                        AddressableLibrary<AudioLibrary>.Instance.ItemPickupAudio,
                        new Vector3?(player.Character.transform.position));
                    EventBus.OnItemPickedUp.Dispatch(itemEntry);
                    EventBus.OnPlayerInventoryChanged.Dispatch();
                }
                else
                {
                    // 3b. Kein Platz -> wie im Original als Pickup in die Welt fallen lassen,
                    // statt es zu verschlucken. Hier ist der normale Interact-Flow gewollt,
                    // der Spieler muss aktiv einsammeln, sobald Platz frei ist.
                    PickupView.InstantiateNew(itemEntry, player.Character.transform.position, itemEntryType, false);
                }
            }

            // 4. Fishing stoppen und State zurücksetzen (identisch zum Original)
            EventBus.OnFishingStopped.Dispatch(fishingData);
            player.StateMachine.GotoState<PlayerDefaultState>(Array.Empty<object>());

            CatchFishInstantState.Active = false;
            yield break;
        }
    }
}