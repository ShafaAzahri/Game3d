using UnityEngine;

// ================================================
// NPC DIALOGUE - Attach ke setiap NPC
// ================================================
public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData dialogueData;

    [Header("Interaction Range")]
    public float interactRange = 2.5f;

    [Header("Prompt Object (World Space)")]
    public GameObject interactPromptObject; // GameObject "Tekan E" yang muncul di atas NPC

    // ================================================
    // Referensi player
    // ================================================
    private Transform player;
    private bool playerInRange = false;
    private bool isRegisteredAsTarget = false;

    // ================================================
    // Static: NPC yang sedang dalam jangkauan (nearest)
    // ================================================
    public static NPCDialogue CurrentNearestNPC = null;

    void Start()
    {
        // Cari player di scene
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Sembunyikan prompt di awal
        if (interactPromptObject != null)
            interactPromptObject.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        // Cek jarak player
        float dist = Vector3.Distance(transform.position, player.position);
        bool inRange = dist <= interactRange;

        if (inRange != playerInRange)
        {
            playerInRange = inRange;

            if (inRange)
                OnPlayerEnterRange();
            else
                OnPlayerExitRange();
        }

        // Selalu update NPC terdekat
        if (playerInRange)
        {
            if (CurrentNearestNPC == null)
            {
                CurrentNearestNPC = this;
            }
            else if (CurrentNearestNPC != this)
            {
                float distOther = Vector3.Distance(CurrentNearestNPC.transform.position, player.position);
                if (dist < distOther)
                {
                    CurrentNearestNPC.HidePrompt();
                    CurrentNearestNPC = this;
                }
            }
        }

        // Prompt hanya tampil jika ini NPC terdekat & dialog tidak sedang terbuka
        if (interactPromptObject != null)
        {
            bool shouldShow = playerInRange &&
                              CurrentNearestNPC == this &&
                              (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueOpen);

            interactPromptObject.SetActive(shouldShow);

            // Prompt selalu menghadap kamera
            if (shouldShow && Camera.main != null)
            {
                interactPromptObject.transform.LookAt(
                    interactPromptObject.transform.position + Camera.main.transform.rotation * Vector3.forward,
                    Camera.main.transform.rotation * Vector3.up
                );
            }
        }
    }

    void OnPlayerEnterRange()
    {
        // Tampilkan prompt
    }

    void OnPlayerExitRange()
    {
        HidePrompt();

        if (CurrentNearestNPC == this)
            CurrentNearestNPC = null;
    }

    public void HidePrompt()
    {
        if (interactPromptObject != null)
            interactPromptObject.SetActive(false);
    }

    // ================================================
    // MULAI DIALOG - dipanggil dari PlayerController
    // ================================================
    public void Interact()
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"[NPCDialogue] {gameObject.name} tidak punya DialogueData!");
            return;
        }

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.StartDialogue(dialogueData);
    }

    // ================================================
    // Visualisasi range di editor
    // ================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
