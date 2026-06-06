using UnityEngine;

/// <summary>
/// ScriptableObject untuk data item (bibit, tanaman, hasil panen, dll).
/// Buat via: klik kanan di Project → Create → Herbal Haven → Item
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Herbal Haven/Item")]
public class InventoryItem : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public ItemType itemType;

    public enum ItemType { Seed, Herb, Cooked, Misc }
}
