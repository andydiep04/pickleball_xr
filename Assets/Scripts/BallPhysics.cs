using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallPhysics : MonoBehaviour
{
    [Range(0.5f, 1.0f)] public float restitution = 0.85f;
    [Range(0f, 1f)] public float tangentFriction = 0.2f;
    [Range(1f, 3f)] public float hitMultiplier = 1f;

    [Tooltip("Minimum speed the ball will travel after being hit, regardless of swing speed.")]
    public float minHitSpeed = 1.5f;

    private float hitCooldown = 0f;
    private const float HIT_COOLDOWN_DURATION = 0.2f;

    private GameManager gameManager;
    private Rigidbody rb;
    private PaddlePhysics paddle;
    private SphereCollider sphereCollider;
    private Collider paddleCollider;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        paddle = FindFirstObjectByType<PaddlePhysics>();
        if (paddle != null)
            paddleCollider = paddle.GetComponent<Collider>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    void FixedUpdate()
    {
        hitCooldown -= Time.fixedDeltaTime;
        if (paddle == null || paddleCollider == null) return;
        CheckSweptPaddleHit();
    }

    void CheckSweptPaddleHit()
    {
        float ballRadius = sphereCollider.radius * transform.localScale.x;

        // --- Path sweep: where was the ball last step, where is it now ---
        Vector3 ballPrev = rb.position - rb.linearVelocity * Time.fixedDeltaTime;
        Vector3 ballCurrent = rb.position;
        Vector3 sweepDir = ballCurrent - ballPrev;
        float sweepDist = sweepDir.magnitude;

        bool hit = false;
        Vector3 hitPoint = Vector3.zero;
        Vector3 hitNormal = Vector3.zero;

        // Cast 1: sweep the ball's path this frame (catches tunneling)
        if (sweepDist > 0.001f)
        {
            if (Physics.SphereCast(
                ballPrev, ballRadius, sweepDir.normalized,
                out RaycastHit rh, sweepDist + ballRadius,
                ~LayerMask.GetMask("Ball"), // hit everything except ball layer
                QueryTriggerInteraction.Ignore))
            {
                if (rh.collider == paddleCollider)
                {
                    hit = true;
                    hitPoint = rh.point;
                    hitNormal = rh.normal;
                }
            }
        }

        // Cast 2: also cast from paddle toward ball this frame (catches fast paddle swings)
        if (!hit)
        {
            Vector3 paddleToBall = ballCurrent - paddle.transform.position;
            float dist = paddleToBall.magnitude;
            if (dist > 0.001f && Physics.SphereCast(
                paddle.transform.position, ballRadius, paddleToBall.normalized,
                out RaycastHit rh2, dist,
                ~LayerMask.GetMask("Ball"),
                QueryTriggerInteraction.Ignore))
            {
                if (rh2.collider == paddleCollider)
                {
                    hit = true;
                    hitPoint = rh2.point;
                    hitNormal = rh2.normal;
                }
            }
        }

        // Cast 3: overlap check — ball already inside paddle
        if (!hit)
        {
            Vector3 closest = paddleCollider.ClosestPoint(ballCurrent);
            Vector3 toCenter = ballCurrent - closest;
            float dist = toCenter.magnitude;
            if (dist < ballRadius)
            {
                hit = true;
                hitPoint = closest;
                hitNormal = dist < 0.001f
                    ? (ballCurrent - paddle.transform.position).normalized
                    : toCenter.normalized;
            }
        }

        if (hit && hitCooldown <= 0f)
            ResolveHit(hitPoint, hitNormal, ballRadius);
    }

    void ResolveHit(Vector3 hitPoint, Vector3 hitNormal, float ballRadius)
    {
        // Push ball out of paddle immediately
        rb.position = hitPoint + hitNormal * (ballRadius + 0.003f);

        // Velocity of the paddle surface at the hit point
        Vector3 paddleSurfaceVel = paddle.VelocityAtPoint(hitPoint);

        // Relative velocity of ball to paddle surface
        Vector3 relVel = rb.linearVelocity - paddleSurfaceVel;

        // Split into normal and tangential components
        float normalSpeed = Vector3.Dot(relVel, hitNormal);
        Vector3 vNormal = normalSpeed * hitNormal;
        Vector3 vTangent = relVel - vNormal;

        // Only resolve if ball is moving into the paddle
        if (normalSpeed < 0)
            vNormal = -vNormal * restitution;
        else
            vNormal = Vector3.zero; // already moving away, don't reverse

        // Final velocity = reflected normal + dampened tangent + paddle surface velocity
        Vector3 newVel = paddleSurfaceVel
            + vNormal * hitMultiplier
            + vTangent * (1f - tangentFriction);

        // Enforce minimum hit speed so light taps still feel responsive
        if (newVel.magnitude < minHitSpeed)
            newVel = newVel.normalized * minHitSpeed;

        rb.linearVelocity = newVel;
        hitCooldown = HIT_COOLDOWN_DURATION;

        if (gameManager != null)
        gameManager.SetLastHitter(GameManager.LastHitter.Player);
    }
}