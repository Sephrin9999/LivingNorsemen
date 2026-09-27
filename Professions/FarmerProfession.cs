using HarmonyLib;
using Norsemen;
using System.Collections.Generic;
using UnityEngine;

namespace LivingNorsemen
{
    public class FarmerProfession
    {
        private readonly LivingNorseman owner;
        private readonly VikingAI ai;

        // ------------------------------------------------------------
        // FARM STATE
        // ------------------------------------------------------------

        private Vector3 farmWanderTarget;
        private bool hasFarmWanderTarget = false;
        private bool farmReturnToWorkPoint = false;

        private bool isFarmKneeling = false;
        private float farmKneelTimer = 0f;
        private const float FarmKneelDuration = 2f;

        private float farmTargetTimer = 0f;
        private const float FarmTargetTimeout = 4f;

        private bool isFarmStandingUp = false;
        private float farmStandUpTimer = 0f;
        private const float FarmStandUpDuration = 0.75f;

        private float farmProductionTimer = 0f;
        private const float FarmProductionCooldown = 25f;

        // ------------------------------------------------------------
        // BASE AI ACCESS
        // ------------------------------------------------------------

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

        private static readonly AccessTools.FieldRef<BaseAI, List<Vector3>>
            aiPathRef =
                AccessTools.FieldRefAccess<BaseAI, List<Vector3>>("m_path");

        // ------------------------------------------------------------
        // CONSTRUCTOR
        // ------------------------------------------------------------

        public FarmerProfession(LivingNorseman owner)
        {
            this.owner = owner;
            ai = owner.GetComponent<VikingAI>();
        }

        // ------------------------------------------------------------
        // PROFESSION CHECKS
        // ------------------------------------------------------------

        public bool HasCultivator()
        {
            return GetCultivator() != null;
        }

