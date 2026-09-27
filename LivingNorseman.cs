using HarmonyLib;
using Norsemen;
using System;
using System.Collections.Generic;
using UnityEngine;


namespace LivingNorsemen
{
    public class LivingNorseman : MonoBehaviour
    {
        private VikingAI ai;

        private ZNetView nview;

        private const string VillagerIdKey = "LivingNorsemen_VillagerID";
        private const string HasHomeKey = "LivingNorsemen_HasHome";
        private const string HomeXKey = "LivingNorsemen_HomeX";
        private const string HomeYKey = "LivingNorsemen_HomeY";
        private const string HomeZKey = "LivingNorsemen_HomeZ";

        private const string HasWorkKey = "LivingNorsemen_HasWork";
        private const string WorkXKey = "LivingNorsemen_WorkX";
        private const string WorkYKey = "LivingNorsemen_WorkY";
        private const string WorkZKey = "LivingNorsemen_WorkZ";

        private const string HasPatrolPointBKey =
            "LivingNorsemen_HasPatrolPointB";
        private const string PatrolPointBXKey =
            "LivingNorsemen_PatrolPointBX";
        private const string PatrolPointBYKey =
            "LivingNorsemen_PatrolPointBY";
        private const string PatrolPointBZKey =
            "LivingNorsemen_PatrolPointBZ";

        public long VillagerId { get; private set; }

        public Vector3 HomePosition { get; private set; }

        public bool HasHome { get; private set; }

        public Bed AssignedBed { get; private set; }

        private static readonly System.Reflection.MethodInfo GetBedOwnerMethod =
            AccessTools.Method(typeof(Bed), "GetOwner");

        private static readonly System.Reflection.MethodInfo SetBedOwnerMethod =
            AccessTools.Method(typeof(Bed), "SetOwner");

        private static readonly System.Reflection.MethodInfo MoveToMethod =
            AccessTools.Method(
                typeof(BaseAI),
                "MoveTo",
                new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }
            );

        private readonly AccessTools.FieldRef<VikingAI, bool> hasWorkTargetRef =
            AccessTools.FieldRefAccess<VikingAI, bool>("hasWorkTarget");

        private readonly AccessTools.FieldRef<VikingAI, MineRock> mineRockRef =
            AccessTools.FieldRefAccess<VikingAI, MineRock>("m_mineRock");

        private readonly AccessTools.FieldRef<VikingAI, MineRock5> mineRock5Ref =
            AccessTools.FieldRefAccess<VikingAI, MineRock5>("m_mineRock5");

        private readonly AccessTools.FieldRef<VikingAI, TreeBase> treeRef =
            AccessTools.FieldRefAccess<VikingAI, TreeBase>("m_tree");

        private readonly AccessTools.FieldRef<VikingAI, Destructible> destructibleRef =
            AccessTools.FieldRefAccess<VikingAI, Destructible>("m_destructible");

        private readonly AccessTools.FieldRef<VikingAI, Fish> fishRef =
            AccessTools.FieldRefAccess<VikingAI, Fish>("m_fish");

        public VillagerCommand CurrentCommand { get; private set; }
        public VillagerState CurrentState { get; private set; }

        private float doorInteractionCooldown;
        private float patrolUtilityScanTimer = 0f;
        private Door lastOpenedDoor;

        private float timer = 0f;

        private FarmerProfession farmerProfession;
        private CookProfession cookProfession;

        private bool lastHasWorkTarget = false;
        public bool WorkDispatchedToday { get; private set; }

        public Vector3 WorkPosition { get; private set; }
        public bool HasWorkPosition { get; private set; }

        public Vector3 PatrolPointB { get; private set; }
        public bool HasPatrolPointB { get; private set; }
        private bool patrolHeadingToB = true;

        //------------------------------------------------------------------------
        private void Awake()
        {
            ai = GetComponent<VikingAI>();

            nview = GetComponent<ZNetView>();

            if (nview != null && nview.GetZDO() != null)
            {
                HasHome = nview.GetZDO().GetBool(HasHomeKey, false);

                if (HasHome)
                {
                    HomePosition = new Vector3(
                        nview.GetZDO().GetFloat(HomeXKey, 0f),
                        nview.GetZDO().GetFloat(HomeYKey, 0f),
                        nview.GetZDO().GetFloat(HomeZKey, 0f)
                    );
                }

                HasWorkPosition = nview.GetZDO().GetBool(HasWorkKey, false);

                if (HasWorkPosition)
                {
                    WorkPosition = new Vector3(
                        nview.GetZDO().GetFloat(WorkXKey, 0f),
                        nview.GetZDO().GetFloat(WorkYKey, 0f),
                        nview.GetZDO().GetFloat(WorkZKey, 0f)
                    );
                }

                HasPatrolPointB =
                    nview.GetZDO().GetBool(
                        HasPatrolPointBKey,
                        false
                    );

                if (HasPatrolPointB)
                {
                    PatrolPointB = new Vector3(
                        nview.GetZDO().GetFloat(
                            PatrolPointBXKey,
                            0f
                        ),
                        nview.GetZDO().GetFloat(
                            PatrolPointBYKey,
                            0f
                        ),
                        nview.GetZDO().GetFloat(
                            PatrolPointBZKey,
                            0f
                        )
                    );
                }

            }

            if (nview != null && nview.GetZDO() != null)
            {
                nview.GetZDO().Set(ZDOVars.s_inBed, false);
            }

            if (nview != null && nview.GetZDO() != null)
            {
                VillagerId = nview.GetZDO().GetLong(VillagerIdKey, 0L);

                if (VillagerId == 0L && nview.IsOwner())
                {
                    VillagerId = DateTime.UtcNow.Ticks;
                    nview.GetZDO().Set(VillagerIdKey, VillagerId);

                    BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                        .LogInfo($"{name} created VillagerId: {VillagerId}");
                }
                else
                {
                    BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                        .LogInfo($"{name} loaded VillagerId: {VillagerId}");
                }
            }

            farmerProfession = new FarmerProfession(this);
            cookProfession = new CookProfession(this);

            BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                .LogInfo($"LivingNorseman Awake on {name}  AI Found: {ai != null}");
        }
        //------------------------------------------------------------------------
        public void AssignHome(Vector3 position)
        {
            HomePosition = position;
            HasHome = true;
            if (nview != null && nview.GetZDO() != null)
            {
                nview.GetZDO().Set(HasHomeKey, true);
                nview.GetZDO().Set(HomeXKey, position.x);
                nview.GetZDO().Set(HomeYKey, position.y);
                nview.GetZDO().Set(HomeZKey, position.z);
            }

            AssignedBed = FindNearestBed();
            if (AssignedBed != null)
            {
                long villagerId = GetInstanceID();
                string villagerName = GetComponent<Viking>().GetName();

                SetBedOwnerMethod.Invoke(
                    AssignedBed,
                    new object[] { villagerId, villagerName }
                );
            }
            if (AssignedBed != null)
            {
                BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                    .LogInfo(
                        $"{name} assigned bed: {AssignedBed.name} at {AssignedBed.transform.position}"
                    );
            }

            BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                .LogInfo($"{name} home assigned at {HomePosition}");
        }

