using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton untuk menyimpan inventory player.
/// Gunakan dari mana saja: InventoryManager.Instance.AddItem("Jahe", 1)
///
/// Kompatibel dengan sistem Cooking — CookingUI bisa cek HasItem / RemoveItem.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // Jumlah tiap item
    private readonly Dictionary<string, int>    counts = new Dictionary<string, int>();
    // Icon tiap item (disimpan saat pertama AddItem dengan InventoryItem)
    private readonly Dictionary<string, Sprite> icons  = new Dictionary<string, Sprite>();

    // Event: dipanggil ketika ada perubahan inventory
    public event System.Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaptureState += CaptureInventory;
            GameManager.Instance.OnApplyState   += ApplyInventory;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaptureState -= CaptureInventory;
            GameManager.Instance.OnApplyState   -= ApplyInventory;
        }
    }

    // ─────────────────────────────────────────
    // SAVE / LOAD (lewat GameManager)
    // ─────────────────────────────────────────

    /// <summary>Tulis isi inventory ke SaveData (dipanggil saat menyimpan).</summary>
    private void CaptureInventory()
    {
        var d = GameManager.Instance.Data;
        d.inventory.Clear();
        foreach (var kv in counts)
            d.inventory.Add(new ItemStack(kv.Key, kv.Value));
    }

    /// <summary>Muat isi inventory dari SaveData (dipanggil saat Continue).</summary>
    private void ApplyInventory()
    {
        var d = GameManager.Instance.Data;
        counts.Clear();
        icons.Clear();
        foreach (var s in d.inventory)
            if (s != null && !string.IsNullOrEmpty(s.itemName) && s.amount > 0)
                counts[s.itemName] = s.amount;
        OnInventoryChanged?.Invoke();
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────

    /// <summary>Tambah item berdasarkan ScriptableObject.</summary>
    public void AddItem(InventoryItem item, int amount = 1)
    {
        if (item == null) return;
        AddItem(item.itemName, amount, item.icon);
    }

    /// <summary>Tambah item berdasarkan nama string (kompatibel dengan Cooking).</summary>
    public void AddItem(string itemName, int amount = 1, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(itemName)) return;

        // Normalisasi nama item Madu Hutan menjadi Madu agar sesuai dengan bahan resep
        if (itemName == "Madu Hutan") itemName = "Madu";

        if (counts.ContainsKey(itemName)) counts[itemName] += amount;
        else                              counts[itemName]  = amount;

        if (icon != null && !icons.ContainsKey(itemName))
            icons[itemName] = icon;

        Debug.Log($"[Inventory] +{amount} {itemName} (total: {counts[itemName]})");
        OnInventoryChanged?.Invoke();
    }

    /// <summary>Kurangi item. Return false jika tidak cukup.</summary>
    public bool RemoveItem(string itemName, int amount = 1)
    {
        if (!HasItem(itemName, amount)) return false;
        counts[itemName] -= amount;
        if (counts[itemName] <= 0) counts.Remove(itemName);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Cek apakah punya cukup item.</summary>
    public bool HasItem(string itemName, int amount = 1)
        => counts.TryGetValue(itemName, out int c) && c >= amount;

    /// <summary>Ambil jumlah item.</summary>
    public int GetAmount(string itemName)
        => counts.TryGetValue(itemName, out int c) ? c : 0;

    /// <summary>Ambil icon item (bisa null).</summary>
    public Sprite GetIcon(string itemName)
        => icons.TryGetValue(itemName, out Sprite s) ? s : null;

    /// <summary>Ambil semua item (buat ditampilkan di UI).</summary>
    public Dictionary<string, int> GetAllItems() => counts;

    /// <summary>Reset seluruh inventory.</summary>
    public void Clear() { counts.Clear(); icons.Clear(); OnInventoryChanged?.Invoke(); }

    // ─────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────

    [ContextMenu("Print Inventory")]
    void PrintInventory()
    {
        if (counts.Count == 0) { Debug.Log("[Inventory] Kosong."); return; }
        foreach (var kv in counts)
            Debug.Log($"[Inventory] {kv.Key}: {kv.Value}");
    }
}
