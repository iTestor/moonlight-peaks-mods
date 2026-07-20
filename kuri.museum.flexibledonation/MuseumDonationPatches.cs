using System.Linq;
using Chicken.UI;
using Chicken.Utilities;
using HarmonyLib;

[HarmonyPatch]
public class MuseumDonationPatches
{
    private static bool manipulateInfo = false;

    private static ItemQualityLevel? FindAvailableQuality(ItemAsset item, int amountNeeded)
    {
        var gameInventory = MonoBehaviourSingleton<GameInventory>.Instance;
        if (gameInventory == null) return null;

        Inventory fallbackInventory;
        Inventory targetInventory = gameInventory.GetInventoryForItem(item, out fallbackInventory);
        if (targetInventory == null) targetInventory = fallbackInventory;
        if (targetInventory == null) return null;

        foreach (ItemQualityLevel currentLevel in ItemQualityHelper.QualityLevels.OrderBy(q => (int)q))
        {
            ItemEntry searchEntry = new ItemEntry(item, amountNeeded, currentLevel, null, null);
            if (targetInventory.GetAmount(searchEntry, ItemEntryCompareMask.QualitySame, false) >= amountNeeded)
            {
                return currentLevel;
            }
        }
        return null;
    }

    [HarmonyPatch(typeof(MuseumEntryItemAddon), "HasItemsInInventory", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool HasItemsInInventory_Prefix(MuseumEntryItemAddon __instance, ref bool __result)
    {

        var available = FindAvailableQuality(__instance.Item, __instance.AmountNeeded);
        __result = available.HasValue;
        return false;
    }

    [HarmonyPatch(typeof(DirectorMuseumInfoState), "HandleDonateClicked")]
    [HarmonyPrefix]
    public static bool HandleDonateClicked_Prefix(DirectorMuseumInfoState __instance, MuseumInfoEntryListWidget widget)
    {
        ItemAsset data = widget.Data;
        var addon = data.MuseumEntryAddon;

        var available = FindAvailableQuality(data, addon.AmountNeeded);
        if (!available.HasValue) return true;

        ItemEntry giveEntry = new ItemEntry(data, addon.AmountNeeded, addon.RequiredQualityLevel, null, null);
        ItemEntry takeEntry = new ItemEntry(data, addon.AmountNeeded, available.Value, null, null);

        var museumPersistence = Traverse.Create(__instance).Field("museumInventoryPersistence").GetValue<EntityInventoryPersistence>();
        if (museumPersistence == null) return true;

        museumPersistence.Inventory.AddItem(giveEntry);
        MonoBehaviourSingleton<GameInventory>.Instance.RemoveItem(takeEntry);
        EventBus.OnEntityInventoryChanged.Dispatch(museumPersistence);
        UIScreen<BatVfxScreen>.Instance.PlayVfx(widget);
        return false;
    }

    [HarmonyPatch(typeof(ItemRequirementInfoDisplay), "Show")]
    [HarmonyPrefix]
    public static void UI_Show_Prefix(ItemRequirementInfoDisplay __instance, ItemAsset itemAsset, ref ItemQualityLevel itemQualityLevel)
    {
        if (itemAsset != null && itemAsset.MuseumEntryAddon != null && manipulateInfo)
        {
            var available = FindAvailableQuality(itemAsset, itemAsset.MuseumEntryAddon.AmountNeeded);

            if (available.HasValue)
            {
                itemQualityLevel = available.Value;
            }
            else
            {
                itemQualityLevel = itemAsset.MuseumEntryAddon.RequiredQualityLevel;
            }

            var disableAscendingField = AccessTools.Field(typeof(ItemRequirementInfoDisplay), "disableAscendingQualityCheck");
            if (disableAscendingField != null)
            {
                disableAscendingField.SetValue(__instance, true);
            }
        }
    }

    [HarmonyPatch(typeof(MuseumInfoEntryListWidget), "UpdateVisual")]
    [HarmonyPrefix]
    public static void MuseumWidget_Prefix()
    {
        manipulateInfo = true;
    }

    [HarmonyPatch(typeof(MuseumInfoEntryListWidget), "UpdateVisual")]
    [HarmonyPostfix]
    public static void MuseumWidget_Postfix()
    {
        manipulateInfo = false;
    }

    [HarmonyPatch(typeof(ItemInfoWidget), "Show", new[] { typeof(ItemAsset), typeof(ItemQualityLevel) })]
    [HarmonyPrefix]
    public static void HideStarsInMuseum_Prefix(ItemInfoWidget __instance, ItemAsset itemAsset)
    {
        if (itemAsset != null && itemAsset.MuseumEntryAddon != null && manipulateInfo)
        {
            __instance.ToggleDisplay<ItemQualityInfoDisplay>(false);
        }
    }

}