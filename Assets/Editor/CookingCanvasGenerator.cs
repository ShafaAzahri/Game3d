using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor Tool untuk generate Canvas Memasak.
/// Hasilnya permanen di Hierarchy (tidak hilang saat stop Play).
/// 
/// CARA PAKAI:
/// Menu bar → Tools → Cooking → Generate Cooking Canvas
/// 
/// Canvas akan dibuat sebagai child dari Canvas yang sudah ada di scene.
/// Kalau belum ada Canvas, akan dibuatkan baru.
/// </summary>
public class CookingCanvasGenerator : EditorWindow
{
    private Sprite bgSprite;

    [MenuItem("Tools/Cooking/Generate Cooking Canvas")]
    static void ShowWindow()
    {
        GetWindow<CookingCanvasGenerator>("Cooking Canvas Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Cooking Canvas Generator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        bgSprite = (Sprite)EditorGUILayout.ObjectField(
            "Background Sprite (bg masak)",
            bgSprite,
            typeof(Sprite),
            false
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Canvas Memasak", GUILayout.Height(40)))
        {
            GenerateCookingCanvas();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Prompt [G] Masak", GUILayout.Height(30)))
        {
            GeneratePrompt();
        }

        GUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "1. Assign sprite 'bg masak' dari Assets/character/mc/\n" +
            "2. Klik 'Generate Canvas Memasak'\n" +
            "3. Klik 'Generate Prompt [G] Masak'\n" +
            "4. Canvas akan muncul permanen di Hierarchy\n" +
            "5. Assign reference di CookingTrigger (di Tungku)",
            MessageType.Info
        );
    }

    void GenerateCookingCanvas()
    {
        // Cari Canvas yang sudah ada
        Canvas existingCanvas = FindObjectOfType<Canvas>();

        Transform parentTransform;

        if (existingCanvas != null)
        {
            parentTransform = existingCanvas.transform;
        }
        else
        {
            // Buat Canvas baru
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            parentTransform = canvasObj.transform;

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
        }

        // =========================
        // PANEL MEMASAK (root container)
        // =========================
        GameObject cookingPanel = CreateUIElement("PanelMemasak", parentTransform);
        RectTransform panelRect = cookingPanel.GetComponent<RectTransform>();
        SetAnchorsStretch(panelRect);

        // Background image
        Image bgImage = cookingPanel.AddComponent<Image>();
        if (bgSprite != null)
        {
            bgImage.sprite = bgSprite;
            bgImage.type = Image.Type.Sliced;
        }
        else
        {
            bgImage.color = new Color(0.25f, 0.18f, 0.12f, 0.97f);
        }

        // =========================
        // TITLE "MEMASAK"
        // =========================
        GameObject titleObj = CreateUIElement("TxtTitle", cookingPanel.transform);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.3f, 0.89f);
        titleRect.anchorMax = new Vector2(0.7f, 0.97f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "MEMASAK";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.95f, 0.85f);
        AddOutline(titleObj);

        // =========================
        // CLOSE BUTTON (X)
        // =========================
        GameObject closeBtn = CreateButton("BtnClose", cookingPanel.transform, "X");
        RectTransform closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.93f, 0.91f);
        closeRect.anchorMax = new Vector2(0.98f, 0.98f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        closeBtn.GetComponent<Image>().color = new Color(0.7f, 0.15f, 0.15f);
        Text closeTxt = closeBtn.GetComponentInChildren<Text>();
        closeTxt.fontSize = 26;
        closeTxt.fontStyle = FontStyle.Bold;
        closeTxt.color = Color.white;

        // =========================
        // LEFT PANEL - RESEP MASAKAN
        // =========================
        GameObject leftPanel = CreateUIElement("LeftPanel", cookingPanel.transform);
        RectTransform leftRect = leftPanel.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0.04f, 0.1f);
        leftRect.anchorMax = new Vector2(0.4f, 0.87f);
        leftRect.offsetMin = Vector2.zero;
        leftRect.offsetMax = Vector2.zero;
        // Image transparan supaya RectTransform punya ukuran
        Image leftBg = leftPanel.AddComponent<Image>();
        leftBg.color = new Color(0, 0, 0, 0);

