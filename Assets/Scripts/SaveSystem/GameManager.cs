using System;
using UnityEngine;

/// <summary>
/// Otak utama save system. Singleton yang hidup di semua scene (DontDestroyOnLoad).
/// Memegang SaveData yang sedang aktif di memori (GameManager.Instance.Data).
///
/// ALUR:
///   New Game  → NewGame(slot)  : Data direset ke kondisi awal
///   Load Game → LoadGame(slot) : Data dibaca dari file
///   Simpan    → SaveGame()     : kumpulkan state sistem → tulis ke file
///
/// Sistem lain (Inventory, Farming, Player, dll) cukup:
///   - subscribe OnCaptureState  → tulis kondisinya ke Data (saat mau disimpan)
///   - subscribe OnApplyState    → baca Data → terapkan ke dirinya (saat load)
///
/// GameManager dibuat otomatis sebelum scene pertama (RuntimeInitializeOnLoad),
/// jadi tidak perlu ditaruh manual di setiap scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Data permainan yang sedang aktif. Tidak pernah null setelah Awake.</summary>
    public SaveData Data { get; private set; }

    /// <summary>Slot yang sedang dipakai. -1 = belum memilih slot (mis. baru buka game).</summary>
    public int CurrentSlot { get; private set; } = -1;

    /// <summary>Dipanggil sebelum menyimpan. Sistem MENULIS kondisinya ke GameManager.Instance.Data.</summary>
    public event Action OnCaptureState;

    /// <summary>Dipanggil setelah load / new game. Sistem MEMBACA Data lalu menerapkannya.</summary>
    public event Action OnApplyState;

    // ─────────────────────────────────────────────────────────────
    // BOOTSTRAP
    // ─────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Default: ada data kosong di memori biar tidak null sebelum New/Load.
        Data ??= SaveData.CreateDefault();
    }

    private void Start()
    {
#if UNITY_EDITOR
        // Jika masuk langsung ke scene gameplay di editor, otomatis set slot ke 0 agar save system aktif
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (activeScene != "MainMenu" && activeScene != "Cutscene" && CurrentSlot == -1)
        {
            CurrentSlot = 0;
            if (SaveManager.HasSave(0))
            {
                LoadGame(0);
                // Karena kita langsung start di scene ini, terapkan state setelah 1 frame agar sistem lain selesai Awake/Start
                StartCoroutine(ApplyStateNextFrame());
            }
            else
            {
                NewGame(0);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private System.Collections.IEnumerator ApplyStateNextFrame()
    {
        yield return null;
        ApplyLoadedState();
    }
#endif

    // ─────────────────────────────────────────────────────────────
    // NEW / LOAD / SAVE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Mulai permainan baru di slot tertentu. Data direset bersih (storyStep 0).
    /// Catatan: ini hanya menyiapkan data + langsung menulis file slot.
    /// Pindah scene gameplay dilakukan oleh pemanggil (mis. tombol menu).
    /// </summary>
    public void NewGame(int slot)
    {
        Data = SaveData.CreateDefault();
        CurrentSlot = slot;
        SaveManager.Save(slot, Data);   // langsung buat filenya
        Debug.Log($"[GameManager] New Game di slot {slot}.");
    }

    /// <summary>
    /// Muat data dari slot ke memori. Return true kalau berhasil.
    /// Penerapan ke scene (OnApplyState) dipanggil terpisah lewat ApplyLoadedState()
    /// SETELAH scene yang benar selesai dimuat.
    /// </summary>
    public bool LoadGame(int slot)
    {
        var data = SaveManager.Load(slot);
        if (data == null)
        {
            Debug.LogWarning($"[GameManager] LoadGame gagal: slot {slot} kosong/korup.");
            return false;
        }
        Data = data;
        CurrentSlot = slot;
        Debug.Log($"[GameManager] Load slot {slot} ke memori.");
        return true;
    }

    /// <summary>
    /// Terapkan Data yang sudah dimuat ke semua sistem yang men-subscribe.
    /// Panggil ini setelah scene tujuan selesai aktif (Start frame).
    /// </summary>
    public void ApplyLoadedState()
    {
        OnApplyState?.Invoke();
        Debug.Log("[GameManager] State diterapkan ke sistem (OnApplyState).");
    }

    /// <summary>
    /// Simpan permainan saat ini ke slot aktif.
    /// Mengumpulkan kondisi tiap sistem (OnCaptureState) lalu menulis ke file.
    /// </summary>
    public bool SaveGame()
    {
        if (CurrentSlot < 0)
        {
            Debug.LogWarning("[GameManager] SaveGame dibatalkan: belum ada slot aktif (CurrentSlot = -1).");
            return false;
        }

        OnCaptureState?.Invoke();              // sistem menulis kondisinya ke Data
        return SaveManager.Save(CurrentSlot, Data);
    }

    /// <summary>Simpan ke slot tertentu (mis. "Save As" di menu).</summary>
    public bool SaveGameToSlot(int slot)
    {
        CurrentSlot = slot;
        return SaveGame();
    }

    // ─────────────────────────────────────────────────────────────
    // UTIL
    // ─────────────────────────────────────────────────────────────

    public bool HasAnySave()
    {
        for (int i = 0; i < SaveManager.MaxSlots; i++)
            if (SaveManager.HasSave(i)) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // AUTO-SAVE saat keluar / pause
    // ─────────────────────────────────────────────────────────────

    private void OnApplicationQuit()
    {
        if (CurrentSlot >= 0) SaveGame();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && CurrentSlot >= 0) SaveGame();
    }
}
