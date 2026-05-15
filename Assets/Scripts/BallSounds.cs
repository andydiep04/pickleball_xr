using UnityEngine;

public class BallSounds : MonoBehaviour
{
    public AudioClip bounceSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only play when hitting the floor/court — ignore paddle hits
        if (collision.gameObject.CompareTag("Ball")) return;
        if (collision.gameObject.CompareTag("Paddle")) return;

        // Only play if impact is strong enough — avoids tiny rolling sounds
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < 0.5f) return;

        if (bounceSound != null && audioSource != null)
            audioSource.PlayOneShot(bounceSound);
    }
}