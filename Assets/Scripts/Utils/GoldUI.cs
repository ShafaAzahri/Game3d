using UnityEngine;
using TMPro;

/// <summary>
/// Tampilkan jumlah gold player di layar (pojok kanan atas).
/// Auto-update saat dipanggil Refresh() atau setiap beberapa detik.
/// </summary>
public class GoldUI : MonoBehaviour
{
    public static GoldUI Instance { get; private set; }

    [Header("UI")]
    public TMP_Text goldText;

    private float refreshTimer;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        // Auto refresh tiap 2 detik (fallback)
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= 2f)
        {
            refreshTimer = 0f;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (goldText == null) return;
        int money = (GameManager.Instance != null) ? GameManager.Instance.Data.money : 0;
        goldText.text = money.ToString("N0") + " G";
    }
}
