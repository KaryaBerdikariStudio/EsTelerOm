using UnityEngine;

/// <summary>
/// Moves the GameObject up and down in world?space along a sine wave
/// between specified minimum and maximum Y values, without using physics.
/// </summary>
public class SandeqMovementAuto : MonoBehaviour
{
    [Header("Vertical Range (world Y)")]
    [Tooltip("Lowest Y position the object will reach.")]
    public float minY = 0f;

    [Tooltip("Highest Y position the object will reach.")]
    public float maxY = 2f;

    [Header("Motion Settings")]
    [Tooltip("How many cycles (up + down) per second.")]
    public float frequency = 0.5f;

    // The midpoint between minY and maxY
    private float _midY;

    // Half the total range (amplitude)
    private float _amplitude;

    // Remember the initial X and Z so we only modify Y
    private Vector3 _startPos;

    private void Awake()
    {
        // Compute midpoint and amplitude
        _midY = (minY + maxY) * 0.5f;
        _amplitude = (maxY - minY) * 0.5f;

        // Cache starting X,Z (we'll override Y each frame)
        _startPos = transform.position;
    }

    private void Update()
    {
        MoveSineWithYMaxYMinWorldUnityNotPhysics();
    }

    /// <summary>
    /// Oscillates the object’s Y between minY and maxY using a sine wave.
    /// </summary>
    private void MoveSineWithYMaxYMinWorldUnityNotPhysics()
    {
        // time since start
        float t = Time.time;

        // sine wave: sin(2? * freq * t) ranges [-1,1]
        float sin = Mathf.Sin(2f * Mathf.PI * frequency * t);

        // map to [minY, maxY]: Y = midY + amplitude * sin
        float y = _midY + _amplitude * sin;

        // apply new position in world space
        transform.position = new Vector3(_startPos.x, y, _startPos.z);
    }
}
