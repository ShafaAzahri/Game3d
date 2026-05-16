using UnityEngine;

public class TimeSystem : MonoBehaviour
{
    public static TimeSystem Instance;

    [Header("Time Settings")]

    [Range(0, 24)]
    public float currentTime = 12f;

    // 60 = 1 menit game per detik nyata
    public float timeSpeed = 60f;

    [Header("Clock")]

    public int hour;
    public int minute;

    [Header("Skybox")]

    // Sunrise
    public Material sunriseSkybox;

    // Siang
    public Material daySkybox;

    // Sunset
    public Material sunsetSkybox;

    // Malam
    public Material nightSkybox;

    // Skybox aktif saat ini
    private Material currentSkybox;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateSkybox();
    }

    void Update()
    {
        UpdateTime();
        UpdateClock();
        UpdateSkybox();
    }

    void UpdateTime()
    {
        // Menambah waktu
        currentTime += Time.deltaTime * (timeSpeed / 3600f);

        // Reset jika lebih dari 24 jam
        if (currentTime >= 24)
        {
            currentTime = 0;
        }
    }

    void UpdateClock()
    {
        // Ambil jam
        hour = Mathf.FloorToInt(currentTime);

        // Ambil menit
        minute = Mathf.FloorToInt(
            (currentTime - hour) * 60
        );
    }

    void UpdateSkybox()
    {
        Material targetSkybox;

        // =========================
        // SUNRISE
        // 05:00 - 06:59
        // =========================
        if (hour >= 5 && hour < 7)
        {
            targetSkybox = sunriseSkybox;
        }

        // =========================
        // SIANG
        // 07:00 - 16:59
        // =========================
        else if (hour >= 7 && hour < 17)
        {
            targetSkybox = daySkybox;
        }

        // =========================
        // SUNSET
        // 17:00 - 18:59
        // =========================
        else if (hour >= 17 && hour < 19)
        {
            targetSkybox = sunsetSkybox;
        }

        // =========================
        // MALAM
        // 19:00 - 04:59
        // =========================
        else
        {
            targetSkybox = nightSkybox;
        }

        // Ganti skybox hanya jika berubah
        if (currentSkybox != targetSkybox)
        {
            currentSkybox = targetSkybox;

            RenderSettings.skybox = currentSkybox;

            // Refresh lighting
            DynamicGI.UpdateEnvironment();

            Debug.Log(
                "Skybox berubah menjadi: " +
                currentSkybox.name
            );
        }
    }

    // Optional
    public string GetTimeString()
    {
        return hour.ToString("00") + ":" + minute.ToString("00");
    }
}