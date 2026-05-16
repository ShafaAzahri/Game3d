using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Mouse")]
    public float mouseSensitivity = 200f;

    [Header("Zoom")]
    public float distance = 4f;
    public float zoomSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Vertical Rotation")]
    public float minY = -15f;
    public float maxY = 60f;

    private float xRotation = 15f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // otomatis cari player kalau target kosong
        if (target == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    void LateUpdate()
    {
        // kalau target hilang jangan lanjut
        if (target == null) return;

        // =========================
        // INPUT MOUSE
        // =========================
        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity *
            Time.deltaTime;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity *
            Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation =
            Mathf.Clamp(xRotation, minY, maxY);

        Quaternion rotation =
            Quaternion.Euler(xRotation, yRotation, 0);

        // =========================
        // ZOOM
        // =========================
        float scroll =
            Input.GetAxis("Mouse ScrollWheel");

        distance -= scroll * zoomSpeed;

        distance =
            Mathf.Clamp(distance, minDistance, maxDistance);

        // =========================
        // CAMERA POSITION
        // =========================
        Vector3 targetPosition =
            target.position + Vector3.up * 1.5f;

        Vector3 cameraPosition =
            targetPosition -
            (rotation * Vector3.forward * distance);

        transform.position = cameraPosition;

        // =========================
        // LOOK AT PLAYER
        // =========================
        transform.LookAt(targetPosition);
    }
}