using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Mengontrol tampilan Canvas Memasak.
/// Menampilkan daftar resep di kiri, detail di kanan.
/// 
/// SETUP:
/// 1. Attach ke PanelMemasak
/// 2. Assign leftPanel (LeftPanel transform)
/// 3. Assign detail references
/// 4. Isi array recipes
/// </summary>
public class CookingUI : MonoBehaviour
{
    /// <summary>Event global: dipanggil saat sebuah resep berhasil dimasak (membawa recipeName).</summary>
    public static event System.Action<string> OnAnyCooked;

    [Header("Recipes Data")]
    public CookingRecipe[] recipes;

    [Header("Left Panel")]
    [Tooltip("Drag LeftPanel ke sini - button resep akan dibuat di sini")]
    public RectTransform leftPanel;

    [Header("Detail Panel (Kanan)")]
    public Text recipeName;
    public Text recipeDescription;
    public Image recipeImage;
    public Text hpRestoreText;

    [Header("Ingredients List")]
    public GameObject[] ingredientSlots;
    public Image[] ingredientIcons;
    public Text[] ingredientNames;
    public Text[] ingredientAmounts;

    [Header("Buttons")]
    public Button cookButton;
    public Button closeButton;

    [Header("Status Message")]
    public Text statusMessage;

    [Header("References")]
    public CookingTrigger cookingTrigger;

    private int selectedIndex = 0;
    private List<Button> spawnedButtons = new List<Button>();
    private float scrollOffset = 0f;
    private float scrollTarget = 0f;
    private float maxScroll = 0f;
    private List<RectTransform> buttonRects = new List<RectTransform>();

    [Header("Cooking Minigame State")]
    private GameObject minigameOverlay;
    private RectTransform needleRect;
    private bool isMinigameActive = false;
    private CookingRecipe activeMinigameRecipe;
    private float needleProgress = 0f;
    private float needleDirection = 1f;
    private float minigameSpeed = 1.5f;

    [Header("Scroll Settings")]
    public float scrollSpeed = 12f;
    public float scrollSensitivity = 0.12f;

    [Header("Grid Settings")]
    public int columns = 4;
    public int visibleRows = 2;
    public float spacingX = 0.01f;
    public float spacingY = 0.01f;

    void OnEnable()
    {
        // Auto-cari leftPanel
        if (leftPanel == null)
        {
            Transform lp = transform.Find("LeftPanel");
            if (lp != null)
                leftPanel = lp.GetComponent<RectTransform>();
        }

        if (leftPanel == null)
        {
            Debug.LogError("[CookingUI] LeftPanel tidak ditemukan!");
            return;
        }

        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[CookingUI] Array recipes kosong!");
            return;
        }

