using UnityEngine;
using System.Collections;

public class PlayerControllerRumah : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float turnSmoothTime = 0.15f;

    [Header("Animator")]
    public Animator animator;

    [Header("Ground")]
    public float groundCheckDistance = 5f;
    public LayerMask groundLayer;
    public float playerHeightOffset = 1.2f;

    [Header("Obstacle")]
    public float obstacleCheckDistance = 0.6f;
    public LayerMask obstacleLayer;

    private float horizontal;
    private float vertical;

    private float turnSmoothVelocity;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // =========================
        // INPUT
        // =========================
        horizontal = -Input.GetAxis("Horizontal"); // Dibalik
        vertical = -Input.GetAxis("Vertical");     // Dibalik

        Vector3 inputMove =
            new Vector3(horizontal, 0f, vertical);

        float speed = inputMove.magnitude;

        // =========================
        // ANIMATION
        // =========================
        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                speed,
                0.1f,
                Time.deltaTime
            );
        }

        // =========================
        // MOVEMENT
        // =========================
        MoveCharacter(inputMove);
    }

    void MoveCharacter(Vector3 inputMove)
    {
        if (Camera.main == null) return;

        // =========================
        // CAMERA DIRECTION
        // =========================
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // =========================
        // MOVE DIRECTION
        // =========================
        Vector3 move =
            camForward * inputMove.z +
            camRight * inputMove.x;

        Vector3 nextPos = transform.position;

        // =========================
        // ROTATION
        // =========================
        if (move.magnitude > 0.1f)
        {
            float targetAngle =
                Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;

            float smoothAngle =
                Mathf.SmoothDampAngle(
                    transform.eulerAngles.y,
                    targetAngle,
                    ref turnSmoothVelocity,
                    turnSmoothTime
                );

            transform.rotation =
                Quaternion.Euler(0f, smoothAngle, 0f);

            // =========================
            // OBSTACLE CHECK
            // =========================
            Ray ray =
                new Ray(
                    transform.position + Vector3.up * 1f,
                    transform.forward
                );

            RaycastHit hit;

            if (!Physics.Raycast(
                ray,
                out hit,
                obstacleCheckDistance,
                obstacleLayer))
            {
                nextPos +=
                    transform.forward *
                    moveSpeed *
                    Time.deltaTime;
            }
        }

        // =========================
        // GROUND CHECK
        // =========================
        Ray groundRay =
            new Ray(
                nextPos + Vector3.up * 3f,
                Vector3.down
            );

        RaycastHit groundHit;

        if (Physics.Raycast(
            groundRay,
            out groundHit,
            groundCheckDistance,
            groundLayer))
        {
            nextPos.y =
                groundHit.point.y + playerHeightOffset;
        }

        transform.position = nextPos;
    }
}