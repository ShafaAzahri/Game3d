using UnityEngine;

public class ThirdPersonCameraRumah : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Mouse")]
    public float mouseSensitivity = 200f;

    [Header("Distance")]
    public float distance = 4f;
    public float zoomSpeed = 2f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Vertical Rotation")]
    public float minY = -15f;
    public float maxY = 60f;

    [Header("Collision")]
    public LayerMask wallLayer;
    public float collisionOffset = 0.2f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // =========================
        // MOUSE INPUT
        // =========================
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, minY, maxY);

        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // =========================
        // ZOOM
        // =========================
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // =========================
        // TARGET POSITION
        // =========================
        Vector3 targetPosition = target.position + Vector3.up * 1.5f;

        // posisi kamera yang diinginkan
        Vector3 desiredPosition =
            targetPosition - (rotation * Vector3.forward * distance);

        // =========================
        // CAMERA COLLISION
        // =========================
        RaycastHit hit;

        Vector3 direction =
            (desiredPosition - targetPosition).normalized;

        float rayDistance =
            Vector3.Distance(targetPosition, desiredPosition);

        if (Physics.Raycast(
            targetPosition,
            direction,
            out hit,
            rayDistance,
            wallLayer))
        {
            transform.position =
                hit.point + hit.normal * collisionOffset;
        }
        else
        {
            transform.position = desiredPosition;
        }

        // =========================
        // LOOK AT PLAYER
        // =========================
        transform.LookAt(targetPosition);
    }
}