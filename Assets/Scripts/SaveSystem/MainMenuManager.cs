using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Mengatur tombol di scene Main Menu (1 slot save).
/// - New Game  : reset save bersih → masuk scene gameplay
/// - Continue  : muat save → masuk scene tersimpan → terapkan state
/// - Quit      : keluar game
///
/// Tombol Continue otomatis non-aktif kalau belum ada save.
/// Referensi tombol di-assign lewat Inspector (atau editor setup script).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Tombol")]
    public Button newGameButton;
    public Button continueButton;
    public Button quitButton;

    [Header("Pengaturan")]
    [Tooltip("Nama scene gameplay yang dibuka saat New Game. Harus ada di Build Settings.")]
    public string gameplayScene = "Dunia";

    [Tooltip("Scene cutscene yang diputar saat New Game (sebelum gameplay). Kosongkan untuk langsung ke gameplay.")]
    public string cutsceneScene = "Cutscene";

    [Tooltip("Slot save yang dipakai (1 slot = 0).")]
    public int slot = 0;

    private bool isLoading = false;

    private void Start()
    {
        if (newGameButton  != null) newGameButton.onClick.AddListener(OnNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuit);

        // Continue hanya aktif kalau ada save
        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSave(slot);

        // Pastikan game tidak dalam keadaan pause/time-scale aneh
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────────────────────

    public void OnNewGame()
    {
        if (isLoading) return;

        // Kalau sudah ada save, konfirmasi sebenarnya idealnya pakai popup.
        // Untuk sekarang: New Game langsung menimpa save lama (mulai bersih).
        if (GameManager.Instance != null)
            GameManager.Instance.NewGame(slot);

        isLoading = true;
        // New Game → putar cutscene dulu (kalau ada), baru gameplay
        string target = !string.IsNullOrEmpty(cutsceneScene) ? cutsceneScene : gameplayScene;
        SceneManager.LoadScene(target);
    }

    public void OnContinue()
    {
        if (isLoading) return;
        if (!SaveManager.HasSave(slot)) return;

        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.LoadGame(slot)) return;

        // Masuk ke scene yang tersimpan, lalu terapkan state setelah scene aktif
        string target = GameManager.Instance.Data.sceneName;
        if (string.IsNullOrEmpty(target)) target = gameplayScene;

        isLoading = true;
        StartCoroutine(LoadThenApply(target));
    }

    public void OnQuit()
    {
        Debug.Log("[MainMenu] Quit.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─────────────────────────────────────────────────────────────
    // INTERNAL
    // ─────────────────────────────────────────────────────────────

    private IEnumerator LoadThenApply(string sceneName)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone) yield return null;

        // Beri 1 frame agar Awake/Start sistem sempat jalan, lalu terapkan state
        yield return null;
        if (GameManager.Instance != null)
            GameManager.Instance.ApplyLoadedState();
    }
}
