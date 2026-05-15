using UnityEngine;

public class PaddleZones : MonoBehaviour
{
    [Header("Zone Radii (inner → outer, in local units)")]
    public float zone1Radius = 0.04f;
    public float zone2Radius = 0.08f;
    public float zone3Radius = 0.12f;
    public float zone4Radius = 0.18f;

    [Header("Audio Clips (assign in Inspector)")]
    public AudioClip zone1Clip;
    public AudioClip zone2Clip;
    public AudioClip zone3Clip;
    public AudioClip zone4Clip;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Zone Center")]
    public Transform zoneCenter; // assign your 'center' child GameObject here

    [Header("Zone Visibility (Scene view only)")]
    public bool showZone1 = true;
    public bool showZone2 = true;
    public bool showZone3 = true;
    public bool showZone4 = true;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Call this from your ball collision script, passing the world-space contact point.
    /// </summary>
    public void HandleHit(Vector3 worldContactPoint)
    {
        Vector3 centerWorld = zoneCenter != null ? zoneCenter.position : transform.position;
        float dist = Vector3.Distance(worldContactPoint, centerWorld);

        AudioClip clip = GetZoneClip(dist);
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }

    private AudioClip GetZoneClip(float dist)
    {
        if (dist <= zone1Radius) return zone1Clip;
        if (dist <= zone2Radius) return zone2Clip;
        if (dist <= zone3Radius) return zone3Clip;
        return zone4Clip;
    }

    void OnDrawGizmos()
    {
        if (zoneCenter == null) return;

        if (showZone1) DrawZoneSphere(zone1Radius, Color.green);
        if (showZone2) DrawZoneSphere(zone2Radius, Color.yellow);
        if (showZone3) DrawZoneSphere(zone3Radius, Color.red);
        if (showZone4) DrawZoneSphere(zone4Radius, new Color(1f, 0f, 1f));

        // Center dot always visible
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(zoneCenter.position, 0.005f);
    }

    private void DrawZoneSphere(float radius, Color color)
    {
        // Translucent fill
        Gizmos.color = new Color(color.r, color.g, color.b, 0.08f);
        Gizmos.DrawSphere(zoneCenter.position, radius);

        // Solid outline
        Gizmos.color = color;
        Gizmos.DrawWireSphere(zoneCenter.position, radius);
    }
}