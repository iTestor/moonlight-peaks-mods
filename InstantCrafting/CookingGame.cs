using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chicken.Utilities;
using HarmonyLib;
using UnityEngine;

namespace InstantCrafting
{
    [HarmonyPatch(typeof(CookingMicrogame), "Initialize")]
    public class CookingMicrogamePatch
    {
        static bool Prefix(CookingMicrogame __instance, Transform targetTransform, ItemGradedQuantity item)
        {
            if (!Plugin.InstantCooking.Value)
            {
                return true;
            }

            __instance.GetType().GetProperty("Target").SetValue(__instance, targetTransform);
            __instance.GetType().GetProperty("CookingItem").SetValue(__instance, item);
            __instance.GetType().GetProperty("IsCompleted").SetValue(__instance, true);

            GamePersistence.Instance.GameStats.UniqueCookedDishes.AddDistinct(item.ItemAsset.SerializedGuid);
            AddressableLibrary<VariableLibrary>.Instance.UniqueDishesCooked.SetValue(GamePersistence.Instance.GameStats.UniqueCookedDishes.Count);
            AddressableLibrary<EventLibrary>.Instance.OnDishCooked.Dispatch(null, EventAsset.ResolveMethod.All);

            return false;
        }
    }
}
