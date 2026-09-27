using System.Collections.Generic;

namespace LivingNorsemen
{
    public enum CookingMethod
    {
        Roast,
        Cauldron
    }

    public class CookIngredient
    {
        public string PrefabName;
        public int Amount;

        public CookIngredient(
            string prefabName,
            int amount)
        {
            PrefabName = prefabName;
            Amount = amount;
        }
    }

    public class CookRecipe
    {
        public string MarkerItem;
        public int Priority;

        public CookingMethod Method;

        public List<CookIngredient> Ingredients;

        public string OutputItem;
        public int OutputAmount;

        public CookRecipe(
            string markerItem,
            int priority,
            CookingMethod method,
            List<CookIngredient> ingredients,
            string outputItem,
            int outputAmount = 1)
        {
            MarkerItem = markerItem;
            Priority = priority;
            Method = method;
            Ingredients = ingredients;
            OutputItem = outputItem;
            OutputAmount = outputAmount;
        }
    }
}
