using System.Collections.Generic;
using System.Linq;
using Chicken.UI;
using Chicken.Utilities;
using HarmonyLib;
using TMPro;

[HarmonyPatch]
public class MuseumDonationPatches
{
    private static bool manipulateInfo = false;

    private static Dictionary<IEntityAsset, Inventory> GetAllInventories()
    {
        var gameInventory = MonoBehaviourSingleton<GameInventory>.Instance;
        if (gameInventory == null) return null;

        return Traverse.Create(gameInventory)
            .Field("allInventories")
            .GetValue<Dictionary<IEntityAsset, Inventory>>();
    }

    private static ItemQualityLevel? FindAvailableQuality(ItemAsset item, int amountNeeded, out Inventory sourceInventory)
    {
        sourceInventory = null;

        var allInventories = GetAllInventories();
        if (allInventories == null || allInventories.Count == 0) return null;

        foreach (ItemQualityLevel currentLevel in ItemQualityHelper.QualityLevels.OrderBy(q => (int)q))
        {
            ItemEntry searchEntry = new ItemEntry(item, amountNeeded, currentLevel, null, null);

            foreach (Inventory inv in allInventories.Values)
            {
                if (inv == null) continue;

                if (inv.GetAmount(searchEntry, ItemEntryCompareMask.QualitySame, false) >= amountNeeded)
                {
                    sourceInventory = inv;
                    return currentLevel;
                }
            }
        }

        return null;
    }

    private static int GetTotalAmountAcrossAllInventories(ItemAsset item, ItemQualityLevel quality)
    {
        var allInventories = GetAllInventories();
        if (allInventories == null) return 0;

        int total = 0;
        ItemEntry entry = new ItemEntry(item, quality);

        foreach (Inventory inv in allInventories.Values)
        {
            if (inv == null) continue;
            total += inv.GetAmount(entry, ItemEntryCompareMask.QualitySame_Metadata_CustomName, false);
        }

        return total;
    }

    [HarmonyPatch(typeof(MuseumEntryItemAddon), "HasItemsInInventory", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool HasItemsInInventory_Prefix(MuseumEntryItemAddon __instance, ref bool __result)
    {
        var available = FindAvailableQuality(__instance.Item, __instance.AmountNeeded, out _);
        __result = available.HasValue;
        return false;
    }

    [HarmonyPatch(typeof(DirectorMuseumInfoState), "HandleDonateClicked")]
    [HarmonyPrefix]
    public static bool HandleDonateClicked_Prefix(DirectorMuseumInfoState __instance, MuseumInfoEntryListWidget widget)
    {
        ItemAsset data = widget.Data;
        var addon = data.MuseumEntryAddon;

        var available = FindAvailableQuality(data, addon.AmountNeeded, out Inventory sourceInventory);
        if (!available.HasValue || sourceInventory == null) return true;

        ItemEntry giveEntry = new ItemEntry(data, addon.AmountNeeded, addon.RequiredQualityLevel, null, null);
        ItemEntry takeEntry = new ItemEntry(data, addon.AmountNeeded, available.Value, null, null);

        var museumPersistence = Traverse.Create(__instance).Field("museumInventoryPersistence").GetValue<EntityInventoryPersistence>();
        if (museumPersistence == null) return true;

        museumPersistence.Inventory.AddItem(giveEntry);
        sourceInventory.RemoveItem(takeEntry, ItemEntryCompareMask.QualitySame_Metadata_CustomName);
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
            var available = FindAvailableQuality(itemAsset, itemAsset.MuseumEntryAddon.AmountNeeded, out _);

            itemQualityLevel = available.HasValue
                ? available.Value
                : itemAsset.MuseumEntryAddon.RequiredQualityLevel;

            var disableAscendingField = AccessTools.Field(typeof(ItemRequirementInfoDisplay), "disableAscendingQualityCheck");
            if (disableAscendingField != null)
            {
                disableAscendingField.SetValue(__instance, true);
            }
        }
    }

    // NEU: behebt den 0/1-Anzeigebug, weil SetupAmounts intern nur
    // GetInventoryForItem (Ziel- + Fallback-Inventar) abfragt, statt alle Inventare.
    [HarmonyPatch(typeof(ItemRequirementInfoDisplay), "SetupAmounts")]
    [HarmonyPostfix]
    public static void SetupAmounts_Postfix(
        int quantity,
        TextMeshProUGUI ___amountInventoryText,
        UIColorable ___amountInventoryTextColorable,
        ItemAsset ___itemAsset,
        ItemQualityLevel ___itemQualityLevel,
        int ___requiredAmount,
        int? ___overrideOwnedAmount,
        ref bool ___hasEnoughInInventory)
    {
        if (!manipulateInfo) return;
        if (___itemAsset == null || ___itemAsset.MuseumEntryAddon == null) return;
        if (___amountInventoryText == null) return;
        if (___overrideOwnedAmount.HasValue) return; // expliziter Override hat Vorrang

        int required = ___requiredAmount * quantity;
        int owned = GetTotalAmountAcrossAllInventories(___itemAsset, ___itemQualityLevel);

        ___hasEnoughInInventory = owned >= required;
        ___amountInventoryText.text = TextUtility.GetFormattedNumber(owned);
        ___amountInventoryTextColorable?.OverrideOrClear(!___hasEnoughInInventory, AddressableLibrary<ColorLibrary>.Instance.SelectionColor, 0f, 0f);
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