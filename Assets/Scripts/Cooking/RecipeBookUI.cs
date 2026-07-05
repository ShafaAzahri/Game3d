using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Buku Resep yang bisa dibuka kapan saja dengan Tab.
/// Menampilkan daftar semua resep (terbuka & terkunci) + detail bahan.
/// Tidak bisa memasak dari sini — hanya lihat referensi.
///
/// SETUP:
/// 1. Buat panel "RecipeBookPanel" di Canvas (full screen atau 80%)
/// 2. Isi references di Inspector
/// 3. Script ini toggle panel saat Tab ditekan
/// </summary>
public class RecipeBookUI : MonoBehaviour
{
    public static RecipeBookUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject bookPanel;

    [Header("Recipe List (Kiri)")]
    public RectTransform listContainer;
    public GameObject recipeButtonPrefab;

    [Header("Detail (Kanan)")]
    public TMP_Text recipeNameText;
    public TMP_Text recipeDescText;
    public Image recipeImage;
    public TMP_Text ingredientsText;
    public TMP_Text lockStatusText;

    [Header("Data")]
    [Tooltip("Semua resep yang ada di game (sama kayak di CookingUI).")]
    public CookingRecipe[] allRecipes;

    private bool isOpen = false;
    private List<GameObject> spawnedButtons = new List<GameObject>();
    private PlayerController playerController;

    void Awake()
    {
        Instance = this;
        if (bookPanel != null) bookPanel.SetActive(false);
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        // Tab toggle
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) Close();
            else Open();
        }

        // Escape tutup
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Open()
    {
        if (isOpen) return;
        if (DialogManager.Instance != null && DialogManager.Instance.IsDialogActive) return;

        isOpen = true;
        if (bookPanel != null) bookPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (playerController != null) playerController.CanMove = false;

        PopulateList();
        if (allRecipes != null && allRecipes.Length > 0)
            SelectRecipe(0);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (bookPanel != null) bookPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        if (playerController != null) playerController.CanMove = true;
    }

    public bool IsOpen => isOpen;

    private void PopulateList()
    {
        // Hapus button lama
        foreach (var go in spawnedButtons)
            if (go != null) Destroy(go);
        spawnedButtons.Clear();

        if (listContainer == null || allRecipes == null) return;

        for (int i = 0; i < allRecipes.Length; i++)
        {
            var recipe = allRecipes[i];
            if (recipe == null) continue;

            bool locked = IsLocked(recipe);

            GameObject btnObj;
            if (recipeButtonPrefab != null)
            {
                btnObj = Instantiate(recipeButtonPrefab, listContainer);
            }
            else
            {
                // Fallback: buat button sederhana
                btnObj = new GameObject("RecipeBtn_" + i);
                btnObj.transform.SetParent(listContainer, false);
                var rt = btnObj.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 40);
                var img = btnObj.AddComponent<Image>();
                img.color = locked ? new Color(0.2f, 0.2f, 0.22f) : new Color(0.3f, 0.3f, 0.35f);
                var btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;

                var txtObj = new GameObject("Label");
                txtObj.transform.SetParent(btnObj.transform, false);
                var txtRt = txtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = new Vector2(10, 0);
                txtRt.offsetMax = new Vector2(-10, 0);
                var tmp = txtObj.AddComponent<TextMeshProUGUI>();
                tmp.text = locked ? "??? (Terkunci)" : recipe.recipeName;
                tmp.fontSize = 16;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = locked ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
            }

            int idx = i;
            var button = btnObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectRecipe(idx));
            }

            spawnedButtons.Add(btnObj);
        }
    }

    public void SelectRecipe(int index)
    {
        if (allRecipes == null || index < 0 || index >= allRecipes.Length) return;
        var recipe = allRecipes[index];
        bool locked = IsLocked(recipe);

        if (recipeNameText != null)
            recipeNameText.text = locked ? "??? (Terkunci)" : recipe.recipeName;

        if (recipeDescText != null)
            recipeDescText.text = locked
                ? "Resep ini belum terbuka.\nLanjutkan cerita untuk mempelajarinya."
                : recipe.description;

        if (recipeImage != null)
        {
            if (recipe.recipeImage != null)
            {
                recipeImage.sprite = recipe.recipeImage;
                recipeImage.color = locked ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
                recipeImage.gameObject.SetActive(true);
            }
            else
                recipeImage.gameObject.SetActive(false);
        }

        if (ingredientsText != null)
        {
            if (locked)
            {
                ingredientsText.text = "";
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<b>Bahan:</b>");
                foreach (var ing in recipe.ingredients)
                    sb.AppendLine($"  • {ing.itemName} x{ing.amountRequired}");
                ingredientsText.text = sb.ToString();
            }
        }

        if (lockStatusText != null)
            lockStatusText.text = locked ? "\uD83D\uDD12 TERKUNCI" : "\u2705 TERBUKA";
    }

    private bool IsLocked(CookingRecipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.unlockId)) return false;
        if (GameManager.Instance == null) return false;
        return !GameManager.Instance.Data.IsRecipeUnlocked(recipe.unlockId);
    }
}
