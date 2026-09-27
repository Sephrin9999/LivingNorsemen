using HarmonyLib;
using Norsemen;
using UnityEngine;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(VikingAI), nameof(VikingAI.DoChopping))]
    public static class LumberjackBonusDropPatch
    {
        [HarmonyPostfix]
        private static void Postfix(VikingAI __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Viking viking =
                __instance.GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            // 10% independent chance for Resin.
            if (UnityEngine.Random.value <= 0.10f)
            {
                TryAddBonusItem(
                    viking,
                    "Resin"
                );
            }

            // 10% independent chance for Feathers.
            if (UnityEngine.Random.value <= 0.10f)
            {
                TryAddBonusItem(
                    viking,
                    "Feathers"
                );
            }
        }

        private static void TryAddBonusItem(
            Viking viking,
            string prefabName)
        {
            GameObject prefab =
                ZNetScene.instance.GetPrefab(prefabName);

            if (prefab == null)
            {
                return;
            }

            Inventory inventory =
                viking.GetInventory();

            if (
                inventory == null ||
                !inventory.CanAddItem(prefab, 1)
            )
            {
                return;
            }

            inventory.AddItem(
                prefab.name,
                1,
                1,
                0,
                0L,
                "",
                false
            );
        }
    }
}
