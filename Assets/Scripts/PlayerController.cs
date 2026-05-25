using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private float turnSmoothVelocity;
    private CapsuleCollider col;

    [Header("Movement")]
    public float turnSmoothTime = 0.15f;
    public float moveSpeed = 5f;
    public float sprintSpeed = 9f;

    [Header("Dash")]
    public float dashSpeed = 14f;
    public float dashDuration = 0.15f;

    [Header("References")]
    public Animator animator;

    [Header("Tool")]
    public GameObject hoe;

    [Header("Ground")]
    public float groundCheckDistance = 8f;
    public LayerMask groundLayer;
    public LayerMask stairLayer;
    public float playerHeightOffset = 0f;

    [Header("Obstacle")]
    public float obstacleCheckDistance = 0.6f;
    public LayerMask obstacleLayer;

    private float horizontal;
    private float vertical;

    private bool isSprinting;
    private bool isDashing;

    // =========================
    // IDLE SYSTEM
    // =========================
    private float idleTimer = 0f;
    private float randomTimer = 0f;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        col = GetComponent<CapsuleCollider>();
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
        // SPRINT SYSTEM
        // =========================
        bool sprintHold =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetMouseButton(1);

        isSprinting = sprintHold;
        animator.SetBool("isSprinting", isSprinting);

        // =========================
        // ANIMATOR SPEED
        // =========================
        float animSpeed = isSprinting ? 2f : speed;
        animator.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);

        // =========================
        // IDLE SYSTEM
        // =========================
        if (speed < 0.1f)
        {
            idleTimer += Time.deltaTime;
            randomTimer += Time.deltaTime;

            if (idleTimer > 2f)
            {
                if (randomTimer > 6f)
                {
                    int rand = Random.Range(0, 2);
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
        // DASH SYSTEM
        // =========================
        if (Input.GetMouseButtonDown(1))
        {
            StartCoroutine(Dash());
        }

        // =========================
        // INPUT CANGKUL
        // =========================
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (speed < 0.1f && animator.GetInteger("IdleRandom") == 0)
            {
                StartCoroutine(CangkulRoutine());
            }
        }

        MoveCharacter(inputMove);
    }

    // =========================
    // DASH
    // =========================
    IEnumerator Dash()
    {
        if (isDashing)
            yield break;

        isDashing = true;
        animator.SetBool("isSprinting", true);

        float timer = 0f;

        while (timer < dashDuration)
        {
            transform.position += transform.forward * dashSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        animator.SetBool("isSprinting", false);
        isDashing = false;
    }

    // =========================
    // CANGKUL SYSTEM
    // =========================
    IEnumerator CangkulRoutine()
    {
        if (hoe != null)
            hoe.SetActive(true);

        animator.SetTrigger("Cangkul");

        yield return new WaitForSeconds(1.2f);

        if (hoe != null)
            hoe.SetActive(false);
    }

    // =========================
    // MOVEMENT
    // =========================
    void MoveCharacter(Vector3 inputMove)
    {
        if (isDashing)
            return;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camForward * inputMove.z + camRight * inputMove.x;
        Vector3 nextPos = transform.position;

        // =========================
        // SMOOTH ROTATION
        // =========================
        if (move.magnitude > 0.1f)
        {
            float targetAngle = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg;
            float currentY = transform.eulerAngles.y;
            float smoothAngle = Mathf.SmoothDampAngle(
                currentY, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

            // =========================
            // OBSTACLE CHECK
            // =========================
            Ray forwardRay = new Ray(
                transform.position + Vector3.up * 1f,
                transform.forward);
            RaycastHit forwardHit;
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

            if (!Physics.Raycast(forwardRay, out forwardHit, obstacleCheckDistance, obstacleLayer))
            {
                nextPos += transform.forward * currentSpeed * Time.deltaTime;
            }
        }

        // =========================
        // GROUND CHECK
        // =========================
        LayerMask combinedMask = groundLayer | stairLayer;

        Ray ray = new Ray(nextPos + Vector3.up * 3f, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, groundCheckDistance, combinedMask))
        {
            float targetY = hit.point.y + playerHeightOffset;
            float distanceToTarget = Mathf.Abs(transform.position.y - targetY);

            // Snap langsung kalau jauh (turun dari tangga / teleport)
            if (distanceToTarget > 0.5f)
                nextPos.y = targetY;
            else
                nextPos.y = Mathf.Lerp(transform.position.y, targetY, 20f * Time.deltaTime);
        }

        transform.position = nextPos;
    }
}