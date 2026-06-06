using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI popup pemilihan bibit saat menanam.
/// Muncul otomatis saat player tekan F di petak yang sudah dicangkul.
///
/// Cara kerja:
/// - Show() dipanggil oleh GardenPlot dengan daftar PlantData yang tersedia
/// - Player pilih dengan klik atau tekan angka (1, 2, 3...)
/// - Setelah memilih, callback dipanggil dan UI hilang
/// - Tekan Escape untuk batal
/// </summary>
public class SeedSelectionUI : MonoBehaviour
{
    public static SeedSelectionUI Instance { get; private set; }

    // ─────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────

    private GameObject              panel;
    private List<GameObject>        cards      = new List<GameObject>();
    private System.Action<PlantData> onSelect;
    private List<PlantData>         currentOptions = new List<PlantData>();
    private bool                    isOpen     = false;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildBasePanel();
    }

    void Update()
    {
        if (!isOpen) return;

        // ESC untuk batal
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
            return;
        }

        // Tekan angka 1–9 untuk pilih
        for (int i = 0; i < currentOptions.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                Select(i);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tampilkan UI pilihan bibit.
    /// options = semua PlantData yang bisa ditanam di plot ini.
    /// callback dipanggil dengan PlantData yang dipilih (null jika batal).
    /// </summary>
    public void Show(List<PlantData> options, System.Action<PlantData> callback)
    {
        if (options == null || options.Count == 0)
        {
            Debug.LogWarning("[SeedSelectionUI] Tidak ada tanaman tersedia!");
            return;
        }

        onSelect       = callback;
        currentOptions = options;
        isOpen         = true;

        // Lock movement saat pilih
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.CanMove = false;

        // Sembunyikan farming prompt saat UI pilih muncul
        FarmingPromptUI.Instance?.Hide();

        RebuildCards();
        panel?.SetActive(true);
    }

    public void Hide()
    {
        isOpen = false;
        panel?.SetActive(false);
        ClearCards();

        // Kembalikan kontrol player
        var pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.CanMove = true;

        onSelect = null;
    }

    public bool IsOpen => isOpen;

    // ─────────────────────────────────────────────
    // SELECTION
    // ─────────────────────────────────────────────

    private void Select(int index)
    {
        if (index < 0 || index >= currentOptions.Count) return;

        PlantData chosen = currentOptions[index];
        var cb = onSelect;
        Hide();
        cb?.Invoke(chosen);
    }

    // ─────────────────────────────────────────────
    // UI BUILD
    // ─────────────────────────────────────────────

    private void BuildBasePanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Panel background (strip horizontal di bawah layar)
        panel = new GameObject("SeedSelectionUI");
        panel.transform.SetParent(canvas.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 0f);
        rect.anchorMax        = new Vector2(1f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.sizeDelta        = new Vector2(0f, 160f);
        rect.anchoredPosition = new Vector2(0f, 0f);

        // Background gelap
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.07f, 0.92f);

        // Label judul
        var titleGO   = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin        = new Vector2(0f, 0.68f);
        titleRect.anchorMax        = new Vector2(1f, 1f);
        titleRect.offsetMin        = new Vector2(16f, 0f);
        titleRect.offsetMax        = new Vector2(-16f, 0f);
        var titleTxt  = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "🌱  Pilih Bibit  |  Klik atau tekan angka  |  ESC untuk batal";
        titleTxt.fontSize  = 13f;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color     = new Color(0.85f, 0.85f, 0.65f, 1f);

        panel.SetActive(false);
    }

    private void RebuildCards()
    {
        ClearCards();
        if (panel == null) return;

        int   count    = currentOptions.Count;
        float cardW    = 110f;
        float cardH    = 100f;
        float spacing  = 12f;
        float totalW   = count * cardW + (count - 1) * spacing;
        float startX   = -totalW / 2f + cardW / 2f;

        for (int i = 0; i < count; i++)
        {
            PlantData pd  = currentOptions[i];
            int       idx = i; // capture for lambda

            bool hasSeed = pd.seedItem == null ||
                (InventoryManager.Instance != null &&
                 InventoryManager.Instance.HasItem(pd.seedItem.itemName));

            // Card container
            var card = new GameObject($"Card_{i}");
            card.transform.SetParent(panel.transform, false);

            var cardRect = card.AddComponent<RectTransform>();
            cardRect.anchorMin        = new Vector2(0.5f, 0f);
            cardRect.anchorMax        = new Vector2(0.5f, 0f);
            cardRect.pivot            = new Vector2(0.5f, 0f);
            cardRect.sizeDelta        = new Vector2(cardW, cardH);
            cardRect.anchoredPosition = new Vector2(startX + i * (cardW + spacing), 8f);

            // Background card
            var cardBg = card.AddComponent<Image>();
            cardBg.color = hasSeed
                ? new Color(0.18f, 0.28f, 0.18f, 1f)   // hijau gelap = punya bibit
                : new Color(0.22f, 0.18f, 0.18f, 1f);   // merah gelap = tidak punya

            // Button
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = cardBg;
            if (hasSeed)
            {
                int capturedIdx = idx;
                btn.onClick.AddListener(() => Select(capturedIdx));

                var cb = btn.colors;
                cb.normalColor      = new Color(0.18f, 0.28f, 0.18f, 1f);
                cb.highlightedColor = new Color(0.25f, 0.45f, 0.25f, 1f);
                cb.pressedColor     = new Color(0.12f, 0.20f, 0.12f, 1f);
                btn.colors          = cb;
            }
            else
            {
                btn.interactable = false;
            }

            // Nomor shortcut (pojok kiri atas)
            var numGO   = new GameObject("Num");
            numGO.transform.SetParent(card.transform, false);
            var numRect = numGO.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0.78f);
            numRect.anchorMax = new Vector2(0.35f, 1f);
            numRect.offsetMin = new Vector2(4f, -2f);
            numRect.offsetMax = new Vector2(-2f, -2f);
            var numTxt  = numGO.AddComponent<TextMeshProUGUI>();
            numTxt.text      = $"{i + 1}";
            numTxt.fontSize  = 13f;
            numTxt.alignment = TextAlignmentOptions.TopLeft;
            numTxt.color     = new Color(1f, 0.85f, 0.3f, hasSeed ? 1f : 0.4f);
            numTxt.fontStyle = FontStyles.Bold;

            // Icon bibit
            var iconGO   = new GameObject("Icon");
            iconGO.transform.SetParent(card.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.38f);
            iconRect.anchorMax = new Vector2(0.9f, 0.92f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImg  = iconGO.AddComponent<Image>();
            iconImg.raycastTarget = false;

            Sprite icon = pd.seedItem?.icon ?? pd.harvestItem?.icon;
            if (icon != null)
            {
                iconImg.sprite         = icon;
                iconImg.preserveAspect = true;
            }
            else
            {
                // Placeholder warna
                iconImg.color = hasSeed
                    ? new Color(0.4f, 0.7f, 0.4f, 0.8f)
                    : new Color(0.5f, 0.4f, 0.4f, 0.5f);
            }

            // Nama tanaman
            var nameGO   = new GameObject("Name");
            nameGO.transform.SetParent(card.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.38f);
            nameRect.offsetMin = new Vector2(4f, 2f);
            nameRect.offsetMax = new Vector2(-4f, -2f);
            var nameTxt  = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text      = pd.plantName;
            nameTxt.fontSize  = 12f;
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.color     = hasSeed
                ? new Color(0.9f, 0.95f, 0.9f, 1f)
                : new Color(0.6f, 0.5f, 0.5f, 1f);
            nameTxt.raycastTarget = false;

            // Jumlah bibit di inventory
            if (pd.seedItem != null)
            {
                int cnt     = InventoryManager.Instance?.GetAmount(pd.seedItem.itemName) ?? 0;
                var cntGO   = new GameObject("Count");
                cntGO.transform.SetParent(card.transform, false);
                var cntRect = cntGO.AddComponent<RectTransform>();
                cntRect.anchorMin = new Vector2(0.55f, 0.78f);
                cntRect.anchorMax = new Vector2(1f, 1f);
                cntRect.offsetMin = new Vector2(0f, -2f);
                cntRect.offsetMax = new Vector2(-4f, -2f);
                var cntTxt  = cntGO.AddComponent<TextMeshProUGUI>();
                cntTxt.text      = $"x{cnt}";
                cntTxt.fontSize  = 12f;
                cntTxt.alignment = TextAlignmentOptions.TopRight;
                cntTxt.color     = hasSeed
                    ? new Color(0.7f, 1f, 0.7f, 1f)
                    : new Color(0.8f, 0.4f, 0.4f, 1f);
                cntTxt.raycastTarget = false;
            }

            // Label "Tidak ada bibit" jika tidak punya
            if (!hasSeed)
            {
                var noSeedGO   = new GameObject("NoSeed");
                noSeedGO.transform.SetParent(card.transform, false);
                var noSeedRect = noSeedGO.AddComponent<RectTransform>();
                noSeedRect.anchorMin = new Vector2(0f, 0.38f);
                noSeedRect.anchorMax = new Vector2(1f, 0.7f);
                noSeedRect.offsetMin = Vector2.zero;
                noSeedRect.offsetMax = Vector2.zero;
                var noSeedTxt  = noSeedGO.AddComponent<TextMeshProUGUI>();
                noSeedTxt.text      = "Tidak ada\nbibit";
                noSeedTxt.fontSize  = 10f;
                noSeedTxt.alignment = TextAlignmentOptions.Center;
                noSeedTxt.color     = new Color(0.8f, 0.5f, 0.5f, 0.8f);
                noSeedTxt.raycastTarget = false;
            }

            cards.Add(card);
        }
    }

    private void ClearCards()
    {
        foreach (var c in cards)
            if (c != null) Destroy(c);
        cards.Clear();
    }
}
