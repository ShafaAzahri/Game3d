using UnityEngine;
using TMPro;

/// <summary>
/// Singleton UI untuk menampilkan prompt aksi bertani.
/// Otomatis membuat UI-nya sendiri di Canvas yang sudah ada.
///
/// Dipanggil oleh GardenPlot: FarmingPromptUI.Instance.Show("[F] Cangkul")
/// </summary>
public class FarmingPromptUI : MonoBehaviour
{
    public static FarmingPromptUI Instance { get; private set; }

    private GameObject promptPanel;
    private TMP_Text   promptText;
    private bool       isVisible = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    // ─────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────

    public void Show(string text)
    {
        if (promptPanel == null) return;
        promptText.text = text;
        if (!isVisible) { promptPanel.SetActive(true); isVisible = true; }
    }

    public void Hide()
    {
        if (promptPanel == null || !isVisible) return;
        promptPanel.SetActive(false);
        isVisible = false;
    }

    // ─────────────────────────────────────────
    // BUILD UI (otomatis)
    // ─────────────────────────────────────────

    private void BuildUI()
    {
        // Cari Canvas yang ada
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[FarmingPromptUI] Canvas tidak ditemukan!");
            return;
        }

        // Panel background
        promptPanel = new GameObject("FarmingPrompt");
        promptPanel.transform.SetParent(canvas.transform, false);

        var rect = promptPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.08f);
        rect.anchorMax = new Vector2(0.5f, 0.08f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(280f, 44f);
        rect.anchoredPosition = Vector2.zero;

        // Background pill shape
        var bg = promptPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.78f);

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(promptPanel.transform, false);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);

        promptText = textGO.AddComponent<TMP_Text>() as TMP_Text;
        // Use the TMP default component
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        promptText = tmp;

        tmp.text      = "";
        tmp.fontSize  = 16f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = new Color(1f, 0.95f, 0.7f, 1f); // warna kuning hangat

        promptPanel.SetActive(false);
    }
}
