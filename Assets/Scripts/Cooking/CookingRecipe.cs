using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe")]
public class CookingRecipe : ScriptableObject
{
    public string recipeName;
    public string description;
    public Sprite recipeImage;

    [Header("Effect")]
    public int hpRestore;

    [Header("Ingredients")]
    public Ingredient[] ingredients;

    [System.Serializable]
    public class Ingredient
    {
        public string itemName;
        public Sprite itemIcon;
        public int amountRequired;
    }
}