        // Setup Buttons
        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(OnCookPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClosePressed);
        }

        // Pastikan ukuran grid sesuai request (xMin:0.02 -> xMax:0.196, yMin:0.704 -> yMax:0.85)
        columns = 5;
        visibleRows = 5;
        spacingX = 0.02f;
        spacingY = 0.02f;

        scrollOffset = 0f;
        scrollTarget = 0f;

        isMinigameActive = false;
        if (minigameOverlay != null)
        {
            Destroy(minigameOverlay);
            minigameOverlay = null;
        }

        SetupRecipeButtons();
        SelectRecipe(0);
    }

    void OnDisable()
    {
        isMinigameActive = false;
        if (minigameOverlay != null)
        {
            Destroy(minigameOverlay);
            minigameOverlay = null;
        }
    }

    void Update()
    {
        if (isMinigameActive)
        {
            // Gerakkan jarum bolak-balik
            needleProgress += Time.unscaledDeltaTime * minigameSpeed * needleDirection;
            if (needleProgress >= 1f)
            {
                needleProgress = 1f;
                needleDirection = -1f;
            }
            else if (needleProgress <= 0f)
            {
                needleProgress = 0f;
                needleDirection = 1f;
            }

            // Update posisi jarum secara visual
            if (needleRect != null)
            {
                float xMin = needleProgress * 0.98f;
                needleRect.anchorMin = new Vector2(xMin, -0.2f);
                needleRect.anchorMax = new Vector2(xMin + 0.02f, 1.2f);
                needleRect.offsetMin = Vector2.zero;
                needleRect.offsetMax = Vector2.zero;
            }

            // Dengar input untuk menghentikan jarum memasak
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.G) || Input.GetMouseButtonDown(0))
            {
                EvaluateCookingResult(needleProgress);
            }
            return; // Lewati input normal menu memasak jika minigame aktif
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            OnCookPressed();
        }

        // Scroll input
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f && buttonRects.Count > 0)
        {
            scrollTarget -= scroll * scrollSensitivity;
            scrollTarget = Mathf.Clamp(scrollTarget, 0f, maxScroll);
        }

        // Smooth lerp ke target
        if (Mathf.Abs(scrollOffset - scrollTarget) > 0.001f)
        {
            scrollOffset = Mathf.Lerp(scrollOffset, scrollTarget, Time.unscaledDeltaTime * scrollSpeed);
            UpdateButtonPositions();
        }
    }

    void SetupRecipeButtons()
    {
        // Hapus button lama
        foreach (Button btn in spawnedButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
        buttonRects.Clear();

        if (leftPanel == null || recipes == null) return;

        // cellHeight dihitung dari visibleRows supaya pas 2 row yang kelihatan
        float visibleArea = 0.85f;
        float cellHeight = (visibleArea - spacingY * (visibleRows + 1)) / visibleRows;

        int totalRows = Mathf.CeilToInt((float)recipes.Length / columns);
        float totalNeeded = totalRows * (cellHeight + spacingY);
        maxScroll = Mathf.Max(0f, totalNeeded - visibleArea);

        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] == null) continue;

            GameObject btnObj = CreateRecipeButton(leftPanel, recipes[i]);
            btnObj.SetActive(true);

            Button btn = btnObj.GetComponent<Button>();
            int index = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectRecipe(index));

            spawnedButtons.Add(btn);
            buttonRects.Add(btnObj.GetComponent<RectTransform>());
        }

        UpdateButtonPositions();
    }

    void UpdateButtonPositions()
    {
        // Teks \"Resep Makanan\" ada di Y=0.88 - 0.95. Kita pasang resep mulai di Y=0.85
        float visibleArea = 0.85f; 
        float cellHeight = (visibleArea - spacingY * (visibleRows + 1)) / visibleRows;
        float cellWidth = (1f - spacingX * (columns + 1)) / columns;
        float startY = 0.85f;

        for (int i = 0; i < buttonRects.Count; i++)
        {
            if (buttonRects[i] == null) continue;

            int col = i % columns;
            int row = i / columns;

            float xMin = spacingX + col * (cellWidth + spacingX);
            float xMax = xMin + cellWidth;

            float yMax = startY - (row * (cellHeight + spacingY)) + scrollOffset;
            float yMin = yMax - cellHeight;

            if (yMax < 0.02f || yMin > 0.85f)
            {
                buttonRects[i].gameObject.SetActive(false);
            }
            else
            {
                buttonRects[i].gameObject.SetActive(true);
                yMax = Mathf.Min(yMax, 0.85f);
                yMin = Mathf.Max(yMin, 0.02f);

                buttonRects[i].anchorMin = new Vector2(xMin, yMin);
                buttonRects[i].anchorMax = new Vector2(xMax, yMax);
                buttonRects[i].offsetMin = Vector2.zero;
                buttonRects[i].offsetMax = Vector2.zero;
            }
        }
    }

    GameObject CreateRecipeButton(RectTransform parent, CookingRecipe recipe)
    {
        bool locked = IsRecipeLocked(recipe);

        GameObject btnObj = new GameObject("RecipeBtn_" + recipe.recipeName);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(0.25f, 0.4f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Outer border (frame gelap)
        Image bg = btnObj.AddComponent<Image>();
        bg.color = new Color(0.22f, 0.22f, 0.25f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // ===== Inner panel (sedikit lebih terang, buat efek border) =====
        GameObject innerObj = new GameObject("Inner");
        innerObj.transform.SetParent(btnObj.transform, false);
        RectTransform innerRect = innerObj.AddComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0.04f, 0.04f);
        innerRect.anchorMax = new Vector2(0.96f, 0.96f);
        innerRect.offsetMin = Vector2.zero;
        innerRect.offsetMax = Vector2.zero;
        Image innerBg = innerObj.AddComponent<Image>();
        innerBg.color = new Color(0.3f, 0.3f, 0.33f, 1f);
        innerBg.raycastTarget = false;

        // ===== Image makanan (full di dalam inner) =====
        GameObject imgObj = new GameObject("RecipeImage");
        imgObj.transform.SetParent(innerObj.transform, false);
        RectTransform imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.05f, 0.2f);
        imgRect.anchorMax = new Vector2(0.95f, 0.95f);
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;
        Image recipeImg = imgObj.AddComponent<Image>();
        recipeImg.raycastTarget = false;
        if (recipe.recipeImage != null)
        {
            recipeImg.sprite = recipe.recipeImage;
            recipeImg.preserveAspect = true;
            // Resep terkunci → tampil siluet gelap
            recipeImg.color = locked ? new Color(0.1f, 0.1f, 0.12f, 1f) : Color.white;
        }
        else
        {
            recipeImg.color = locked
                ? new Color(0.1f, 0.1f, 0.12f, 1f)
                : new Color(0.45f, 0.42f, 0.4f, 1f);
        }

        // ===== Bar nama (gradient hitam di bawah) =====
        GameObject nameBg = new GameObject("NameBar");
        nameBg.transform.SetParent(innerObj.transform, false);
        RectTransform nameBgRect = nameBg.AddComponent<RectTransform>();
        nameBgRect.anchorMin = new Vector2(0f, 0f);
        nameBgRect.anchorMax = new Vector2(1f, 0.22f);
        nameBgRect.offsetMin = Vector2.zero;
        nameBgRect.offsetMax = Vector2.zero;
        Image nameBgImg = nameBg.AddComponent<Image>();
        nameBgImg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        nameBgImg.raycastTarget = false;

        // ===== Nama resep =====
        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(nameBg.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0f);
        textRect.anchorMax = new Vector2(0.95f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObj.AddComponent<Text>();
        text.text = locked ? "???" : recipe.recipeName;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 8;
        text.resizeTextMaxSize = 13;
        text.raycastTarget = false;

        // Outline pada text biar lebih jelas
        Outline textOutline = textObj.AddComponent<Outline>();
        textOutline.effectColor = new Color(0, 0, 0, 0.7f);
        textOutline.effectDistance = new Vector2(1, -1);

        // ===== Overlay GEMBOK untuk resep terkunci =====
        if (locked)
        {
            // Lapisan gelap menutupi seluruh inner
            GameObject overlay = new GameObject("LockOverlay");
            overlay.transform.SetParent(innerObj.transform, false);
            RectTransform ovRect = overlay.AddComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero;
            ovRect.anchorMax = Vector2.one;
            ovRect.offsetMin = Vector2.zero;
            ovRect.offsetMax = Vector2.zero;
            Image ovImg = overlay.AddComponent<Image>();
            ovImg.color = new Color(0f, 0f, 0f, 0.55f);
            ovImg.raycastTarget = false;

            // Ikon gembok (glyph) di tengah
            GameObject lockObj = new GameObject("LockIcon");
            lockObj.transform.SetParent(overlay.transform, false);
            RectTransform lockRect = lockObj.AddComponent<RectTransform>();
            lockRect.anchorMin = new Vector2(0.25f, 0.35f);
            lockRect.anchorMax = new Vector2(0.75f, 0.9f);
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;
            Text lockTxt = lockObj.AddComponent<Text>();
            lockTxt.text = "\uD83D\uDD12";   // 🔒 (fallback ke kotak bila font tak punya glyph)
            lockTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lockTxt.fontSize = 28;
            lockTxt.alignment = TextAnchor.MiddleCenter;
            lockTxt.color = new Color(1f, 0.85f, 0.4f, 1f);
            lockTxt.resizeTextForBestFit = true;
            lockTxt.resizeTextMinSize = 10;
            lockTxt.resizeTextMaxSize = 40;
            lockTxt.raycastTarget = false;

            // Label "TERKUNCI" kecil sebagai jaminan keterbacaan
            GameObject lblObj = new GameObject("LockLabel");
            lblObj.transform.SetParent(overlay.transform, false);
            RectTransform lblRect = lblObj.AddComponent<RectTransform>();
            lblRect.anchorMin = new Vector2(0.05f, 0.05f);
            lblRect.anchorMax = new Vector2(0.95f, 0.3f);
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;
            Text lbl = lblObj.AddComponent<Text>();
            lbl.text = "TERKUNCI";
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize = 10;
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.color = new Color(1f, 0.85f, 0.4f, 1f);
            lbl.resizeTextForBestFit = true;
            lbl.resizeTextMinSize = 6;
            lbl.resizeTextMaxSize = 12;
            lbl.raycastTarget = false;
        }

        return btnObj;
    }

    public void SelectRecipe(int index)
    {
        if (recipes == null || index < 0 || index >= recipes.Length)
            return;

        selectedIndex = index;
        CookingRecipe recipe = recipes[index];

        bool locked = IsRecipeLocked(recipe);

        if (recipeName != null)
            recipeName.text = locked ? "??? (Terkunci)" : recipe.recipeName;

        if (recipeDescription != null)
            recipeDescription.text = locked
                ? "Resep ini belum terbuka. Lanjutkan cerita untuk mempelajarinya."
                : recipe.description;

        if (recipeImage != null && recipe.recipeImage != null)
        {
            recipeImage.sprite = recipe.recipeImage;
            recipeImage.color  = locked ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.white;
        }

        if (hpRestoreText != null)
            hpRestoreText.text = locked ? "" : "\u2665 Memulihkan " + recipe.hpRestore + " HP";

        // Tombol masak dimatikan untuk resep terkunci
        if (cookButton != null) cookButton.interactable = !locked;

        UpdateIngredients(locked ? null : recipe);
        HighlightButton(index);
    }

    /// <summary>True kalau resep punya unlockId dan id tsb belum ter-unlock di SaveData.</summary>
    private bool IsRecipeLocked(CookingRecipe recipe)
    {
        if (recipe == null) return false;
        if (string.IsNullOrEmpty(recipe.unlockId)) return false;
        if (GameManager.Instance == null) return false;   // editor/test tanpa save → anggap terbuka
        return !GameManager.Instance.Data.IsRecipeUnlocked(recipe.unlockId);
    }

    void UpdateIngredients(CookingRecipe recipe)
    {
        if (ingredientSlots == null) return;

        // Resep terkunci / null → sembunyikan semua slot bahan
        if (recipe == null)
        {
            for (int i = 0; i < ingredientSlots.Length; i++)
                if (ingredientSlots[i] != null) ingredientSlots[i].SetActive(false);
            return;
        }

        for (int i = 0; i < ingredientSlots.Length; i++)
        {
            if (i < recipe.ingredients.Length)
            {
                ingredientSlots[i].SetActive(true);

                CookingRecipe.Ingredient ing = recipe.ingredients[i];

                if (ingredientIcons != null && i < ingredientIcons.Length &&
                    ingredientIcons[i] != null && ing.itemIcon != null)
                    ingredientIcons[i].sprite = ing.itemIcon;

                if (ingredientNames != null && i < ingredientNames.Length &&
                    ingredientNames[i] != null)
                    ingredientNames[i].text = ing.itemName;

                // Tampilkan jumlah yang dimiliki vs yang dibutuhkan
                if (ingredientAmounts != null && i < ingredientAmounts.Length &&
                    ingredientAmounts[i] != null)
                {
                    int owned = 0;
                    if (InventoryManager.Instance != null)
                        owned = InventoryManager.Instance.GetAmount(ing.itemName);

                    ingredientAmounts[i].text = owned + " / " + ing.amountRequired;

                    // Warna merah jika tidak cukup
                    ingredientAmounts[i].color = (owned >= ing.amountRequired)
                        ? new Color(0.7f, 1f, 0.7f, 1f)   // hijau
                        : new Color(1f, 0.45f, 0.4f, 1f);  // merah
                }
            }
            else
            {
                ingredientSlots[i].SetActive(false);
            }
        }
    }

    void HighlightButton(int index)
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] == null) continue;

            ColorBlock cb = spawnedButtons[i].colors;

            if (i == index)
            {
                // Selected: border terang keemasan
                cb.normalColor = new Color(0.75f, 0.65f, 0.35f);
                cb.highlightedColor = new Color(0.8f, 0.7f, 0.4f);
                cb.pressedColor = new Color(0.65f, 0.55f, 0.3f);
            }
            else
            {
                // Normal: border gelap
                cb.normalColor = new Color(0.22f, 0.22f, 0.25f);
                cb.highlightedColor = new Color(0.4f, 0.4f, 0.42f);
                cb.pressedColor = new Color(0.18f, 0.18f, 0.2f);
            }

            spawnedButtons[i].colors = cb;
        }
    }

    public void OnCookPressed()
    {
        if (recipes == null || selectedIndex < 0 || selectedIndex >= recipes.Length)
            return;

        CookingRecipe recipe = recipes[selectedIndex];

        // Resep terkunci tidak bisa dimasak
        if (IsRecipeLocked(recipe))
        {
            ShowStatus("Resep belum terbuka! Lanjutkan cerita dulu.", false);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            ShowStatus("Inventory tidak ditemukan!", false);
            return;
        }

        // 1. Cek apakah semua bahan cukup
        foreach (var ing in recipe.ingredients)
        {
            if (!InventoryManager.Instance.HasItem(ing.itemName, ing.amountRequired))
            {
                ShowStatus("Bahan tidak cukup! Kurang " + ing.itemName, false);
                return;
            }
        }

        // Mulai minigame memasak!
        StartCookingMinigame(recipe);
    }

    private void StartCookingMinigame(CookingRecipe recipe)
    {
        isMinigameActive = true;
        activeMinigameRecipe = recipe;
        needleProgress = 0f;
        needleDirection = 1f;

        // Atur kecepatan jarum berdasarkan seberapa kuat efek penyembuhan masakan (makin tinggi HP, makin menantang)
        minigameSpeed = 1.3f + (recipe.hpRestore / 80f);

        // Hancurkan overlay minigame lama jika ada
        if (minigameOverlay != null)
        {
            Destroy(minigameOverlay);
        }

        // 1. Buat Container Fullscreen Overlay
        minigameOverlay = new GameObject("CookingMinigameOverlay");
        minigameOverlay.transform.SetParent(this.transform, false);
        RectTransform overlayRect = minigameOverlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image bgImage = minigameOverlay.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.88f); // Latar belakang gelap transparan premium

        // 2. Buat Panel Tengah
        GameObject panel = new GameObject("MinigamePanel");
        panel.transform.SetParent(minigameOverlay.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.25f, 0.35f);
        panelRect.anchorMax = new Vector2(0.75f, 0.65f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.16f, 0.16f, 0.20f, 1f);

        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.75f, 0.65f, 0.35f, 0.5f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        // 3. Teks Judul
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.05f, 0.72f);
        titleRect.anchorMax = new Vector2(0.95f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "MEMASAK: " + recipe.recipeName.ToUpper();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.95f, 0.82f, 0.38f, 1f); // Warna emas khas

        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = Color.black;
        titleOutline.effectDistance = new Vector2(1f, -1f);

        // 4. Bar Pengukur Horizontal (Background)
        GameObject barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(panel.transform, false);
        RectTransform barBgRect = barBgObj.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0.1f, 0.44f);
        barBgRect.anchorMax = new Vector2(0.9f, 0.56f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;

        Image barBgImage = barBgObj.AddComponent<Image>();
        barBgImage.color = new Color(0.08f, 0.08f, 0.1f, 1f);

        Outline barBgOutline = barBgObj.AddComponent<Outline>();
        barBgOutline.effectColor = new Color(0.35f, 0.35f, 0.38f, 0.8f);
        barBgOutline.effectDistance = new Vector2(1f, -1f);

        // 5. Good Zone (Zona Hijau: 45% - 90%)
        GameObject goodZoneObj = new GameObject("GoodZone");
        goodZoneObj.transform.SetParent(barBgObj.transform, false);
        RectTransform goodZoneRect = goodZoneObj.AddComponent<RectTransform>();
        goodZoneRect.anchorMin = new Vector2(0.45f, 0f);
        goodZoneRect.anchorMax = new Vector2(0.90f, 1f);
        goodZoneRect.offsetMin = Vector2.zero;
        goodZoneRect.offsetMax = Vector2.zero;

        Image goodZoneImage = goodZoneObj.AddComponent<Image>();
        goodZoneImage.color = new Color(0.2f, 0.7f, 0.35f, 0.6f);

        // 6. Perfect Zone (Zona Emas: 70% - 85%)
        GameObject perfectZoneObj = new GameObject("PerfectZone");
        perfectZoneObj.transform.SetParent(barBgObj.transform, false);
        RectTransform perfectZoneRect = perfectZoneObj.AddComponent<RectTransform>();
        perfectZoneRect.anchorMin = new Vector2(0.70f, 0f);
        perfectZoneRect.anchorMax = new Vector2(0.85f, 1f);
        perfectZoneRect.offsetMin = Vector2.zero;
        perfectZoneRect.offsetMax = Vector2.zero;

        Image perfectZoneImage = perfectZoneObj.AddComponent<Image>();
        perfectZoneImage.color = new Color(0.98f, 0.72f, 0.2f, 0.9f);

        // 7. Jarum Petunjuk (Needle)
        GameObject needleObj = new GameObject("Needle");
        needleObj.transform.SetParent(barBgObj.transform, false);
        needleRect = needleObj.AddComponent<RectTransform>();
        needleRect.anchorMin = new Vector2(0f, -0.2f);
        needleRect.anchorMax = new Vector2(0.02f, 1.2f);
        needleRect.offsetMin = Vector2.zero;
        needleRect.offsetMax = Vector2.zero;

        Image needleImage = needleObj.AddComponent<Image>();
        needleImage.color = Color.white;

        Outline needleOutline = needleObj.AddComponent<Outline>();
        needleOutline.effectColor = Color.black;
        needleOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // 8. Teks Petunjuk
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(panel.transform, false);
        RectTransform hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.05f, 0.15f);
        hintRect.anchorMax = new Vector2(0.95f, 0.35f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;

        Text hintText = hintObj.AddComponent<Text>();
        hintText.text = "Tekan [SPACE], [G], atau [KLIK] pada ZONA EMAS!";
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 12;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(0.85f, 0.85f, 0.9f, 1f);

        Outline hintOutline = hintObj.AddComponent<Outline>();
        hintOutline.effectColor = Color.black;
        hintOutline.effectDistance = new Vector2(1f, -1f);

        // 9. Teks Hasil (Disembunyikan di awal)
        GameObject resultObj = new GameObject("ResultText");
        resultObj.transform.SetParent(panel.transform, false);
        RectTransform resultRect = resultObj.AddComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.05f, 0.38f);
        resultRect.anchorMax = new Vector2(0.95f, 0.68f);
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;

        Text resultText = resultObj.AddComponent<Text>();
        resultText.text = "";
        resultText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resultText.fontSize = 32;
        resultText.fontStyle = FontStyle.Bold;
        resultText.alignment = TextAnchor.MiddleCenter;
        resultText.raycastTarget = false;
        resultObj.SetActive(false);
    }

    private void EvaluateCookingResult(float finalValue)
    {
        isMinigameActive = false; // Matikan input minigame

        CookingRecipe recipe = activeMinigameRecipe;
        if (recipe == null) return;

        // Evaluasi zona needle
        bool isPerfect = finalValue >= 0.70f && finalValue <= 0.85f;
        bool isGood = finalValue >= 0.45f && finalValue <= 0.90f;

        string resultTitle = "";
        Color resultColor = Color.white;
        int rewardAmount = 0;
        bool isFail = false;

        if (isPerfect)
        {
            resultTitle = "SEMPURNA!";
            resultColor = new Color(0.95f, 0.82f, 0.38f); // Emas keemasan
            rewardAmount = 2; // Bonus 2x masakan untuk Perfect!
        }
        else if (isGood)
        {
            resultTitle = "BERHASIL!";
            resultColor = new Color(0.4f, 1f, 0.4f); // Hijau cerah
            rewardAmount = 1;
        }
        else
        {
            resultTitle = "GOSONG!";
            resultColor = new Color(1f, 0.35f, 0.3f); // Merah membara
            rewardAmount = 1;
            isFail = true;
        }

        // 1. Kurangi bahan-bahan masakan dari inventory
        foreach (var ing in recipe.ingredients)
        {
            InventoryManager.Instance.RemoveItem(ing.itemName, ing.amountRequired);
        }

        // 2. Tambahkan hasil masakan ke inventory
        if (isFail)
        {
            // Berikan makanan gosong dengan sprite
            Sprite burntSprite = null;
#if UNITY_EDITOR
            burntSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Herbal/masakan_gosong.png");
#endif
            InventoryManager.Instance.AddItem("Masakan Gosong", 1, burntSprite);
            ShowStatus("Masakan gosong! Mendapat 1 Masakan Gosong di tas.", false);
        }
        else
        {
            if (recipe.resultItem != null)
            {
                InventoryManager.Instance.AddItem(recipe.resultItem, rewardAmount);
                ShowStatus($"Berhasil memasak {recipe.resultItem.itemName} ({resultTitle})! Jumlah: {rewardAmount}x.", true);
            }
            else
            {
                InventoryManager.Instance.AddItem(recipe.recipeName, rewardAmount, recipe.recipeImage);
                ShowStatus($"Berhasil memasak {recipe.recipeName} ({resultTitle})! Jumlah: {rewardAmount}x.", true);
            }
            OnAnyCooked?.Invoke(recipe.recipeName);
        }

        // 3. Visual feedback di dalam overlay
        if (minigameOverlay != null)
        {
            Transform panelTrans = minigameOverlay.transform.Find("MinigamePanel");
            if (panelTrans != null)
            {
                Transform barTrans = panelTrans.Find("BarBackground");
                if (barTrans != null) barTrans.gameObject.SetActive(false);

                Transform hintTrans = panelTrans.Find("HintText");
                if (hintTrans != null) hintTrans.gameObject.SetActive(false);

                Transform resultTrans = panelTrans.Find("ResultText");
                if (resultTrans != null)
                {
                    resultTrans.gameObject.SetActive(true);
                    Text rText = resultTrans.GetComponent<Text>();
                    rText.text = resultTitle;
                    rText.color = resultColor;

                    // Outline hasil biar makin menonjol
                    Outline outline = resultTrans.gameObject.GetComponent<Outline>();
                    if (outline == null) outline = resultTrans.gameObject.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(2f, -2f);

                    resultTrans.localScale = Vector3.zero;
                    StartCoroutine(AnimateResultPop(resultTrans));
                }
            }
        }

        // 4. Hancurkan minigame overlay setelah 1.5 detik
        StartCoroutine(DestroyMinigameAfterDelay(1.5f));
    }

    private IEnumerator AnimateResultPop(Transform trans)
    {
        float duration = 0.25f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Back-ease-out scale pop effect
            float s = Mathf.Sin(t * Mathf.PI * 0.5f) * 1.2f;
            if (t >= 0.8f) s = 1.0f;
            trans.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        trans.localScale = Vector3.one;
    }

    private IEnumerator DestroyMinigameAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (minigameOverlay != null)
        {
            Destroy(minigameOverlay);
            minigameOverlay = null;
        }

        // Perbarui list bahan setelah stok berubah
        if (recipes != null && selectedIndex >= 0 && selectedIndex < recipes.Length)
        {
            UpdateIngredients(IsRecipeLocked(recipes[selectedIndex]) ? null : recipes[selectedIndex]);
        }
    }

    private void ShowStatus(string message, bool success)
    {
        if (statusMessage != null)
        {
            statusMessage.text = message;
            statusMessage.color = success
                ? new Color(0.5f, 1f, 0.5f, 1f)   // hijau
                : new Color(1f, 0.4f, 0.4f, 1f);   // merah
        }

        Debug.Log($"[Cooking] {message}");

        // Auto-hide status setelah 2 detik
        StopAllCoroutines();
        StartCoroutine(HideStatusAfter(2f));
    }

    private IEnumerator HideStatusAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (statusMessage != null)
            statusMessage.text = "";
    }

    public void OnClosePressed()
    {
        if (cookingTrigger != null)
            cookingTrigger.CloseCooking();
    }
}
