using UnityEngine;

public class TreeWind : MonoBehaviour
{
    public float windStrength = 2f;
    public float windSpeed = 1f;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float swayX = Mathf.Sin(Time.time * windSpeed) * windStrength;
        float swayZ = Mathf.Cos(Time.time * windSpeed * 0.7f) * windStrength;

        transform.localRotation =
            startRotation * Quaternion.Euler(swayX, 0f, swayZ);
    }
}