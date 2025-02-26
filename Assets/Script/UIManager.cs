using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance { get; private set; }

    public KeyboardAvaible keyboardScript;
    public RandomizerKata randomizerScript;
    public GameObject popUpGameOver;
    public GameObject inputField;
    public GameObject keyboard;
    public GameObject stringPlace;
    public GameObject heartsContainer; // Parent of hearts
    public Image[] heartsIcons; // Drag & drop Hearts_{int} images in Inspector

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        popUpGameOver.SetActive(false);
        stringPlace.SetActive(true);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 🔥 Subscribe to GameManager's event
        GameManager.instance.OnHeartsChanged += UpdateHeartsUI;
        UpdateHeartsUI(GameManager.instance.heartsRemaining); // Initialize
    }

    private void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnHeartsChanged -= UpdateHeartsUI;
        }
    }

    // ✅ Updates Hearts UI when heartsRemaining changes
    private void UpdateHeartsUI(int currentHearts)
    {
        for (int i = 0; i < heartsIcons.Length; i++)
        {
            heartsIcons[i].enabled = (i < currentHearts); // 🔥 Enable only the active hearts
        }

        Debug.Log($"❤️ UI Updated: {currentHearts} Hearts Active");
    }
}