        public void AssignWork(Vector3 position)
        {
            WorkPosition = position;
            HasWorkPosition = true;
            if (nview != null && nview.GetZDO() != null)
            {
                nview.GetZDO().Set(HasWorkKey, true);
                nview.GetZDO().Set(WorkXKey, position.x);
                nview.GetZDO().Set(WorkYKey, position.y);
                nview.GetZDO().Set(WorkZKey, position.z);
            }

            BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                .LogInfo($"{name} work position assigned at {WorkPosition}");
        }
        //------------------------------------------------------------------------
        public void AssignPatrolPointB(Vector3 position)
        {
            PatrolPointB = position;
            HasPatrolPointB = true;

            if (
                nview != null &&
                nview.IsValid() &&
                nview.IsOwner()
            )
            {
                ZDO zdo =
                    nview.GetZDO();

                zdo.Set(
                    PatrolPointBXKey,
                    PatrolPointB.x
                );

                zdo.Set(
                    PatrolPointBYKey,
                    PatrolPointB.y
                );

                zdo.Set(
                    PatrolPointBZKey,
                    PatrolPointB.z
                );

                zdo.Set(
                    HasPatrolPointBKey,
                    true
                );
            }

            Debug.Log(
                $"[LivingNorsemen] {name} Patrol Point B assigned: " +
                $"{PatrolPointB}"
            );
        }
        //------------------------------------------------------------------------
        public bool MoveTowardHome(float deltaTime)
        {
            if (!HasHome || ai == null || MoveToMethod == null)
            {
                return false;
            }

            MoveToMethod.Invoke(
                ai,
                new object[]
                {
            deltaTime,
            HomePosition,
            1f,
            false
                }
            );

            float distanceToHome = Vector3.Distance(
                transform.position,
                HomePosition
            );

            return distanceToHome <= 1f;
        }
        //------------------------------------------------------------------------
        public bool MoveTowardWork(float deltaTime)
        {
            if (!HasWorkPosition || ai == null || MoveToMethod == null)
            {
                return false;
            }

            MoveToMethod.Invoke(
                ai,
                new object[]
                {
            deltaTime,
            WorkPosition,
            0.5f,
            false
                }
            );

            float distanceToWork = Vector3.Distance(
                transform.position,
                WorkPosition
            );

            return distanceToWork <= 1.5f;
        }
        //------------------------------------------------------------------------
        private void MovePatrol(float deltaTime)
        {
            if (
                ai == null ||
                MoveToMethod == null ||
                !HasWorkPosition ||
                !HasPatrolPointB
            )
            {
                return;
            }

            Vector3 patrolTarget =
                patrolHeadingToB
                    ? PatrolPointB
                    : WorkPosition;

            MoveToMethod.Invoke(
                ai,
                new object[]
                {
            deltaTime,
            patrolTarget,
            0.5f,
            false
                }
            );

            float distanceToTarget =
                Vector3.Distance(
                    transform.position,
                    patrolTarget
                );

            if (distanceToTarget <= 1.5f)
            {
                patrolHeadingToB =
                    !patrolHeadingToB;
            }
        }
        //------------------------------------------------------------------------
        private void DepositFarmerGoods()
        {
            Viking viking = GetComponent<Viking>();

            ProfessionDepositBox depositBox =
                ProfDepotService.FindNearest(
                    transform.position,
                    "Farmer"
                );

            ProfDepotService.DepositGoods(
                viking,
                depositBox,
                "Farmer"
            );
        }

        //------------------------------------------------------------------------
        private void DepositLumberjackGoods()
        {
            Viking viking = GetComponent<Viking>();

            ProfessionDepositBox depositBox =
                ProfDepotService.FindNearest(
                    transform.position,
                    "Lumberjack"
                );

            ProfDepotService.DepositGoods(
                viking,
                depositBox,
                "Lumberjack"
            );
        }
        //------------------------------------------------------------------------

        private void DepositCookedFoods()
        {
            Viking viking =
                GetComponent<Viking>();

            ProfessionDepositBox depositBox =
                ProfDepotService.FindNearest(
                    transform.position,
                    "Cooked Foods"
                );

            ProfDepotService.DepositGoods(
                viking,
                depositBox,
                "Cooked Foods"
            );
        }
        //------------------------------------------------------------------------

