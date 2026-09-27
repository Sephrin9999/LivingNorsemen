using BepInEx;
using HarmonyLib;

namespace LivingNorsemen
{
    [BepInPlugin(
        "com.kylet.livingnorsemen",
        "Living Norsemen",
        "0.0.1"
    )]
        //[BepInDependency(
        //"RustyMods.Norsemen",
        //"0.4.1"
    //)]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Harmony harmony = new Harmony("com.kylet.livingnorsemen");
            harmony.PatchAll();

            Logger.LogInfo("Living Norsemen automatic deployment test!");
        }
    }
}