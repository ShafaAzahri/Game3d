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

    [Header("Audio (Ditemukan Otomatis)")]
    public AudioClip bgmClip;
    public AudioClip clickSFX;

    private AudioSource bgmSource;
    private AudioSource sfxSource;
    private bool isLoading = false;

    private void Start()
    {
        // 1. Setup BGM & SFX AudioSources
        SetupAudio();

        // 2. Setup listeners untuk tombol New, Continue, Quit
        if (newGameButton  != null) newGameButton.onClick.AddListener(OnNewGame);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (quitButton     != null) quitButton.onClick.AddListener(OnQuit);

        // 3. Tambahkan click sound ke SEMUA button di scene secara dinamis
        var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            btn.onClick.AddListener(PlayClickSound);
        }

        // Continue hanya aktif kalau ada save
        if (continueButton != null)
            continueButton.interactable = SaveManager.HasSave(slot);

        // Pastikan game tidak dalam keadaan pause/time-scale aneh
        Time.timeScale = 1f;
    }

    private void SetupAudio()
    {
        // Pastikan ada AudioListener di scene agar audio bisa terdengar
        if (Object.FindObjectOfType<AudioListener>() == null)
        {
            var cam = Camera.main != null ? Camera.main : Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log("[MainMenuManager] Menambahkan AudioListener ke kamera: " + cam.name);
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[MainMenuManager] Menambahkan AudioListener ke GameObject MainMenuManager");
            }
        }

        // BGM Source
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.4f;

            if (bgmClip == null)
                bgmClip = Resources.Load<AudioClip>("Music/lagu main menu");

            bgmSource.clip = bgmClip;
            if (bgmClip != null)
            {
                bgmSource.Play();
                Debug.Log("[MainMenuManager] BGM 'lagu main menu' berhasil diputar.");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Gagal memuat BGM di 'Resources/Music/lagu main menu'!");
            }
        }

        // SFX Source
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = 0.5f;

            if (clickSFX == null)
                clickSFX = Resources.Load<AudioClip>("Music/click button");

            if (clickSFX == null)
            {
                Debug.LogWarning("[MainMenuManager] Gagal memuat SFX klik di 'Resources/Music/click button'!");
            }
        }
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && clickSFX != null)
        {
            sfxSource.PlayOneShot(clickSFX);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────────────────────

    public void OnNewGame()
    {
        if (isLoading) return;
        StartCoroutine(DelayNewGame());
    }

    private IEnumerator DelayNewGame()
    {
        isLoading = true;
        yield return new WaitForSecondsRealtime(0.2f);

        if (GameManager.Instance != null)
            GameManager.Instance.NewGame(slot);

        string target = !string.IsNullOrEmpty(cutsceneScene) ? cutsceneScene : gameplayScene;
        SceneManager.LoadScene(target);
    }

    public void OnContinue()
    {
        if (isLoading) return;
        if (!SaveManager.HasSave(slot)) return;

        StartCoroutine(DelayContinue());
    }

    private IEnumerator DelayContinue()
    {
        isLoading = true;
        yield return new WaitForSecondsRealtime(0.2f);

        if (GameManager.Instance == null) yield break;
        if (!GameManager.Instance.LoadGame(slot)) yield break;

        string target = GameManager.Instance.Data.sceneName;
        if (string.IsNullOrEmpty(target)) target = gameplayScene;

        StartCoroutine(LoadThenApply(target));
    }

    public void OnQuit()
    {
        StartCoroutine(DelayQuit());
    }

    private IEnumerator DelayQuit()
    {
        yield return new WaitForSecondsRealtime(0.2f);
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
