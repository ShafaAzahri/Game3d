using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;

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
    [Tooltip("Warna siluet hitam/gelap untuk karakter yang sedang diam.")]
    public Color silhouetteColor = new Color(0.08f, 0.08f, 0.1f, 1f);

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
    private string currentConversationNpcName = ""; // nama NPC untuk percakapan aktif
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

        // Scan nama NPC dalam percakapan ini
        currentConversationNpcName = "";
        foreach (var l in lines)
        {
            if (l != null && !l.isPlayerLine && !string.IsNullOrEmpty(l.speakerName) && !l.speakerName.ToLower().Contains("robby") && !l.speakerName.ToLower().Contains("mc"))
            {
                currentConversationNpcName = l.speakerName;
                break;
            }
        }

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

        // Scan nama NPC dalam percakapan ini
        currentConversationNpcName = "";
        foreach (var l in lines)
        {
            if (l != null && !l.isPlayerLine && !string.IsNullOrEmpty(l.speakerName) && !l.speakerName.ToLower().Contains("robby") && !l.speakerName.ToLower().Contains("mc"))
            {
                currentConversationNpcName = l.speakerName;
                break;
            }
        }

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
        // 1. Tentukan sprite untuk sisi kiri (Robby / Player)
        Sprite leftSprite = null;
        bool leftActive = line.isPlayerLine;

        string leftEmotion = leftActive ? AnalyzeEmotion("Robby", line.text) : "normal";
        leftSprite = GetDynamicSprite("Robby", leftEmotion);

        // Jika leftSprite gagal dimuat di editor, gunakan fallback playerSilhouette original
        if (leftSprite == null) leftSprite = playerSilhouette;

        // 2. Tentukan sprite untuk sisi kanan (NPC)
        Sprite rightSprite = null;
        bool rightActive = !line.isPlayerLine;

        // Deteksi nama NPC aktif
        string activeNpcName = "";
        if (rightActive)
        {
            activeNpcName = line.speakerName;
            // Catat nama NPC aktif untuk dijadikan siluet ketika Robby membalas pembicaraan
            if (!string.IsNullOrEmpty(activeNpcName) && !activeNpcName.ToLower().Contains("robby") && !activeNpcName.ToLower().Contains("mc"))
            {
                currentConversationNpcName = activeNpcName;
            }
        }
        else
        {
            // Jika Robby yang bicara, gunakan nama NPC yang sudah dicatat tadi
            activeNpcName = currentConversationNpcName;
        }

        string npcFolder = GetCharacterFolderName(activeNpcName);

        if (!string.IsNullOrEmpty(npcFolder))
        {
            string rightEmotion = rightActive ? AnalyzeEmotion(npcFolder, line.text) : "normal";
            rightSprite = GetDynamicSprite(npcFolder, rightEmotion);
        }
        else
        {
            // Jika NPC lain yang belum terdaftar foldernya, gunakan sprite bawaan dari dialog line / siluet default
            if (rightActive)
            {
                rightSprite = line.expression != null ? line.expression : npcSilhouette;
            }
            else
            {
                rightSprite = npcSilhouette;
            }
        }

        // Jika rightSprite gagal dimuat, gunakan fallback npcSilhouette original
        if (rightSprite == null) rightSprite = npcSilhouette;

        // Terapkan ke Image UI
        SetPortrait(leftPortrait, leftSprite, leftActive);
        SetPortrait(rightPortrait, rightSprite, rightActive);
    }

    private string AnalyzeEmotion(string speaker, string text)
    {
        text = text.ToLower();
        
        // Deteksi ekspresi berdasarkan kata kunci umum yang ada di text
        if (text.Contains("aduh") || text.Contains("shock") || text.Contains("kaget") || 
            text.Contains("melilit") || text.Contains("sakit") || text.Contains("kenapa") || 
            text.Contains("gimana") || text.Contains("kurang sehat") || text.Contains("lelah") || 
            text.Contains("pusing") || text.Contains("mual") || text.Contains("demam"))
        {
            if (speaker.ToLower().Contains("laras") || text.Contains("malu") || text.Contains("blush") || text.Contains("cantik") || text.Contains("manis"))
                return "blushing";
                
            return "sad"; 
        }

        if (text.Contains("senang") || text.Contains("terima kasih") || text.Contains("hebat") || 
            text.Contains("siap") || text.Contains("enak") || text.Contains("bangga") || 
            text.Contains("bagus") || text.Contains("hehe") || text.Contains("lucu") || 
            text.Contains("tercinta") || text.Contains("selamat") || text.Contains("jodoh") || 
            text.Contains("cocok") || text.Contains("cicipi") || text.Contains("coba") || 
            text.Contains("sehat") || text.Contains("pulih") || text.Contains("untunglah"))
        {
            return "happy"; 
        }

        if (text.Contains("sedih") || text.Contains("sayang") || text.Contains("meninggal") || 
            text.Contains("kasihan") || text.Contains("khawatir") || text.Contains("cemas") || 
            text.Contains("takut") || text.Contains("maaf"))
        {
            return "sad"; 
        }

        return "normal";
    }

    private string GetCharacterFolderName(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName)) return null;
        string name = speakerName.ToLower();
        if (name.Contains("robby") || name.Contains("mc") || name.Contains("player")) return "Robby";
        if (name.Contains("nenek")) return "Nenek";
        if (name.Contains("nisa")) return "Nisa";
        if (name.Contains("bahri")) return "Pak Bahri";
        if (name.Contains("darsono") || name.Contains("kades") || name.Contains("kepala desa")) return "Pak Darsono";
        if (name.Contains("darma")) return "Pak Darma";
        if (name.Contains("seno")) return "Pak Seno";
        if (name.Contains("ratri")) return "Ratri";
        if (name.Contains("sekar")) return "Sekar";
        if (name.Contains("laras")) return "Laras";
        return null;
    }

    private Sprite GetDynamicSprite(string folderName, string emotion)
    {
        // Path in Resources: e.g. "DialogPortraits/Laras/"
        string resourceDir = "DialogPortraits/" + folderName + "/";
        
        var candidates = new System.Collections.Generic.List<string>();
        string charLower = folderName.ToLower();
        string emotLower = emotion != null ? emotion.ToLower() : "";

        // 1. Cari file gambar yang cocok dengan kata kunci emosi spesifik
        if (emotLower == "happy" || emotLower == "senang")
        {
            if (charLower == "robby") { candidates.Add("mc happy pose"); }
            else if (charLower == "nenek") { candidates.Add("nenek happy pose"); }
            else if (charLower == "pak darma") { candidates.Add("pak darma_healthy_happy"); }
            else
            {
                candidates.Add(charLower + "_happy");
                candidates.Add(charLower + "_cheerfull");
                candidates.Add(charLower + "_rellived");
            }
        }
        else if (emotLower == "sad" || emotLower == "sedih" || emotLower == "sick" || emotLower == "sakit")
        {
            if (charLower == "robby") { candidates.Add("mc sad pose"); }
            else if (charLower == "nenek") { candidates.Add("nenek sad pose"); candidates.Add("nenek bagging"); }
            else
            {
                candidates.Add(charLower + "_sick");
                candidates.Add(charLower + "_worried");
                candidates.Add(charLower + "_sad");
            }
        }
        else if (emotLower == "shock" || emotLower == "kaget" || emotLower == "blush")
        {
            if (charLower == "robby") { candidates.Add("mc shock pose"); }
            else
            {
                candidates.Add(charLower + "_blushing");
                candidates.Add(charLower + "_shock");
            }
        }

        // Selalu tambahkan nama emosi mentah sebagai kandidat
        candidates.Add(charLower + "_" + emotion);
        candidates.Add(charLower + "_" + emotLower);
        candidates.Add(emotion);
        candidates.Add(emotLower);

        // Coba load kandidat secara berurutan
        foreach (var c in candidates)
        {
            Sprite s = Resources.Load<Sprite>(resourceDir + c);
            if (s != null) return s;
        }

        // 2. Jika tidak ditemukan emosi spesifik, cari netral/normal
        var neutralCandidates = new System.Collections.Generic.List<string>();
        if (charLower == "robby") { neutralCandidates.Add("mc normal pose"); }
        else if (charLower == "nenek") { neutralCandidates.Add("nenek normal pose"); }
        else
        {
            neutralCandidates.Add(charLower + "_netral");
            neutralCandidates.Add(charLower + "_normal");
            neutralCandidates.Add(charLower + "_default");
        }

        foreach (var c in neutralCandidates)
        {
            Sprite s = Resources.Load<Sprite>(resourceDir + c);
            if (s != null) return s;
        }

        // 3. Fallback terakhir: coba nama berkas siluet atau default lain
        if (charLower == "nenek") return Resources.Load<Sprite>(resourceDir + "siluet nenek");
        if (charLower == "robby") return Resources.Load<Sprite>(resourceDir + "siluet mc");

        // Coba memuat nama file apa saja secara langsung jika emosinya adalah nama file itu sendiri
        return Resources.Load<Sprite>(resourceDir + emotion);
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
        // Jika aktif, gunakan activePortraitColor (terang).
        // Jika diam (siluet), gunakan silhouetteColor (sangat gelap/hitam) agar tidak kelihatan warna aslinya dan presisi posisinya.
        img.color  = isActive ? activePortraitColor : silhouetteColor;

        // Atur tinggi dasar agar ukuran visual Robby (lanskap) dan NPC seimbang
        float targetHeight = 300f;
        float sizeMultiplier = 1f;
        string spriteName = sprite.name.ToLower();
        if (!spriteName.Contains("mc") && !spriteName.Contains("robby") && !spriteName.Contains("player"))
        {
            sizeMultiplier = 0.95f; // Semua NPC di-scale agar seimbang terhadap padding Robby
        }
        
        float aspect = sprite.rect.width / sprite.rect.height;
        float finalHeight = targetHeight * sizeMultiplier;
        img.rectTransform.sizeDelta = new Vector2(finalHeight * aspect, finalHeight);

        // Geser ke pinggiran (leftPortrait digeser ke kiri/negatif, rightPortrait digeser ke kanan/positif)
        if (img == leftPortrait)
        {
            img.rectTransform.anchoredPosition = new Vector2(-120f, 0f);
        }
        else if (img == rightPortrait)
        {
            img.rectTransform.anchoredPosition = new Vector2(120f, 0f);
        }

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
