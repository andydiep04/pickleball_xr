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
    // How far forward (toward player, +Z) the AI can step to meet the ball
    public float aiMaxAdvanceZ = -3.5f;

    [Header("Hit Settings")]
    public float hitSpeed = 10f;
    [Range(0f, 1f)] public float missChance = 0.15f;

    [Header("Paddle Tuning")]
    public Vector3 paddleRestLocalPos = new Vector3(0.5f, 0.8f, 0f);
    // How far in front (local Z) the paddle reaches at contact
    public float swingReachZ = 1.4f;
    // How far behind (local Z) the windup pulls back
    public float windupReachZ = -0.8f;

    // ── Swing state machine ──────────────────────────────────────────
    private enum SwingState { Idle, Windup, Swing, FollowThrough }
    private SwingState swingState = SwingState.Idle;

    // Trigger a swing when ball is this close in Z to the AI
    private const float SWING_TRIGGER_Z_DIST = 2.2f;

    // Phase lerp speeds
    private const float WINDUP_SPEED      = 16f;
    private const float SWING_SPEED       = 55f;   // violent snap forward
    private const float FOLLOW_SPEED      = 12f;
    private const float RETURN_SPEED      = 7f;

    // Y rotation is ALWAYS 90° — paddle face always toward player (+Z world)
    private const float PADDLE_Y_ROT = 90f;

    // Computed swing targets (local space)
    private Vector3 windupLocal;
    private Vector3 swingLocal;

    // Rotation targets — only X and Z axes ever change; Y is locked to 90
    // Windup: tilt top of paddle back (negative X rotation = face tips away from player)
    private const float WINDUP_ROT_X = -35f;   // paddle face angled upward/back
    private const float WINDUP_ROT_Z =  15f;   // slight side tilt

    // Contact: drive through — positive X tilts face forward/down (topspin)
    private const float SWING_ROT_X  =  20f;
    private const float SWING_ROT_Z  = -10f;

    // Rest: flat face toward player
    private const float REST_ROT_X   =   0f;
    private const float REST_ROT_Z   =   0f;

    // ── Runtime state ────────────────────────────────────────────────
    private Rigidbody ballRb;
    private Transform ball;
    private GameManager gameManager;

    private Vector3 targetBodyPosition;
    private Vector3 predictedBallPos;
    private bool hasMissed = false;
    private float reactionTimer = 0f;

    // Track paddle world position each frame so AIPaddleHit can read velocity
    [HideInInspector] public Vector3 paddleVelocityWorld;
    [HideInInspector] public Vector3 bodyVelocityWorld;
    private Vector3 _lastPaddleWorldPos;
    private Vector3 _lastBodyWorldPos;

    void Start()
    {
        targetBodyPosition = new Vector3(0f, transform.position.y, aiBaselineZ);
        gameManager = FindFirstObjectByType<GameManager>();

        aiPaddle.localPosition = paddleRestLocalPos;
        aiPaddle.localRotation = MakeLockedRotation(REST_ROT_X, REST_ROT_Z);
        _lastPaddleWorldPos = aiPaddle.position;
        _lastBodyWorldPos = transform.position;
    }

    void Update()
    {
        FindBall();

        if (ball == null || ballRb == null)
        {
            targetBodyPosition = new Vector3(0f, transform.position.y, aiBaselineZ);
            MoveBody();
            RunSwingStateMachine();
            TrackPaddleVelocity();
            return;
        }

        bool ballComingTowardAI = ball.position.z < 0f && ballRb.linearVelocity.z < -0.1f;

        if (ballComingTowardAI)
        {
            reactionTimer += Time.deltaTime;
            if (reactionTimer >= reactionDelay)
                PredictAndMove();

            // Idle tracking: keep paddle loosely in front of body at ball height
            if (swingState == SwingState.Idle)
                IdleTrackBall();
        }
        else
        {
            reactionTimer = 0f;
            if (swingState == SwingState.Idle)
                hasMissed = false;

            // Retreat back to baseline (Z) and anticipate X loosely
            float anticipatedX = Mathf.Clamp(
                ball.position.x * 0.5f, -courtHalfWidth, courtHalfWidth);
            targetBodyPosition = new Vector3(
                anticipatedX, transform.position.y, aiBaselineZ);
        }

        // Decide whether to start a swing
        if (swingState == SwingState.Idle && ballComingTowardAI)
            TryTriggerSwing();

        MoveBody();
        RunSwingStateMachine();
        TrackPaddleVelocity();
    }

    // ── Paddle velocity tracking (used by AIPaddleHit) ──────────────
    void TrackPaddleVelocity()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        paddleVelocityWorld = (aiPaddle.position - _lastPaddleWorldPos) / dt;
        bodyVelocityWorld   = (transform.position  - _lastBodyWorldPos)  / dt;
        _lastPaddleWorldPos = aiPaddle.position;
        _lastBodyWorldPos   = transform.position;
    }

    // ── Locked-axis rotation helper ──────────────────────────────────
    // Y is always PADDLE_Y_ROT; only X and Z tilt the paddle face
    Quaternion MakeLockedRotation(float xDeg, float zDeg)
    {
        return Quaternion.Euler(xDeg, PADDLE_Y_ROT, zDeg);
    }

    // ── Ball finding ─────────────────────────────────────────────────
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

    // ── Body positioning ─────────────────────────────────────────────
    void PredictAndMove()
    {
        if (ballRb == null) return;

        Vector3 vel = ballRb.linearVelocity;
        float speedZ = Mathf.Abs(vel.z);
        if (speedZ < 0.01f) return;

        // Predict where the ball will be when it reaches the AI baseline
        float timeToBaseline = Mathf.Abs(ball.position.z - aiBaselineZ) / speedZ;
        predictedBallPos = ball.position
            + vel * timeToBaseline
            + 0.5f * Physics.gravity * timeToBaseline * timeToBaseline;
        predictedBallPos.y = Mathf.Clamp(predictedBallPos.y, 0.3f, 2.5f);

        // Also find where the ball will be sooner — step forward to intercept
        // rather than waiting at the baseline. Meet it at swingReachZ in front of us.
        float interceptZ = Mathf.Clamp(ball.position.z + 0.8f, aiMaxAdvanceZ, aiBaselineZ);
        float timeToIntercept = Mathf.Abs(ball.position.z - interceptZ) / Mathf.Max(speedZ, 0.01f);
        Vector3 interceptPos = ball.position
            + vel * timeToIntercept
            + 0.5f * Physics.gravity * timeToIntercept * timeToIntercept;

        Vector3 aimPos = interceptPos;
        if (hasMissed)
            aimPos.x += Random.Range(0.8f, 1.5f) * (Random.value > 0.5f ? 1 : -1);

        float clampedX = Mathf.Clamp(aimPos.x, -courtHalfWidth, courtHalfWidth);
        // Step forward in Z to intercept — clamped so AI never crosses mid-court
        float clampedZ = Mathf.Clamp(interceptZ, aiMaxAdvanceZ, aiBaselineZ);

        targetBodyPosition = new Vector3(clampedX, transform.position.y, clampedZ);
    }

    void MoveBody()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, targetBodyPosition, moveSpeed * Time.deltaTime);
    }

    // ── Idle: keep paddle loosely in front of body at ball height ────
    // Y rotation stays locked; only X/Z allowed to tilt subtly
    void IdleTrackBall()
    {
        // Target position: in front of body (positive local Z), at ball height
        Vector3 ballLocal = transform.InverseTransformPoint(ball.position);
        Vector3 targetLocal = new Vector3(
            Mathf.Clamp(ballLocal.x, -0.6f, 0.6f),
            Mathf.Clamp(ballLocal.y, 0.3f, 2.0f),
            Mathf.Clamp(ballLocal.z, -0.1f, 0.3f)   // stay roughly in front
        );

        aiPaddle.localPosition = Vector3.Lerp(
            aiPaddle.localPosition, targetLocal, paddleSpeed * 0.4f * Time.deltaTime);

        // Keep rotation flat (face toward player) during idle
        aiPaddle.localRotation = Quaternion.Lerp(
            aiPaddle.localRotation,
            MakeLockedRotation(REST_ROT_X, REST_ROT_Z),
            paddleSpeed * 0.5f * Time.deltaTime);
    }

    // ── Swing trigger ────────────────────────────────────────────────
    void TryTriggerSwing()
    {
        float zDist = Mathf.Abs(ball.position.z - transform.position.z);
        if (zDist > SWING_TRIGGER_Z_DIST) return;

        // --- Windup target: pull paddle behind body ---
        float ballLocalY = transform.InverseTransformPoint(predictedBallPos != Vector3.zero
            ? predictedBallPos : ball.position).y;

        windupLocal = new Vector3(
            paddleRestLocalPos.x,
            Mathf.Clamp(ballLocalY, 0.4f, 2.0f),
            windupReachZ   // behind the body
        );

        // --- Swing target: push paddle forward to contact point ---
        Vector3 contactLocal = transform.InverseTransformPoint(
            predictedBallPos != Vector3.zero ? predictedBallPos : ball.position);

        swingLocal = new Vector3(
            Mathf.Clamp(contactLocal.x, -0.8f, 0.8f),
            Mathf.Clamp(contactLocal.y, 0.3f, 2.0f),
            swingReachZ    // in FRONT of the body — this is what drives the hit
        );

        swingState = SwingState.Windup;
    }

    // ── Swing state machine ──────────────────────────────────────────
    void RunSwingStateMachine()
    {
        switch (swingState)
        {
            // ── Idle: drift back to rest ──────────────────────────────
            case SwingState.Idle:
                if (ball == null || ballRb == null || ball.position.z > 0f)
                {
                    aiPaddle.localPosition = Vector3.Lerp(
                        aiPaddle.localPosition, paddleRestLocalPos, RETURN_SPEED * Time.deltaTime);
                    aiPaddle.localRotation = Quaternion.Lerp(
                        aiPaddle.localRotation,
                        MakeLockedRotation(REST_ROT_X, REST_ROT_Z),
                        RETURN_SPEED * Time.deltaTime);
                }
                break;

            // ── Windup: pull paddle back ──────────────────────────────
            case SwingState.Windup:
                aiPaddle.localPosition = Vector3.Lerp(
                    aiPaddle.localPosition, windupLocal, WINDUP_SPEED * Time.deltaTime);
                aiPaddle.localRotation = Quaternion.Lerp(
                    aiPaddle.localRotation,
                    MakeLockedRotation(WINDUP_ROT_X, WINDUP_ROT_Z),
                    WINDUP_SPEED * Time.deltaTime);

                if (Vector3.Distance(aiPaddle.localPosition, windupLocal) < 0.12f)
                    swingState = SwingState.Swing;
                break;

            // ── Swing: snap paddle forward through the contact point ──
            case SwingState.Swing:
                aiPaddle.localPosition = Vector3.Lerp(
                    aiPaddle.localPosition, swingLocal, SWING_SPEED * Time.deltaTime);
                aiPaddle.localRotation = Quaternion.Lerp(
                    aiPaddle.localRotation,
                    MakeLockedRotation(SWING_ROT_X, SWING_ROT_Z),
                    SWING_SPEED * Time.deltaTime);

                if (Vector3.Distance(aiPaddle.localPosition, swingLocal) < 0.15f)
                    swingState = SwingState.FollowThrough;
                break;

            // ── Follow-through: hold forward, wait for ball to leave ──
            case SwingState.FollowThrough:
                aiPaddle.localPosition = Vector3.Lerp(
                    aiPaddle.localPosition, swingLocal, FOLLOW_SPEED * Time.deltaTime);
                aiPaddle.localRotation = Quaternion.Lerp(
                    aiPaddle.localRotation,
                    MakeLockedRotation(SWING_ROT_X, SWING_ROT_Z),
                    FOLLOW_SPEED * Time.deltaTime);

                if (ball == null || ball.position.z > 0.5f ||
                    (ballRb != null && ballRb.linearVelocity.z > 0f))
                    swingState = SwingState.Idle;
                break;
        }
    }

    // ── Reset (called by GameManager on point) ───────────────────────
    public void ResetBallReference()
    {
        ball = null;
        ballRb = null;
        reactionTimer = 0f;
        hasMissed = false;
        swingState = SwingState.Idle;

        aiPaddle.localPosition = paddleRestLocalPos;
        aiPaddle.localRotation = MakeLockedRotation(REST_ROT_X, REST_ROT_Z);
    }
}