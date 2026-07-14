using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace InstantCrafting
{
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
