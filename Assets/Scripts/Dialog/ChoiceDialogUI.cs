using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI pilihan ganda (mis. pilih pacar di Chapter 3).
/// Tampil di tengah layar dengan 3 tombol + teks pertanyaan.
/// Dipanggil oleh QuestManager saat step bertipe Choice.
///
/// SETUP:
/// 1. Buat panel di Canvas: ChoicePanel (background gelap)
/// 2. Di dalamnya: questionText (TMP), 3 Button dengan TMP_Text anak
/// 3. Assign semuanya di Inspector
/// 4. Drag ChoiceDialogUI ke QuestManager (atau panggil via script)
/// </summary>
public class ChoiceDialogUI : MonoBehaviour
{
    private static ChoiceDialogUI instance;
    public static ChoiceDialogUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindAnyObjectByType<ChoiceDialogUI>();
                if (instance == null)
                {
                    GameObject go = new GameObject("ChoiceDialogUI_Fallback");
                    instance = go.AddComponent<ChoiceDialogUI>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("UI")]
    public GameObject choicePanel;
    public TMP_Text questionText;
    public Button[] choiceButtons;      // 3 tombol (atau lebih)
    public TMP_Text[] choiceLabels;     // teks di tombol

    private string currentGroup;
    private System.Action<string> customCallback;

    private bool useFallbackGUI = false;
    private string fallbackQuestion;
    private string[] fallbackOptions;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    /// <summary>
    /// Tampilkan dialog pilihan dengan custom callback (untuk non-QuestManager choice).
    /// </summary>
    public void Show(string question, string[] options, System.Action<string> onChoice)
    {
        customCallback = onChoice;
        currentGroup = "custom_callback";

        // Setup UI
        if (choicePanel == null || questionText == null || choiceButtons == null || choiceButtons.Length == 0)
        {
            useFallbackGUI = true;
            fallbackQuestion = question;
            fallbackOptions = options;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        useFallbackGUI = false;
        if (questionText != null) questionText.text = question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                if (i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = options[i];

                int idx = i;
                string opt = options[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceMade(opt));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        if (choicePanel != null) choicePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Tampilkan dialog pilihan.
    /// choiceGroup = id untuk simpan pilihan (mis. "pacar").
    /// question = teks pertanyaan.
    /// options = label tombol (mis. {"Laras","Nisa","Ratri"}).
    /// </summary>
    public void Show(string choiceGroup, string question, string[] options)
    {
        customCallback = null;
        currentGroup = choiceGroup;

        // Jika choicePanel atau referensi lainnya belum di-setup di Inspector, gunakan fallback IMGUI
        if (choicePanel == null || questionText == null || choiceButtons == null || choiceButtons.Length == 0)
        {
            useFallbackGUI = true;
            fallbackQuestion = question;
            fallbackOptions = options;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        useFallbackGUI = false;
        if (questionText != null) questionText.text = question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < options.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                if (i < choiceLabels.Length && choiceLabels[i] != null)
                    choiceLabels[i].text = options[i];

                int idx = i;
                string opt = options[i];
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceMade(opt));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        if (choicePanel != null) choicePanel.SetActive(true);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnChoiceMade(string option)
    {
        Debug.Log($"[ChoiceDialogUI] Player memilih: '{option}' (group: {currentGroup})");

        if (choicePanel != null) choicePanel.SetActive(false);

        // Lock cursor kembali
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Custom callback
        if (currentGroup == "custom_callback")
        {
            customCallback?.Invoke(option);
            return;
        }

        // Lapor ke QuestManager
        if (QuestManager.Instance != null)
            QuestManager.Instance.NotifyChoice(currentGroup, option);
    }

    void OnGUI()
    {
        if (!useFallbackGUI) return;

        // Gambar background panel di tengah layar
        Rect rect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 120, 400, 240);
        
        // Render box luar
        GUI.Box(rect, "PILIHAN PASANGAN");

        // Pertanyaan
        GUIStyle questionStyle = new GUIStyle(GUI.skin.label);
        questionStyle.alignment = TextAnchor.MiddleCenter;
        questionStyle.fontStyle = FontStyle.Bold;
        questionStyle.fontSize = 16;
        GUI.Label(new Rect(rect.x + 20, rect.y + 40, rect.width - 40, 50), fallbackQuestion, questionStyle);

        // Render tombol-tombol pilihan
        float startY = rect.y + 100;
        for (int i = 0; i < fallbackOptions.Length; i++)
        {
            Rect btnRect = new Rect(rect.x + 50, startY + (i * 40), rect.width - 100, 30);
            if (GUI.Button(btnRect, fallbackOptions[i]))
            {
                useFallbackGUI = false;
                OnChoiceMade(fallbackOptions[i]);
            }
        }
    }
}
