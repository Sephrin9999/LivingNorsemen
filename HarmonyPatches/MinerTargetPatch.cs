using HarmonyLib;
using Norsemen;
using UnityEngine;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(VikingAI), nameof(VikingAI.FindWorkTargets))]
    public static class MinerTargetPatch
    {

        private static readonly AccessTools.FieldRef<VikingAI, Destructible>
            destructibleRef =
        AccessTools.FieldRefAccess<VikingAI, Destructible>(
            "m_destructible"
        );

        private static readonly AccessTools.FieldRef<VikingAI, MineRock5>
            mineRock5Ref =
                AccessTools.FieldRefAccess<VikingAI, MineRock5>(
                    "m_mineRock5"
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
                viking.m_pickaxe == null
            )
            {
                return;
            }

            // LivingNorsemen commands take priority.
            LivingNorseman livingNorseman =
                __instance.GetComponent<LivingNorseman>();

            if (
                livingNorseman != null &&
                livingNorseman.CurrentCommand !=
                    VillagerCommand.None
            )
            {
                return;
            }

            string requestedOre =
                GetRequestedOre(viking);

            // No ore marker:
            // leave VikingNPC's normal mining selection alone.
            if (string.IsNullOrEmpty(requestedOre))
            {
                return;
            }
            if (requestedOre == "CopperOre")
            {
                MineRock5 copperTarget =
                    FindRequestedMineRock5(
                        viking,
                        requestedOre
                    );

                if (copperTarget == null)
                {
                    // Fall back to VikingNPC's normal mining target.
                    return;
                }

                mineRock5Ref(__instance) =
                    copperTarget;

                hasWorkTargetRef(__instance) =
                    true;
            }
            else
            {
                Destructible target =
                    FindRequestedDestructible(
                        viking,
                        requestedOre
                    );

                if (target == null)
                {
                    // Fall back to VikingNPC's normal mining target.
                    return;
                }

                destructibleRef(__instance) =
                    target;

                hasWorkTargetRef(__instance) =
                    true;
            }

            viking.EquipItem(
                viking.m_pickaxe,
                true
            );
        }

        

        private static string GetRequestedOre(
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
                    case "Copper":
                        return "CopperOre";

                    case "Tin":
                        return "TinOre";
                }
            }

            return null;
        }

        private static Destructible FindRequestedDestructible(
            Viking viking,
            string requestedOre,
            float searchRadius = 50f)
                {
            Destructible nearest = null;
            float nearestDistance = float.MaxValue;

            Destructible[] destructibles =
                Object.FindObjectsByType<Destructible>(
                    FindObjectsSortMode.None
                );

            foreach (Destructible destructible in destructibles)
            {
                if (destructible == null)
                {
                    continue;
                }

                if (
                    viking.m_pickaxe.m_shared.m_toolTier <
                    destructible.m_minToolTier
                )
                {
                    continue;
                }

                string prefabName =
                    Utils.GetPrefabName(
                        destructible.gameObject
                    );

                bool matches = false;

                if (
                    requestedOre == "CopperOre" &&
                    prefabName.IndexOf(
                        "copper",
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
                    matches = true;
                }
                else if (
                    requestedOre == "TinOre" &&
                    prefabName.IndexOf(
                        "tin",
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
                    matches = true;
                }

                if (!matches)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        viking.transform.position,
                        destructible.transform.position
                    );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = destructible;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static MineRock5 FindRequestedMineRock5(
            Viking viking,
            string requestedOre,
            float searchRadius = 50f)
        {
            MineRock5 nearest = null;
            float nearestDistance = float.MaxValue;

            MineRock5[] rocks =
                Object.FindObjectsByType<MineRock5>(
                    FindObjectsSortMode.None
                );

            foreach (MineRock5 rock in rocks)
            {
                if (rock == null)
                {
                    continue;
                }

                string prefabName =
                    Utils.GetPrefabName(
                        rock.gameObject
                    );

                bool matches =
                    requestedOre == "CopperOre" &&
                    prefabName.IndexOf(
                        "copper",
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0;

                if (!matches)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        viking.transform.position,
                        rock.transform.position
                    );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = rock;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
    }
}