using UnityEngine;

/// <summary>
/// Komponen sederhana untuk memutar musik latar (BGM) di scene tertentu.
/// Cukup pasang di GameObject baru di scene (misal: "BGMManager").
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Header("BGM Settings")]
    [Tooltip("Nama file BGM di folder Resources/Music (tanpa ekstensi).")]
    public string bgmName = "lagu untuk scene dunia";

    [Range(0f, 1f)]
    public float volume = 0.4f;
    public bool loop = true;

    private AudioSource audioSource;

    void Start()
    {
        // 1. Pastikan ada AudioListener di scene agar suara terdengar
        if (Object.FindAnyObjectByType<AudioListener>() == null)
        {
            var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.gameObject.AddComponent<AudioListener>();
                Debug.Log($"[SceneBGM] Menambahkan AudioListener ke kamera: {cam.name}");
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
                Debug.Log("[SceneBGM] Menambahkan AudioListener ke GameObject SceneBGM");
            }
        }

        // 2. Setup AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = loop;
        audioSource.volume = volume;
        audioSource.playOnAwake = false;

        // 3. Muat file audio dari Resources/Music/
        string resourcePath = "Music/" + bgmName;
        AudioClip clip = Resources.Load<AudioClip>(resourcePath);

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            Debug.Log($"[SceneBGM] Berhasil memutar BGM: {resourcePath}");
        }
        else
        {
            Debug.LogWarning($"[SceneBGM] Gagal memuat BGM di path: Resources/{resourcePath}");
        }
    }
}
