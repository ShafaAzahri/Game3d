using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel 2 tombol pilihan yang muncul setelah dialog selesai.
/// Contoh: "Lihat Item Toko" vs "Tinggalkan".
/// Singleton — 1 panel global, dipakai semua NPC yang butuh.
///
/// SETUP:
/// 1. Buat panel "PostDialogChoice" di Canvas (tengah bawah layar)
/// 2. Assign 2 Button + TMP_Text
/// 3. Script ini di Canvas (selalu aktif)
/// </summary>
public class PostDialogChoiceUI : MonoBehaviour
{
    public static PostDialogChoiceUI Instance { get; private set; }

    [Header("UI")]
    public GameObject choicePanel;
    public Button button1;
    public Button button2;
    public TMP_Text label1;
    public TMP_Text label2;

    private System.Action callback1;
    private System.Action callback2;

    void Awake()
    {
        Instance = this;
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    /// <summary>
    /// Tampilkan 2 pilihan. callback null = tutup panel saja.
    /// </summary>
    public void Show(string text1, string text2, System.Action onChoice1, System.Action onChoice2)
    {
        callback1 = onChoice1;
        callback2 = onChoice2;

        if (label1 != null) label1.text = text1;
        if (label2 != null) label2.text = text2;

        if (button1 != null) { button1.onClick.RemoveAllListeners(); button1.onClick.AddListener(OnBtn1); }
        if (button2 != null) { button2.onClick.RemoveAllListeners(); button2.onClick.AddListener(OnBtn2); }

        if (choicePanel != null) choicePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnBtn1()
    {
        Hide();
        callback1?.Invoke();
    }

    private void OnBtn2()
    {
        Hide();
        callback2?.Invoke();
    }

    private void Hide()
    {
        if (choicePanel != null) choicePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
