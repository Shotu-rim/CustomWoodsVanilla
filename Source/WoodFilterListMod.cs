using HarmonyLib;
using Verse;

namespace WoodFilterList
{
    [StaticConstructorOnStartup]
    public static class WoodFilterListModLog
    {
        static WoodFilterListModLog()
        {
            var harmony = new Harmony("WoodFilterList.Mod");
            harmony.PatchAll();

            Log.Message("[WoodFilterList] Harmony initialized!");

        }
    }
}
