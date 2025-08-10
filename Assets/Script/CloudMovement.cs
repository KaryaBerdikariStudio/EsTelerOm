using UnityEngine;

/// <summary>
/// Attach this script to each cloud sprite to make it drift horizontally
/// and bob up and down. When it exits the camera's left edge,
/// it loops back to the right edge dynamically, while enforcing
/// absolute Y-position limits (world coordinates).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CloudMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the cloud moves horizontally (units/sec)")]
    public float horizontalSpeed = 1f;

    [Header("Vertical Bobbing")]
    [Tooltip("Amplitude of the bobbing sine wave (units)")]
    public float amplitude = 0.5f;
    [Tooltip("Frequency of the bobbing sine wave (cycles/sec)")]
    public float frequency = 1f;

    [Header("Absolute Y Limits")]
    [Tooltip("Minimum world Y-position the cloud can reach")]
    public float minY = -1.5f;
    [Tooltip("Maximum world Y-position the cloud can reach")]
    public float maxY = 3.5f;

    // internal state
    private float initialY;
    private float phaseOffset;

    // screen bounds
    private float leftBoundaryX;
    private float rightBoundaryX;

    void Start()
    {
        initialY = transform.position.y;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("CloudMovement: No Main Camera found.");
            return;
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float halfWidth = sr.bounds.extents.x;
        Vector3 leftEdge = cam.ViewportToWorldPoint(new Vector3(0f, 0.5f, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        Vector3 rightEdge = cam.ViewportToWorldPoint(new Vector3(1f, 0.5f, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        leftBoundaryX = leftEdge.x - halfWidth;
        rightBoundaryX = rightEdge.x + halfWidth;
    }

    void Update()
    {
        // Move horizontally and loop
        float newX = transform.position.x - horizontalSpeed * Time.deltaTime;
        if (newX < leftBoundaryX) newX = rightBoundaryX;

        // Calculate sine-based bobbing
        float t = Time.time * frequency * 2f * Mathf.PI + phaseOffset;
        float rawOffset = Mathf.Sin(t) * amplitude;
        float newY = initialY + rawOffset;

        // Clamp to absolute world limits
        newY = Mathf.Clamp(newY, minY, maxY);

        transform.position = new Vector3(newX, newY, transform.position.z);
    }
}
