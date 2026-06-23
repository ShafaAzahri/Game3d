using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Memutar video cutscene lalu lanjut ke scene gameplay.
/// - Otomatis lanjut saat video selesai
/// - Bisa di-skip: tekan Space / Enter / Esc / klik
/// - Kalau VideoClip tidak ada/valid, langsung lanjut (anti-stuck)
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

    private void Start()
    {
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnFinished;
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("[Cutscene] VideoClip kosong/tidak valid — langsung lanjut ke " + nextScene);
            GoNext();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetMouseButtonDown(0))
        {
            GoNext();
        }
    }

    private void OnFinished(VideoPlayer vp) => GoNext();

    private void GoNext()
    {
        if (loading) return;
        loading = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(nextScene);
    }
}
