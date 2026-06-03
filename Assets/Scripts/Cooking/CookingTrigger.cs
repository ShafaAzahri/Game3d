using UnityEngine;

/// <summary>
/// Taruh script ini di GameObject Tungku.
/// Pastikan Tungku punya Collider dengan isTrigger = true.
/// 
/// SETUP:
/// 1. Attach ke Tungku
/// 2. Tambah Box/Sphere Collider, centang "Is Trigger"
/// 3. Assign cookingCanvas (Canvas Memasak)
/// 4. promptUI akan otomatis dicari dari CookingPromptUI
/// </summary>
public class CookingTrigger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag 'PromptMasak' dari Hierarchy ke sini")]
    public GameObject promptUI;

    [Tooltip("Drag 'PanelMemasak' dari Hierarchy ke sini")]
    public GameObject cookingCanvas;

    private bool playerInRange = false;
    private bool isOpen = false;

    void Start()
    {
        if (promptUI != null)
            promptUI.SetActive(false);

        if (cookingCanvas != null)
            cookingCanvas.SetActive(false);
    }

    void Update()
    {
        // Buka canvas memasak
        if (playerInRange && !isOpen && Input.GetKeyDown(KeyCode.G))
        {
            OpenCooking();
        }

        // Tutup canvas memasak
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseCooking();
        }
    }

    void OpenCooking()
    {
        isOpen = true;

        if (cookingCanvas != null)
            cookingCanvas.SetActive(true);

        if (promptUI != null)
            promptUI.SetActive(false);

        // Unlock cursor untuk UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause game
        Time.timeScale = 0f;
    }

    public void CloseCooking()
    {
        isOpen = false;

        if (cookingCanvas != null)
            cookingCanvas.SetActive(false);

        if (promptUI != null && playerInRange)
            promptUI.SetActive(true);

        // Lock cursor kembali
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Resume game
        Time.timeScale = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (!isOpen && promptUI != null)
                promptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (promptUI != null)
                promptUI.SetActive(false);

            // Auto tutup kalau player pergi
            if (isOpen)
                CloseCooking();
        }
    }
}
