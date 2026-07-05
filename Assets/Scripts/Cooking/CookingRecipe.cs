using UnityEngine;

[CreateAssetMenu(fileName = "New Recipe", menuName = "Cooking/Recipe")]
public class CookingRecipe : ScriptableObject
{
    public string recipeName;
    public string description;
    public Sprite recipeImage;

    [Header("Unlock")]
    [Tooltip("Kosong = resep selalu terbuka. Kalau diisi (mis. 'level1','level2','level3'), " +
             "resep TERKUNCI sampai id tsb ter-unlock lewat progres cerita (QuestManager reward).")]
    public string unlockId;

    [Header("Result")]
    [Tooltip("Item hasil masakan yang masuk inventory. Jika null, akan pakai recipeName & recipeImage.")]
    public InventoryItem resultItem;

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

