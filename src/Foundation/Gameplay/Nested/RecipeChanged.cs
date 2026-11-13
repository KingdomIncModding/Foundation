using System;
using Assets.Source.Adventure;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using HarmonyLib;
using KingdomFoundation.Api;

namespace KingdomFoundation
{
    [HarmonyPatch(typeof(RecipeElement), nameof(RecipeElement.SetupRecipe))]
    internal static class RecipeChanged
    {
        static void Prefix(RecipeElement __instance, out CraftingRecipe __state) => __state = __instance.Recipe;
        static void Postfix(RecipeElement __instance, CraftingRecipe __state)
        {
            if (__state != __instance.Recipe)
                global::KingdomFoundation.GameplayIntegration.Publish(new MachineRecipeChangedEvent(__instance, __state, __instance.Recipe));
        }
    }
}
