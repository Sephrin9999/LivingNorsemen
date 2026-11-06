using Norsemen;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace LivingNorsemen
{
    public static class ProfDepotService
    {

        private static ProfessionDepositBox[] cachedDepositBoxes;

        private static float depositBoxCacheTimer = 0f;

        private const float DepositBoxCacheInterval = 5f;


        private static readonly System.Reflection.MethodInfo MoveToMethod =
            AccessTools.Method(
                typeof(BaseAI),
                "MoveTo",
                new[]
                {
                    typeof(float),
                    typeof(Vector3),
                    typeof(float),
                    typeof(bool)
                }
            );

        public static bool MoveToward(
            LivingNorseman owner,
            VikingAI ai,
            string profession,
            float deltaTime,
            float searchRadius = 30f)
        {
            if (
                owner == null ||
                ai == null ||
                MoveToMethod == null
            )
            {
                return false;
            }

            ProfessionDepositBox depositBox =
                FindNearest(
                    owner.transform.position,
                    profession,
                    searchRadius
                );

            if (depositBox == null)
            {
                return false;
            }

            // Deposit travel uses normal running movement.
            Viking viking =
                owner.GetComponent<Viking>();

            if (viking != null)
            {
                viking.SetWalk(false);
            }

            Vector3 target =
                depositBox.transform.position;

            MoveToMethod.Invoke(
                ai,
                new object[]
                {
            deltaTime,
            target,
            1.5f,
            false
                }
            );

            // Verify actual X/Z arrival rather than trusting MoveTo().
            float distance = Vector2.Distance(
                new Vector2(
                    owner.transform.position.x,
                    owner.transform.position.z
                ),
                new Vector2(
                    target.x,
                    target.z
                )
            );

            return distance <= 2f;
        }

        public static ProfessionDepositBox FindNearest(
            Vector3 origin,
            string profession,
            float searchRadius = 30f)
        {
            ProfessionDepositBox nearest = null;
            float nearestDistance = float.MaxValue;

            depositBoxCacheTimer -= Time.deltaTime;

            if (
                cachedDepositBoxes == null ||
                depositBoxCacheTimer <= 0f
            )
            {
                cachedDepositBoxes =
                    Object.FindObjectsByType<ProfessionDepositBox>(
                        FindObjectsSortMode.None
                    );

                depositBoxCacheTimer =
                    DepositBoxCacheInterval;
            }

            foreach (
                ProfessionDepositBox depositBox
                in cachedDepositBoxes
            )
            {
                if (depositBox == null)
                {
                    continue;
                }

                if (depositBox.GetDepositType() != profession)
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    origin,
                    depositBox.transform.position
                );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = depositBox;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        public static bool HasIngredients(
            ProfessionDepositBox depositBox,
            List<CookIngredient> ingredients)
        {
            if (
                depositBox == null ||
                ingredients == null
            )
            {
                return false;
            }

            Container container =
                depositBox.GetComponent<Container>();

            if (container == null)
            {
                return false;
            }

            Inventory chestInventory =
                container.GetInventory();

            if (chestInventory == null)
            {
                return false;
            }

            foreach (CookIngredient ingredient in ingredients)
            {
                if (ingredient == null)
                {
                    continue;
                }

                GameObject prefab =
                    ObjectDB.instance.GetItemPrefab(
                        ingredient.PrefabName
                    );

                if (prefab == null)
                {
                    return false;
                }

                ItemDrop itemDrop =
                    prefab.GetComponent<ItemDrop>();

                if (itemDrop == null)
                {
                    return false;
                }

                string sharedName =
                    itemDrop.m_itemData.m_shared.m_name;

                int available =
                    chestInventory.CountItems(
                        sharedName,
                        -1,
                        true
                    );

                if (available < ingredient.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool TakeIngredients(
            Viking viking,
            ProfessionDepositBox depositBox,
            List<CookIngredient> ingredients)
        {
            if (
                viking == null ||
                depositBox == null ||
                ingredients == null
            )
            {
                return false;
            }

            Container container =
                depositBox.GetComponent<Container>();

            if (container == null)
            {
                return false;
            }

            Inventory villagerInventory =
                viking.GetInventory();

            Inventory chestInventory =
                container.GetInventory();

            if (
                villagerInventory == null ||
                chestInventory == null
            )
            {
                return false;
            }

            // Verify the complete recipe exists first.
            if (!HasIngredients(depositBox, ingredients))
            {
                return false;
            }

            foreach (CookIngredient ingredient in ingredients)
            {
                GameObject prefab =
                    ObjectDB.instance.GetItemPrefab(
                        ingredient.PrefabName
                    );

                if (prefab == null)
                {
                    return false;
                }

                ItemDrop itemDrop =
                    prefab.GetComponent<ItemDrop>();

                if (itemDrop == null)
                {
                    return false;
                }

                string sharedName =
                    itemDrop.m_itemData.m_shared.m_name;

                // Make sure the villager can actually carry the items
                // before removing them from the chest.
                if (
                    !villagerInventory.CanAddItem(
                        prefab,
                        ingredient.Amount
                    )
                )
                {
                    return false;
                }

                chestInventory.RemoveItem(
                    sharedName,
                    ingredient.Amount,
                    -1,
                    true
                );

                ItemDrop.ItemData addedItem =
                    villagerInventory.AddItem(
                        prefab.name,
                        ingredient.Amount,
                        1,
                        0,
                        0L,
                        "",
                        false
                    );

                if (addedItem == null)
                {
                    Debug.LogWarning(
                        $"[Cook] Failed to add " +
                        $"{ingredient.Amount} x {ingredient.PrefabName} " +
                        $"to {viking.GetName()}"
                    );

                    return false;
                }
            }

            return true;
        }

        public static int TakeAllowedItems(
            Viking viking,
            ProfessionDepositBox depositBox,
            string profession,
            int amountNeeded)
        {
            if (
                viking == null ||
                depositBox == null ||
                amountNeeded <= 0
            )
            {
                return 0;
            }

            Container container =
                depositBox.GetComponent<Container>();

            if (container == null)
            {
                return 0;
            }

            Inventory villagerInventory =
                viking.GetInventory();

            Inventory chestInventory =
                container.GetInventory();

            if (
                villagerInventory == null ||
                chestInventory == null
            )
            {
                return 0;
            }

            List<ItemDrop.ItemData> items =
                new List<ItemDrop.ItemData>(
                    chestInventory.GetAllItems()
                );

            int totalTaken = 0;

            foreach (ItemDrop.ItemData item in items)
            {
                if (
                    item == null ||
                    totalTaken >= amountNeeded
                )
                {
                    continue;
                }

                string prefabName =
                    item.m_dropPrefab != null
                        ? item.m_dropPrefab.name
                        : "";

                if (
                    !IsAllowedItem(
                        profession,
                        prefabName
                    )
                )
                {
                    continue;
                }

                int amountToTake =
                    Mathf.Min(
                        item.m_stack,
                        amountNeeded - totalTaken
                    );

                for (int i = 0; i < amountToTake; i++)
                {
                    if (
                        !villagerInventory.CanAddItem(
                            item,
                            1
                        )
                    )
                    {
                        return totalTaken;
                    }

                    ItemDrop.ItemData addedItem =
                        villagerInventory.AddItem(
                            prefabName,
                            1,
                            item.m_quality,
                            item.m_variant,
                            item.m_crafterID,
                            item.m_crafterName,
                            new Vector2i(-1, -1),
                            false
                        );

                    if (addedItem == null)
                    {
                        return totalTaken;
                    }

                    chestInventory.RemoveItem(
                        item,
                        1
                    );

                    totalTaken++;
                }
            }

            return totalTaken;
        }


        public static bool DepositGoods(
            Viking viking,
            ProfessionDepositBox depositBox,
            string profession)
        {
            Debug.Log(
                $"[DepositGoods] Profession={profession}, " +
                $"Viking={(viking != null ? viking.GetName() : "NULL")}, " +
                $"Box={(depositBox != null ? depositBox.name : "NULL")}"
            );

            if (viking == null || depositBox == null)
            {
                return false;
            }

            Container container =
                depositBox.GetComponent<Container>();

            if (container == null)
            {
                return false;
            }

            Inventory villagerInventory =
                viking.GetInventory();

            Inventory chestInventory =
                container.GetInventory();

            if (villagerInventory == null || chestInventory == null)
            {
                return false;
            }

            List<ItemDrop.ItemData> items =
                new List<ItemDrop.ItemData>(
                    villagerInventory.GetAllItems()
                );

            bool depositedAny = false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null)
                {
                    continue;
                }

                string prefabName =
                    item.m_dropPrefab != null
                        ? item.m_dropPrefab.name
                        : "";

                if (!IsAllowedItem(profession, prefabName))
                {
                    continue;
                }

                int villagerBefore =
                    CountAllowedItems(viking, profession);

                int chestBefore =
                    chestInventory.NrOfItems();

                chestInventory.MoveItemToThis(
                    villagerInventory,
                    item
                );

                int villagerAfter =
                    CountAllowedItems(viking, profession);

                int chestAfter =
                    chestInventory.NrOfItems();

                if (profession == "Lumberjack")
                {
                    Debug.Log(
                        $"[Lumberjack Deposit] " +
                        $"Item={prefabName}, " +
                        $"VillagerBefore={villagerBefore}, " +
                        $"VillagerAfter={villagerAfter}, " +
                        $"ChestBefore={chestBefore}, " +
                        $"ChestAfter={chestAfter}"
                    );
                }

                if (villagerAfter < villagerBefore)
                {
                    depositedAny = true;
                }
            }

            // Check for remaining lumberjack goods AFTER all transfers.
            if (profession == "Lumberjack")
            {
                int remaining =
                    CountAllowedItems(viking, profession);

                if (remaining > 0)
                {
                    Debug.LogWarning(
                        $"[Lumberjack Deposit] {viking.GetName()} " +
                        $"still carries {remaining} items after depositing. " +
                        $"Deposit box may be full."
                    );

                    return false;
                }
            }

            return depositedAny;
        }


        public static int CountAllowedItems(
            Viking viking,
            string profession)
        {
            if (viking == null)
            {
                return 0;
            }

            Inventory inventory =
                viking.GetInventory();

            if (inventory == null)
            {
                return 0;
            }

            int totalCount = 0;

            foreach (
                ItemDrop.ItemData item
                in inventory.GetAllItems()
            )
            {
                if (item == null)
                {
                    continue;
                }

                string prefabName =
                    item.m_dropPrefab != null
                        ? item.m_dropPrefab.name
                        : "";

                if (
                    !IsAllowedItem(
                        profession,
                        prefabName
                    )
                )
                {
                    continue;
                }

                totalCount += item.m_stack;
            }

            return totalCount;
        }

        public static bool HasAllowedItems(
            Viking viking,
            string profession)
        {
            if (viking == null)
            {
                return false;
            }

            Inventory inventory =
                viking.GetInventory();

            if (inventory == null)
            {
                return false;
            }

            foreach (
                ItemDrop.ItemData item
                in inventory.GetAllItems()
            )
            {
                if (item == null)
                {
                    continue;
                }

                string prefabName =
                    item.m_dropPrefab != null
                        ? item.m_dropPrefab.name
                        : "";

                if (
                    IsAllowedItem(
                        profession,
                        prefabName
                    )
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAllowedItem(
            string profession,
            string prefabName)
        {
            switch (profession)
            {
                case "Farmer":
                    return
                        prefabName == "Carrot" ||
                        prefabName == "Turnip" ||
                        prefabName == "Onion" ||
                        prefabName == "Barley" ||
                        prefabName == "Flax" ||
                        prefabName == "Honey" ||
                        prefabName == "Raspberry" ||
                        prefabName == "Blueberries" ||
                        prefabName == "Cloudberry" ||
                        prefabName == "Mushroom";

                case "Lumberjack":
                    return
                        prefabName == "Wood" ||
                        prefabName == "FineWood" ||
                        prefabName == "RoundLog" ||
                        prefabName == "YggdrasilWood" ||
                        prefabName == "Resin" ||
                        prefabName == "Feathers";

                case "Miner":
                    return
                        prefabName == "CopperOre" ||
                        prefabName == "TinOre" ||
                        prefabName == "IronScrap" ||
                        prefabName == "SilverOre" ||
                        prefabName == "BlackMetalScrap" ||
                        prefabName == "FlametalOreNew" ||
                        prefabName == "Stone";

                case "Fisherman":
                    return
                        prefabName.StartsWith("Fish") &&
                        !prefabName.StartsWith("Fishing");

                case "Cooked Foods":
                    return
                        prefabName == "NeckTailGrilled" ||
                        prefabName == "CookedDeerMeat" ||
                        prefabName == "CookedMeat" ||
                        prefabName == "CookedWolfMeat" ||
                        prefabName == "CookedLoxMeat" ||
                        prefabName == "CookedHareMeat" ||
                        prefabName == "CookedChickenMeat" ||
                        prefabName == "FishCooked" ||
                        prefabName == "Sausages";

                default:
                    return false;
            }
        }
    }
}
