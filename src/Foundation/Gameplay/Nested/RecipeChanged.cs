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
    internal static partial class GameplayIntegration
    {
        [HarmonyPatch(typeof(RecipeElement), nameof(RecipeElement.SetupRecipe))]
        private static class RecipeChanged
        {
            static void Prefix(RecipeElement __instance, out CraftingRecipe __state) => __state = __instance.Recipe;
            static void Postfix(RecipeElement __instance, CraftingRecipe __state)
            { if (__state != __instance.Recipe) Publish(new MachineRecipeChangedEvent(__instance, __state, __instance.Recipe)); }
        }
    }

}
