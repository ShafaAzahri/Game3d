using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Genshin Impact-style Dialog Manager.
/// - Tekan G untuk lanjut dialog (bukan tombol)
/// - Tombol Lanjut diganti indikator ▶ kecil yang berkedip
/// - Warna nama berbeda untuk MC vs NPC
/// - Prompt "[G] Bicara" otomatis hilang saat dialog aktif
/// </summary>
public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance { get; private set; }

    public int LastEndFrame { get; private set; } = -1;

    [Header("UI References")]
    public GameObject dialogPanel;
    public TMP_Text speakerNameText;
    public TMP_Text speakerSubtitleText;
    public TMP_Text dialogText;
    public Button nextButton;            // disembunyikan — hanya sebagai fallback
    public GameObject nextIndicator;     // objek ▶ kecil yang berkedip
    public GameObject interactPrompt;
    public GameObject hotbarUI;          // Hotbar UI yang akan dinonaktifkan saat dialog

    [Header("Portrait VN-Style (Kiri & Kanan)")]
    [Tooltip("Image portrait sisi KIRI — biasanya untuk Player/MC (Robby).")]
    public Image leftPortrait;
    [Tooltip("Image portrait sisi KANAN — biasanya untuk NPC (Nenek, Laras, dll).")]
    public Image rightPortrait;
    [Tooltip("Warna untuk karakter yang SEDANG bicara (terang).")]
    public Color activePortraitColor = Color.white;
    [Tooltip("Warna untuk karakter yang TIDAK bicara (redup, biar siluet makin gelap).")]
    public Color inactivePortraitColor = new Color(0.35f, 0.35f, 0.40f, 1f);

    [Header("Typewriter Settings")]
    public float typewriterSpeed = 0.03f;

    [Header("Colors")]
    public Color npcNameColor  = new Color(0.95f, 0.82f, 0.38f, 1f); // emas
    public Color playerNameColor = new Color(0.50f, 0.85f, 1.00f, 1f); // biru muda

    // State
    private DialogLine[] currentLines;
    private int currentLineIndex = 0;

    // Siluet kedua karakter untuk dialog yang sedang berjalan
    private Sprite playerSilhouette;   // siluet sisi kiri (MC)
    private Sprite npcSilhouette;      // siluet sisi kanan (NPC)
    private bool isTyping = false;
    private bool dialogActive = false;
    private Coroutine typewriterCoroutine;
    private Coroutine blinkCoroutine;

    // Cooldown: cegah G ditekan saat start dialog ikut ter-trigger di frame yang sama
    private bool inputCooldown = false;

    // Mode cutscene: auto-advance tanpa G key
    private bool cutsceneMode = false;
    private float cutsceneAutoDelay = 3f;
    private Coroutine cutsceneAutoCoroutine;

    // Callback saat dialog selesai
    private System.Action onDialogComplete;

    private PlayerController playerController;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (dialogPanel != null) dialogPanel.SetActive(false);
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);
    }

    void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        if (nextButton != null) nextButton.onClick.AddListener(AdvanceDialog);
    }

    void Update()
    {
        // Reset cooldown setelah 1 frame
        if (inputCooldown)
        {
            inputCooldown = false;
            return; // skip input frame ini
        }

        // G key advances dialog (only if dialog is active)
        if (dialogActive && Input.GetKeyDown(KeyCode.G))
        {
            AdvanceDialog();
        }
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Set gambar siluet untuk kedua sisi sebelum dialog dimulai.
    /// playerSil = siluet MC (kiri), npcSil = siluet NPC (kanan).
    /// </summary>
    public void SetPortraitSilhouettes(Sprite playerSil, Sprite npcSil)
    {
        playerSilhouette = playerSil;
        npcSilhouette    = npcSil;
    }

    /// <summary>
    /// Mulai dialog normal (G key untuk lanjut).
    /// onComplete dipanggil ketika semua baris selesai.
    /// </summary>
    public void StartDialog(DialogLine[] lines, System.Action onComplete = null)
    {
        if (lines == null || lines.Length == 0) return;
        if (dialogActive) return;

        cutsceneMode      = false;
        onDialogComplete  = onComplete;

        dialogActive   = true;
        inputCooldown  = true;
        currentLines   = lines;
        currentLineIndex = 0;

        ShowInteractPrompt(false);
        SetPlayerMovement(false);
        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (hotbarUI != null) hotbarUI.SetActive(false);

        ShowLine(currentLines[0]);
    }

    /// <summary>
    /// Mulai dialog mode cutscene: baris tampil otomatis tanpa perlu tekan G.
    /// Cocok untuk monolog/narasi saat cutscene berjalan.
    /// </summary>
    public void StartCutsceneDialog(DialogLine[] lines, float autoDelay = 3f, System.Action onComplete = null)
    {
        if (lines == null || lines.Length == 0) { onComplete?.Invoke(); return; }
        if (dialogActive) return;

        cutsceneMode     = true;
        cutsceneAutoDelay = autoDelay;
        onDialogComplete = onComplete;

        dialogActive     = true;
        currentLines     = lines;
        currentLineIndex = 0;

        ShowInteractPrompt(false);
        if (dialogPanel != null) dialogPanel.SetActive(true);
        if (hotbarUI != null) hotbarUI.SetActive(false);
        if (nextIndicator != null) nextIndicator.SetActive(false);

        ShowLine(currentLines[0]);
        // Auto-advance dimulai setelah typewriter selesai — ditangani di TypewriterEffect
    }

    /// <summary>
    /// Tampilkan/sembunyikan interact prompt.
    /// Hanya tampilkan kalau dialog TIDAK sedang aktif.
    /// </summary>
    public void ShowInteractPrompt(bool show)
    {
        if (interactPrompt == null) return;
        // Jangan tampilkan prompt kalau dialog lagi jalan
        interactPrompt.SetActive(show && !dialogActive);
    }

    public bool IsDialogActive => dialogActive;

    // ─────────────────────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────────────────────

    private void ShowLine(DialogLine line)
    {
        // Nama speaker
        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
            speakerNameText.color = line.isPlayerLine ? playerNameColor : npcNameColor;
        }

        // Subtitle (jabatan / keterangan)
        if (speakerSubtitleText != null)
        {
            speakerSubtitleText.text = line.subtitle;
            speakerSubtitleText.gameObject.SetActive(!string.IsNullOrEmpty(line.subtitle));
        }

        // Gambar ekspresi / portrait — gaya Visual Novel (kiri & kanan)
        // Yang sedang bicara: tampil pose ekspresinya (terang).
        // Lawan bicara: tampil siluet (redup).
        UpdatePortraits(line);

        // Sembunyikan indikator ▶ saat mengetik
        if (nextIndicator != null) nextIndicator.SetActive(false);

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
    }

    /// <summary>
    /// Atur dua portrait gaya Visual Novel berdasarkan siapa yang bicara.
    /// Speaker → pose ekspresi (terang). Lawan bicara → siluet (redup).
    /// </summary>
    private void UpdatePortraits(DialogLine line)
    {
        if (line.isPlayerLine)
        {
            // Player (MC) bicara di sisi KIRI
            SetPortrait(leftPortrait,  line.expression != null ? line.expression : playerSilhouette, true);
            SetPortrait(rightPortrait, npcSilhouette, false);
        }
        else
        {
            // NPC bicara di sisi KANAN
            SetPortrait(rightPortrait, line.expression != null ? line.expression : npcSilhouette, true);
            SetPortrait(leftPortrait,  playerSilhouette, false);
        }
    }

    private void SetPortrait(Image img, Sprite sprite, bool isActive)
    {
        if (img == null) return;

        if (sprite == null)
        {
            // Tidak ada gambar untuk sisi ini → sembunyikan
            img.gameObject.SetActive(false);
            return;
        }

        img.sprite = sprite;
        img.color  = isActive ? activePortraitColor : inactivePortraitColor;
        img.gameObject.SetActive(true);
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        if (dialogText != null) dialogText.text = "";

        foreach (char c in text)
        {
            if (dialogText != null) dialogText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;

        if (cutsceneMode)
        {
            // Mode cutscene: auto-advance setelah delay
            if (cutsceneAutoCoroutine != null) StopCoroutine(cutsceneAutoCoroutine);
            cutsceneAutoCoroutine = StartCoroutine(AutoAdvance());
        }
        else
        {
            // Mode normal: tampilkan indikator ▶ berkedip
            if (nextIndicator != null)
            {
                nextIndicator.SetActive(true);
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkIndicator());
            }
        }
    }

    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(cutsceneAutoDelay);
        AdvanceDialog();
    }

    private IEnumerator BlinkIndicator()
    {
        if (nextIndicator == null) yield break;
        var cg = nextIndicator.GetComponent<CanvasGroup>();
        if (cg == null) cg = nextIndicator.AddComponent<CanvasGroup>();

        while (true)
        {
            // Fade in
            for (float t = 0f; t < 1f; t += Time.deltaTime * 3f)
            { cg.alpha = t; yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSeconds(0.2f);
            // Fade out
            for (float t = 1f; t > 0f; t -= Time.deltaTime * 3f)
            { cg.alpha = t; yield return null; }
            cg.alpha = 0f;
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ForceEndDialog()
    {
        EndDialog();
    }

    /// <summary>
    /// Dipanggil saat G ditekan atau tombol Next diklik.
    /// </summary>
    public void AdvanceDialog()
    {
        if (!dialogActive) return;

        if (isTyping)
        {
            // Skip typewriter
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            if (dialogText != null) dialogText.text = currentLines[currentLineIndex].text;
            isTyping = false;

            // Tampilkan indikator setelah skip
            if (nextIndicator != null)
            {
                nextIndicator.SetActive(true);
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkIndicator());
            }
            return;
        }

        // Maju ke baris berikutnya
        currentLineIndex++;
        if (currentLineIndex < currentLines.Length)
        {
            ShowLine(currentLines[currentLineIndex]);
        }
        else
        {
            EndDialog();
        }
    }

    private void EndDialog()
    {
        dialogActive = false;
        cutsceneMode = false;
        LastEndFrame = Time.frameCount;

        if (typewriterCoroutine != null)    StopCoroutine(typewriterCoroutine);
        if (blinkCoroutine != null)         StopCoroutine(blinkCoroutine);
        if (cutsceneAutoCoroutine != null)  StopCoroutine(cutsceneAutoCoroutine);

        if (dialogPanel != null)    dialogPanel.SetActive(false);
        if (nextIndicator != null)  nextIndicator.SetActive(false);
        if (leftPortrait != null)   leftPortrait.gameObject.SetActive(false);
        if (rightPortrait != null)  rightPortrait.gameObject.SetActive(false);
        if (hotbarUI != null)       hotbarUI.SetActive(true);

        SetPlayerMovement(true);

        // Panggil callback (NPCDialog.OnDialogFinished atau callback cutscene)
        var cb = onDialogComplete;
        onDialogComplete = null;
        cb?.Invoke();
    }

    private void SetPlayerMovement(bool canMove)
    {
        if (playerController != null) playerController.CanMove = canMove;
    }
}
