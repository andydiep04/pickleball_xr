using UnityEngine;

public class AIPaddleHit : MonoBehaviour
{
    private GameManager gameManager;
    private AIOpponent aiOpponent;
    private float hitCooldown = 0f;
    private const float HIT_COOLDOWN = 0.3f;

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

        // Use collision normal to determine hit direction
        // so ball always reflects away from paddle face correctly
        Vector3 contactNormal = collision.contacts[0].normal;

        // Blend reflection with a target direction toward player
        Vector3 targetDir = new Vector3(
            Random.Range(-0.3f, 0.3f),
            Random.Range(0.15f, 0.35f),
            1f
        ).normalized;

        // 50/50 blend between physics reflection and aimed direction
        Vector3 hitDir = Vector3.Lerp(
            Vector3.Reflect(rb.linearVelocity.normalized, contactNormal),
            targetDir,
            0.5f
        ).normalized;

        // Make sure Z is always positive (toward player)
        if (hitDir.z < 0.1f)
            hitDir.z = 0.3f;
        hitDir.Normalize();

        rb.linearVelocity = hitDir * (aiOpponent != null ? aiOpponent.hitSpeed : 6f);

        if (gameManager != null)
            gameManager.SetLastHitter(GameManager.LastHitter.AI);

        hitCooldown = HIT_COOLDOWN;
    }
}