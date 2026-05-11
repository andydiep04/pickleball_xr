using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public GameObject ballPrefab;
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;

    public HandManager handManager;

    private GameObject heldBall;
    private Transform spawnHand; // the hand NOT holding the paddle

    void Update()
    {
        // The throwing hand is whichever hand isn't holding the paddle
        spawnHand = handManager.isRightHand ? leftControllerAnchor : rightControllerAnchor;

        // Correct button: A (Button.One) if paddle is right, X (Button.Three) if paddle is left
        // OVRInput.Button spawnButton = handManager.isRightHand
        //     ? OVRInput.Button.Three        // X button on left controller
        //     : OVRInput.Button.One;         // A button on right controller

        OVRInput.Controller spawnController = handManager.isRightHand
            ? OVRInput.Controller.LTouch
            : OVRInput.Controller.RTouch;

        // Hold button to hold ball at hand position
        if (OVRInput.GetDown(OVRInput.Button.One, spawnController))
        {
            SpawnHeldBall();
        }

        // Keep held ball glued to the throwing hand while button is held
        if (heldBall != null && OVRInput.Get(OVRInput.Button.One, spawnController))
        {
            heldBall.transform.position = spawnHand.position;
            heldBall.transform.rotation = spawnHand.rotation;
        }

        // Release → throw
        if (heldBall != null && OVRInput.GetUp(OVRInput.Button.One, spawnController))
        {
            ThrowBall();
        }
    }

    void SpawnHeldBall()
    {
        // Don't spawn a second ball if one is already held
        if (heldBall != null) return;

        heldBall = Instantiate(ballPrefab, spawnHand.position, spawnHand.rotation);

        // Freeze physics while holding
        Rigidbody rb = heldBall.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void ThrowBall()
    {
        Rigidbody rb = heldBall.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        OVRInput.Controller throwController = handManager.isRightHand
            ? OVRInput.Controller.LTouch
            : OVRInput.Controller.RTouch;

        // Convert local controller velocity to world space
        Vector3 localVelocity = OVRInput.GetLocalControllerVelocity(throwController);
        Vector3 worldVelocity = Camera.main.transform.TransformDirection(localVelocity);

        rb.linearVelocity = worldVelocity;
        heldBall = null;
    }
}