        public bool IsActive()
        {
            Viking viking = owner.GetComponent<Viking>();

            if (viking == null)
            {
                return false;
            }

            return
                owner.CurrentCommand == VillagerCommand.None &&
                owner.HasWorkPosition &&
                !EnvMan.IsNight() &&
                GetCultivator() != null &&
                !viking.IsFollowing() &&
                !viking.IsHungry();
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------

        public void Update(float deltaTime)
        {
            if (!owner.HasWorkPosition || ai == null)
            {
                return;
            }

            farmProductionTimer += deltaTime;

            // --------------------------------------------------------
            // KNEELING
            // --------------------------------------------------------

            if (isFarmKneeling)
            {
                farmKneelTimer += deltaTime;

                ai.StopMoving();

                if (farmKneelTimer < FarmKneelDuration)
                {
                    return;
                }

                StopFarmKneel();

                isFarmKneeling = false;
                farmKneelTimer = 0f;

                isFarmStandingUp = true;
                farmStandUpTimer = 0f;

                return;
            }

            // --------------------------------------------------------
            // STANDING BACK UP
            // --------------------------------------------------------

            if (isFarmStandingUp)
            {
                farmStandUpTimer += deltaTime;

                ai.StopMoving();

                if (farmStandUpTimer < FarmStandUpDuration)
                {
                    return;
                }

                isFarmStandingUp = false;
                farmStandUpTimer = 0f;

                hasFarmWanderTarget = false;

                return;
            }

            // --------------------------------------------------------
            // FARM WALKING / CULTIVATOR
            // --------------------------------------------------------

            Viking viking = owner.GetComponent<Viking>();

            if (viking != null)
            {
                viking.SetWalk(true);

                ItemDrop.ItemData cultivator = GetCultivator();

                if (
                    cultivator != null &&
                    !cultivator.m_equipped
                )
                {
                    viking.EquipItem(cultivator, true);
                }
            }

            // --------------------------------------------------------
            // CHOOSE TARGET
            // --------------------------------------------------------

            if (!hasFarmWanderTarget)
            {
                ChooseNewFarmWanderTarget();
            }

            if (!hasFarmWanderTarget)
            {
                return;
            }

            farmTargetTimer += deltaTime;

            if (farmTargetTimer >= FarmTargetTimeout)
            {
                farmTargetTimer = 0f;
                hasFarmWanderTarget = false;

                return;
            }

            MoveToMethod.Invoke(
                ai,
                new object[]
                {
                    deltaTime,
                    farmWanderTarget,
                    0.5f,
                    false
                }
            );

            // Verify arrival ourselves.
            float distanceToFarmTarget = Vector2.Distance(
                new Vector2(
                    owner.transform.position.x,
                    owner.transform.position.z
                ),
                new Vector2(
                    farmWanderTarget.x,
                    farmWanderTarget.z
                )
            );

            bool reachedTarget =
                distanceToFarmTarget <= 0.75f;

            if (!reachedTarget)
            {
                return;
            }

            farmTargetTimer = 0f;

            Pickable nearbyPickable =
                FindNearbyPickable(1.5f);

            if (nearbyPickable != null)
            {
                ai.StopMoving();

                StartFarmKneel();

                isFarmKneeling = true;
                farmKneelTimer = 0f;

                // Visual harvesting can happen frequently.
                // Actual production is rate-limited.
                if (
                    farmProductionTimer >=
                    FarmProductionCooldown
                )
                {
                    farmProductionTimer = 0f;

                    TrySimulateFarmHarvest();
                    TrySimulateHoneyHarvest();
                }

                return;
            }

            // Nothing at this location.
            hasFarmWanderTarget = false;
        }

        // ------------------------------------------------------------
        // CANCEL FARMING
        // ------------------------------------------------------------

        public void Cancel()
        {
            Viking viking = owner.GetComponent<Viking>();

            if (viking != null)
            {
                viking.SetWalk(false);
            }

            StopFarmKneel();

            isFarmKneeling = false;
            farmKneelTimer = 0f;

            isFarmStandingUp = false;
            farmStandUpTimer = 0f;

            hasFarmWanderTarget = false;
            farmReturnToWorkPoint = false;

            farmTargetTimer = 0f;

            if (ai != null)
            {
                List<Vector3> currentPath =
                    aiPathRef(ai);

                if (currentPath != null)
                {
                    currentPath.Clear();
                }
            }
        }

        // ------------------------------------------------------------
        // CULTIVATOR
        // ------------------------------------------------------------

        private ItemDrop.ItemData GetCultivator()
        {
            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return null;
            }

            foreach (
                ItemDrop.ItemData item
                in viking.GetInventory().GetAllItems()
            )
            {
                if (
                    item.m_dropPrefab != null &&
                    item.m_dropPrefab.name == "Cultivator"
                )
                {
                    return item;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        // FARM TARGET
        // ------------------------------------------------------------

        private void ChooseNewFarmWanderTarget()
        {
            if (!owner.HasWorkPosition)
            {
                return;
            }

            // Every second destination returns to WorkPosition.
            if (farmReturnToWorkPoint)
            {
                farmWanderTarget = new Vector3(
                    owner.WorkPosition.x,
                    owner.transform.position.y,
                    owner.WorkPosition.z
                );

                hasFarmWanderTarget = true;

                if (ai != null)
                {
                    List<Vector3> returnPath =
                        aiPathRef(ai);

                    if (returnPath != null)
                    {
                        returnPath.Clear();
                    }
                }

                farmReturnToWorkPoint = false;
                return;
            }

            Vector2 direction =
                UnityEngine.Random.insideUnitCircle;

            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();

            // Current established Farmer radius.
            float distance = 4f;

            farmWanderTarget = new Vector3(
                owner.WorkPosition.x +
                    direction.x * distance,

                owner.transform.position.y,

                owner.WorkPosition.z +
                    direction.y * distance
            );

            hasFarmWanderTarget = true;

            if (ai != null)
            {
                List<Vector3> currentPath =
                    aiPathRef(ai);

                if (currentPath != null)
                {
                    currentPath.Clear();
                }
            }

            farmReturnToWorkPoint = true;

            BepInEx.Logging.Logger
                .CreateLogSource("LivingNorseman")
                .LogInfo(
                    $"[Farm Target] {owner.name} -> " +
                    $"({farmWanderTarget.x:F1}, " +
                    $"{farmWanderTarget.z:F1}) " +
                    $"ReturnToWork=" +
                    $"{farmReturnToWorkPoint}"
                );
        }

        // ------------------------------------------------------------
        // FIND CROPS
        // ------------------------------------------------------------

        private Pickable FindNearbyPickable(
            float searchRadius = 1.25f)
        {
            Pickable nearest = null;
            float nearestDistance = float.MaxValue;

            Pickable[] pickables =
                Object.FindObjectsByType<Pickable>(
                    FindObjectsSortMode.None
                );

            foreach (Pickable pickable in pickables)
            {
                if (
                    pickable == null ||
                    pickable.m_itemPrefab == null
                )
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    owner.transform.position,
                    pickable.transform.position
                );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = pickable;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        // ------------------------------------------------------------
        // FIND BEEHIVE
        // ------------------------------------------------------------

        private Beehive FindNearbyBeehive(
            float searchRadius = 6f)
        {
            Beehive nearest = null;
            float nearestDistance = float.MaxValue;

            Beehive[] hives =
                Object.FindObjectsByType<Beehive>(
                    FindObjectsSortMode.None
                );

            foreach (Beehive hive in hives)
            {
                if (
                    hive == null ||
                    hive.m_honeyItem == null
                )
                {
                    continue;
                }

                float distance = Vector3.Distance(
                    owner.transform.position,
                    hive.transform.position
                );

                if (
                    distance <= searchRadius &&
                    distance < nearestDistance
                )
                {
                    nearest = hive;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        // ------------------------------------------------------------
        // PRODUCE HARVEST
        // ------------------------------------------------------------

        private void TrySimulateFarmHarvest()
        {
            ItemDrop.ItemData cultivator =
                GetCultivator();

            if (cultivator == null)
            {
                return;
            }

            Pickable nearbyPickable =
                FindNearbyPickable();

            if (
                nearbyPickable == null ||
                nearbyPickable.m_itemPrefab == null
            )
            {
                return;
            }

            string cropName =
                nearbyPickable.m_itemPrefab.name;

            GameObject harvestPrefab =
                nearbyPickable.m_itemPrefab;

            if (cropName == "CarrotSeeds")
            {
                harvestPrefab =
                    ZNetScene.instance.GetPrefab("Carrot");
            }
            else if (cropName == "TurnipSeeds")
            {
                harvestPrefab =
                    ZNetScene.instance.GetPrefab("Turnip");
            }
            else if (cropName == "OnionSeeds")
            {
                harvestPrefab =
                    ZNetScene.instance.GetPrefab("Onion");
            }

            if (harvestPrefab == null)
            {
                return;
            }

            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            if (
                !viking.GetInventory()
                    .CanAddItem(harvestPrefab, 1)
            )
            {
                return;
            }

            viking.GetInventory().AddItem(
                harvestPrefab.name,
                1,
                1,
                0,
                0L,
                "",
                false
            );

            BepInEx.Logging.Logger
                .CreateLogSource("LivingNorseman")
                .LogInfo(
                    $"{owner.name} simulated harvest: " +
                    $"{harvestPrefab.name}"
                );
        }

        // ------------------------------------------------------------
        // HONEY HARVEST
        // ------------------------------------------------------------

        private void TrySimulateHoneyHarvest()
        {
            Beehive nearbyHive =
                FindNearbyBeehive(6f);

            if (
                nearbyHive == null ||
                nearbyHive.m_honeyItem == null
            )
            {
                return;
            }

            // Current Honey chance: 5%.
            if (UnityEngine.Random.value > 0.05f)
            {
                return;
            }

            Viking viking =
                owner.GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            GameObject honeyPrefab =
                nearbyHive.m_honeyItem.gameObject;

            if (honeyPrefab == null)
            {
                return;
            }

            if (
                !viking.GetInventory()
                    .CanAddItem(honeyPrefab, 1)
            )
            {
                return;
            }

            viking.GetInventory().AddItem(
                honeyPrefab.name,
                1,
                1,
                0,
                0L,
                "",
                false
            );

            BepInEx.Logging.Logger
                .CreateLogSource("LivingNorseman")
                .LogInfo(
                    $"{owner.name} simulated " +
                    $"beekeeping harvest: " +
                    $"{honeyPrefab.name}"
                );
        }

        // ------------------------------------------------------------
        // KNEEL
        // ------------------------------------------------------------

        private void StartFarmKneel()
        {
            ZSyncAnimation zanim =
                owner.GetComponent<ZSyncAnimation>();

            if (zanim == null)
            {
                return;
            }

            ai?.StopMoving();

            zanim.SetBool(
                "emote_kneel",
                true
            );
        }

        private void StopFarmKneel()
        {
            ZSyncAnimation zanim =
                owner.GetComponent<ZSyncAnimation>();

            if (zanim == null)
            {
                return;
            }

            zanim.SetBool(
                "emote_kneel",
                false
            );

            zanim.SetTrigger(
                "emote_stop"
            );
        }
    }
}