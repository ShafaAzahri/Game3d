using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Memutar video cutscene lalu lanjut ke scene gameplay.
/// - Otomatis memuat scene gameplay secara asinkron (di background) selama video diputar.
/// - Otomatis lanjut saat video selesai atau jika di-skip.
/// - Bisa di-skip: tekan Space / Enter / Esc / klik.
/// - Kalau VideoClip tidak ada/valid, langsung lanjut (anti-stuck).
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Scene Tujuan")]
    public string nextScene = "Dunia";

    [Header("UI")]
    public GameObject skipHint;

    private bool loading = false;
    private AsyncOperation asyncLoad;
    private bool videoFinished = false;

    private float skipCooldown = 1.0f; // Jeda waktu sebelum input skip aktif (mencegah carry-over click dari Main Menu)
    private float activeTimer = 0f;

    private void Start()
    {
        // 1. Mulai memuat scene berikutnya di background secara asinkron
        StartCoroutine(LoadSceneAsyncCoroutine());

        // 2. Putar video
        if (videoPlayer != null)
        {
            // Pastikan camera ter-assign dengan benar dan dinamis demi menghindari referensi rusak/stale
            if (videoPlayer.targetCamera == null || videoPlayer.targetCamera.gameObject == null)
            {
                videoPlayer.targetCamera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            }

            if (videoPlayer.clip != null)
            {
                Debug.Log("[Cutscene] Memulai pemutaran video: " + videoPlayer.clip.name + " pada kamera: " + videoPlayer.targetCamera.name);
                videoPlayer.isLooping = false;
                videoPlayer.loopPointReached += OnVideoFinished;
                videoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("[Cutscene] VideoClip kosong/tidak valid — langsung lanjut ke " + nextScene);
                videoFinished = true;
                TriggerSceneActivation();
            }
        }
        else
        {
            Debug.LogWarning("[Cutscene] VideoPlayer kosong — langsung lanjut ke " + nextScene);
            videoFinished = true;
            TriggerSceneActivation();
        }
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        yield return null; // Beri waktu 1 frame agar Start selesai

        asyncLoad = SceneManager.LoadSceneAsync(nextScene);
        if (asyncLoad != null)
        {
            // Jangan aktifkan scene baru otomatis sebelum video selesai/di-skip
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                // Di Unity, jika allowSceneActivation = false, progress akan stuck di 0.9
                if (asyncLoad.progress >= 0.9f && videoFinished)
                {
                    asyncLoad.allowSceneActivation = true;
                }
                yield return null;
            }
        }
    }

    private void Update()
    {
        // Update timer jeda input
        activeTimer += Time.deltaTime;
        if (activeTimer < skipCooldown) return;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(0))
        {
            Debug.Log("[Cutscene] Pemain menekan tombol skip cutscene.");
            SkipCutscene();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoFinished = true;
        TriggerSceneActivation();
    }

    private void SkipCutscene()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        videoFinished = true;
        TriggerSceneActivation();
    }

    private void TriggerSceneActivation()
    {
        if (loading) return;
        loading = true;
        Time.timeScale = 1f;

        // Fallback jika pemuatan asinkron gagal atau belum dimulai
        if (asyncLoad == null)
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            // Aktifkan scene asinkron yang sudah siap
            asyncLoad.allowSceneActivation = true;
        }
    }
}
