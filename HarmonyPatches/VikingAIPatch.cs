using HarmonyLib;
using Norsemen;
using UnityEngine;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(VikingAI), nameof(VikingAI.FindWorkTargets))]
    public static class VikingAIPatch
    {
        private static int callCount;

        private static void Prefix(VikingAI __instance)
        {
            callCount++;

            if (callCount % 300 != 0)
            {
                return;
            }

            BepInEx.Logging.Logger.CreateLogSource("LivingNorsemen")
                .LogInfo($"FindWorkTargets is running on {__instance.name}");
        }
    }

    [HarmonyPatch(typeof(VikingAI), "UpdateAI")]
    public static class VikingAIUpdatePatch
    {
        private static bool Prefix(VikingAI __instance)
        {
            LivingNorseman component =
                __instance.GetComponent<LivingNorseman>();

            if (
                component != null &&
                (
                    component.CurrentCommand == VillagerCommand.GoHome ||
                    component.CurrentCommand == VillagerCommand.GoToWork ||
                    (
                        EnvMan.IsNight() &&
                        component.HasHome &&
                        Vector3.Distance(
                            component.transform.position,
                            component.HomePosition
                        ) <= 4f
                    )
                )
            )
            {
                return false;
            }

            return true;
        }
    }
}
