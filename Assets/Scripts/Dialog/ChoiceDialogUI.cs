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
    public static ChoiceDialogUI Instance { get; private set; }

    [Header("UI")]
    public GameObject choicePanel;
    public TMP_Text questionText;
    public Button[] choiceButtons;      // 3 tombol (atau lebih)
    public TMP_Text[] choiceLabels;     // teks di tombol

    private string currentGroup;

    void Awake()
    {
        Instance = this;
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    /// <summary>
    /// Tampilkan dialog pilihan.
    /// choiceGroup = id untuk simpan pilihan (mis. "pacar").
    /// question = teks pertanyaan.
    /// options = label tombol (mis. {"Laras","Nisa","Ratri"}).
    /// </summary>
    public void Show(string choiceGroup, string question, string[] options)
    {
        currentGroup = choiceGroup;

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

        // Lapor ke QuestManager
        if (QuestManager.Instance != null)
            QuestManager.Instance.NotifyChoice(currentGroup, option);
    }
}
