using HarmonyLib;
using Norsemen;
using UnityEngine;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(VikingAI), nameof(VikingAI.FindWorkTargets))]
    public static class LumberjackTargetPatch
    {
        private static readonly AccessTools.FieldRef<VikingAI, TreeBase>
            treeRef =
                AccessTools.FieldRefAccess<VikingAI, TreeBase>(
                    "m_tree"
                );

        private static readonly AccessTools.FieldRef<VikingAI, bool>
            hasWorkTargetRef =
                AccessTools.FieldRefAccess<VikingAI, bool>(
                    "hasWorkTarget"
                );

        

        [HarmonyPostfix]
        private static void Postfix(VikingAI __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Viking viking =
                __instance.GetComponent<Viking>();

            if (
                viking == null ||
                viking.m_axe == null
            )
            {
                return;
            }

            LivingNorseman livingNorseman =
                __instance.GetComponent<LivingNorseman>();

            if (
                livingNorseman != null &&
                livingNorseman.CurrentCommand != VillagerCommand.None
            )
            {
                return;
            }

            string requestedTree =
                GetRequestedTree(viking);

            // No marker seed/cone:
            // leave VikingNPC's normal tree selection alone.
            if (string.IsNullOrEmpty(requestedTree))
            {
                return;
            }

            TreeBase target =
                FindNearestRequestedTree(
                    viking.transform.position,
                    requestedTree,
                    viking.m_axe,
                    50f
                );

            if (target == null)
            {
                // For now, fall back to whatever VikingNPC selected.
                return;
            }

            treeRef(__instance) = target;
            hasWorkTargetRef(__instance) = true;

            viking.EquipItem(
                viking.m_axe,
                true
            );

        }

        private static string GetRequestedTree(
             Viking viking)
        {
            foreach (
                ItemDrop.ItemData item
                in viking.GetInventory().GetAllItems()
            )
            {
                if (item?.m_dropPrefab == null)
                {
                    continue;
                }

                string prefab =
                    item.m_dropPrefab.name;

                switch (prefab)
                {
                    case "BeechSeeds":
                        return "Beech1";

                    case "BirchSeeds":
                        return "Birch1";

                    case "FirCone":
                        return "FirTree";

                    case "PineCone":
                        return "Pinetree_01";
                }
            }

            return null;
        }

        private static TreeBase FindNearestRequestedTree(
            Vector3 origin,
            string requestedTree,
            ItemDrop.ItemData axe,
            float searchRadius)
        {
            TreeBase nearest = null;
            float nearestDistance = float.MaxValue;

            TreeBase[] trees =
                Object.FindObjectsByType<TreeBase>(
                    FindObjectsSortMode.None
                );

            foreach (TreeBase tree in trees)
            {
               
                if (tree == null)
                {
                    continue;
                }

                if (
                    axe.m_shared.m_toolTier <
                    tree.m_minToolTier
                )
                {
                    continue;
                }

                string treeName =
                    Utils.GetPrefabName(
                        tree.gameObject
                    );

                if (
                    treeName.IndexOf(
                        requestedTree,
                        System.StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        origin,
                        tree.transform.position
                    );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = tree;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}