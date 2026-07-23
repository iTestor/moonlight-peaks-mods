using System;
using System.Collections;
using Chicken.Utilities;
using HarmonyLib;

namespace kuri.moonlightpeaks.critterplus
{
    [HarmonyPatch(typeof(PlayerPresentItemState), "PresentItemRoutine")]
    public static class CritterInstantPresentItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerPresentItemState __instance, ref IEnumerator __result)
        {
            if(!Plugin.SkipPresentItemAnimation.Value)
            {
                return true;
            }

            var itemAsset = Traverse.Create(__instance).Field("itemAsset").GetValue<ItemAsset>();

            if (itemAsset != null && IsCritter(itemAsset))
            {
                __result = InstantRoutine(__instance);
                return false;
            }

            return true;
        }

        private static bool IsCritter(ItemAsset itemAsset)
        {
            string name = itemAsset.name.ToLower();
            if (name.Contains("item_critter"))
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