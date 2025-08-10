using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGameManager : MonoBehaviour
{
    public static SceneGameManager instance { get; private set; }
    public string currentSceneName = "";
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (currentSceneName != UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)
        {
            currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
    }


    public void LoadMainMenu()
    {
        // Load the main menu scene based on the selected language
        Debug.Log($"[SceneManager] Loading Main Menu: Menu{GameManager.instance.bahasa}");
        StartCoroutine(LoadScene($"Menu{GameManager.instance.bahasa}"));
    }

    public void LoadLevel(string levelName)
    {
        // Load the level scene based on the selected language
        Debug.Log($"[SceneManager] Loading level: {levelName}{GameManager.instance.bahasa}");

        switch (levelName)
        {
            case "Main":
                StartCoroutine(NetworkManager.instance.GetAllPlayers(
                    Player => Debug.Log("[SceneManager] Success updating Unity Status"),
                    err => Debug.LogError("[SceneManager] Failed updating Unity Status: " + err)
                ));
                StartCoroutine(LoadScene($"LevelHangman{GameManager.instance.bahasa}"));
                //StartCoroutine(LevelManager.instance.Start());
                break;
            case "Kamus":
                StartCoroutine(LoadScene($"Kamus{GameManager.instance.bahasa}"));
                break;
            case "Skor":
                StartCoroutine(LoadScene($"Skor{GameManager.instance.bahasa}"));
                break;
            case "TambahKata":
                StartCoroutine(LoadScene($"TambahKata{GameManager.instance.bahasa}"));
                break;
        }
    }
    public void ResetGame(string bahasa)
    {
        // Reset game state here if needed
        LevelManager.instance.kamusUsed.Clear();
        StartCoroutine(DestroyLevel());
        StartCoroutine(LevelManager.instance.Start());
    }

    public void NextLevel()
    {
        // Increment the current level and load the next level scene
        LevelManager.instance.currentLevel++;
        StartCoroutine(DestroyLevel());
        StartCoroutine(LevelManager.instance.Start());
    }

    public void Kamus(string bahasa)
    {
        // Load the dictionary scene based on the selected language
        StartCoroutine(LoadScene($"Kamus{bahasa}"));
    }

    public void MainMenu()
    {
        // Load the main menu scene
        StartCoroutine(LoadScene($"Menu{GameManager.instance.bahasa}"));
    }


    private IEnumerator DestroyLevel()
    {
        Destroy(LevelManager.instance.level);
        yield return null;
    }

    IEnumerator LoadScene(string sceneName)
    {
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        yield return null; // Wait for the next frame to ensure the scene is loaded
    }

}
