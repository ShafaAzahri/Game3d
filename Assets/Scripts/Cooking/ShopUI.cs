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
    
    [HideInInspector] public bool isSellingMode = false;

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

        if (listContainer == null) return;

        if (isSellingMode)
        {
            if (InventoryManager.Instance == null) return;
            var items = InventoryManager.Instance.GetAllItems();
            int index = 0;
            foreach (var kv in items)
            {
                string itemName = kv.Key;
                int amount = kv.Value;
                Sprite icon = InventoryManager.Instance.GetIcon(itemName);
                int price = GetSellPrice(itemName);

                ShopItem tempItem = new ShopItem
                {
                    itemName = $"{itemName} (x{amount})",
                    icon = icon,
                    price = price,
                    description = ""
                };

                var btnGO = CreateItemButton(tempItem, index, true, itemName);
                spawnedButtons.Add(btnGO);
                index++;
            }
        }
        else
        {
            List<ShopItem> currentShopItems = new List<ShopItem>();
            if (shopItems != null)
                currentShopItems.AddRange(shopItems);
            
            // Always sell Pakan Ternak (15 G) in Nisa's shop
            if (!currentShopItems.Exists(x => x.itemName == "Pakan Ternak"))
            {
                currentShopItems.Add(new ShopItem {
                    itemName = "Pakan Ternak",
                    price = 15,
                    description = "Pakan bergizi untuk sapi dan hewan ternak lainnya."
                });
            }

            // Sell Bulu Biru (500 G) if Quest Step is 30 onwards (Beli Bulu Biru)
            bool sellBlueFeather = QuestManager.Instance != null && QuestManager.Instance.CurrentStep >= 30;
            if (sellBlueFeather && !currentShopItems.Exists(x => x.itemName == "Bulu Biru"))
            {
                currentShopItems.Add(new ShopItem {
                    itemName = "Bulu Biru",
                    price = 500,
                    description = "Bulu legendaris berwarna biru indah untuk melamar pasangan hidup."
                });
            }

            for (int i = 0; i < currentShopItems.Count; i++)
            {
                var item = currentShopItems[i];
                var btnGO = CreateItemButton(item, i, false, null, currentShopItems);
                spawnedButtons.Add(btnGO);
            }
        }
    }

    private GameObject CreateItemButton(ShopItem item, int index, bool isSell, string sellItemName = null, List<ShopItem> activeBuyList = null)
    {
        var go = new GameObject("ShopItem_" + item.itemName);
        go.transform.SetParent(listContainer, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        
        if (isSell)
        {
            string nameToSell = sellItemName;
            btn.onClick.AddListener(() => SellItem(nameToSell));
        }
        else
        {
            int idx = index;
            var listToBuy = activeBuyList;
            btn.onClick.AddListener(() => BuyItemFromList(idx, listToBuy));
        }

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

        // Name
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
        priceTMP.text = (isSell ? "+" : "") + item.price + " G";
        priceTMP.fontSize = 16;
        priceTMP.alignment = TextAlignmentOptions.MidlineRight;
        priceTMP.color = isSell ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.85f, 0.3f);
        priceTMP.raycastTarget = false;

        return go;
    }

    private void BuyItemFromList(int index, List<ShopItem> activeList)
    {
        if (activeList == null || index < 0 || index >= activeList.Count) return;
        var item = activeList[index];

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

    private void BuyItem(int index)
    {
        if (shopItems == null || index < 0 || index >= shopItems.Length) return;
        BuyItemFromList(index, new List<ShopItem>(shopItems));
    }

    private void SellItem(string itemName)
    {
        if (InventoryManager.Instance == null || GameManager.Instance == null) return;

        if (!InventoryManager.Instance.HasItem(itemName, 1))
        {
            ShowStatus("Item tidak ditemukan!", false);
            return;
        }

        int price = GetSellPrice(itemName);

        // Remove from inventory
        InventoryManager.Instance.RemoveItem(itemName, 1);

        // Add money
        GameManager.Instance.Data.money += price;
        GameManager.Instance.SaveGame();

        UpdateMoneyDisplay();
        ShowStatus($"Berhasil menjual 1 {itemName} (+{price}G)!", true);

        // Refresh list
        PopulateItems();
    }

    public int GetSellPrice(string itemName)
    {
        if (itemName.Contains("Bibit") || itemName.Contains("Seed")) return 5;
        
        // Hasil panen
        if (itemName == "Jahe") return 15;
        if (itemName == "Kunyit") return 15;
        if (itemName == "Temulawak") return 20;
        if (itemName == "Kencur") return 20;
        if (itemName == "Madu" || itemName == "Madu Hutan") return 50;
        
        // Jamu
        if (itemName == "Jamu Jahe") return 40;
        if (itemName == "Jamu Pegal Linu") return 60;
        if (itemName == "Ramuan Penurun Panas" || itemName == "Tolak Angin") return 80;
        if (itemName == "Ramuan Anti Mual" || itemName == "Antimo") return 80;
        if (itemName == "Jamu Sehat Desa" || itemName == "Jamu Sehat") return 100;
        
        return 10; // Default
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
