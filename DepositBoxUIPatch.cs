using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LivingNorsemen
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
    public static class DepositBoxUIPatch

    {

        private static GameObject depositButtonRow;
        private static Button depositTypeButton;
        private static TMP_Text depositTypeText;

        [HarmonyPostfix]
        public static void Postfix(
            InventoryGui __instance,
            Container container
        )
        {
            if (container == null)
            {
                if (depositButtonRow != null)
                {
                    depositButtonRow.SetActive(false);
                }

                return;
            }

            ProfessionDepositBox depositBox =
                container.GetComponent<ProfessionDepositBox>();

            if (depositBox == null)
            {
                return;
            }

            if (depositButtonRow == null)
            {
                depositButtonRow = new GameObject(
                    "LivingNorsemen_DepositButtonRow",
                    typeof(RectTransform),
                    typeof(HorizontalLayoutGroup)
                );

                depositButtonRow.transform.SetParent(
                    __instance.m_inventoryRoot,
                    false
                );

                RectTransform rowRect =
                    depositButtonRow.GetComponent<RectTransform>();

                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);

                rowRect.anchoredPosition = new Vector2(35f, -405f);
                rowRect.sizeDelta = new Vector2(580f, 50f);

                HorizontalLayoutGroup layout =
                    depositButtonRow.GetComponent<HorizontalLayoutGroup>();

                GameObject farmerButtonObject =
                    Object.Instantiate(
                        __instance.m_stackAllButton.gameObject,
                        depositButtonRow.transform
                    );

                farmerButtonObject.name =
                    "LivingNorsemen_DepositType";

                depositTypeButton =
                    farmerButtonObject.GetComponent<Button>();

                depositTypeText =
                    farmerButtonObject.transform
                        .Find("Text")
                        .GetComponent<TMP_Text>();

                //layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.spacing = 4f;
            }

            depositButtonRow.SetActive(true);

            depositTypeText.text =
                depositBox.GetDepositDisplayName();

            depositTypeButton.onClick.RemoveAllListeners();

            depositTypeButton.onClick.AddListener(() =>
            {
                depositBox.CycleDepositType();

                depositTypeText.text =
                    depositBox.GetDepositDisplayName();
            });

            Debug.Log(
                $"[LivingNorsemen] Opened profession deposit box: " +
                $"{depositBox.GetDepositDisplayName()}"
            );
        }
    }
}
