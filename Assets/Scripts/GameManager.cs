using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Score")]
    public int playerScore = 0;
    public int aiScore = 0;
    public TextMeshProUGUI scoreText;

    [Header("Ball Management")]
    public float outOfBoundsY = -5f;
    public float courtHalfWidth = 3.05f;
    public float courtHalfLength = 6.7f;
    public float stillSpeedThreshold = 0.15f;
    public float stillTimeThreshold = 1.5f;

    public enum LastHitter { None, Player, AI }
    public LastHitter lastHitter = LastHitter.None;

    // Track consecutive bounces on each side
    private int consecutivePlayerSideBounces = 0;
    private int consecutiveAISideBounces = 0;
    private string lastBounceSide = ""; // "player" or "ai"

    private GameObject currentBall;
    private float stillTimer = 0f;
    private bool pointDecided = false; // prevent double scoring

    void Update()
    {
        FindBall();
        if (currentBall == null) return;
        if (pointDecided) return;

        CheckBounces();
        CheckOutOfBounds();
        CheckStill();
    }

    void FindBall()
    {
        if (currentBall != null) return;
        GameObject b = GameObject.FindGameObjectWithTag("Ball");
        if (b == null) return;

        currentBall = b;
        stillTimer = 0f;
        lastHitter = LastHitter.None;
        consecutivePlayerSideBounces = 0;
        consecutiveAISideBounces = 0;
        lastBounceSide = "";
        pointDecided = false;
    }

    void CheckBounces()
    {
        // Detect ball hitting the floor on each side using its Y velocity
        // and Z position to determine which side
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Ball is near the floor and moving upward = just bounced
        bool justBounced = currentBall.transform.position.y < 0.1f
                           && rb.linearVelocity.y > 0.1f;

        if (!justBounced) return;

        float ballZ = currentBall.transform.position.z;
        string bounceSide = ballZ >= 0f ? "player" : "ai";

        if (bounceSide == lastBounceSide)
        {
            // Second consecutive bounce on the same side
            if (bounceSide == "player")
            {
                // Ball bounced twice on player's side — AI scores
                Debug.Log("Double bounce on player side — AI scores");
                ScorePoint(false);
            }
            else
            {
                // Ball bounced twice on AI's side — Player scores
                Debug.Log("Double bounce on AI side — Player scores");
                ScorePoint(true);
            }
        }
        else
        {
            // Bounce switched sides — reset consecutive count
            lastBounceSide = bounceSide;
            consecutivePlayerSideBounces = bounceSide == "player" ? 1 : 0;
            consecutiveAISideBounces = bounceSide == "ai" ? 1 : 0;
            Debug.Log($"Bounce on {bounceSide} side");
        }
    }

    void CheckOutOfBounds()
    {
        if (currentBall == null) return;
        Vector3 pos = currentBall.transform.position;

        bool outY = pos.y < outOfBoundsY;
        bool outX = Mathf.Abs(pos.x) > courtHalfWidth + 0.5f;
        bool outZ = Mathf.Abs(pos.z) > courtHalfLength + 0.5f;

        if (!outY && !outX && !outZ) return;

        // Whoever hit it last loses the point
        if (lastHitter == LastHitter.Player)
        {
            Debug.Log("Out of bounds after player hit — AI scores");
            ScorePoint(false);
        }
        else if (lastHitter == LastHitter.AI)
        {
            Debug.Log("Out of bounds after AI hit — Player scores");
            ScorePoint(true);
        }
        else
        {
            Debug.Log("Out of bounds, no hitter — AI scores");
            ScorePoint(false);
        }
    }

    void CheckStill()
    {
        if (currentBall == null) return;
        Rigidbody rb = currentBall.GetComponent<Rigidbody>();
        if (rb == null) return;

        if (rb.linearVelocity.magnitude < stillSpeedThreshold)
        {
            stillTimer += Time.deltaTime;
            if (stillTimer >= stillTimeThreshold)
            {
                // Ball rolling to a stop counts as a bounce-out
                // whoever's side it stopped on, the other player scores
                float ballZ = currentBall.transform.position.z;
                if (ballZ >= 0f)
                {
                    Debug.Log("Ball stopped on player side — AI scores");
                    ScorePoint(false);
                }
                else
                {
                    Debug.Log("Ball stopped on AI side — Player scores");
                    ScorePoint(true);
                }
            }
        }
        else
        {
            stillTimer = 0f;
        }
    }

    void ScorePoint(bool playerScored)
    {
        if (pointDecided) return;
        pointDecided = true;

        AddScore(playerScored);
        Invoke(nameof(DespawnBall), 0.5f); // small delay so player sees where it landed
    }

    void DespawnBall()
    {
        if (currentBall != null)
        {
            Destroy(currentBall);
            currentBall = null;
        }

        // Reset AI so it searches for the next ball cleanly
        AIOpponent ai = FindFirstObjectByType<AIOpponent>();
        if (ai != null) ai.ResetBallReference();

        stillTimer = 0f;
        lastHitter = LastHitter.None;
        consecutivePlayerSideBounces = 0;
        consecutiveAISideBounces = 0;
        lastBounceSide = "";
        pointDecided = false;
    }

    public void AddScore(bool playerScored)
    {
        if (playerScored) playerScore++;
        else aiScore++;
        UpdateScoreUI();
        Debug.Log($"Score — Player: {playerScore}, AI: {aiScore}");
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"{playerScore} - {aiScore}";
    }

    public void SetLastHitter(LastHitter hitter)
    {
        lastHitter = hitter;
    }
}