        private void TryOpenNearbyDoor(float searchRadius = 4f)
        {
            doorInteractionCooldown -= Time.deltaTime;

            if (doorInteractionCooldown > 0f)
            {
                return;
            }

            doorInteractionCooldown = 0.1f;

            Viking viking = GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            Door[] doors = FindObjectsByType<Door>(
                FindObjectsSortMode.None
            );

            foreach (Door door in doors)
            {
                if (Vector3.Distance(transform.position, door.transform.position) > searchRadius)
                {
                    continue;
                }

                ZNetView doorView = door.GetComponent<ZNetView>();

                if (doorView == null || doorView.GetZDO() == null)
                {
                    continue;
                }

                int doorState = doorView.GetZDO().GetInt(ZDOVars.s_state, 0);

                // State 0 means closed. States 1 and -1 are open directions.
                if (doorState == 0)
                {
                    door.Interact(viking, false, false);
                    lastOpenedDoor = door;
                    doorInteractionCooldown = 1f;
                }

                return;
            }
        }
        //------------------------------------------------------------------------
        private void TryCloseLastDoor()
        {
            if (lastOpenedDoor == null)
            {
                return;
            }

            Viking viking = GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            ZNetView doorView = lastOpenedDoor.GetComponent<ZNetView>();

            if (doorView == null || doorView.GetZDO() == null)
            {
                lastOpenedDoor = null;
                return;
            }

            int doorState = doorView.GetZDO().GetInt(ZDOVars.s_state, 0);

            if (doorState != 0)
            {
                lastOpenedDoor.Interact(viking, false, false);
            }

            lastOpenedDoor = null;
        }
        //------------------------------------------------------------------------

        private void TryCloseNearbyOpenDoor(
            float searchRadius = 5f)
        {
            doorInteractionCooldown -= Time.deltaTime;

            if (doorInteractionCooldown > 0f)
            {
                return;
            }

            Viking viking =
                GetComponent<Viking>();

            if (viking == null)
            {
                return;
            }

            Door[] doors =
                FindObjectsByType<Door>(
                    FindObjectsSortMode.None
                );

            Door nearestOpenDoor = null;
            float nearestDistance = float.MaxValue;

            foreach (Door door in doors)
            {
                if (door == null)
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        transform.position,
                        door.transform.position
                    );

                if (
                    distance > searchRadius ||
                    distance >= nearestDistance
                )
                {
                    continue;
                }

                ZNetView doorView =
                    door.GetComponent<ZNetView>();

                if (
                    doorView == null ||
                    doorView.GetZDO() == null
                )
                {
                    continue;
                }

                int doorState =
                    doorView.GetZDO().GetInt(
                        ZDOVars.s_state,
                        0
                    );

                // 0 = closed.
                // 1 / -1 = open in either direction.
                if (doorState == 0)
                {
                    continue;
                }

                nearestOpenDoor = door;
                nearestDistance = distance;
            }

            if (nearestOpenDoor == null)
            {
                return;
            }

            nearestOpenDoor.Interact(
                viking,
                false,
                false
            );

            doorInteractionCooldown = 1f;

