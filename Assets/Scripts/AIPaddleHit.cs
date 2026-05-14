using UnityEngine;

public class AIPaddleHit : MonoBehaviour
{
    private GameManager gameManager;
    private AIOpponent aiOpponent;
    private float hitCooldown = 0f;
    private const float HIT_COOLDOWN = 0.5f;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        aiOpponent = GetComponentInParent<AIOpponent>();
    }

    void FixedUpdate()
    {
        hitCooldown -= Time.fixedDeltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hitCooldown > 0f) return;
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        float speed = aiOpponent != null ? aiOpponent.hitSpeed : 6f;

        // ── Use paddle swing velocity as the primary direction source ──
        // This ties the hit physics directly to the swing motion, so a
        // forward swing always sends the ball forward.
        Vector3 paddleVel = aiOpponent != null ? aiOpponent.paddleVelocityWorld : Vector3.zero;

        Vector3 hitDir;

        if (paddleVel.sqrMagnitude > 0.5f)
        {
            // Blend the paddle's swing direction with a guaranteed +Z bias
            // so even off-axis swings still push the ball toward the player.
            Vector3 swingDir = paddleVel.normalized;

            // Guarantee the ball always travels toward the player (positive Z)
            float zBias = Mathf.Max(swingDir.z, 0.3f);

            hitDir = new Vector3(
                swingDir.x * 0.6f + Random.Range(-0.15f, 0.15f),  // allow side aiming, add tiny spread
                Mathf.Clamp(swingDir.y * 0.4f + Random.Range(0.05f, 0.2f), 0.08f, 0.45f),
                zBias
            ).normalized;
        }
        else
        {
            // Fallback when paddle hasn't moved: aim straight toward player
            // with mild random X spread and slight loft
            hitDir = new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(0.1f, 0.25f),
                1f
            ).normalized;
        }

        // Final safety: Z must be positive or ball goes backwards
        if (hitDir.z < 0.25f)
        {
            hitDir.z = 0.25f;
            hitDir = hitDir.normalized;
        }

        rb.linearVelocity = hitDir * speed;

        if (gameManager != null)
            gameManager.SetLastHitter(GameManager.LastHitter.AI);

        hitCooldown = HIT_COOLDOWN;
    }
}