        // Label "RESEP MASAKAN"
        GameObject labelResep = CreateUIElement("TxtResepLabel", leftPanel.transform);
        RectTransform labelRect = labelResep.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0.9f);
        labelRect.anchorMax = new Vector2(0.95f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        Text labelText = labelResep.AddComponent<Text>();
        labelText.text = "RESEP MASAKAN";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(1f, 0.95f, 0.85f);
        AddOutline(labelResep);

        // =========================
        // RIGHT PANEL - DETAIL
        // =========================
        GameObject rightPanel = CreateUIElement("RightPanel", cookingPanel.transform);
        RectTransform rightRect = rightPanel.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.42f, 0.1f);
        rightRect.anchorMax = new Vector2(0.96f, 0.87f);
        rightRect.offsetMin = Vector2.zero;
        rightRect.offsetMax = Vector2.zero;

        // Nama Resep
        GameObject nameObj = CreateUIElement("TxtRecipeName", rightPanel.transform);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.03f, 0.87f);
        nameRect.anchorMax = new Vector2(0.55f, 0.98f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = "Nasi Goreng";
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 30;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = new Color(0.2f, 0.15f, 0.1f);

        // Deskripsi
        GameObject descObj = CreateUIElement("TxtRecipeDesc", rightPanel.transform);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0.03f, 0.8f);
        descRect.anchorMax = new Vector2(0.55f, 0.88f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        Text descText = descObj.AddComponent<Text>();
        descText.text = "Nasi goreng spesial desa.";
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 17;
        descText.alignment = TextAnchor.MiddleLeft;
        descText.color = new Color(0.35f, 0.3f, 0.25f);

        // Gambar Makanan
        GameObject foodImgObj = CreateUIElement("ImgFood", rightPanel.transform);
        RectTransform foodRect = foodImgObj.GetComponent<RectTransform>();
        foodRect.anchorMin = new Vector2(0.6f, 0.68f);
        foodRect.anchorMax = new Vector2(0.95f, 0.98f);
        foodRect.offsetMin = Vector2.zero;
        foodRect.offsetMax = Vector2.zero;
        Image foodImg = foodImgObj.AddComponent<Image>();
        foodImg.color = new Color(0.9f, 0.85f, 0.75f);

        // HP Restore
        GameObject hpObj = CreateUIElement("TxtHPRestore", rightPanel.transform);
        RectTransform hpRect = hpObj.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0.03f, 0.72f);
        hpRect.anchorMax = new Vector2(0.55f, 0.8f);
        hpRect.offsetMin = Vector2.zero;
        hpRect.offsetMax = Vector2.zero;
        Text hpText = hpObj.AddComponent<Text>();
        hpText.text = "\u2665 Memulihkan 50 HP";
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 19;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleLeft;
        hpText.color = new Color(0.15f, 0.55f, 0.15f);

        // Label "BAHAN YANG DIBUTUHKAN:"
        GameObject bahanLabel = CreateUIElement("TxtBahanLabel", rightPanel.transform);
        RectTransform bahanLabelRect = bahanLabel.GetComponent<RectTransform>();
        bahanLabelRect.anchorMin = new Vector2(0.03f, 0.63f);
        bahanLabelRect.anchorMax = new Vector2(0.7f, 0.71f);
        bahanLabelRect.offsetMin = Vector2.zero;
        bahanLabelRect.offsetMax = Vector2.zero;
        Text bahanLabelText = bahanLabel.AddComponent<Text>();
        bahanLabelText.text = "BAHAN YANG DIBUTUHKAN:";
        bahanLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bahanLabelText.fontSize = 17;
        bahanLabelText.fontStyle = FontStyle.Bold;
        bahanLabelText.alignment = TextAnchor.MiddleLeft;
        bahanLabelText.color = new Color(0.2f, 0.15f, 0.1f);

        // Ingredient Slots (4 buah)
        string[] ingredients = { "Beras", "Telur", "Bawang", "Minyak" };
        string[] amounts = { "1 / 1", "2 / 2", "1 / 1", "1 / 1" };

        for (int i = 0; i < 4; i++)
        {
            float yMax = 0.62f - (i * 0.1f);
            float yMin = yMax - 0.09f;
            CreateIngredientSlot("IngredientSlot_" + i, rightPanel.transform, ingredients[i], amounts[i], yMin, yMax);
        }

        // Tombol MASAK
        GameObject cookBtn = CreateButton("BtnMasak", rightPanel.transform, "MASAK");
        RectTransform cookBtnRect = cookBtn.GetComponent<RectTransform>();
        cookBtnRect.anchorMin = new Vector2(0.15f, 0.02f);
        cookBtnRect.anchorMax = new Vector2(0.85f, 0.12f);
        cookBtnRect.offsetMin = Vector2.zero;
        cookBtnRect.offsetMax = Vector2.zero;
        cookBtn.GetComponent<Image>().color = new Color(0.5f, 0.32f, 0.12f);
        Text cookTxt = cookBtn.GetComponentInChildren<Text>();
        cookTxt.fontSize = 26;
        cookTxt.fontStyle = FontStyle.Bold;
        cookTxt.color = new Color(1f, 0.95f, 0.85f);

        // =========================
        // BOTTOM BAR
        // =========================
        GameObject bottomBar = CreateUIElement("TxtBottomBar", cookingPanel.transform);
        RectTransform bottomRect = bottomBar.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.2f, 0.02f);
        bottomRect.anchorMax = new Vector2(0.8f, 0.08f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;
        Text bottomText = bottomBar.AddComponent<Text>();
        bottomText.text = "ESC  Kembali     |     G  Masak";
        bottomText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bottomText.fontSize = 19;
        bottomText.alignment = TextAnchor.MiddleCenter;
        bottomText.color = new Color(1f, 0.95f, 0.85f);
        AddOutline(bottomBar);

        // =========================
        // ATTACH CookingUI
        // =========================
        CookingUI cookingUI = cookingPanel.AddComponent<CookingUI>();

        // Auto-assign references
        cookingUI.leftPanel = leftRect;
        cookingUI.recipeName = nameText;
        cookingUI.recipeDescription = descText;
        cookingUI.recipeImage = foodImg;
        cookingUI.hpRestoreText = hpText;
        cookingUI.cookButton = cookBtn.GetComponent<Button>();
        cookingUI.closeButton = closeBtn.GetComponent<Button>();

        // Ingredient references
        GameObject[] slots = new GameObject[4];
        Image[] icons = new Image[4];
        Text[] names = new Text[4];
        Text[] amts = new Text[4];

        for (int i = 0; i < 4; i++)
        {
            Transform slot = rightPanel.transform.Find("IngredientSlot_" + i);
            slots[i] = slot.gameObject;
            icons[i] = slot.Find("Icon").GetComponent<Image>();
            names[i] = slot.Find("Name").GetComponent<Text>();
            amts[i] = slot.Find("Amount").GetComponent<Text>();
        }

        cookingUI.ingredientSlots = slots;
        cookingUI.ingredientIcons = icons;
        cookingUI.ingredientNames = names;
        cookingUI.ingredientAmounts = amts;

        // Default: nonaktif
        cookingPanel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(cookingPanel, "Generate Cooking Canvas");
        Selection.activeGameObject = cookingPanel;

        Debug.Log("Canvas Memasak berhasil di-generate! (PanelMemasak di Hierarchy)");
        EditorUtility.DisplayDialog("Selesai", "Canvas Memasak berhasil dibuat!\n\nCari 'PanelMemasak' di Hierarchy.\nAssign ke CookingTrigger.cookingCanvas", "OK");
    }

    void GeneratePrompt()
    {
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas == null)
        {
            EditorUtility.DisplayDialog("Error", "Tidak ada Canvas di scene! Buat Canvas dulu atau generate Cooking Canvas.", "OK");
            return;
        }

        // Buat prompt container
        GameObject container = CreateUIElement("PromptMasak", existingCanvas.transform);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.38f, 0.13f);
        containerRect.anchorMax = new Vector2(0.62f, 0.2f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Background hitam transparan
        Image bg = container.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        // Text
        GameObject textObj = CreateUIElement("TxtPrompt", container.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        SetAnchorsStretch(textRect);
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        Text text = textObj.AddComponent<Text>();
        text.text = "[G]  Masak";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        AddOutline(textObj);

        container.SetActive(false);

        Undo.RegisterCreatedObjectUndo(container, "Generate Cooking Prompt");
        Selection.activeGameObject = container;

        Debug.Log("Prompt [G] Masak berhasil di-generate! (PromptMasak di Hierarchy)");
        EditorUtility.DisplayDialog("Selesai", "Prompt '[G] Masak' berhasil dibuat!\n\nCari 'PromptMasak' di Hierarchy.\nAssign ke CookingTrigger.promptUI", "OK");
    }

    // =========================
    // HELPER METHODS
    // =========================

    GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    GameObject CreateButton(string name, Transform parent, string label)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        btnObj.AddComponent<RectTransform>();

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.4f, 0.3f, 0.2f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;

        // Label text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        SetAnchorsStretch(textRect);

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        return btnObj;
    }

    GameObject CreateScrollView(string name, Transform parent)
    {
        // Root
        GameObject scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);
        scrollObj.AddComponent<RectTransform>();

        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.05f);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform viewRect = viewport.AddComponent<RectTransform>();
        SetAnchorsStretch(viewRect);

        Image viewImg = viewport.AddComponent<Image>();
        viewImg.color = new Color(1, 1, 1, 0);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        // Grid Layout
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(130, 160);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.UpperCenter;

        // Content Size Fitter (supaya scroll jalan)
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Assign
        scrollRect.viewport = viewRect;
        scrollRect.content = contentRect;

        return scrollObj;
    }

    void CreateIngredientSlot(string name, Transform parent, string ingredientName, string amount, float yMin, float yMax)
    {
        GameObject slot = CreateUIElement(name, parent);
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0.03f, yMin);
        slotRect.anchorMax = new Vector2(0.88f, yMax);
        slotRect.offsetMin = Vector2.zero;
        slotRect.offsetMax = Vector2.zero;

        Image slotBg = slot.AddComponent<Image>();
        slotBg.color = new Color(0.92f, 0.87f, 0.78f, 0.4f);

        // Icon
        GameObject iconObj = CreateUIElement("Icon", slot.transform);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.02f, 0.1f);
        iconRect.anchorMax = new Vector2(0.14f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.color = new Color(0.75f, 0.68f, 0.58f);

        // Name
        GameObject nameObj = CreateUIElement("Name", slot.transform);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.16f, 0.1f);
        nameRect.anchorMax = new Vector2(0.7f, 0.9f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        Text nameText = nameObj.AddComponent<Text>();
        nameText.text = ingredientName;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.color = new Color(0.2f, 0.15f, 0.1f);

        // Amount
        GameObject amountObj = CreateUIElement("Amount", slot.transform);
        RectTransform amountRect = amountObj.GetComponent<RectTransform>();
        amountRect.anchorMin = new Vector2(0.72f, 0.1f);
        amountRect.anchorMax = new Vector2(0.98f, 0.9f);
        amountRect.offsetMin = Vector2.zero;
        amountRect.offsetMax = Vector2.zero;
        Text amountText = amountObj.AddComponent<Text>();
        amountText.text = amount;
        amountText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        amountText.fontSize = 19;
        amountText.fontStyle = FontStyle.Bold;
        amountText.alignment = TextAnchor.MiddleRight;
        amountText.color = new Color(0.15f, 0.5f, 0.15f);
    }

    void SetAnchorsStretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void AddOutline(GameObject obj)
    {
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.6f);
        outline.effectDistance = new Vector2(1, -1);
    }
}
