using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Chicken.Utilities;
using HarmonyLib;
using UnityEngine;

namespace InstantCrafting
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        public static ConfigEntry<bool> InstantCrafting;
        public static ConfigEntry<bool> InstantCooking;
        public static ConfigEntry<bool> InstantBrewing;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            InstantCrafting = Config.Bind("General", "Instant Crafting", true, "Enable or disable instant crafting.");
            InstantCooking = Config.Bind("General", "Instant Cooking", true, "Enable or disable instant cooking.");
            InstantBrewing = Config.Bind("General", "Instant Brewing", true, "Enable or disable instant brewing.");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.instantcrafting");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            field?.SetValue(obj, value);
        }
    }

    [HarmonyPatch(typeof(MicrogameDifficultyAsset), "Difficulty", MethodType.Getter)]
    public static class MicrogameDifficultyAsset_get_Difficulty_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if(Plugin.InstantCrafting.Value)
            {
                __result = 0;
            }
        }
    }

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

    [HarmonyPatch(typeof(BrewingMicrogame), "Initialize")]
    public class BrewingMicrogamePatch
    {
        static bool Prefix(BrewingMicrogame __instance, CauldronInteractable cauldron, ItemAsset itemAsset)
        {
            if (!Plugin.InstantBrewing.Value)
            {
                return true;
            }

            __instance.GetType().GetProperty("Cauldron").SetValue(__instance, cauldron);
            __instance.GetType().GetProperty("BrewingItem").SetValue(__instance, itemAsset);
            __instance.GetType().GetProperty("IsCompleted").SetValue(__instance, true);

            MoonlightPeaksAudio.PlaySound(AddressableLibrary<AudioLibrary>.Instance.BrewingSuccess, null);
            if (cauldron != null && cauldron.View != null)
            {
                cauldron.View.PlayPoofParticles();
            }

            var spawnItemMethod = typeof(BrewingMicrogame).GetMethod("SpawnItem", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spawnItemMethod != null)
            {
                spawnItemMethod.Invoke(__instance, null);
            }

            return false;
        }
    }

}
