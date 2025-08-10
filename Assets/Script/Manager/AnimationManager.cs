using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager instance;

    [Header("Heart Animators (Hati 0–2)")]
    // Assign your three heart Animator components in the Inspector
    public Animator[] hatiAnimators = new Animator[3];

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Triggers the 'isSalah' bool on heart index (0–2), plays the wrong‐answer crack,
    /// then resets the bool so it only fires once.
    /// </summary>
    public void PlayHatiSalah(int index)
    {
        if (!ValidIndex(index)) return;
        Animator anim = hatiAnimators[index];

        // Set isSalah true to transition Idle→HatiRetak
        anim.SetBool("isSalah", true);
    }

    /// <summary>
    /// Triggers the 'isBenar' bool on heart index (0–2), plays the correct‐answer reset,
    /// then resets the bool so it only fires once.
    /// </summary>
    public void PlayHatiBenar()
    {
        Animator[] anim = hatiAnimators;

        foreach (Animator item in anim)
        {
            // Set isBenar true to transition HatiRetak→HatiReset
            item.SetBool("isBenar", true);
        }
    }

    /// <summary>
    /// Helper to ensure 0 ≤ index < 3.
    /// </summary>
    private bool ValidIndex(int i)
    {
        if (hatiAnimators == null || i < 0 || i >= hatiAnimators.Length)
        {
            Debug.LogWarning($"[AnimationManager] Invalid heart index: {i}");
            return false;
        }
        return true;
    }
}
