using UnityEngine;

/// <summary>
/// Berikan item awal ke player saat game start.
/// Pasang di GameObject manapun (misalnya di InventoryManager).
///
/// SETUP:
/// 1. Tambah komponen ini ke GameObject
/// 2. Klik + pada Starter Items
/// 3. Drag InventoryItem asset (misal: Bibit Jahe) dan isi jumlahnya
/// 4. Item akan otomatis diberikan saat Play
///
/// DEBUG: Tekan Backquote (`) untuk reset inventory dan beri ulang starter items.
/// </summary>
public class StarterInventory : MonoBehaviour
{
    [System.Serializable]
    public class StarterItem
    {
        public InventoryItem item;
        [Min(0)] public int amount = 3;
    }

    [Header("Item Awal Player")]
    public StarterItem[] starterItems;

    [Header("Debug")]
    [Tooltip("Tekan tombol ini untuk reset inventory dan beri ulang starter items")]
    public KeyCode debugRefillKey = KeyCode.BackQuote; // tombol `

    void Start()
    {
        GiveStarterItems();
    }

    void Update()
    {
        if (Input.GetKeyDown(debugRefillKey))
        {
            Debug.Log("[StarterInventory] Debug refill bibit!");
            GiveStarterItems();
        }
    }

    public void GiveStarterItems()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[StarterInventory] InventoryManager tidak ditemukan!");
            return;
        }

        if (starterItems == null || starterItems.Length == 0) return;

        foreach (var si in starterItems)
        {
            if (si.item == null || si.amount <= 0) continue;
            InventoryManager.Instance.AddItem(si.item, si.amount);
        }

        Debug.Log($"[StarterInventory] {starterItems.Length} jenis item diberikan ke player.");
    }
}
