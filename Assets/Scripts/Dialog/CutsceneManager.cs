using System.Collections;
using UnityEngine;

/// <summary>
/// Cutscene pembuka Herbal Haven.
///
/// CARA SETUP DI INSPECTOR:
/// 1. Klik "CutsceneManager" di Hierarchy
/// 2. Isi:
///    - Player Obj   → drag MC/Robby dari Hierarchy
///    - Npc Nenek    → drag "Nenek MC" dari Hierarchy
/// 3. Atur "Stopping Distance" = jarak berhenti sebelum Nenek (default 2m)
/// 4. Jalankan Play Mode — cutscene otomatis berjalan
///
/// ALUR CUTSCENE:
/// [1] Player berjalan otomatis menuju Nenek Rukmini
/// [2] Monolog Robby muncul satu per satu (auto, tidak perlu tekan G)
/// [3] Sampai di depan Nenek (jarak = stoppingDistance) → berhenti
/// [4] Robby menghadap Nenek
/// [5] Dialog Nenek dimulai otomatis
/// [6] Selesai → kontrol kembali ke player
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Referensi")]
    [Tooltip("GameObject player MC/Robby")]
    public GameObject playerObj;

    [Tooltip("NPCDialog pada Nenek Rukmini")]
    public NPCDialog npcNenek;

    [Header("Pengaturan Jarak & Kecepatan")]
    [Tooltip("Berhenti sejauh ini dari Nenek (meter). Naikkan jika player masih nembus.")]
    public float stoppingDistance = 2.2f;

    [Tooltip("Kecepatan berjalan saat cutscene")]
    public float walkSpeed = 3f;

    [Header("Monolog Robby (auto-advance)")]
    [Tooltip("Baris monolog yang tampil otomatis saat Robby berjalan mendekat.")]
    public DialogLine[] monologLines = new DialogLine[]
    {
        new DialogLine
        {
            speakerName  = "Robby",
            subtitle     = "",
            text         = "Aduh, Nenek gimana ya... SMS-nya bilang kurang sehat.",
            isPlayerLine = true
        },
        new DialogLine
        {
            speakerName  = "Robby",
            subtitle     = "",
            text         = "Kenapa baru bilang sekarang? Harusnya dari tadi aku pulang...",
            isPlayerLine = true
        },
        new DialogLine
        {
            speakerName  = "Robby",
            subtitle     = "",
            text         = "Sebentar lagi, Nek. Aku sudah dekat!",
            isPlayerLine = true
        }
    };

    [Tooltip("Detik setiap baris monolog sebelum auto-advance ke baris berikutnya")]
    public float monologDelay = 3.0f;

    [Header("Timing")]
    [Tooltip("Delay (detik) setelah tiba sebelum dialog Nenek dimulai")]
    public float pauseBeforeDialog = 0.6f;

    [Tooltip("Jalankan cutscene otomatis saat scene load?")]
    public bool autoStartOnLoad = true;

    // ─────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────

    private PlayerController playerController;
    private bool             cutsceneRunning = false;

    // ─────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        if (playerObj != null)
            playerController = playerObj.GetComponent<PlayerController>();

        if (autoStartOnLoad)
            StartCoroutine(RunCutscene());
    }

    // ─────────────────────────────────────────────
    // PUBLIC
    // ─────────────────────────────────────────────

    /// <summary>Trigger cutscene dari luar (misal tombol, event).</summary>
    public void TriggerCutscene()
    {
        if (!cutsceneRunning)
            StartCoroutine(RunCutscene());
    }

    // ─────────────────────────────────────────────
    // CUTSCENE COROUTINE
    // ─────────────────────────────────────────────

    private IEnumerator RunCutscene()
    {
        if (playerObj == null || npcNenek == null)
        {
            Debug.LogWarning("[Cutscene] playerObj atau npcNenek belum diisi di Inspector!");
            yield break;
        }

        cutsceneRunning = true;

        // ── FASE 1: Lock input player ──────────────────────────────────
        if (playerController != null) playerController.CanMove = false;

        // ── FASE 2: Tampilkan monolog + gerak bersamaan ────────────────
        bool monologDone = false;

        if (DialogManager.Instance != null && monologLines != null && monologLines.Length > 0)
        {
            DialogManager.Instance.StartCutsceneDialog(
                monologLines,
                monologDelay,
                onComplete: () => monologDone = true
            );
        }
        else
        {
            monologDone = true;
        }

        // Gerakkan player berjalan ke arah Nenek
        yield return StartCoroutine(WalkTowardNenek());

        // Tunggu monolog selesai (jika belum)
        float safety = 0f;
        while (!monologDone && safety < 20f) { safety += Time.deltaTime; yield return null; }

        // ── FASE 3: Berhenti, hadapkan ke Nenek ───────────────────────
        Vector3 lookDir = (npcNenek.transform.position - playerObj.transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            playerObj.transform.rotation = Quaternion.LookRotation(lookDir);

        // ── FASE 4: Jeda singkat ───────────────────────────────────────
        yield return new WaitForSeconds(pauseBeforeDialog);

        // ── FASE 5: Dialog Nenek ───────────────────────────────────────
        npcNenek.ForceStartDialog();

        // Tunggu dialog Nenek selesai
        yield return new WaitUntil(() =>
            DialogManager.Instance == null || !DialogManager.Instance.IsDialogActive
        );

        // ── SELESAI ────────────────────────────────────────────────────
        cutsceneRunning = false;
        Debug.Log("[Cutscene] Selesai — kontrol kembali ke player.");
    }

    /// <summary>Gerakkan player mendekati Nenek, berhenti di stoppingDistance.</summary>
    private IEnumerator WalkTowardNenek()
    {
        while (true)
        {
            if (playerObj == null || npcNenek == null) yield break;

            // Hitung jarak horizontal (abaikan Y)
            Vector3 playerFlat = new Vector3(playerObj.transform.position.x, 0, playerObj.transform.position.z);
            Vector3 nenekFlat  = new Vector3(npcNenek.transform.position.x, 0, npcNenek.transform.position.z);
            float   dist       = Vector3.Distance(playerFlat, nenekFlat);

            if (dist <= stoppingDistance) break;

            // Arah menuju Nenek (horizontal saja)
            Vector3 dir = (nenekFlat - playerFlat).normalized;

            // Geser posisi player
            playerObj.transform.position += new Vector3(dir.x, 0, dir.z) * walkSpeed * Time.deltaTime;

            // Hadapkan player ke Nenek
            if (dir != Vector3.zero)
                playerObj.transform.rotation = Quaternion.Slerp(
                    playerObj.transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 8f
                );

            yield return null;
        }
    }

    // Editor: visualisasi stopping distance
    void OnDrawGizmosSelected()
    {
        if (npcNenek != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Gizmos.DrawSphere(npcNenek.transform.position, stoppingDistance);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(npcNenek.transform.position, stoppingDistance);
        }
    }
}
