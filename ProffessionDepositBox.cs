using HarmonyLib;
using UnityEngine;

namespace LivingNorsemen
{
    public class ProfessionDepositBox : MonoBehaviour
    {
        private const string DepositTypeKey =
            "LivingNorsemen_DepositType";

        private ZNetView nview;

        private void Awake()
        {
            nview = GetComponent<ZNetView>();
        }

        public void SetDepositType(string profession)
        {
            if (
                nview == null ||
                nview.GetZDO() == null ||
                !nview.IsOwner()
            )
            {
                return;
            }

            nview.GetZDO().Set(
                DepositTypeKey,
                profession ?? ""
            );
        }

        public string GetDepositType()
        {
            if (
                nview == null ||
                nview.GetZDO() == null
            )
            {
                return "";
            }

            return nview.GetZDO().GetString(
                DepositTypeKey,
                ""
            );
        }

        public void CycleDepositType()
        {
            string currentType = GetDepositType();

            switch (currentType)
            {
                case "":
                    SetDepositType("Farmer");
                    break;

                case "Farmer":
                    SetDepositType("Lumberjack");
                    break;

                case "Lumberjack":
                    SetDepositType("Miner");
                    break;

                case "Miner":
                    SetDepositType("Fisherman");
                    break;

                case "Fisherman":
                    SetDepositType("Raw Meat");
                    break;

                case "Raw Meat":
                    SetDepositType("Cooked Foods");
                    break;

                case "Cooked Foods":
                default:
                    SetDepositType("");
                    break;
            }
        }

        public string GetDepositDisplayName()
        {
            string type = GetDepositType();

            if (string.IsNullOrEmpty(type))
            {
                return "Deposit: None";
            }

            return $"Deposit: {type}";
        }
    }

    //*************************************************************
    [HarmonyPatch(typeof(Container), "Awake")]
    public static class Container_Awake_DepositBoxPatch
    {
        private static void Postfix(Container __instance)
        {
            if (__instance == null)
            {
                return;
            }

            ProfessionDepositBox depositBox =
                __instance.GetComponent<ProfessionDepositBox>();

            if (depositBox == null)
            {
                __instance.gameObject.AddComponent<ProfessionDepositBox>();
            }
        }
    }
}
