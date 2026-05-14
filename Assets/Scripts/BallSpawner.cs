using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;
    public HandManager handManager;

    private GameObject heldBall;
    private Transform spawnHand;
    private OVRInput.Controller throwController;

    void Update()
    {
        // Spawn hand and throw controller are always the hand NOT holding the paddle
        if (handManager.isRightHand)
        {
            spawnHand = leftControllerAnchor;
            throwController = OVRInput.Controller.LTouch;
        }
        else
        {
            spawnHand = rightControllerAnchor;
            throwController = OVRInput.Controller.RTouch;
        }

        OVRInput.Button spawnButton = OVRInput.Button.One;

        // Press to spawn
        if (OVRInput.GetDown(spawnButton, throwController))
        {
            SpawnHeldBall();
        }

        // Hold to keep ball at hand
        if (heldBall != null && OVRInput.Get(spawnButton, throwController))
        {
            heldBall.transform.position = spawnHand.position;
            heldBall.transform.rotation = spawnHand.rotation;
        }

        // Release to throw
        if (heldBall != null && OVRInput.GetUp(spawnButton, throwController))
        {
            ThrowBall();
        }
    }

    void SpawnHeldBall()
    {
        if (heldBall != null) return;

        // Don't spawn if a ball already exists in the scene
        if (GameObject.FindGameObjectWithTag("Ball") != null) return;

        heldBall = Instantiate(ballPrefab, spawnHand.position, spawnHand.rotation);
        Rigidbody rb = heldBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void ThrowBall()
    {
        Rigidbody rb = heldBall.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        // Use the correct controller velocity — the one holding the ball
        Vector3 localVelocity = OVRInput.GetLocalControllerVelocity(throwController);
        Vector3 worldVelocity = Camera.main.transform.TransformDirection(localVelocity);

        rb.linearVelocity = worldVelocity;
        heldBall = null;
    }
}