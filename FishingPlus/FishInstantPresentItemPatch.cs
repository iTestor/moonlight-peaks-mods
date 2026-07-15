using System;
using System.Collections;
using Chicken.Utilities;
using HarmonyLib;

namespace FishingPlus
{
    [HarmonyPatch(typeof(PlayerPresentItemState), "PresentItemRoutine")]
    public static class FishInstantPresentItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerPresentItemState __instance, ref IEnumerator __result)
        {
            if(!Plugin.SkipFishPresentItemAnimation.Value)
            {
                return true;
            }

            var itemAsset = Traverse.Create(__instance).Field("itemAsset").GetValue<ItemAsset>();

            if (itemAsset != null && IsFish(itemAsset))
            {
                __result = InstantRoutine(__instance);
                return false;
            }

            return true;
        }

        private static bool IsFish(ItemAsset itemAsset)
        {
            string name = itemAsset.name.ToLower();
            if (name.Contains("item_fish"))
            {
                return true;
            }

            return false;
        }

        private static IEnumerator InstantRoutine(PlayerPresentItemState state)
        {
            var context = Traverse.Create(state).Field("context").GetValue<PlayerPresentItemState.Context>();

            if (context != null)
            {
                context.OnAddItems?.Invoke();
            }

            var player = MonoBehaviourSingleton<PlayerView>.Instance;
            if (player != null && player.StateMachine != null)
            {
                player.StateMachine.GotoState<PlayerDefaultState>(Array.Empty<object>());
            }

            yield break;
        }
    }
}