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
    [Header("Dialog Content")]
    [Tooltip("Daftar baris dialog. Gunakan isPlayerLine=true untuk baris MC/Player.")]
    public DialogLine[] dialogLines = new DialogLine[]
    {
        new DialogLine
        {
            speakerName  = "Nenek Rukmini",
            subtitle     = "Tabib Desa",
            text         = "Robby... sudah lama tidak pulang. Ayo masuk, Nenek sudah masak.",
            isPlayerLine = false
        },
        new DialogLine
        {
            speakerName  = "Robby",
            subtitle     = "",
            text         = "Nenek gimana kondisinya? Kata orang Nenek sakit?",
            isPlayerLine = true
        },
        new DialogLine
        {
            speakerName  = "Nenek Rukmini",
            subtitle     = "Tabib Desa",
            text         = "Ah, Nenek baik-baik saja. Yang sakit itu desa kita ini, Cu...",
            isPlayerLine = false
        },
        new DialogLine
        {
            speakerName  = "Nenek Rukmini",
            subtitle     = "Tabib Desa",
            text         = "Kebun herbal sudah lama tidak dirawat. Nenek mau minta tolong kamu.",
            isPlayerLine = false
        },
        new DialogLine
        {
            speakerName  = "Robby",
            subtitle     = "",
            text         = "Oke Nek, apa yang perlu aku lakukan?",
            isPlayerLine = true
        }
    };

    [Header("Interaction Settings")]
    [Tooltip("Radius interaksi dalam meter")]
    public float interactRadius = 3f;

    private bool playerInRange = false;
    private Transform playerTransform;

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
            if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
                DialogManager.Instance.StartDialog(dialogLines);
        }
    }

    /// <summary>Paksa mulai dialog dari luar (misal: cutscene).</summary>
    public void ForceStartDialog()
    {
        if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
            DialogManager.Instance.StartDialog(dialogLines);
    }

    private void UpdatePrompt()
    {
        if (DialogManager.Instance == null) return;
        DialogManager.Instance.ShowInteractPrompt(playerInRange);
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
