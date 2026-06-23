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
        SetupRecipeButtons();
        SelectRecipe(0);
    }

    void Update()
    {
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
        }
        else
        {
            recipeImg.color = new Color(0.45f, 0.42f, 0.4f, 1f);
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
        text.text = recipe.recipeName;
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

        return btnObj;
    }

    public void SelectRecipe(int index)
    {
        if (recipes == null || index < 0 || index >= recipes.Length)
            return;

        selectedIndex = index;
        CookingRecipe recipe = recipes[index];

        if (recipeName != null)
            recipeName.text = recipe.recipeName;

        if (recipeDescription != null)
            recipeDescription.text = recipe.description;

        if (recipeImage != null && recipe.recipeImage != null)
            recipeImage.sprite = recipe.recipeImage;

        if (hpRestoreText != null)
            hpRestoreText.text = "\u2665 Memulihkan " + recipe.hpRestore + " HP";

        UpdateIngredients(recipe);
        HighlightButton(index);
    }

    void UpdateIngredients(CookingRecipe recipe)
    {
        if (ingredientSlots == null) return;

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

        // 2. Kurangi semua bahan dari inventory
        foreach (var ing in recipe.ingredients)
        {
            InventoryManager.Instance.RemoveItem(ing.itemName, ing.amountRequired);
        }

        // 3. Tambah hasil masakan ke inventory
        if (recipe.resultItem != null)
        {
            InventoryManager.Instance.AddItem(recipe.resultItem, 1);
            Debug.Log($"[Cooking] Berhasil memasak {recipe.resultItem.itemName}!");
        }
        else
        {
            // Fallback: gunakan recipeName + recipeImage
            InventoryManager.Instance.AddItem(recipe.recipeName, 1, recipe.recipeImage);
            Debug.Log($"[Cooking] Berhasil memasak {recipe.recipeName}!");
        }

        // 4. Tampilkan pesan sukses
        ShowStatus("Berhasil memasak " + recipe.recipeName + "! (+" + recipe.hpRestore + " HP)", true);
        OnAnyCooked?.Invoke(recipe.recipeName);

        // 5. Update tampilan ingredient (stok berubah)
        UpdateIngredients(recipe);
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
