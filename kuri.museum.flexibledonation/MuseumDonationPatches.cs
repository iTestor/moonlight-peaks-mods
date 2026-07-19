using Chicken.UI;
using Chicken.Utilities;
using HarmonyLib;
using kuri.museum.flexibledonation;

[HarmonyPatch]
public class MuseumDonationPatches
{
    private static ItemQualityLevel _cachedAvailableQuality = ItemQualityLevel.Regular;

    [HarmonyPatch(typeof(MuseumEntryItemAddon), "HasItemsInInventory", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool HasItemsInInventory_Prefix(MuseumEntryItemAddon __instance, ref bool __result)
    {
        if(!Plugin.Enabled.Value)
        {
            return true;
        }

        ItemAsset requiredItem = __instance.Item;
        int amountNeeded = __instance.AmountNeeded;
        ItemQualityLevel requiredQuality = __instance.RequiredQualityLevel;

        var gameInventory = MonoBehaviourSingleton<GameInventory>.Instance;
        if (gameInventory == null) return true;

        Inventory fallbackInventory;
        Inventory targetInventory = gameInventory.GetInventoryForItem(requiredItem, out fallbackInventory);
        if (targetInventory == null) targetInventory = fallbackInventory;
        if (targetInventory == null) return true;

        int targetQualityInt = (int)requiredQuality;

        foreach (ItemQualityLevel currentLevel in ItemQualityHelper.QualityLevels)
        {
            if (currentLevel >= (ItemQualityLevel)targetQualityInt)
            {
                ItemEntry searchEntry = new ItemEntry(requiredItem, amountNeeded, currentLevel, null, null);

                if (targetInventory.GetAmount(searchEntry, ItemEntryCompareMask.QualitySame_Metadata_CustomName, false) >= amountNeeded)
                {
                    _cachedAvailableQuality = currentLevel;
                    __result = true;
                    return false;
                }
            }
        }

        __result = false;
        return false;
    }

    [HarmonyPatch(typeof(DirectorMuseumInfoState), "HandleDonateClicked")]
    [HarmonyPrefix]
    public static bool HandleDonateClicked_Prefix(DirectorMuseumInfoState __instance, MuseumInfoEntryListWidget widget)
    {
        if (!Plugin.Enabled.Value)
        {
            return true;
        }

        ItemAsset data = widget.Data;

        ItemEntry giveEntry = new ItemEntry(data, data.MuseumEntryAddon.AmountNeeded, data.MuseumEntryAddon.RequiredQualityLevel, null, null);
        ItemEntry takeEntry = new ItemEntry(data, data.MuseumEntryAddon.AmountNeeded, _cachedAvailableQuality, null, null);

        var museumPersistence = Traverse.Create(__instance).Field("museumInventoryPersistence").GetValue<EntityInventoryPersistence>();
        if (museumPersistence == null) return true;

        museumPersistence.Inventory.AddItem(giveEntry);

        MonoBehaviourSingleton<GameInventory>.Instance.RemoveItem(takeEntry);

        EventBus.OnEntityInventoryChanged.Dispatch(museumPersistence);
        UIScreen<BatVfxScreen>.Instance.PlayVfx(widget);

        return false;
    }
}