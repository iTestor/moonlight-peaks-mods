using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ManaPlus
{
    internal class Patches_MoveGridObjectSpellDecorateStateMachineContext
    {
        public static bool ManipulateMoveGrid = false;
        public static MoveGridObjectSpellAsset spellAsset = null;

        [HarmonyPatch(typeof(MoveGridObjectSpellDecorateStateMachineContext), "OnContextDeactivate")]
        public static class MGOSPDMC_OnContextDeactivate_Patch
        {
            [HarmonyPrefix]
            public static void Prefix(MoveGridObjectSpellDecorateStateMachineContext __instance)
            {
                if(!Plugin.ManaPlusIsActive.Value)
                {
                    return;
                }

                spellAsset = ReflectionHelpers.GetMemberValue(__instance, "spellAsset") as MoveGridObjectSpellAsset;
                ManipulateMoveGrid = true;
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!Plugin.ManaPlusIsActive.Value)
                {
                    return;
                }

                ManipulateMoveGrid = false;
                spellAsset = null;
            }
        }
    }
}
