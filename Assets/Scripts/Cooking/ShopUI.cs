using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Toko bibit & bahan milik Nisa.
/// Tampilkan daftar item yang bisa dibeli → kurangi uang → tambah ke inventory.
///
/// SETUP:
/// 1. Buat panel "ShopPanel" di Canvas
/// 2. Assign references
/// 3. Di NPCDialog Nisa, set shopUI = referensi ini
/// </summary>
public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject shopPanel;

    [Header("Item List")]
    public RectTransform listContainer;

    [Header("Data")]
    public ShopItem[] shopItems;

    [Header("UI Feedback")]
    public TMP_Text moneyText;
    public TMP_Text statusText;

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private PlayerController playerController;

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        public Sprite icon;
        public int price;
        [TextArea] public string description;
    }

    void Awake()
    {
        Instance = this;
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        if (shopPanel != null && shopPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
        if (playerController != null) playerController.CanMove = false;

        UpdateMoneyDisplay();
        PopulateItems();
    }

    public void Close()
    {
        if (shopPanel == null) return;
        shopPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;
        if (playerController != null) playerController.CanMove = true;
    }

    private void PopulateItems()
    {
        foreach (var go in spawnedButtons)
            if (go != null) Destroy(go);
        spawnedButtons.Clear();

        if (listContainer == null || shopItems == null) return;

        for (int i = 0; i < shopItems.Length; i++)
        {
            var item = shopItems[i];
            var btnGO = CreateItemButton(item, i);
            spawnedButtons.Add(btnGO);
        }
    }

    private GameObject CreateItemButton(ShopItem item, int index)
    {
        var go = new GameObject("ShopItem_" + item.itemName);
        go.transform.SetParent(listContainer, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        int idx = index;
        btn.onClick.AddListener(() => BuyItem(idx));

        // Icon
        if (item.icon != null)
        {
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.02f, 0.1f);
            iconRect.anchorMax = new Vector2(0.15f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = item.icon;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }

        // Name + desc
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(go.transform, false);
        var nameRect = nameGO.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.17f, 0.3f);
        nameRect.anchorMax = new Vector2(0.7f, 0.9f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
        nameTMP.text = item.itemName;
        nameTMP.fontSize = 16;
        nameTMP.color = Color.white;
        nameTMP.raycastTarget = false;

        // Price
        var priceGO = new GameObject("Price");
        priceGO.transform.SetParent(go.transform, false);
        var priceRect = priceGO.AddComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.72f, 0.2f);
        priceRect.anchorMax = new Vector2(0.98f, 0.8f);
        priceRect.offsetMin = Vector2.zero;
        priceRect.offsetMax = Vector2.zero;
        var priceTMP = priceGO.AddComponent<TextMeshProUGUI>();
        priceTMP.text = item.price + " G";
        priceTMP.fontSize = 16;
        priceTMP.alignment = TextAlignmentOptions.MidlineRight;
        priceTMP.color = new Color(1f, 0.85f, 0.3f);
        priceTMP.raycastTarget = false;

        return go;
    }

    private void BuyItem(int index)
    {
        if (shopItems == null || index < 0 || index >= shopItems.Length) return;
        var item = shopItems[index];

        // Cek uang
        if (GameManager.Instance == null) return;
        int money = GameManager.Instance.Data.money;

        if (money < item.price)
        {
            ShowStatus("Uang tidak cukup!", false);
            return;
        }

        // Kurangi uang
        GameManager.Instance.Data.money -= item.price;

        // Tambah ke inventory
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(item.itemName, 1, item.icon);

        UpdateMoneyDisplay();
        ShowStatus("Berhasil membeli " + item.itemName + "!", true);
        Debug.Log($"[ShopUI] Bought '{item.itemName}' for {item.price}G");
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyText == null) return;
        int money = (GameManager.Instance != null) ? GameManager.Instance.Data.money : 0;
        moneyText.text = money + " G";
    }

    private void ShowStatus(string msg, bool success)
    {
        if (statusText == null) return;
        statusText.text = msg;
        statusText.color = success ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.4f, 0.4f);
    }
}
