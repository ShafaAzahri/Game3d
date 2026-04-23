using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target; // Player
    public float mouseSensitivity = 200f;
    public float distance = 4f;        // jarak default kamera
    public float zoomSpeed = 2f;       // kecepatan zoom
    public float minDistance = 2f;     // jarak zoom minimum
    public float maxDistance = 8f;     // jarak zoom maksimum

    public float minY = -15f; // batas bawah
    public float maxY = 60f;  // batas atas

    float xRotation = 0f;
    float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        // =========================
        // ROTASI KAMERA
        // =========================
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);

        // =========================
        // ZOOM
        // =========================
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // =========================
        // POSISI KAMERA
        // =========================
        Vector3 position = target.position - (rotation * Vector3.forward * distance);
        transform.position = position;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}