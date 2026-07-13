using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Popup notifikasi reward sederhana (fade in → tahan → fade out).
/// Dipanggil: RewardPopup.Instance.Show("Reward: Buku Resep Lv.1 terbuka!");
/// </summary>
public class RewardPopup : MonoBehaviour
{
    public static RewardPopup Instance { get; private set; }

    [Header("Refs")]
    public CanvasGroup group;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Timing")]
    public float fadeIn = 0.4f;
    public float hold = 3.5f;
    public float fadeOut = 0.6f;

    private void Awake()
    {
        Instance = this;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null) group.alpha = 0f;
    }

    /// <summary>Tampilkan reward. title opsional.</summary>
    public void Show(string body, string title = "REWARD")
    {
        gameObject.SetActive(true);
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        if (group == null) yield break;

        // Fade in
        for (float t = 0f; t < fadeIn; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(0f, 1f, t / fadeIn);
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(hold);

        // Fade out
        for (float t = 0f; t < fadeOut; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(1f, 0f, t / fadeOut);
            yield return null;
        }
        group.alpha = 0f;
    }
}
