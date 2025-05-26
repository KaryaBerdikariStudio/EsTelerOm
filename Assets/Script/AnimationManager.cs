using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    public static AnimationManager instance;

    [Header("Hati Animators")]
    public Animator hati_1;
    public Animator hati_2;
    public Animator hati_3;
    public Animator resetHati;

    [Header("Salah/Benar Animators")]
    public Animator salah_1;
    public Animator salah_2;
    public Animator salah_3;
    public Animator resetBenar;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Plays the "hati" animation on the given index (1-3).
    /// </summary>
    public void PlayHati(int index)
    {
        ResetHati();
        switch (index)
        {
            case 2: hati_1.SetTrigger("Play"); break;
            case 1: hati_2.SetTrigger("Play"); break;
            case 0: hati_3.SetTrigger("Play"); break;
            default: Debug.LogWarning($"Invalid hati index: {index}"); break;
        }
    }

    /// <summary>
    /// Plays the "salah" animation on the given index (1-3).
    /// </summary>
    public void PlaySalah(int index)
    {
        ResetBenar();
        switch (index)
        {
            case 2: salah_1.SetTrigger("Play"); break;
            case 1: salah_2.SetTrigger("Play"); break;
            case 0: salah_3.SetTrigger("Play"); break;
            default: Debug.LogWarning($"Invalid salah index: {index}"); break;
        }
    }

    /// <summary>
    /// Resets all hati animators to their default state.
    /// </summary>
    public void ResetHati()
    {
        resetHati.SetTrigger("Reset");
    }

    /// <summary>
    /// Resets all salah/benar animators to their default state.
    /// </summary>
    public void ResetBenar()
    {
        resetBenar.SetTrigger("Reset");
    }
}
