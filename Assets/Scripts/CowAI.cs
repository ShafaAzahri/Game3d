using UnityEngine;

public class CowAI : MonoBehaviour
{
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float moveRadius = 5f;
    public float stopDistance = 0.3f;
    public float rotationSpeed = 5f;

    [Header("Obstacle Detection")]
    public LayerMask obstacleLayer;
    public float obstacleCheckDistance = 1.2f;

    [Header("Ground")]
    public float groundCheckDistance = 8f;
    public LayerMask groundLayer;
    public float heightOffset = 0f;

    [Header("Cow Avoidance")]
    public LayerMask cowLayer;
    public float avoidDistance = 1.5f;
    public float avoidForce = 2f;

    [Header("Timing")]
    public float minActionTime = 3f;
    public float maxActionTime = 7f;

    private float timer;
    private float currentActionTime;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private bool isMoving = false;

    void Start()
    {
        startPosition = transform.position;

        SetNextAction();
        ChooseAction();
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Always snap to ground (even when idle)
        SnapToGround();

        if (isMoving)
        {
            MoveToTarget();
        }

        if (timer >= currentActionTime)
        {
            ChooseAction();
            SetNextAction();
        }
    }

    void SnapToGround()
    {
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain != null)
        {
            float terrainY = activeTerrain.SampleHeight(transform.position) + activeTerrain.transform.position.y;
            
            // Auto-calculate offset from collider bounds (feet position)
            float offset = heightOffset;
            Collider col = GetComponent<Collider>();
            if (col != null && offset == 0f)
            {
                offset = col.bounds.extents.y;
            }
            
            terrainY += offset;
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, terrainY, 15f * Time.deltaTime);
            transform.position = pos;
        }
    }

    void ChooseAction()
    {
        int action = Random.Range(0, 3);

        switch (action)
        {
            case 0:
                Idle();
                break;

            case 1:
                IdleVariation();
                break;

            case 2:
                Walk();
                break;
        }
    }

    void Idle()
    {
        isMoving = false;

        animator.SetFloat("speed", 0f);
    }

    void IdleVariation()
    {
        isMoving = false;

        animator.SetFloat("speed", 0f);
        animator.SetTrigger("idleVariant");
    }

    void Walk()
    {
        isMoving = true;

        animator.SetFloat("speed", 1f);

        SetRandomTarget();
    }

    void SetRandomTarget()
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * moveRadius;

            Vector3 candidateTarget =
                startPosition +
                new Vector3(randomCircle.x, 0, randomCircle.y);

            Vector3 direction =
                (candidateTarget - transform.position).normalized;

            // CEK PAGAR / TEMBOK
            bool hitObstacle = Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                direction,
                obstacleCheckDistance,
                obstacleLayer
            );

            if (!hitObstacle)
            {
                targetPosition = candidateTarget;
                return;
            }
        }

        // fallback
        targetPosition = startPosition;
    }

    void MoveToTarget()
    {
        Vector3 direction =
            (targetPosition - transform.position).normalized;

        // =========================
        // CEK PAGAR
        // =========================

        bool hitObstacle = Physics.Raycast(
            transform.position + Vector3.up * 0.5f,
            direction,
            obstacleCheckDistance,
            obstacleLayer
        );

        if (hitObstacle)
        {
            SetRandomTarget();
            return;
        }

        // =========================
        // AVOID SAPI LAIN
        // =========================

        Collider[] nearbyCows = Physics.OverlapSphere(
            transform.position,
            avoidDistance,
            cowLayer
        );

        Vector3 avoidDirection = Vector3.zero;

        foreach (Collider cow in nearbyCows)
        {
            if (cow.gameObject == gameObject)
                continue;

            Vector3 pushDir =
                transform.position - cow.transform.position;

            float distance =
                Vector3.Distance(transform.position, cow.transform.position);

            // makin dekat makin kuat dorongannya
            float forceMultiplier = 1f / Mathf.Max(distance, 0.1f);

            avoidDirection +=
                pushDir.normalized * forceMultiplier;
        }

        direction += avoidDirection * avoidForce;

        direction.Normalize();

        // =========================
        // ROTATE
        // =========================

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * rotationSpeed
            );
        }

        // =========================
        // MOVE
        // =========================

        Vector3 nextPos = transform.position + direction * moveSpeed * Time.deltaTime;

        // =========================
        // GROUND CHECK (snap to terrain)
        // =========================

        // Method 1: Use Terrain.SampleHeight (always works with terrain)
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain != null)
        {
            float terrainY = activeTerrain.SampleHeight(nextPos) + activeTerrain.transform.position.y + heightOffset;
            nextPos.y = Mathf.Lerp(transform.position.y, terrainY, 10f * Time.deltaTime);
        }
        else
        {
            // Fallback: Raycast for non-terrain ground
            Ray groundRay = new Ray(nextPos + Vector3.up * 5f, Vector3.down);
            RaycastHit groundHit;
            if (Physics.Raycast(groundRay, out groundHit, groundCheckDistance, groundLayer))
            {
                nextPos.y = Mathf.Lerp(transform.position.y, groundHit.point.y + heightOffset, 10f * Time.deltaTime);
            }
        }

        transform.position = nextPos;

        // =========================
        // SAMPAI TUJUAN
        // =========================

        if (Vector3.Distance(transform.position, targetPosition)
            <= stopDistance)
        {
            Idle();
        }
    }

    void SetNextAction()
    {
        timer = 0;

        currentActionTime =
            Random.Range(minActionTime, maxActionTime);
    }

    // =========================
    // DEBUG GIZMOS
    // =========================

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        // obstacle ray
        Gizmos.color = Color.red;

        Gizmos.DrawRay(
            transform.position + Vector3.up * 0.5f,
            transform.forward * obstacleCheckDistance
        );

        // avoidance radius
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            avoidDistance
        );

        // target
        Gizmos.color = Color.green;

        Gizmos.DrawSphere(targetPosition, 0.2f);
    }
}