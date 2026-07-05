using UnityEngine;

/// <summary>
/// Pasang di GameObject NPC (Nenek MC, dll.).
/// Tag NPC harus sudah diset di Inspector.
///
/// Cara pakai:
/// - Tambah DialogLine baru di array "dialogLines"
/// - Set speakerName, subtitle, text, isPlayerLine
/// - Susun urutan NPC → MC → NPC dst.
/// - Tekan G untuk mulai dan lanjut dialog
/// </summary>
public class NPCDialog : MonoBehaviour
{
    [Header("Identitas NPC")]
    [Tooltip("ID NPC untuk dicocokkan oleh QuestManager pada objektif 'talk'. " +
             "Contoh: 'Nenek', 'Laras', 'Nisa', 'Sekar', 'Bahri', 'Ratri', 'Darma', 'Darsono'.")]
    public string npcId = "Nenek";

    [Header("Healing Item (Chapter 2 — Counter Quest)")]
    [Tooltip("Item jamu yang harus diserahkan player saat bicara untuk menyembuhkan NPC ini (kosongkan jika bukan pasien).")]
    public string healItemNeeded;

    [Header("Dialog Content")]
    [Tooltip("Daftar baris dialog UTAMA (stage 0). Gunakan isPlayerLine=true untuk baris MC/Player.")]
    public DialogLine[] dialogLines = new DialogLine[] { };

    [Header("Multi-Stage Dialog")]
    [Tooltip("Dialog tambahan per-stage (stage 1, 2, dst). Dipakai saat QuestManager set stage NPC ini.")]
    public DialogLine[][] extraStages;

    /// <summary>Stage dialog aktif. 0 = dialogLines utama, 1+ = dari extraStages.</summary>
    [HideInInspector] public int dialogStage = 0;

    /// <summary>Set oleh QuestManager: apakah NPC ini boleh diajak bicara lagi walaupun HasTalked.</summary>
    [HideInInspector] public bool canTalkAgain = false;

    [Header("Interaction Settings")]
    [Tooltip("Radius interaksi dalam meter")]
    public float interactRadius = 3f;

    [Header("Portrait Siluet (Visual Novel)")]
    [Tooltip("Siluet NPC ini (tampil di KANAN saat NPC TIDAK sedang bicara).")]
    public Sprite npcSilhouette;
    [Tooltip("Siluet Player/MC (tampil di KIRI saat MC TIDAK sedang bicara).")]
    public Sprite playerSilhouette;

    [Header("Post-Dialog Shop (opsional)")]
    [Tooltip("Kalau diisi, setelah dialog selesai muncul pilihan 'Lihat Toko' / 'Tinggalkan'.")]
    public ShopUI shopUI;

    private bool playerInRange = false;
    private Transform playerTransform;

    /// <summary>True setelah pemain selesai ngobrol dengan NPC ini minimal sekali.</summary>
    public bool HasTalked { get; set; }

    /// <summary>Dipanggil sekali saat dialog NPC ini pertama kali selesai.</summary>
    public event System.Action OnTalked;

    private void HandleDialogComplete()
    {
        if (!HasTalked)
        {
            HasTalked = true;
            OnTalked?.Invoke();
        }

        // Lapor ke QuestManager
        if (QuestManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(healItemNeeded))
                QuestManager.Instance.NotifyHealPatient(npcId, healItemNeeded);
            else
                QuestManager.Instance.NotifyTalked(npcId);
        }

        // Post-dialog: tawarin toko kalau ada shopUI
        if (shopUI != null && PostDialogChoiceUI.Instance != null)
        {
            PostDialogChoiceUI.Instance.Show(
                "Lihat Item Toko",
                "Tinggalkan",
                () => shopUI.Open(),
                null // null = tutup aja
            );
        }
    }

    void Start()
    {
        CachePlayer();
    }

    void Update()
    {
        if (playerTransform == null) { CachePlayer(); return; }

        float dist   = Vector3.Distance(transform.position, playerTransform.position);
        bool  isNear = dist <= interactRadius;

        if (isNear != playerInRange)
        {
            playerInRange = isNear;
            UpdatePrompt();
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.G))
        {
            bool canStart = (!HasTalked || canTalkAgain)
                            && DialogManager.Instance != null
                            && !DialogManager.Instance.IsDialogActive;

            if (canStart)
            {
                // Reset canTalkAgain SEBELUM mulai dialog — jangan bisa trigger lagi
                canTalkAgain = false;

                DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
                DialogManager.Instance.StartDialog(GetCurrentDialogLines(), HandleDialogComplete);
            }
        }
    }

    /// <summary>Paksa mulai dialog dari luar (misal: cutscene).</summary>
    public void ForceStartDialog()
    {
        if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
        {
            DialogManager.Instance.SetPortraitSilhouettes(playerSilhouette, npcSilhouette);
            DialogManager.Instance.StartDialog(GetCurrentDialogLines(), HandleDialogComplete);
        }
    }

    private DialogLine[] GetCurrentDialogLines()
    {
        return dialogLines;
    }

    private void UpdatePrompt()
    {
        if (DialogManager.Instance == null) return;
        bool show = playerInRange && (!HasTalked || canTalkAgain);
        DialogManager.Instance.ShowInteractPrompt(show);
    }

    private void CachePlayer()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawSphere(transform.position, interactRadius);
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
