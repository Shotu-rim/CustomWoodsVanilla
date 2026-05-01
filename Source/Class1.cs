using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace WoodFilterList
{

    // MOD SUPPORT

    public class WoodRequirementExtension : DefModExtension
    {
        public ThingDef requiredWood;
    }

    // CORE - checking is Wood a wood

    public static class WoodSystem
    {
        public static bool IsWoodLike(Thing t)
        {
            if (t == null || t.def == null)
                return false;

            if (t.def == ThingDefOf.WoodLog)
                return true;

            if (WoodHelper.IsWoody(t))
                return true;

            var ext = t.def.GetModExtension<WoodRequirementExtension>();
            if (ext != null && ext.requiredWood != null)
                return t.def == ext.requiredWood;

            return false;
        }
    }


    // HELPER

    public static class WoodHelper
    {
        public static bool IsWoody(Thing t)
        {
            return t?.Stuff?.stuffProps?.categories != null &&
                   t.Stuff.stuffProps.categories.Contains(StuffCategoryDefOf.Woody);
        }
    }

    // INVENTORY - checking whether wood is in the inventory

    [HarmonyPatch]
    public static class Patch_Inventory_Count
    {
        static MethodBase TargetMethod()
        {
            return typeof(Pawn_InventoryTracker)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m =>
                    m.Name == "Count" &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType == typeof(ThingDef));
        }

        static void Postfix(ThingDef def, Pawn_InventoryTracker __instance, ref int __result)
        {
            if (def != ThingDefOf.WoodLog)
                return;

            int extra = 0;

            for (int i = 0; i < __instance.innerContainer.Count; i++)
            {
                Thing t = __instance.innerContainer[i];
                if (WoodSystem.IsWoodLike(t))
                    extra += t.stackCount;
            }

            __result += extra;
        }
    }


    // Filter

    [HarmonyPatch(typeof(ThingFilter))]
    [HarmonyPatch("Allows", new Type[] { typeof(Thing) })]
    public static class Patch_ThingFilter_Wood
    {
        static void Postfix(Thing t, ref bool __result)
        {
            if (!__result)
                return;

            if (WoodSystem.IsWoodLike(t))
                __result = true;
        }
    }

   

 


    // Designators filter - is material on the map available

    [HarmonyPatch(typeof(Designator_Build), "Visible", MethodType.Getter)]
    public static class Patch_Designator_WoodDropdownFix
    {
        private static readonly HashSet<string> WoodDropdowns =
            new HashSet<string>
            {
                "JA_Floor_WoodTypes",
                "JA_Bridges"
            };

        static void Postfix(Designator_Build __instance, ref bool __result)
        {
            if (!__result)
                return;

            TerrainDef terrain = __instance.PlacingDef as TerrainDef;
            if (terrain == null)
                return;

            Map map = Find.CurrentMap;
            if (map == null)
                return;

            string dropdown = terrain.designatorDropdown?.defName;

            if (dropdown != null && WoodDropdowns.Contains(dropdown))
            {
                WoodRequirementExtension ext =
                    terrain.GetModExtension<WoodRequirementExtension>();

                if (ext == null || ext.requiredWood == null)
                    return;

                bool hasWood = map.listerThings.AllThings
                    .Any(t => t.def == ext.requiredWood);

                if (!hasWood)
                    __result = false;

                return;
            }

            if (terrain.costList == null)
                return;

            ThingDef required = terrain.costList
                .Select(c => c.thingDef)
                .FirstOrDefault();

            if (required == null)
                return;

            bool exists = map.listerThings.AllThings
                .Any(t => t.def == required);

            if (!exists)
                __result = false;
        }
    }
   
}