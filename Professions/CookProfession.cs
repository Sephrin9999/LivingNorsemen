using Norsemen;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace LivingNorsemen
{
    public class CookProfession
    {
        private readonly LivingNorseman owner;

        private float lastFailedIngredientCheckTime = -9999f;

        private const float IngredientCheckCooldown = 120f;

        private static CookingStation[] cachedCookingStations;

        private static float cookingStationCacheTimer = 0f;

        private const float CookingStationCacheInterval = 5f;

        private static readonly System.Reflection.MethodInfo
            HaveDoneItemMethod =
                AccessTools.Method(
                    typeof(CookingStation),
                    "HaveDoneItem"
                );

        private static readonly System.Reflection.MethodInfo
            GetSlotMethod =
                AccessTools.Method(
                    typeof(CookingStation),
                    "GetSlot"
                );

        private static readonly System.Reflection.MethodInfo
            SetSlotMethod =
                AccessTools.Method(
                    typeof(CookingStation),
                    "SetSlot"
                );

        private static readonly System.Reflection.MethodInfo
            GetFreeSlotMethod =
                AccessTools.Method(
                    typeof(CookingStation),
                    "GetFreeSlot"
                );

        private static readonly List<CookRecipe> Recipes =
            new List<CookRecipe>
            {
                // ----------------------------------------------------
                // Mountains
                // ----------------------------------------------------
                new CookRecipe(
                    markerItem: "TrophyWolf",
                    priority: 35,
                    method: CookingMethod.Roast,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("WolfMeat", 1)
                    },
                    outputItem: "CookedWolfMeat",
                    outputAmount: 1
                ),


                // ----------------------------------------------------
                // SWAMP
                // ----------------------------------------------------

                new CookRecipe(
                    markerItem: "TrophyDraugr",
                    priority: 40,
                    method: CookingMethod.Cauldron,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("Entrails", 1),
                        new CookIngredient("Thistle", 1),
                        new CookIngredient("RawMeat", 1)
                    },
                    outputItem: "Sausages",
                    outputAmount: 1
                ),

                // ----------------------------------------------------
                // BLACK FOREST / MEADOWS
                // ----------------------------------------------------

                new CookRecipe(
                    markerItem: "TrophyDeer",
                    priority: 30,
                    method: CookingMethod.Roast,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("DeerMeat", 1)
                    },
                    outputItem: "CookedDeerMeat",
                    outputAmount: 1
                ),

                new CookRecipe(
                    markerItem: "TrophyBoar",
                    priority: 20,
                    method: CookingMethod.Roast,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("RawMeat", 1)
                    },
                    outputItem: "CookedMeat",
                    outputAmount: 1
                ),

                new CookRecipe(
                    markerItem: "TrophyNeck",
                    priority: 10,
                    method: CookingMethod.Roast,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("NeckTail", 1)
                    },
                    outputItem: "NeckTailGrilled",
                    outputAmount: 1
                ),

                new CookRecipe(
                    markerItem: "",
                    priority: 5,
                    method: CookingMethod.Roast,
                    ingredients: new List<CookIngredient>
                    {
                        new CookIngredient("FishRaw", 1)
                    },
                    outputItem: "FishCooked",
                    outputAmount: 1
)

            };

        public CookProfession(LivingNorseman owner)
        {
            this.owner = owner;
        }

        public CookRecipe GetHighestPriorityRecipe()
        {
            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return null;
            }

            Inventory inventory =
                viking.GetInventory();

            if (inventory == null)
            {
                return null;
            }

            ProfessionDepositBox rawMeatBox =
                ProfDepotService.FindNearest(
                    owner.transform.position,
                    "Raw Meat"
                );

            if (rawMeatBox == null)
            {
                return null;
            }

            CookRecipe selectedRecipe = null;

            foreach (CookRecipe recipe in Recipes)
            {
                // Empty MarkerItem means this recipe requires no trophy.
                if (!string.IsNullOrEmpty(recipe.MarkerItem))
                {
                    string trophySharedName =
                        GetSharedName(
                            recipe.MarkerItem
                        );

                    if (string.IsNullOrEmpty(trophySharedName))
                    {
                        continue;
                    }

                    if (
                        !inventory.HaveItem(
                            trophySharedName,
                            true
                        )
                    )
                    {
                        continue;
                    }
                }

                // Source chest must currently contain
                // every required ingredient.
                if (
                    !ProfDepotService.HasIngredients(
                        rawMeatBox,
                        recipe.Ingredients
                    )
                )
                {
                    continue;
                }

                if (
                    selectedRecipe == null ||
                    recipe.Priority >
                        selectedRecipe.Priority
                )
                {
                    selectedRecipe = recipe;
                }
            }

            return selectedRecipe;
        }

        private static string GetSharedName(
            string prefabName)
        {
            GameObject prefab =
                ObjectDB.instance.GetItemPrefab(
                    prefabName
                );

            if (prefab == null)
            {
                return "";
            }

            ItemDrop itemDrop =
                prefab.GetComponent<ItemDrop>();

            if (itemDrop == null)
            {
                return "";
            }

            return itemDrop
                .m_itemData
                .m_shared
                .m_name;
        }

        public CookingStation FindNearestCookingStation(
            float searchRadius = 8f)
        {
            if (!owner.HasWorkPosition)
            {
                return null;
            }

            CookingStation nearest = null;
            float nearestDistance = float.MaxValue;

            cookingStationCacheTimer -= Time.deltaTime;

            if (
                cachedCookingStations == null ||
                cookingStationCacheTimer <= 0f
            )
            {
                cachedCookingStations =
                    Object.FindObjectsByType<CookingStation>(
                        FindObjectsSortMode.None
                    );

                cookingStationCacheTimer =
                    CookingStationCacheInterval;
            }

            foreach (
                CookingStation station
                in cachedCookingStations
            )
            {
                if (station == null)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        owner.WorkPosition,
                        station.transform.position
                    );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = station;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        public bool CanCheckIngredientBox()
        {
            return
                Time.time - lastFailedIngredientCheckTime >=
                IngredientCheckCooldown;
        }

        public void RecordFailedIngredientCheck()
        {
            lastFailedIngredientCheckTime = Time.time;
        }

        public void ClearIngredientCheckCooldown()
        {
            lastFailedIngredientCheckTime = -9999f;
        }

        public bool HasIngredientsInInventory(
            CookRecipe recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            Viking viking =
                owner.GetComponent<Viking>();

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

            foreach (CookIngredient ingredient in recipe.Ingredients)
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

                int available =
                    inventory.CountItems(
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

        public CookRecipe GetHighestPriorityHeldRecipe()
        {
            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return null;
            }

            Inventory inventory =
                viking.GetInventory();

            if (inventory == null)
            {
                return null;
            }

            CookRecipe selectedRecipe = null;

            foreach (CookRecipe recipe in Recipes)
            {
                // Empty MarkerItem means this recipe requires no trophy.
                if (!string.IsNullOrEmpty(recipe.MarkerItem))
                {
                    string trophySharedName =
                        GetSharedName(
                            recipe.MarkerItem
                        );

                    if (string.IsNullOrEmpty(trophySharedName))
                    {
                        continue;
                    }

                    if (
                        !inventory.HaveItem(
                            trophySharedName,
                            true
                        )
                    )
                    {
                        continue;
                    }
                }

                if (
                    selectedRecipe == null ||
                    recipe.Priority > selectedRecipe.Priority
                )
                {
                    selectedRecipe = recipe;
                }
            }

            return selectedRecipe;
        }

        public CookRecipe GetHighestPriorityActionableHeldRecipe()
        {
            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return null;
            }

            Inventory inventory =
                viking.GetInventory();

            if (inventory == null)
            {
                return null;
            }

            CookRecipe selectedRecipe = null;

            foreach (CookRecipe recipe in Recipes)
            {
                // Empty MarkerItem means this recipe requires no trophy.
                if (!string.IsNullOrEmpty(recipe.MarkerItem))
                {
                    string trophySharedName =
                        GetSharedName(
                            recipe.MarkerItem
                        );

                    if (string.IsNullOrEmpty(trophySharedName))
                    {
                        continue;
                    }

                    if (
                        !inventory.HaveItem(
                            trophySharedName,
                            true
                        )
                    )
                    {
                        continue;
                    }
                }

                // Cook must ALSO currently possess
                // the ingredients for this recipe.
                if (
                    !HasIngredientsInInventory(
                        recipe
                    )
                )
                {
                    continue;
                }

                if (
                    selectedRecipe == null ||
                    recipe.Priority >
                        selectedRecipe.Priority
                )
                {
                    selectedRecipe = recipe;
                }
            }

            return selectedRecipe;
        }

        public CookRecipe EvaluateWork()
        {
            CookRecipe desiredRecipe =
                GetHighestPriorityHeldRecipe();

            if (desiredRecipe == null)
            {
                return null;
            }

            // If the Cook already carries what the highest-priority
            // trophy requires, that recipe is ready for the next step.
            if (HasIngredientsInInventory(desiredRecipe))
            {
                ClearIngredientCheckCooldown();
                return desiredRecipe;
            }

            // Recently checked the Raw Meat box and failed.
            // Do not run the whole pantry waterfall again yet.
            if (!CanCheckIngredientBox())
            {
                return null;
            }

            // The Cook does not have the ingredients personally,
            // so now check the Raw Meat box using the full hierarchy.
            CookRecipe availableRecipe =
                GetHighestPriorityRecipe();

            if (availableRecipe == null)
            {
                RecordFailedIngredientCheck();
                return null;
            }

            return availableRecipe;
        }

        public bool HasFreeCookingSlot(
            CookingStation station)
        {
            if (
                station == null ||
                GetFreeSlotMethod == null
            )
            {
                return false;
            }

            int freeSlot =
                (int)GetFreeSlotMethod.Invoke(
                    station,
                    null
                );

            return freeSlot >= 0;
        }

        public bool HasFinishedFood(
            CookingStation station)
        {
            if (
                station == null ||
                HaveDoneItemMethod == null
            )
            {
                return false;
            }

            return (bool)HaveDoneItemMethod.Invoke(
                station,
                null
            );
        }

        public bool TryCollectFinishedFood(CookingStation station)
        {
            if (
                station == null ||
                GetSlotMethod == null ||
                SetSlotMethod == null
            )
            {
                return false;
            }

            Viking viking =
                owner.GetComponent<Viking>();

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

            for (int i = 0; i < station.m_slots.Length; i++)
            {
                System.Type getSlotStatusType =
                    GetSlotMethod
                        .GetParameters()[3]
                        .ParameterType
                        .GetElementType();

                object statusValue =
                    System.Enum.ToObject(
                        getSlotStatusType,
                        0
                    );

                object[] args =
                {
                    i,
                    null,
                    0f,
                    statusValue,
                    false
                };

                GetSlotMethod.Invoke(
                    station,
                    args
                );

                string itemName =
                    args[1] as string;

                float cookedTime =
                    (float)args[2];

                int status =
                    System.Convert.ToInt32(
                        args[3]
                    );

                // Status 2 is the finished/cooked state.
                if (
                    string.IsNullOrEmpty(itemName) ||
                    (status != 1 && status != 2)
                )
                {
                    continue;
                }

                GameObject cookedPrefab =
                    ZNetScene.instance.GetPrefab(
                        itemName
                    );

                if (cookedPrefab == null)
                {
                    continue;
                }

                ItemDrop cookedDrop =
                    cookedPrefab.GetComponent<ItemDrop>();

                if (cookedDrop == null)
                {
                    continue;
                }

                if (
                    !inventory.CanAddItem(
                        cookedDrop.m_itemData,
                        1
                    )
                )
                {
                    return false;
                }

                inventory.AddItem(
                    itemName,
                    1,
                    1,
                    0,
                    0L,
                    "",
                    new Vector2i(-1, -1),
                    false
                );

                System.Type statusType =
                    SetSlotMethod
                        .GetParameters()[3]
                        .ParameterType;

                object notDoneStatus =
                    System.Enum.ToObject(
                        statusType,
                        0
                    );

                SetSlotMethod.Invoke(
                    station,
                    new object[]
                    {
                        i,
                        "",
                        0f,
                        notDoneStatus,
                        false
                    }
                );

                Debug.Log(
                    $"[Cook] {viking.GetName()} collected " +
                    $"{itemName} from " +
                    $"{Utils.GetPrefabName(station.gameObject)}."
                );

                return true;
            }

            return false;
        }

        public bool HasOccupiedCookingSlot(
            CookingStation station)
        {
            if (
                station == null ||
                GetSlotMethod == null
            )
            {
                return false;
            }

            for (int i = 0; i < station.m_slots.Length; i++)
            {
                System.Type getSlotStatusType =
                    GetSlotMethod
                        .GetParameters()[3]
                        .ParameterType
                        .GetElementType();

                object statusValue =
                    System.Enum.ToObject(
                        getSlotStatusType,
                        0
                    );

                object[] args =
                {
                    i,
                    null,
                    0f,
                    statusValue,
                    false
                };

                GetSlotMethod.Invoke(
                    station,
                    args
                );

                string itemName =
                    args[1] as string;

                if (!string.IsNullOrEmpty(itemName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
