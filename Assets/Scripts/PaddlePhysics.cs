using UnityEngine;

public class PaddlePhysics : MonoBehaviour
{
    public Vector3 Velocity { get; private set; }
    public Vector3 AngularVelocity { get; private set; }

    private Vector3 lastPosition;
    private Quaternion lastRotation;

    // Ring buffer for smoothing — averages last N samples to kill tracking jitter
    private const int SAMPLE_COUNT = 4;
    private Vector3[] velocitySamples = new Vector3[SAMPLE_COUNT];
    private Vector3[] angularSamples = new Vector3[SAMPLE_COUNT];
    private int sampleIndex = 0;

    void Start()
    {
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (dt <= 0f) return;

        // Linear velocity (instantaneous, this physics step)
        Vector3 instantVel = (transform.position - lastPosition) / dt;

        // Angular velocity from quaternion delta
        Quaternion deltaRot = transform.rotation * Quaternion.Inverse(lastRotation);
        deltaRot.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        Vector3 instantAngVel = axis.normalized * (angle * Mathf.Deg2Rad / dt);
        if (float.IsNaN(instantAngVel.x)) instantAngVel = Vector3.zero;

        // Push into ring buffer
        velocitySamples[sampleIndex] = instantVel;
        angularSamples[sampleIndex] = instantAngVel;
        sampleIndex = (sampleIndex + 1) % SAMPLE_COUNT;

        // Average
        Vector3 vSum = Vector3.zero, aSum = Vector3.zero;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            vSum += velocitySamples[i];
            aSum += angularSamples[i];
        }
        Velocity = vSum / SAMPLE_COUNT;
        AngularVelocity = aSum / SAMPLE_COUNT;

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    // World-space velocity at a specific point on the paddle.
    // Includes the contribution from rotation — critical for wrist-flick hits.
    public Vector3 VelocityAtPoint(Vector3 worldPoint)
    {
        return Velocity + Vector3.Cross(AngularVelocity, worldPoint - transform.position);
    }
}