            Debug.Log(
                $"[Patrol] {name} closed nearby door " +
                $"{nearestOpenDoor.name}."
            );
        }
        //------------------------------------------------------------------------

        private void TryRelightNearbyFireplace(
            float searchRadius = 5f)
        {
            Fireplace[] fireplaces =
                FindObjectsByType<Fireplace>(
                    FindObjectsSortMode.None
                );

            Fireplace nearestFireplace = null;
            float nearestDistance = float.MaxValue;

            foreach (Fireplace fireplace in fireplaces)
            {
                if (
                    fireplace == null ||
                    fireplace.m_infiniteFuel
                )
                {
                    continue;
                }

                float distance =
                    Vector3.Distance(
                        transform.position,
                        fireplace.transform.position
                    );

                if (
                    distance > searchRadius ||
                    distance >= nearestDistance
                )
                {
                    continue;
                }

                ZNetView fireplaceView =
                    fireplace.GetComponent<ZNetView>();

                if (
                    fireplaceView == null ||
                    !fireplaceView.IsValid() ||
                    fireplaceView.GetZDO() == null
                )
                {
                    continue;
                }

                float fuel =
                    fireplaceView
                        .GetZDO()
                        .GetFloat(
                            ZDOVars.s_fuel,
                            0f
                        );

                if (fuel > 0f)
                {
                    continue;
                }

                nearestFireplace = fireplace;
                nearestDistance = distance;
            }

            if (nearestFireplace == null)
            {
                return;
            }

            nearestFireplace.SetFuel(
                1f
            );

            Debug.Log(
                $"[Patrol] {name} relit " +
                $"{nearestFireplace.name}."
            );
        }
        //------------------------------------------------------------------------
        public bool HasBedNearHome(float searchRadius = 8f)
        {
            if (!HasHome)
            {
                return false;
            }

            Bed[] beds = FindObjectsByType<Bed>(
                FindObjectsSortMode.None
            );

            foreach (Bed bed in beds)
            {
                float distance = Vector3.Distance(HomePosition, bed.transform.position);

                if (distance <= searchRadius)
                {
                    return true;
                }
            }

            return false;
        }
        //------------------------------------------------------------------------
        public Bed FindNearestBed(float searchRadius = 8f)
        {
            if (!HasHome)
            {
                return null;
            }

            Bed nearestBed = null;
            float nearestDistance = float.MaxValue;

            Bed[] beds = FindObjectsByType<Bed>(
                FindObjectsSortMode.None
            );

            foreach (Bed bed in beds)
            {
                long ownerId = (long)GetBedOwnerMethod.Invoke(bed, null);

                if (ownerId != 0L)
                {
                    continue;
                }

                float distance = Vector3.Distance(HomePosition, bed.transform.position);

                if (distance <= searchRadius && distance < nearestDistance)
                {
                    nearestBed = bed;
                    nearestDistance = distance;
                }
            }

            return nearestBed;
        }

        public bool IsAtHome { get; private set; }
        //------------------------------------------------------------------------
        public void IssueCommand(VillagerCommand command)
        {
            if (CurrentCommand == command)
            {
                return;
            }

            // Leaving patrol restores normal running movement.
            if (
                CurrentCommand == VillagerCommand.PatrolRoute &&
                command != VillagerCommand.PatrolRoute
            )
            {
                Viking patrolViking =
                    GetComponent<Viking>();

                if (patrolViking != null)
                {
                    patrolViking.SetWalk(false);
                }
            }

            switch (command)
            {
                case VillagerCommand.None:
                    // Set None BEFORE StopMoving so our Harmony
                    // travel override does not block the stop.
                    CurrentCommand = VillagerCommand.None;

                    ai?.StopMoving();

                    BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                        .LogInfo($"{name} issued command: {command}");

                    return;

                case VillagerCommand.GoHome:
                    ai?.ResetWorkTargets();
                    break;

                case VillagerCommand.DepositFarmerGoods:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.DepositLumberjackGoods:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.DepositMinerGoods:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.DepositFishermanGoods:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.GetCookIngredients:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.GoToCookingStation:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.DepositCookedFoods:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.GetBreakfast:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;

                case VillagerCommand.PatrolRoute:
                    ai?.ResetWorkTargets();
                    ai?.StopMoving();
                    break;
            }

            CurrentCommand = command;

            BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                .LogInfo($"{name} issued command: {command}");
        }
        //------------------------------------------------------------------------
        public void StartGoingHome()
        {
            if (!HasHome || ai == null)
            {
                return;
            }

            if (IsAtHome || CurrentCommand == VillagerCommand.GoHome)
            {
                return;
            }

            IssueCommand(VillagerCommand.GoHome);
        }
        //------------------------------------------------------------------------
        public void StartGoingToWork()
        {
            if (!HasWorkPosition || ai == null)
            {
                return;
            }

            CancelFarmingState();

            IssueCommand(VillagerCommand.GoToWork);
        }

        //------------------------------------------------------------------------
        
        //------------------------------------------------------------------------
        
        //------------------------------------------------------------------------

        //------------------------------------------------------------------------

        //------------------------------------------------------------------------
        [HarmonyPatch(typeof(BaseAI), "IdleMovement")]
        private static class BaseAI_IdleMovement_FarmingPatch
        {
            private static bool Prefix(BaseAI __instance)
            {
                // IdleMovement belongs to BaseAI and is used by many creatures.
                // Only interfere with Norsemen VikingAI.
                VikingAI vikingAI = __instance as VikingAI;

                if (vikingAI == null)
                {
                    return true;
                }

                LivingNorseman livingNorseman =
                    vikingAI.GetComponent<LivingNorseman>();

                if (livingNorseman == null)
                {
                    return true;
                }

                if (
                    livingNorseman.IsActivelyFarming() ||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.DepositFarmerGoods ||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.DepositLumberjackGoods||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.DepositMinerGoods ||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.DepositFishermanGoods ||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.GetCookIngredients ||
                    livingNorseman.CurrentCommand ==
                        VillagerCommand.GetBreakfast
                )
                {
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(BaseAI), "StopMoving")]
        private static class BaseAI_StopMoving_LivingCommandPatch
        {
            private static bool Prefix(BaseAI __instance)
            {
                // StopMoving belongs to BaseAI and is used by many creatures.
                // Only interfere with Norsemen VikingAI.
                VikingAI vikingAI =
                    __instance as VikingAI;

                if (vikingAI == null)
                {
                    return true;
                }

                LivingNorseman livingNorseman =
                    vikingAI.GetComponent<LivingNorseman>();

                if (livingNorseman == null)
                {
                    return true;
                }

                switch (livingNorseman.CurrentCommand)
                {
                    case VillagerCommand.GoHome:
                    case VillagerCommand.GoToWork:
                    case VillagerCommand.DepositFarmerGoods:
                    case VillagerCommand.DepositLumberjackGoods:
                    case VillagerCommand.DepositMinerGoods:
                    case VillagerCommand.DepositFishermanGoods:
                    case VillagerCommand.GetCookIngredients:
                    case VillagerCommand.GoToCookingStation:
                    case VillagerCommand.DepositCookedFoods:
                    case VillagerCommand.GetBreakfast:
                    case VillagerCommand.PatrolRoute:

                        // LivingNorsemen is controlling movement.
                        // Do not let VikingNPC Guard mode cancel it.
                        return false;
                }

                return true;
            }
        }

        //------------------------------------------------------------------------
        public void CancelFarmingState()
        {
            farmerProfession?.Cancel();
        }
        //------------------------------------------------------------------------

        public bool IsActivelyFarming()
        {
            return
                farmerProfession != null &&
                farmerProfession.IsActive();
        }
        //------------------------------------------------------------------------

        public bool HasNightShiftTorch()
        {
            Viking viking =
                GetComponent<Viking>();

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

            GameObject torchPrefab =
                ObjectDB.instance.GetItemPrefab(
                    "Torch"
                );

            if (torchPrefab == null)
            {
                return false;
            }

            ItemDrop torchDrop =
                torchPrefab.GetComponent<ItemDrop>();

            if (torchDrop == null)
            {
                return false;
            }

            string torchSharedName =
                torchDrop.m_itemData.m_shared.m_name;

            return inventory.HaveItem(
                torchSharedName,
                true
            );
        }

        public bool IsWorkShift()
        {
            bool isNight =
                EnvMan.IsNight();

            // Torch carriers work nights.
            if (HasNightShiftTorch())
            {
                return isNight;
            }

            // Everyone else works days.
            return !isNight;
        }

        public bool IsOffShift()
        {
            return !IsWorkShift();
        }

        private bool IsCook()
        {
            if (
                cookProfession == null ||
                !HasWorkPosition
            )
            {
                return false;
            }

            CookingStation cookingStation =
                cookProfession.FindNearestCookingStation();

            if (cookingStation == null)
            {
                return false;
            }

            float distanceFromWork =
                Vector3.Distance(
                    WorkPosition,
                    cookingStation.transform.position
                );

            if (distanceFromWork > 8f)
            {
                return false;
            }

            Viking viking =
                GetComponent<Viking>();

            if (viking == null)
            {
                return false;
            }

            // Other established professions are not Cooks.
            if (
                farmerProfession != null &&
                farmerProfession.HasCultivator()
            )
            {
                return false;
            }

            if (viking.m_axe != null)
            {
                return false;
            }

            if (viking.m_pickaxe != null)
            {
                return false;
            }

            if (
                viking.m_vikingAI != null &&
                viking.m_vikingAI.m_moveType == Movement.Guard &&
                viking.m_vikingAI.m_behaviour == Emotion.Aggressive
            )
            {
                return false;
            }

            return true;
        }

       

        //------------------------------------------------------------------------
        private void Update()
        {


            //------------------------------------------------------------------------
            if (
                HasHome &&
                Player.m_localPlayer != null &&
                UnityEngine.Input.GetKeyDown(KeyCode.H)
            )
            {
                float distanceToPlayer = Vector3.Distance(
                    transform.position,
                    Player.m_localPlayer.transform.position
                );

                if (distanceToPlayer <= 5f)
                {
                    float distanceToHome = Vector3.Distance(
                        transform.position,
                        HomePosition
                    );

                    if (distanceToHome > 4f)
                    {
                        IsAtHome = false;
                    }

                    StartGoingHome();

                    Player.m_localPlayer.Message(
                        MessageHud.MessageType.Center,
                        $"{GetComponent<Viking>().GetName()} is going home."
                    );
                }
            }
            //----------------------------------------------------------------------
            if (
                HasWorkPosition &&
                Player.m_localPlayer != null &&
                UnityEngine.Input.GetKeyDown(KeyCode.K)
            )
            {
                float distanceToPlayer = Vector3.Distance(
                    transform.position,
                    Player.m_localPlayer.transform.position
                );

                if (distanceToPlayer <= 5f)
                {
                    StartGoingToWork();

                    Player.m_localPlayer.Message(
                        MessageHud.MessageType.Center,
                        $"{GetComponent<Viking>().GetName()} is going to work."
                    );
                }
            }
            //------------------------------------------------------------------------

            switch (CurrentCommand)
            {
                case VillagerCommand.GoHome:

                    TryOpenNearbyDoor();

                    if (MoveTowardHome(Time.deltaTime))
                    {
                        IssueCommand(VillagerCommand.None);
                        CurrentState = VillagerState.Idle;
                        IsAtHome = true;
                        WorkDispatchedToday = false;
                        TryCloseLastDoor();

                        BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                            .LogInfo($"{name} arrived home.");
                    }

                    return;

                case VillagerCommand.GoToWork:

                    TryOpenNearbyDoor(6f);

                    if (MoveTowardWork(Time.deltaTime))
                    {
                        WorkDispatchedToday = true;

                        IssueCommand(VillagerCommand.None);
                        CurrentState = VillagerState.Idle;

                        TryCloseLastDoor();

                        BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                            .LogInfo($"{name} arrived at work.");
                        
                    }

                    return;

                case VillagerCommand.DepositFarmerGoods:
                    {
                        bool reachedDeposit =
                             ProfDepotService.MoveToward(
                                 this,
                                 ai,
                                 "Farmer",
                                 Time.deltaTime
                             );

                        if (reachedDeposit)
                        {
                            Debug.Log(
                                $"[LivingNorsemen] {name} reached Farmer deposit box."
                            );

                            DepositFarmerGoods();

                            IssueCommand(VillagerCommand.GoHome);
                        }

                        break;
                    }

                case VillagerCommand.DepositLumberjackGoods:
                    {
                        bool reachedDeposit =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Lumberjack",
                                Time.deltaTime
                            );

                        if (reachedDeposit)
                        {
                            Debug.Log(
                                $"[LivingNorsemen] {name} reached Lumberjack deposit box."
                            );

                            DepositLumberjackGoods();

                            IssueCommand(VillagerCommand.GoHome);
                        }

                        break;
                    }

                case VillagerCommand.DepositMinerGoods:
                    {
                        bool reachedDeposit =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Miner",
                                Time.deltaTime
                            );

                        if (reachedDeposit)
                        {
                            Debug.Log(
                                $"[LivingNorsemen] {name} reached Miner deposit box."
                            );

                            Viking minerViking =
                                GetComponent<Viking>();

                            ProfessionDepositBox minerDeposit =
                                ProfDepotService.FindNearest(
                                    transform.position,
                                    "Miner"
                                );

                            ProfDepotService.DepositGoods(
                                minerViking,
                                minerDeposit,
                                "Miner"
                            );

                            IssueCommand(VillagerCommand.GoHome);
                        }

                        break;
                    }


                case VillagerCommand.DepositFishermanGoods:
                    {
                        bool reachedDeposit =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Fisherman",
                                Time.deltaTime
                            );

                        if (reachedDeposit)
                        {
                            Debug.Log(
                                $"[LivingNorsemen] {name} reached Fisherman deposit box."
                            );

                            Viking fishermanViking =
                                GetComponent<Viking>();

                            ProfessionDepositBox fishermanDeposit =
                                ProfDepotService.FindNearest(
                                    transform.position,
                                    "Fisherman"
                                );

                            ProfDepotService.DepositGoods(
                                fishermanViking,
                                fishermanDeposit,
                                "Fisherman"
                            );

                            IssueCommand(VillagerCommand.GoHome);
                        }

                        break;
                    }

                case VillagerCommand.GetCookIngredients:
                    {
                        bool reachedRawMeatBox =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Raw Meat",
                                Time.deltaTime
                            );

                        if (!reachedRawMeatBox)
                        {
                            return;
                        }

                        // We have physically reached the pantry.
                        // NOW run the trophy + available ingredient hierarchy.
                        CookRecipe selectedRecipe =
                            cookProfession?.GetHighestPriorityRecipe();

                        if (selectedRecipe == null)
                        {
                            // Nothing usable was found.
                            // Start the 2-minute pantry-check cooldown.
                            cookProfession?.RecordFailedIngredientCheck();

                            Debug.Log(
                                $"[Cook] {name} found no usable ingredients in Raw Meat box."
                            );

                            // Return to the kitchen/work point.
                            StartGoingToWork();
                            return;
                        }

                        Viking cookViking =
                            GetComponent<Viking>();

                        ProfessionDepositBox rawMeatBox =
                            ProfDepotService.FindNearest(
                                transform.position,
                                "Raw Meat"
                            );

                        bool tookIngredients =
                            ProfDepotService.TakeIngredients(
                                cookViking,
                                rawMeatBox,
                                selectedRecipe.Ingredients
                            );

                        if (tookIngredients)
                        {
                            // Successful pantry trip.
                            cookProfession.ClearIngredientCheckCooldown();

                            Debug.Log(
                                $"[Cook] {name} picked up ingredients for " +
                                $"{selectedRecipe.OutputItem}."
                            );
                        }
                        else
                        {
                            // Something changed between selection and pickup.
                            cookProfession.RecordFailedIngredientCheck();

                            Debug.Log(
                                $"[Cook] {name} could not retrieve ingredients for " +
                                $"{selectedRecipe.OutputItem}."
                            );
                        }

                        // Whether successful or not, return to the WorkPosition.
                        // Once there, the Cook re-evaluates the workflow.
                        StartGoingToWork();
                        return;
                    }

                case VillagerCommand.GoToCookingStation:
                    {
                        CookingStation cookingStation =
                            cookProfession?.FindNearestCookingStation();

                        if (cookingStation == null)
                        {
                            IssueCommand(VillagerCommand.GoToWork);
                            return;
                        }

                        Viking cookViking =
                            GetComponent<Viking>();

                        if (cookViking != null)
                        {
                            cookViking.SetWalk(false);
                        }

                        Vector3 cookingCenter =
                            cookingStation.transform.position;

                        Vector3 directionFromStation =
                            transform.position - cookingCenter;

                        directionFromStation.y = 0f;

                        if (directionFromStation.sqrMagnitude < 0.01f)
                        {
                            directionFromStation =
                                cookingStation.transform.forward;
                        }

                        directionFromStation.Normalize();

                        Vector3 cookingTarget =
                            cookingCenter +
                            directionFromStation * 1.25f;

                        MoveToMethod.Invoke(
                            ai,
                            new object[]
                            {
                                Time.deltaTime,
                                cookingTarget,
                                .5f,
                                false
                            }
                        );

                        float cookingDistance =
                            Vector2.Distance(
                                new Vector2(
                                    transform.position.x,
                                    transform.position.z
                                ),
                                new Vector2(
                                    cookingTarget.x,
                                    cookingTarget.z
                                )
                            );

                        if (cookingDistance <= 1.5f)
                        {

                            if (
                                IsCook() &&
                                cookProfession.HasFinishedFood(cookingStation)
                            )
                            {
                                bool collectedFinishedFood =
                                    cookProfession.TryCollectFinishedFood(
                                        cookingStation
                                    );

                                if (collectedFinishedFood)
                                {
                                    Debug.Log(
                                        $"[Cook] {name} collected finished food from " +
                                        $"{Utils.GetPrefabName(cookingStation.gameObject)}."
                                    );

                                    IssueCommand(
                                        VillagerCommand.GoToWork
                                    );

                                    return;
                                }
                            }
                            CookRecipe desiredRecipe =
                                cookProfession?.GetHighestPriorityActionableHeldRecipe();

                            if (
                                desiredRecipe == null ||
                                desiredRecipe.Ingredients == null ||
                                desiredRecipe.Ingredients.Count == 0
                            )
                            {
                                IssueCommand(VillagerCommand.GoToWork);
                                return;
                            }

                            CookIngredient rawIngredient =
                                desiredRecipe.Ingredients[0];

                            GameObject rawPrefab =
                                ObjectDB.instance.GetItemPrefab(
                                    rawIngredient.PrefabName
                                );

                            if (rawPrefab == null)
                            {
                                IssueCommand(VillagerCommand.GoToWork);
                                return;
                            }

                            ItemDrop rawItemDrop =
                                rawPrefab.GetComponent<ItemDrop>();

                            if (rawItemDrop == null)
                            {
                                IssueCommand(VillagerCommand.GoToWork);
                                return;
                            }

                            string rawSharedName =
                                rawItemDrop.m_itemData.m_shared.m_name;

                            ItemDrop.ItemData rawItem =
                                cookViking.GetInventory().GetItem(
                                    rawSharedName,
                                    -1,
                                    false
                                );

                            if (rawItem == null)
                            {
                                IssueCommand(VillagerCommand.GoToWork);
                                return;
                            }

                            bool placedOnStation =
                                cookingStation.UseItem(
                                    cookViking,
                                    rawItem
                                );

                            if (placedOnStation)
                            {
                                Debug.Log(
                                    $"[Cook] {name} placed " +
                                    $"{rawIngredient.PrefabName} onto " +
                                    $"{Utils.GetPrefabName(cookingStation.gameObject)}."
                                );

                                IssueCommand(
                                    VillagerCommand.GoToWork
                                );

                                return;
                            }

                            Debug.Log(
                                $"[Cook] {name} could not use " +
                                $"{Utils.GetPrefabName(cookingStation.gameObject)}."
                            );

                            IssueCommand(
                                VillagerCommand.GoToWork
                            );

                            return;
                        }

                        return;
                    }

                case VillagerCommand.DepositCookedFoods:
                    {
                        bool reachedDeposit =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Cooked Foods",
                                Time.deltaTime
                            );

                        if (reachedDeposit)
                        {
                            Debug.Log(
                                $"[LivingNorsemen] {name} reached Cooked Foods deposit box."
                            );

                            DepositCookedFoods();

                            IssueCommand(
                                VillagerCommand.GoHome
                            );
                        }

                        break;
                    }

                case VillagerCommand.GetBreakfast:
                    {
                        bool reachedFoodBox =
                            ProfDepotService.MoveToward(
                                this,
                                ai,
                                "Cooked Foods",
                                Time.deltaTime
                            );

                        if (!reachedFoodBox)
                        {
                            return;
                        }

                        Viking breakfastViking =
                            GetComponent<Viking>();

                        if (breakfastViking == null)
                        {
                            StartGoingToWork();
                            return;
                        }

                        int currentFoodCount =
                            ProfDepotService.CountAllowedItems(
                                breakfastViking,
                                "Cooked Foods"
                            );

                        int foodNeeded =
                            Mathf.Max(
                                0,
                                3 - currentFoodCount
                            );

                        if (foodNeeded <= 0)
                        {
                            StartGoingToWork();
                            return;
                        }

                        ProfessionDepositBox cookedFoodsBox =
                            ProfDepotService.FindNearest(
                                transform.position,
                                "Cooked Foods"
                            );

                        int foodTaken =
                            ProfDepotService.TakeAllowedItems(
                                breakfastViking,
                                cookedFoodsBox,
                                "Cooked Foods",
                                foodNeeded
                            );

                        Debug.Log(
                            $"[Breakfast] {name} took {foodTaken} food item(s). " +
                            $"Now carrying " +
                            $"{ProfDepotService.CountAllowedItems(breakfastViking, "Cooked Foods")}."
                        );

                        StartGoingToWork();
                        return;
                    }

                case VillagerCommand.PatrolRoute:
                    {
                     
                        if (IsOffShift())
                        {
                            IssueCommand(
                                VillagerCommand.None
                            );

                            return;
                        }

                        Viking patrolViking =
                            GetComponent<Viking>();

                        if (patrolViking != null)
                        {
                            patrolViking.SetWalk(true);
                        }

                        patrolUtilityScanTimer -= Time.deltaTime;

                        if (patrolUtilityScanTimer <= 0f)
                        {
                            patrolUtilityScanTimer = 1f;

                            TryCloseNearbyOpenDoor(
                                5f
                            );

                            TryRelightNearbyFireplace(
                                5f
                            );
                        }

                        MovePatrol(
                            Time.deltaTime
                        );

                        return;
                    }

            }
            //------------------------------------------------------------------------

            //---PATROL
            if (
                CurrentCommand == VillagerCommand.None &&
                HasWorkPosition &&
                HasPatrolPointB &&
                IsWorkShift()
            )
            {
                Viking patrolViking =
                    GetComponent<Viking>();

                if (
                    patrolViking != null &&
                    patrolViking.m_vikingAI != null &&
                    patrolViking.m_vikingAI.m_moveType ==
                        Movement.Guard &&
                    patrolViking.m_vikingAI.m_behaviour ==
                        Emotion.Aggressive &&
                    !patrolViking.IsFollowing()
                )
                {
                    patrolHeadingToB = false;
                    IssueCommand(
                        VillagerCommand.PatrolRoute
                    );

                    return;
                }
            }


            //---COOKING
            if (
                CurrentCommand == VillagerCommand.None &&
                HasWorkPosition &&
                IsWorkShift() &&
                IsCook()
            )
            {
                float cookDistanceToWork =
                    Vector3.Distance(
                        transform.position,
                        WorkPosition
                    );

                if (cookDistanceToWork <= 2f)
                {

                    CookingStation nearbyCookingStation =
                        cookProfession.FindNearestCookingStation();

                    if (
                        nearbyCookingStation != null &&
                        cookProfession.HasFinishedFood(
                            nearbyCookingStation
                        )
                    )
                    {
                        IssueCommand(
                            VillagerCommand.GoToCookingStation
                        );

                        Debug.Log(
                            $"[Cook] {name} detected finished food at " +
                            $"{Utils.GetPrefabName(nearbyCookingStation.gameObject)}."
                        );

                        return;
                    }

                    // determine the highest-priority instruction
                    // from the trophies the Cook is carrying.
                    CookRecipe desiredRecipe =
                        cookProfession.GetHighestPriorityActionableHeldRecipe();

                    if (desiredRecipe == null)
                    {
                        desiredRecipe =
                            cookProfession.GetHighestPriorityHeldRecipe();
                    }

                    if (desiredRecipe != null)
                    {
                        // Does the Cook already have the ingredients
                        // for the highest-priority trophy?
                        if (
                            cookProfession.HasIngredientsInInventory(
                                desiredRecipe
                            )
                        )
                        {
                            CookingStation cookingStation =
                                cookProfession.FindNearestCookingStation();

                            if (cookingStation != null)
                            {
                                if (
                                    cookProfession.HasFreeCookingSlot(
                                        cookingStation
                                    )
                                )
                                {
                                    IssueCommand(
                                        VillagerCommand.GoToCookingStation
                                    );

                                    Debug.Log(
                                        $"[Cook] {name} going to " +
                                        $"{Utils.GetPrefabName(cookingStation.gameObject)} " +
                                        $"to cook {desiredRecipe.OutputItem}."
                                    );

                                    return;
                                }

                                // Station is full.
                                // Keep the raw ingredient in inventory
                                // and wait at WorkPosition until a slot opens.
                            }
                            else
                            {
                                Debug.Log(
                                    $"[Cook] {name} has ingredients for " +
                                    $"{desiredRecipe.OutputItem}, " +
                                    $"but no CookingStation was found near WorkPosition."
                                );
                            }

                            // Next phase:
                            // send Cook to the proper cooking station.
                        }
                        else if (
                            cookProfession.CanCheckIngredientBox()
                        )
                        {
                            IssueCommand(
                                VillagerCommand.GetCookIngredients
                            );

                            Debug.Log(
                                $"[Cook] {name} going to check Raw Meat box."
                            );

                            return;
                        }

                        // Otherwise the previous pantry check failed less
                        // than 120 seconds ago, so remain at WorkPosition.
                    }
                }
            }

            float nightDistanceToHome = Vector3.Distance(
                transform.position,
                HomePosition
            );

            Viking viking = GetComponent<Viking>();

            if (
                CurrentCommand == VillagerCommand.None &&
                HasHome &&
                nightDistanceToHome > 4f &&
                IsOffShift() &&
                viking != null &&
                !viking.IsFollowing()
            )
            {
                IsAtHome = false;

                // Stop farming movement, slow-walk, kneeling, and farm pathing.
                CancelFarmingState();

                // ------------------------------------------------------------
                // COOK NIGHT CLEANUP
                // A Cook may not end the workday while food remains
                // on the cooking station.
                // ------------------------------------------------------------
                if (IsCook())
                {
                    CookingStation cookingStation =
                        cookProfession.FindNearestCookingStation();

                    if (
                        cookingStation != null &&
                        cookProfession.HasOccupiedCookingSlot(
                            cookingStation
                        )
                    )
                    {
                        // If something is finished, go collect it.
                        if (
                            cookProfession.HasFinishedFood(
                                cookingStation
                            )
                        )
                        {
                            IssueCommand(
                                VillagerCommand.GoToCookingStation
                            );

                            Debug.Log(
                                $"[Cook] {name} staying on duty to collect " +
                                $"finished food before going home."
                            );
                        }

                        // If food is still cooking, do nothing.
                        // Remain on duty until it finishes.
                        return;
                    }
                }

                ProfessionDepositBox cookedFoodsDeposit =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Cooked Foods"
                    );

                ProfessionDepositBox farmerDeposit =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Farmer"
                    );

                ProfessionDepositBox lumberjackDeposit =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Lumberjack"
                    );

                ProfessionDepositBox minerDeposit =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Miner"
                    );

                ProfessionDepositBox fishermanDeposit =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Fisherman"
                    );


                if (
                    IsCook() &&
                    cookedFoodsDeposit != null &&
                    ProfDepotService.HasAllowedItems(
                        viking,
                        "Cooked Foods"
                    )
                )
                {
                    IssueCommand(
                        VillagerCommand.DepositCookedFoods
                    );
                }
                else if (
                    farmerProfession != null &&
                    farmerProfession.HasCultivator() &&
                    farmerDeposit != null
                )
                {
                    IssueCommand(VillagerCommand.DepositFarmerGoods);
                }
                else if (
                   viking.m_axe != null &&
                    lumberjackDeposit != null
                )
                {
                    IssueCommand(VillagerCommand.DepositLumberjackGoods);
                }
                else if (
                    viking.m_pickaxe != null &&
                    minerDeposit != null
                )
                {
                    IssueCommand(VillagerCommand.DepositMinerGoods);
                }
                else if (
                    fishermanDeposit != null &&
                    ProfDepotService.HasAllowedItems(
                        viking,
                        "Fisherman"
                    )
                )
                {
                    IssueCommand(
                        VillagerCommand.DepositFishermanGoods
                    );
                }

                else
                {
                    StartGoingHome();
                }

                return;
            }

            float dayDistanceToWork = Vector3.Distance(
                transform.position,
                WorkPosition
            );

            if (
                CurrentCommand == VillagerCommand.None &&
                HasWorkPosition &&
                !WorkDispatchedToday &&
                dayDistanceToWork > 1.5f &&
                IsWorkShift()
            )
            {
                int foodCount =
                    ProfDepotService.CountAllowedItems(
                        viking,
                        "Cooked Foods"
                    );

                ProfessionDepositBox cookedFoodsBox =
                    ProfDepotService.FindNearest(
                        transform.position,
                        "Cooked Foods"
                    );

                if (
                    foodCount < 3 &&
                    cookedFoodsBox != null
                )
                {
                    IssueCommand(
                        VillagerCommand.GetBreakfast
                    );

                    Debug.Log(
                        $"[Breakfast] {name} going to Cooked Foods box. " +
                        $"Currently carrying {foodCount}/3 food."
                    );

                    return;
                }

                StartGoingToWork();
                return;
            }

            if (
                CurrentCommand == VillagerCommand.None &&
                HasWorkPosition &&
                IsWorkShift() &&
                farmerProfession != null &&
                farmerProfession.HasCultivator() &&
                viking != null &&
                !viking.IsFollowing() &&
                dayDistanceToWork > 6f
            )
            {
                StartGoingToWork();
                return;
            }

            

            //------------------------------------------------------------------------

            if (IsActivelyFarming())
            {
                farmerProfession.Update(Time.deltaTime);
            }
            else
            {
                Viking movementViking = GetComponent<Viking>();

                if (movementViking != null)
                {
                    movementViking.SetWalk(false);
                }
            }

            timer += Time.deltaTime;

            if (timer < 1f)
            {
                return;
            }

            timer = 0f;

            
            bool hasWorkTarget = hasWorkTargetRef(ai);

            if (hasWorkTarget != lastHasWorkTarget)
            {
                lastHasWorkTarget = hasWorkTarget;

                string targetType = "None";

                if (treeRef(ai) != null)
                {
                    targetType = $"Tree: {treeRef(ai).name}";
                }
                else if (mineRockRef(ai) != null)
                {
                    targetType = $"MineRock: {mineRockRef(ai).name}";
                }
                else if (mineRock5Ref(ai) != null)
                {
                    targetType = $"MineRock5: {mineRock5Ref(ai).name}";
                }
                else if (destructibleRef(ai) != null)
                {
                    targetType = $"Destructible: {destructibleRef(ai).name}";
                }
                else if (fishRef(ai) != null)
                {
                    targetType = $"Fish: {fishRef(ai).name}";
                }

                BepInEx.Logging.Logger.CreateLogSource("LivingNorseman")
                    .LogInfo($"{name} hasWorkTarget = {hasWorkTarget}, Target = {targetType}");
            }
        }
        //------------------------------------------------------------------------
    }
}
