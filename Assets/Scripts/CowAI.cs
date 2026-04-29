using UnityEngine;

public class CowAI : MonoBehaviour
{
    public Animator animator;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float moveRadius = 3f; // radius kandang
    public float stopDistance = 0.2f;

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
    }

    void Update()
    {
        timer += Time.deltaTime;

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

    void ChooseAction()
    {
        int action = Random.Range(0, 3);

        // 0 = idle, 1 = idle variation, 2 = jalan
        if (action == 0)
        {
            Idle();
        }
        else if (action == 1)
        {
            IdleVariation();
        }
        else
        {
            Walk();
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

        Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
        targetPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            // Rotate ke arah jalan
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Sampai tujuan
        if (Vector3.Distance(transform.position, targetPosition) < stopDistance)
        {
            Idle();
        }
    }

    void SetNextAction()
    {
        timer = 0;
        currentActionTime = Random.Range(minActionTime, maxActionTime);
    }
}