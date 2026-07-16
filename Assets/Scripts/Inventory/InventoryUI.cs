using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pasang di Canvas/InventoryUI.
/// Toggle dengan Tab. Menampilkan isi InventoryManager ke slot-slot SlotGrid.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Referensi (auto-cari di Awake jika kosong)")]
    public Transform slotGrid;

    [Header("Toggle Key")]
    public KeyCode toggleKey = KeyCode.Tab;

    // ─────────────────────────────────────────────
    private List<SlotRef> slots = new List<SlotRef>();
    private bool          isOpen = false;

    private class SlotRef
    {
        public Image    icon;
        public TMP_Text count;
        public TMP_Text itemName;
    }

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        FindSlotGrid();
    }

    void Start()
    {
        if (slots.Count == 0)
        {
            InitSlots();
            Refresh();
        }
    }

    void OnEnable()
    {
        // Subscribe setiap kali panel diaktifkan
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    // Tab toggle dihandle oleh PlayerController (karena InventoryUI bisa nonaktif)

    // ─────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public void Open()
    {
        isOpen = true;
        gameObject.SetActive(true);   // → OnEnable → subscribe
        Refresh();                     // refresh setelah panel aktif

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.CanMove = false;

        Debug.Log("[InventoryUI] Dibuka.");
    }

    public void Close()
    {
        isOpen = false;
        gameObject.SetActive(false);   // → OnDisable → unsubscribe

        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.CanMove = true;

        Debug.Log("[InventoryUI] Ditutup.");
    }

    // ─────────────────────────────────────────────
    // INIT SLOTS
    // ─────────────────────────────────────────────

    private void FindSlotGrid()
    {
        if (slotGrid != null) return;

        // Coba cari berdasarkan path relatif
        slotGrid = transform.Find("Scroll View/Viewport/Content/SlotGrid");
        if (slotGrid != null) { Debug.Log("[InventoryUI] SlotGrid ditemukan via path."); return; }

        // Fallback: cari berdasarkan nama di seluruh scene
        var go = GameObject.Find("SlotGrid");
        if (go != null)
        {
            slotGrid = go.transform;
            Debug.Log("[InventoryUI] SlotGrid ditemukan via GameObject.Find.");
            return;
        }

        Debug.LogError("[InventoryUI] SlotGrid TIDAK DITEMUKAN! Pastikan ada di Canvas/InventoryUI/Scroll View/Viewport/Content/SlotGrid.");
    }

    private void InitSlots()
    {
        slots.Clear();

        if (slotGrid == null) { FindSlotGrid(); }
        if (slotGrid == null)
        {
            Debug.LogError("[InventoryUI] InitSlots gagal: slotGrid null.");
            return;
        }

        Debug.Log($"[InventoryUI] Inisialisasi {slotGrid.childCount} slot...");

        foreach (Transform slotTF in slotGrid)
        {
            var sr = new SlotRef();

            // ── Icon Image (atas, sisakan ruang nama di bawah) ──
            Transform iconTF = slotTF.Find("ItemIcon");
            if (iconTF == null)
            {
                var iconGO = new GameObject("ItemIcon");
                iconGO.transform.SetParent(slotTF, false);
                var rt = iconGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.08f, 0.30f);
                rt.anchorMax = new Vector2(0.92f, 0.95f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                sr.icon = iconGO.AddComponent<Image>();
                sr.icon.raycastTarget = false;
                sr.icon.preserveAspect = true;
            }
            else
            {
                sr.icon = iconTF.GetComponent<Image>();
            }

            // ── Count Text (pojok kanan atas) ───────────────
            Transform countTF = slotTF.Find("ItemCount");
            if (countTF == null)
            {
                var countGO = new GameObject("ItemCount");
                countGO.transform.SetParent(slotTF, false);
                var rt = countGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.55f, 0.70f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(0, -2f);
                rt.offsetMax = new Vector2(-3f, 0);
                sr.count = countGO.AddComponent<TextMeshProUGUI>();
                sr.count.fontSize = 13f;
                sr.count.fontStyle = FontStyles.Bold;
                sr.count.alignment = TextAlignmentOptions.TopRight;
                sr.count.color = Color.white;
                sr.count.raycastTarget = false;
            }
            else
            {
                sr.count = countTF.GetComponent<TMP_Text>();
            }

            // ── Item Name (bawah slot) ──────────────────────
            Transform nameTF = slotTF.Find("ItemName");
            if (nameTF == null)
            {
                var nameGO = new GameObject("ItemName");
                nameGO.transform.SetParent(slotTF, false);
                var rt = nameGO.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0.30f);
                rt.offsetMin = new Vector2(2f, 2f);
                rt.offsetMax = new Vector2(-2f, 0f);
                sr.itemName = nameGO.AddComponent<TextMeshProUGUI>();
                sr.itemName.fontSize = 11f;
                sr.itemName.fontStyle = FontStyles.Bold;
                sr.itemName.alignment = TextAlignmentOptions.Center;
                sr.itemName.color = new Color(0.95f, 0.9f, 0.75f);
                sr.itemName.enableWordWrapping = true;
                sr.itemName.overflowMode = TextOverflowModes.Ellipsis;
                sr.itemName.raycastTarget = false;
            }
            else
            {
                sr.itemName = nameTF.GetComponent<TMP_Text>();
            }

            SetEmpty(sr);
            slots.Add(sr);
        }

        Debug.Log($"[InventoryUI] {slots.Count} slot siap.");
    }

    // ─────────────────────────────────────────────
    // REFRESH
    // ─────────────────────────────────────────────

    public void Refresh()
    {
        // Re-init jika slots belum diinit
        if (slots.Count == 0 && slotGrid != null)
            InitSlots();

        if (slots.Count == 0)
        {
            Debug.LogWarning("[InventoryUI] Refresh dipanggil tapi slots kosong!");
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUI] InventoryManager.Instance null saat Refresh!");
            return;
        }

        var allItems = new List<KeyValuePair<string, int>>(InventoryManager.Instance.GetAllItems());
        Debug.Log($"[InventoryUI] Refresh: {allItems.Count} item di inventory, {slots.Count} slot.");

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < allItems.Count)
            {
                string itemName = allItems[i].Key;
                int    qty      = allItems[i].Value;
                Sprite icon     = InventoryManager.Instance.GetIcon(itemName);
                SetItem(slots[i], icon, itemName, qty);
            }
            else
            {
                SetEmpty(slots[i]);
            }
        }
    }

    // ─────────────────────────────────────────────
    // SLOT HELPERS
    // ─────────────────────────────────────────────

    private void SetItem(SlotRef sr, Sprite icon, string name, int qty)
    {
        if (sr.icon != null)
        {
            sr.icon.enabled = true;
            if (icon != null)
            {
                sr.icon.sprite = icon;
                sr.icon.color  = Color.white;
            }
            else
            {
                // Tidak ada icon → tampilkan placeholder abu-abu
                sr.icon.sprite = null;
                sr.icon.color  = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            }
        }

        if (sr.count != null)
        {
            sr.count.enabled = true;
            sr.count.text = qty > 1 ? $"x{qty}" : "";
        }

        if (sr.itemName != null)
        {
            sr.itemName.enabled = true;
            sr.itemName.text = name;
        }
    }

    private void SetEmpty(SlotRef sr)
    {
        if (sr.icon     != null) sr.icon.enabled     = false;
        if (sr.count    != null) { sr.count.text    = ""; sr.count.enabled    = false; }
        if (sr.itemName != null) { sr.itemName.text = ""; sr.itemName.enabled = false; }
    }
}
