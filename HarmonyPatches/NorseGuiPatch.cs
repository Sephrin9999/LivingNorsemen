using HarmonyLib;
using Norsemen;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using LivingNorsemen.Home;

namespace LivingNorsemen.HarmonyPatches
{
    [HarmonyPatch(typeof(NorseGui), nameof(NorseGui.Awake))]
    public static class NorseGuiPatch
    {
        internal static GameObject patrolBButtonObject;

        private static void Postfix(NorseGui __instance)
        {
            GameObject homeButtonObject = Object.Instantiate(
                __instance.access.go,
                __instance.access.go.transform.parent
            );

            homeButtonObject.name = "LivingNorsemen_AssignHome";


            Button homeButton = homeButtonObject.GetComponent<Button>();
            homeButton.onClick.RemoveAllListeners();

            homeButton.onClick.AddListener(() =>
            {
                Viking currentViking = VikingGui.m_currentViking;

                if (currentViking == null)
                {
                    return;
                }

                LivingNorseman component =
                    currentViking.GetComponent<LivingNorseman>();

                if (component == null)
                {
                    return;
                }

                component.AssignHome(Player.m_localPlayer.transform.position);

                var home = HomeValidator.Validate(component);

                string message;

                if (home.IsValid)
                {
                    message = $"{currentViking.GetName()}'s home is suitable!";
                }
                else if (!home.HasBed)
                {
                    message = $"{currentViking.GetName()}'s home needs an unclaimed bed.";
                }
                else if (!home.HasRoof)
                {
                    message = $"{currentViking.GetName()}'s home needs a roof.";
                }
                else if (!home.HasFire)
                {
                    message = $"{currentViking.GetName()}'s home needs a fire.";
                }
                else
                {
                    message = $"{currentViking.GetName()}'s home is incomplete.";
                }

                Player.m_localPlayer.Message(
                    MessageHud.MessageType.Center,
                    message
                );
            });

            TMP_Text homeText =
                homeButtonObject.transform.Find("Text").GetComponent<TMP_Text>();

            homeText.text = "Assign Home";

            GameObject workButtonObject = Object.Instantiate(
                __instance.access.go,
                __instance.access.go.transform.parent
            );

            workButtonObject.name = "LivingNorsemen_AssignWork";

            Button workButton = workButtonObject.GetComponent<Button>();
            workButton.onClick.RemoveAllListeners();

            workButton.onClick.AddListener(() =>
            {
                Viking currentViking = VikingGui.m_currentViking;

                if (currentViking == null)
                {
                    return;
                }

                LivingNorseman component =
                    currentViking.GetComponent<LivingNorseman>();

                if (component == null)
                {
                    return;
                }

                component.AssignWork(Player.m_localPlayer.transform.position);

                Player.m_localPlayer.Message(
                    MessageHud.MessageType.Center,
                    $"{currentViking.GetName()}'s work location assigned."
                );
            });

            TMP_Text workText =
                workButtonObject.transform.Find("Text").GetComponent<TMP_Text>();

            workText.text = "Assign Work";

            patrolBButtonObject = Object.Instantiate(
                __instance.access.go,
                __instance.access.go.transform.parent
            );

            patrolBButtonObject.name =
                "LivingNorsemen_AssignPatrolB";

            Button patrolBButton =
                patrolBButtonObject.GetComponent<Button>();

            patrolBButton.onClick.RemoveAllListeners();

            patrolBButton.onClick.AddListener(() =>
            {
                Viking currentViking =
                    VikingGui.m_currentViking;

                if (currentViking == null)
                {
                    return;
                }

                VikingAI currentAI =
                    currentViking.m_vikingAI;

                if (
                    currentAI == null ||
                    currentAI.m_moveType != Movement.Guard ||
                    currentAI.m_behaviour != Emotion.Aggressive
                )
                {
                    return;
                }

                LivingNorseman component =
                    currentViking.GetComponent<LivingNorseman>();

                if (component == null)
                {
                    return;
                }

                component.AssignPatrolPointB(
                    Player.m_localPlayer.transform.position
                );

                Player.m_localPlayer.Message(
                    MessageHud.MessageType.Center,
                    $"{currentViking.GetName()}'s patrol point B assigned."
                );
            });

            TMP_Text patrolBText =
                patrolBButtonObject
                    .transform
                    .Find("Text")
                    .GetComponent<TMP_Text>();

            patrolBText.text = "Assign Patrol B";

            // Hidden by default.
            // NorseGui.Show patch will decide whether it is visible.
            patrolBButtonObject.SetActive(false);

        }
    }

    [HarmonyPatch(typeof(NorseGui), nameof(NorseGui.Show))]
    public static class NorseGuiShowPatrolPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (
                NorseGuiPatch.patrolBButtonObject == null
            )
            {
                return;
            }

            Viking currentViking =
                VikingGui.m_currentViking;

            if (
                currentViking == null ||
                currentViking.m_vikingAI == null
            )
            {
                NorseGuiPatch
                    .patrolBButtonObject
                    .SetActive(false);

                return;
            }

            bool isPatrollingGuard =
                currentViking.m_vikingAI.m_moveType ==
                    Movement.Guard &&
                currentViking.m_vikingAI.m_behaviour ==
                    Emotion.Aggressive;

            NorseGuiPatch
                .patrolBButtonObject
                .SetActive(isPatrollingGuard);
        }
    }

}
