using UnityEngine;

/// <summary>
/// Simple Day/Night cycle - only rotates sun & moon lights.
/// Does NOT touch skybox, fog, or ambient settings.
/// 
/// SETUP:
/// 1. Attach to empty GameObject
/// 2. Assign Directional Light as "sunLight"
/// 3. (Optional) Create 2nd Directional Light for moon
/// 4. Disable old TimeSystem
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("Time")]
    [Range(0, 24)]
    public float currentTime = 8f;
    [Tooltip("60 = 1 menit game per detik real")]
    public float timeSpeed = 60f;

    [Header("Clock")]
    public int hour;
    public int minute;

    [Header("Sun")]
    public Light sunLight;
    [Range(0, 2)]
    public float sunMaxIntensity = 1.2f;
    public Gradient sunColor;

    [Header("Sun Visual")]
    [Tooltip("Sphere yang terlihat sebagai matahari di langit")]
    public Transform sunVisual;
    public float sunDistance = 200f;

    [Header("Moon")]
    public Light moonLight;
    [Range(0, 1)]
    public float moonMaxIntensity = 0.2f;

    [Header("Moon Visual")]
    [Tooltip("Sphere yang terlihat sebagai bulan di langit")]
    public Transform moonVisual;
    public float moonDistance = 200f;

    void Awake()
    {
        Instance = this;

        // Default sun color gradient if not set
        if (sunColor == null || sunColor.colorKeys.Length < 2)
        {
            sunColor = new Gradient();
            sunColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0f),    // sunrise (orange)
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.3f), // morning (warm white)
                    new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f),    // noon (white)
                    new GradientColorKey(new Color(1f, 0.95f, 0.85f), 0.7f), // afternoon
                    new GradientColorKey(new Color(1f, 0.4f, 0.2f), 1f),     // sunset (red-orange)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                }
            );
        }
    }

    void Update()
    {
        // Update time
        currentTime += Time.deltaTime * (timeSpeed / 3600f);
        if (currentTime >= 24f) currentTime -= 24f;

        hour = Mathf.FloorToInt(currentTime);
        minute = Mathf.FloorToInt((currentTime - hour) * 60f);

        // Rotate sun & moon
        UpdateSun();
        UpdateMoon();
    }

    void UpdateSun()
    {
        if (sunLight == null) return;

        float sunAngle = ((currentTime - 6f) / 24f) * 360f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);

        float sunDot = Mathf.Clamp01(Vector3.Dot(-sunLight.transform.forward, Vector3.up));

        if (sunDot > 0.01f)
        {
            sunLight.enabled = true;
            sunLight.intensity = sunDot * sunMaxIntensity;

            float dayT = Mathf.InverseLerp(6f, 18f, currentTime);
            sunLight.color = sunColor.Evaluate(dayT);
        }
        else
        {
            sunLight.enabled = false;
        }

        // Position sun visual (sphere) in sky
        if (sunVisual != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Place sun visual far away in the direction the light comes FROM
                Vector3 sunDir = -sunLight.transform.forward;
                sunVisual.position = cam.transform.position + sunDir * sunDistance;
                sunVisual.gameObject.SetActive(sunDot > 0.01f);
            }
        }
    }

    void UpdateMoon()
    {
        if (moonLight == null) return;

        float moonAngle = ((currentTime - 6f) / 24f) * 360f + 180f;
        moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);

        float moonDot = Mathf.Clamp01(Vector3.Dot(-moonLight.transform.forward, Vector3.up));

        if (moonDot > 0.01f && (sunLight == null || !sunLight.enabled))
        {
            moonLight.enabled = true;
            moonLight.intensity = moonDot * moonMaxIntensity;
            moonLight.color = new Color(0.6f, 0.7f, 0.9f);
        }
        else
        {
            moonLight.enabled = false;
        }

        // Position moon visual in sky
        if (moonVisual != null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 moonDir = -moonLight.transform.forward;
                moonVisual.position = cam.transform.position + moonDir * moonDistance;
                bool moonVisible = moonDot > 0.01f && (sunLight == null || !sunLight.enabled);
                moonVisual.gameObject.SetActive(moonVisible);
            }
        }
    }

    public string GetTimeString()
    {
        return hour.ToString("00") + ":" + minute.ToString("00");
    }
}
