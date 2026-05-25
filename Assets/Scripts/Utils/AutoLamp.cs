using UnityEngine;

public class AutoLamp : MonoBehaviour
{
    [Header("Lamp Components")]
    public Light lampLight;

    [Header("Optional Emission")]
    public Renderer lampRenderer;

    public Material lampOnMaterial;
    public Material lampOffMaterial;

    void Update()
    {
        if (TimeSystem.Instance == null) return;

        int hour = TimeSystem.Instance.hour;

        // Nyala jam 19:00 - 04:59
        bool isNight = (hour >= 19 || hour < 5);

        // Aktif/nonaktif lampu
        lampLight.enabled = isNight;

        // Optional ganti material bohlam
        if (lampRenderer != null)
        {
            lampRenderer.material =
                isNight ? lampOnMaterial : lampOffMaterial;
        }
    }
}