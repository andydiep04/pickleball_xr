using UnityEngine;

public class AIOpponent : MonoBehaviour
{
    [Header("References")]
    public Transform aiPaddle;
    public Transform body;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float paddleSpeed = 12f;
    public float reactionDelay = 0.15f;

    [Header("Court Bounds")]
    public float courtHalfWidth = 3.0f;
    public float aiBaselineZ = -5.5f;

    [Header("Hit Settings")]
    public float hitSpeed = 6f;
    [Range(0f, 1f)] public float missChance = 0.15f;
    
    // NEW: Expose the rotation to the Inspector so you can change it while playing!
    [Header("Paddle Tuning")]
    public Vector3 targetPaddleRotation = new Vector3(0f, 90f, 0f);

    private Rigidbody ballRb;
    private Transform ball;
    private GameManager gameManager;

    private Vector3 targetBodyPosition;
    private Vector3 predictedBallPos;
    private bool hasMissed = false;
    private float reactionTimer = 0f;

    private Vector3 paddleRestLocal;

    void Start()
    {
        targetBodyPosition = new Vector3(0f, transform.position.y, aiBaselineZ);
        gameManager = FindFirstObjectByType<GameManager>();

        // We only capture the position now. Rotation is handled by the public variable.
        paddleRestLocal = aiPaddle.localPosition;
    }

    void Update()
    {
        FindBall();

        if (ball == null || ballRb == null)
        {
            targetBodyPosition = new Vector3(0f, transform.position.y, aiBaselineZ);
            MoveBody();
            ReturnPaddleToRest();
            return;
        }

        if (ball.position.z < 0f)
        {
            reactionTimer += Time.deltaTime;
            if (reactionTimer >= reactionDelay)
                PredictAndMove();

            TrackPaddleToBall();
        }
        else
        {
            reactionTimer = 0f;
            hasMissed = false;
            float anticipatedX = Mathf.Clamp(
                ball.position.x * 0.5f, -courtHalfWidth, courtHalfWidth);
            targetBodyPosition = new Vector3(
                anticipatedX, transform.position.y, aiBaselineZ);
            ReturnPaddleToRest();
        }

        MoveBody();
    }

    void FindBall()
    {
        if (ball != null) return;
        GameObject b = GameObject.FindGameObjectWithTag("Ball");
        if (b == null) return;

        ball = b.transform;
        ballRb = b.GetComponent<Rigidbody>();
        hasMissed = false;
        reactionTimer = 0f;
        hasMissed = Random.value < missChance;
    }

    void PredictAndMove()
    {
        if (ballRb == null) return;

        Vector3 vel = ballRb.linearVelocity;
        float speedZ = Mathf.Abs(vel.z);
        if (speedZ < 0.01f) return;

        float timeToReach = Mathf.Abs(ball.position.z - aiBaselineZ) / speedZ;

        predictedBallPos = ball.position
            + vel * timeToReach
            + 0.5f * Physics.gravity * timeToReach * timeToReach;

        predictedBallPos.y = Mathf.Clamp(predictedBallPos.y, 0.3f, 2.5f);

        Vector3 aimPos = predictedBallPos;
        if (hasMissed)
            aimPos.x += Random.Range(0.8f, 1.5f) * (Random.value > 0.5f ? 1 : -1);

        float clampedX = Mathf.Clamp(aimPos.x, -courtHalfWidth, courtHalfWidth);
        targetBodyPosition = new Vector3(clampedX, transform.position.y, aiBaselineZ);
    }

    void MoveBody()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, targetBodyPosition, moveSpeed * Time.deltaTime);
    }

    void TrackPaddleToBall()
    {
        if (ball == null) return;

        Vector3 ballLocal = transform.InverseTransformPoint(ball.position);
        ballLocal.y = Mathf.Clamp(ballLocal.y, 0.3f, 2.5f);

        if (hasMissed)
            ballLocal.x += 1.2f;

        aiPaddle.localPosition = Vector3.Lerp(
            aiPaddle.localPosition, ballLocal, paddleSpeed * Time.deltaTime);

        // Convert your Inspector Vector3 into a Quaternion and apply it
        aiPaddle.localRotation = Quaternion.Lerp(
            aiPaddle.localRotation,
            Quaternion.Euler(targetPaddleRotation),
            paddleSpeed * Time.deltaTime);
    }

    void ReturnPaddleToRest()
    {
        aiPaddle.localPosition = Vector3.Lerp(
            aiPaddle.localPosition, paddleRestLocal, paddleSpeed * Time.deltaTime);
        
        // Convert your Inspector Vector3 into a Quaternion and apply it
        aiPaddle.localRotation = Quaternion.Lerp(
            aiPaddle.localRotation,
            Quaternion.Euler(targetPaddleRotation),
            paddleSpeed * Time.deltaTime);
    }

    public void ResetBallReference()
    {
        ball = null;
        ballRb = null;
        reactionTimer = 0f;
        hasMissed = false;
        
        aiPaddle.localPosition = paddleRestLocal;
        aiPaddle.localRotation = Quaternion.Euler(targetPaddleRotation);
    }
}