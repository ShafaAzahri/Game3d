using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menampilkan title card "Prolog Selesai • Chapter 1 Dimulai" dll di tengah layar,
/// lalu fade out otomatis. Dipanggil oleh QuestManager saat perpindahan chapter.
///
/// SETUP:
/// 1. Buat Panel full-screen di Canvas (hitam transparan / backdrop)
/// 2. Di dalam panel, buat 2 TMP_Text: titleText (atas) dan subtitleText (bawah)
/// 3. Assign ke Inspector ChapterTitleUI
/// 4. Drag ChapterTitleUI ke QuestManager.chapterTitleUI
/// </summary>
public class ChapterTitleUI : MonoBehaviour
{
    public static ChapterTitleUI Instance { get; private set; }

    [Header("UI Elements")]
    public CanvasGroup canvasGroup;
    public TMP_Text titleText;       // "PROLOG SELESAI"
    public TMP_Text subtitleText;    // "Chapter 1 Dimulai"

    [Header("Timing")]
    public float fadeInDuration  = 0.5f;
    public float holdDuration    = 2.5f;
    public float fadeOutDuration = 1.0f;

    private Coroutine showCoroutine;

    void Awake()
    {
        Instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Tampilkan title card. Contoh: Show("Prolog Selesai", "Chapter 1 Dimulai")
    /// </summary>
    public void Show(string title, string subtitle = "")
    {
        gameObject.SetActive(true);
        if (showCoroutine != null)
        {
            if (QuestManager.Instance != null) QuestManager.Instance.StopCoroutine(showCoroutine);
            else if (GameManager.Instance != null) GameManager.Instance.StopCoroutine(showCoroutine);
            else StopCoroutine(showCoroutine);
        }

        if (QuestManager.Instance != null)
            showCoroutine = QuestManager.Instance.StartCoroutine(ShowRoutine(title, subtitle));
        else if (GameManager.Instance != null)
            showCoroutine = GameManager.Instance.StartCoroutine(ShowRoutine(title, subtitle));
        else
            showCoroutine = StartCoroutine(ShowRoutine(title, subtitle));
    }

    private IEnumerator ShowRoutine(string title, string subtitle)
    {

        if (titleText != null) titleText.text = title;
        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }

        // Fade in
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
    }
}
