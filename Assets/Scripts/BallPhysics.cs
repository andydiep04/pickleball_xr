using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallPhysics : MonoBehaviour
{
    [Range(1f, 3f)]
    public float hitMultiplier = 1.5f;

    private float hitCooldown = 0f;
    private const float HIT_COOLDOWN_DURATION = 0.15f;

    private Rigidbody rb;
    private PaddlePhysics paddle;
    private SphereCollider sphereCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        paddle = FindFirstObjectByType<PaddlePhysics>();
    }

    void FixedUpdate()
    {
        hitCooldown -= Time.fixedDeltaTime;
        if (paddle == null) return;

        CheckSweptPaddleHit();
    }

    void CheckSweptPaddleHit()
    {
        float ballRadius = sphereCollider.radius * transform.localScale.x;
        Collider paddleCollider = paddle.GetComponent<Collider>();

        // Check where ball was last frame and where it is now
        Vector3 ballPrev = rb.position - rb.linearVelocity * Time.fixedDeltaTime;
        Vector3 ballCurrent = rb.position;

        // Sweep the ball's path as a capsule (prev to current position)
        // and check if it intersects the paddle
        Vector3 sweepDir = ballCurrent - ballPrev;
        float sweepDist = sweepDir.magnitude;

        bool overlapping = false;
        Vector3 hitNormal = Vector3.zero;
        Vector3 hitPoint = Vector3.zero;

        if (sweepDist > 0.001f)
        {
            // Cast from previous position toward current
            if (Physics.SphereCast(
                ballPrev,
                ballRadius,
                sweepDir.normalized,
                out RaycastHit hit,
                sweepDist,
                LayerMask.GetMask("Paddle")))
            {
                if (hit.collider == paddleCollider)
                {
                    overlapping = true;
                    hitNormal = hit.normal;
                    hitPoint = hit.point;
                }
            }
        }

        // Also check current overlap in case ball spawned inside or got stuck
        if (!overlapping)
        {
            Vector3 closest = paddleCollider.ClosestPoint(ballCurrent);
            Vector3 toCenter = ballCurrent - closest;
            float dist = toCenter.magnitude;

            if (dist < ballRadius)
            {
                overlapping = true;
                hitNormal = dist < 0.001f ? paddle.transform.forward : toCenter.normalized;
                hitPoint = closest;
            }
        }

        if (overlapping && hitCooldown <= 0f)
        {
            // Push ball out of paddle
            rb.position = hitPoint + hitNormal * (ballRadius + 0.002f);
            ApplyHit(hitNormal);
        }
    }

    void ApplyHit(Vector3 normal)
    {
        Vector3 reflected = Vector3.Reflect(rb.linearVelocity, normal);

        Vector3 paddleContrib = Vector3.zero;
        if (Vector3.Dot(paddle.Velocity, -normal) > 0)
            paddleContrib = paddle.Velocity * hitMultiplier;

        rb.linearVelocity = reflected + paddleContrib;
        hitCooldown = HIT_COOLDOWN_DURATION;
    }
}