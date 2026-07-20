using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Pemutar video cutscene awal game yang dijalankan di dalam scene Dunia.
/// - Hanya diputar sekali pada awal New Game.
/// - Mematikan input player, UI, dan BGM selama video diputar.
/// - Mendukung skip dengan jeda cooldown 1 detik.
/// - Setelah selesai, otomatis mengaktifkan in-engine CutsceneManager (jalan ke Nenek).
/// </summary>
public class DuniaVideoCutscenePlayer : MonoBehaviour
{
    [Header("Video Setup")]
    public VideoPlayer videoPlayer;
    public VideoClip cutsceneClip;

    [Header("UI Skip Hint")]
    public GameObject skipHint;

    private bool isPlayingCutscene = false;
    private float skipCooldown = 1.0f;
    private float activeTimer = 0f;

    private List<Canvas> disabledCanvases = new List<Canvas>();
    private SceneBGM sceneBgm;
    private PlayerController player;

    private void Awake()
    {
        // 1. Tentukan apakah cutscene harus diputar
        bool shouldPlay = true;
        if (GameManager.Instance != null)
        {
            shouldPlay = !GameManager.Instance.Data.IsStepDone("video_cutscene_done");
        }

        if (!shouldPlay)
        {
            // Jika sudah pernah diputar, langsung matikan skrip/objek ini
            gameObject.SetActive(false);
            return;
        }

        isPlayingCutscene = true;

        // 2. Temukan referensi BGM, Player, dan CutsceneManager di scene
        sceneBgm = Object.FindAnyObjectByType<SceneBGM>();
        if (sceneBgm != null)
        {
            // Matikan BGM agar tidak bersuara selama video
            sceneBgm.enabled = false;
        }

    }

    private void Start()
    {
        if (!isPlayingCutscene) return;

        // 3. Matikan input pergerakan player
        player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.CanMove = false;
        }

        // 4. Matikan semua Canvas di scene agar UI tertutup
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.enabled)
            {
                c.enabled = false;
                disabledCanvases.Add(c);
            }
        }

        // Tampilkan skip hint jika ada
        if (skipHint != null)
        {
            skipHint.SetActive(true);
            // Cari Canvas di skipHint sendiri (jika ada) agar skipHint tetap tampil
            Canvas skipCanvas = skipHint.GetComponentInParent<Canvas>();
            if (skipCanvas != null && disabledCanvases.Contains(skipCanvas))
            {
                skipCanvas.enabled = true;
                disabledCanvases.Remove(skipCanvas);
            }
        }

        // 5. Setup Video Player
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        videoPlayer.clip = cutsceneClip;
        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;

        if (videoPlayer.clip != null)
        {
            Debug.Log("[DuniaVideoCutscene] Memutar video: " + videoPlayer.clip.name);
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("[DuniaVideoCutscene] Video clip kosong! Menyelesaikan cutscene otomatis.");
            FinishCutscene();
        }
    }

    private void Update()
    {
        if (!isPlayingCutscene) return;

        activeTimer += Time.deltaTime;
        if (activeTimer < skipCooldown) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(0))
        {
            Debug.Log("[DuniaVideoCutscene] Cutscene di-skip oleh pemain.");
            FinishCutscene();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[DuniaVideoCutscene] Cutscene selesai.");
        FinishCutscene();
    }

    private void FinishCutscene()
    {
        if (!isPlayingCutscene) return;
        isPlayingCutscene = false;

        // 1. Berhenti memutar video
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // 2. Kembalikan semua UI/Canvas yang dimatikan
        foreach (var c in disabledCanvases)
        {
            if (c != null) c.enabled = true;
        }

        if (skipHint != null)
        {
            skipHint.SetActive(false);
        }

        // 3. Simpan state di SaveData agar tidak memutar video lagi
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Data.MarkStepDone("video_cutscene_done");
            GameManager.Instance.SaveGame();
        }

        // 4. Hidupkan BGM
        if (sceneBgm != null)
        {
            sceneBgm.enabled = true;
        }

        // 5. Kembalikan kontrol ke player langsung
        if (player != null)
        {
            player.CanMove = true;
        }

        // 6. Hancurkan GameObject pemutar video ini agar bersih
        Destroy(gameObject);
    }
}
