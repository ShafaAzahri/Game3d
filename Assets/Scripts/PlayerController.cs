using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private float turnSmoothVelocity; // internal untuk SmoothDampAngle
    public float turnSmoothTime = 0.15f; // sesuaikan smoothness
    public Animator animator;
    public float moveSpeed = 5f;

    [Header("Tool")]
    public GameObject hoe;

    [Header("Ground")]
    public float groundCheckDistance = 5f;
    public LayerMask groundLayer;
    public float playerHeightOffset = 1.2f;

    [Header("Obstacle")]
    public float obstacleCheckDistance = 0.6f;
    public LayerMask obstacleLayer;

    private float horizontal;
    private float vertical;

    // =========================
    // IDLE SYSTEM
    // =========================
    private float idleTimer = 0f;
    private float randomTimer = 0f;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // =========================
        // INPUT GERAK
        // =========================
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector3 inputMove = new Vector3(horizontal, 0, vertical);
        float speed = inputMove.magnitude;

        // =========================
        // ANIMATOR SPEED (lari)
        // =========================
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // =========================
        // IDLE SYSTEM (GENSHIN STYLE)
        // =========================
        if (speed < 0.1f)
        {
            idleTimer += Time.deltaTime;
            randomTimer += Time.deltaTime;

            // masuk idle setelah delay
            if (idleTimer > 2f)
            {
                // random idle 2 (stretch)
                if (randomTimer > 6f)
                {
                    int rand = Random.Range(0, 2); // 0 atau 1
                    animator.SetInteger("IdleRandom", rand);

                    randomTimer = 0f;
                }
            }
        }
        else
        {
            idleTimer = 0f;
            randomTimer = 0f;
            animator.SetInteger("IdleRandom", 0);
        }

        animator.SetFloat("IdleDelay", idleTimer);

        // =========================
        // INPUT CANGKUL (F)
        // =========================
        if (Input.GetKeyDown(KeyCode.F))
        {
            // hanya saat diam & tidak sedang idle 2
            if (speed < 0.1f && animator.GetInteger("IdleRandom") == 0)
            {
                StartCoroutine(CangkulRoutine());
            }
        }

        MoveCharacter(inputMove);
    }

    // =========================
    // CANGKUL SYSTEM
    // =========================
    IEnumerator CangkulRoutine()
    {
        // munculkan cangkul
        if (hoe != null)
            hoe.SetActive(true);

        // trigger animasi
        animator.SetTrigger("Cangkul");

        // tunggu durasi animasi (sesuaikan!)
        yield return new WaitForSeconds(1.2f);

        // hilangkan cangkul
        if (hoe != null)
            hoe.SetActive(false);
    }

    // =========================
    // MOVEMENT
    // =========================
    void MoveCharacter(Vector3 inputMove)
    {
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * inputMove.z + camRight * inputMove.x;
        Vector3 nextPos = transform.position;

        // =========================
        // SMOOTH ROTATION & CEK OBSTACLE
        // =========================
        if (move.magnitude > 0.1f)
        {
            // Smooth rotasi karakter
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            float currentY = transform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(currentY, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // Cek obstacle di arah move
            Ray forwardRay = new Ray(transform.position + Vector3.up * 1f, transform.forward);
            RaycastHit forwardHit;
            if (!Physics.Raycast(forwardRay, out forwardHit, obstacleCheckDistance, obstacleLayer))
            {
                nextPos += transform.forward * moveSpeed * Time.deltaTime;
            }
        }

        // =========================
        // GROUND CHECK
        // =========================
        Ray ray = new Ray(nextPos + Vector3.up * 3f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, groundCheckDistance, groundLayer))
        {
            float targetY = hit.point.y + playerHeightOffset;
            nextPos.y = targetY;
        }

        transform.position = nextPos;
    }
}