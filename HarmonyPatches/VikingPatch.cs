using BepInEx.Logging;
using HarmonyLib;
using Norsemen;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(Norseman), "Create")]
    public static class VikingPatch
    {
        private static void Postfix(Norseman __instance)
        {
            if (__instance.Prefab.GetComponent<LivingNorseman>() == null)
            {
                __instance.Prefab.AddComponent<LivingNorseman>();

                ManualLogSource logger =
                    BepInEx.Logging.Logger.CreateLogSource("LivingNorsemen");

                logger.LogInfo(
                    $"Attached LivingNorseman to {__instance.name}"
                );
            }
        }
    }
}