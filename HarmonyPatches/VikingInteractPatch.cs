using HarmonyLib;
using Norsemen;
using UnityEngine;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(Viking), nameof(Viking.GetHoverText))]
    public static class VikingHoverPatch
    {
        private static void Postfix(Viking __instance, ref string __result)
        {
            if (!__instance.IsTamed())
            {
                return;
            }

            LivingNorseman component =
                __instance.GetComponent<LivingNorseman>();

            if (component == null || !component.HasHome)
            {
                return;
            }

            __result += "\n[<color=yellow><b>H</b></color>] Go Home";

            if (component.HasWorkPosition)
            {
                __result += "\n[<color=yellow><b>K</b></color>] Go To Work";
            }
        }
    }
}
