using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// ================================================
// DIALOGUE MANAGER - Singleton untuk mengontrol UI Dialog
// Attach ke GameObject "DialogueManager" di scene
// ================================================
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI npcNameText;
    public TextMeshProUGUI dialogueText;
    public Image npcPortraitImage;
    public GameObject portraitFrame;
    public TextMeshProUGUI continuePromptText; // "[E] Lanjut / [ESC] Tutup"

    [Header("Typewriter Effect")]
    public float typewriterSpeed = 0.04f; // detik per karakter

    [Header("Settings")]
    public bool pausePlayerOnDialogue = true;
    public AudioClip typingSFX;
    public AudioSource audioSource;

    // State internal
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isTyping = false;
    private bool isDialogueOpen = false;
    private Coroutine typewriterCoroutine;

    // Event supaya PlayerController bisa subscribe
    public static event System.Action<bool> OnDialogueStateChanged;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pastikan panel tertutup di awal
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    // ================================================
    // MULAI DIALOG - dipanggil dari NPCDialogue
    // ================================================
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.lines.Count == 0) return;

        currentDialogue = data;
        currentLineIndex = 0;
        isDialogueOpen = true;

        // Setup nama NPC
        if (npcNameText != null)
            npcNameText.text = data.npcName;

        // Setup portrait
        if (npcPortraitImage != null && portraitFrame != null)
        {
            if (data.npcPortrait != null)
            {
                npcPortraitImage.sprite = data.npcPortrait;
                portraitFrame.SetActive(true);
            }
            else
            {
                portraitFrame.SetActive(false);
            }
        }

        dialoguePanel.SetActive(true);

        // Notify player controller
        OnDialogueStateChanged?.Invoke(true);

        ShowLine(currentLineIndex);
    }

    // ================================================
    // TAMPILKAN BARIS DIALOG
    // ================================================
    void ShowLine(int index)
    {
        if (currentDialogue == null) return;
        if (index >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        string line = currentDialogue.lines[index];

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(TypewriterEffect(line));
    }

    // ================================================
    // TYPEWRITER EFFECT
    // ================================================
    IEnumerator TypewriterEffect(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        // Update prompt saat mengetik
        if (continuePromptText != null)
            continuePromptText.text = "[E] Skip";

        foreach (char c in line)
        {
            dialogueText.text += c;

            // Play typing SFX
            if (typingSFX != null && audioSource != null && c != ' ')
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(typingSFX, 0.4f);
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;

        // Update prompt setelah selesai mengetik
        bool isLastLine = (currentLineIndex >= currentDialogue.lines.Count - 1);
        if (continuePromptText != null)
            continuePromptText.text = isLastLine ? "[E] Tutup" : "[E] Lanjutkan";
    }

    // ================================================
    // LANJUT / SKIP - dipanggil saat tekan E
    // ================================================
    public void AdvanceDialogue()
    {
        if (!isDialogueOpen) return;

        if (isTyping)
        {
            // Skip typewriter - langsung tampilkan full text
            if (typewriterCoroutine != null)
                StopCoroutine(typewriterCoroutine);

            isTyping = false;
            dialogueText.text = currentDialogue.lines[currentLineIndex];

            bool isLastLine = (currentLineIndex >= currentDialogue.lines.Count - 1);
            if (continuePromptText != null)
                continuePromptText.text = isLastLine ? "[E] Tutup" : "[E] Lanjutkan";
        }
        else
        {
            // Lanjut ke baris berikutnya
            currentLineIndex++;
            ShowLine(currentLineIndex);
        }
    }

    // ================================================
    // TUTUP DIALOG
    // ================================================
    public void EndDialogue()
    {
        isDialogueOpen = false;
        currentDialogue = null;
        currentLineIndex = 0;

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Notify player controller
        OnDialogueStateChanged?.Invoke(false);
    }

    public bool IsDialogueOpen => isDialogueOpen;

    // ================================================
    // UPDATE - handle input
    // ================================================
    void Update()
    {
        if (!isDialogueOpen) return;

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceDialogue();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            EndDialogue();
        }
    }
}
