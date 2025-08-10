using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScene : MonoBehaviour
{
    [Header("Text Fields")]
    public TextMeshProUGUI gameOverStateText;
    public TextMeshProUGUI submitStateText;

    [Header("UI Containers")]
    public Transform scoreListParent;     // assign your "ScoreText" container's Transform

    [Header("Prefabs")]
    public GameObject scoreEntryPrefab;// prefab with InputField + child Text for score

    [Header("Buttons / Panels")]
    public GameObject resetGO;            // the Reset button GameObject
    public GameObject nextGO;             // the Next  button GameObject
    public GameObject submitFormGO;       // panel that wraps the submit form
    public Button submitBtn;         // the Submit button component
    public Button menuBtn;           // your Menu button component

    // cached components
    private Button resetBtn;
    private Button nextBtn;

    void Awake()
    {
        // cache button components
        resetBtn = resetGO.GetComponent<Button>();
        nextBtn = nextGO.GetComponent<Button>();

        // hook menu
        menuBtn.onClick.AddListener(() =>
            SceneGameManager.instance.LoadMainMenu()
        );
    }

    void OnEnable()
    {
        // clear any old listeners
        resetBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.RemoveAllListeners();
        submitBtn.onClick.RemoveAllListeners();

        // hide both outcome buttons
        resetGO.SetActive(false);
        nextGO.SetActive(false);

        // show submit UI
        submitFormGO.SetActive(true);
        submitBtn.gameObject.SetActive(true);

        // build the dynamic score list
        InitializeSkor();

        // configure win/lose/reset/next
        ConfigureOutcome();
    }

    private void ConfigureOutcome()
    {
        Debug.Log("[ConfigureOutcome] Called");

        // Clear all old listeners
        nextBtn.onClick.RemoveAllListeners();
        resetBtn.onClick.RemoveAllListeners();
        submitBtn.onClick.RemoveAllListeners();

        bool won = LevelManager.instance.benarCount >= LevelManager.instance.jumlahBenarYangDibutuhkan;
        bool moreLevels = LevelManager.instance.currentLevel < LevelManager.instance.maxLevel;

        if (won)
        {
            gameOverStateText.text = moreLevels
                ? "Kamu menang! Lanjut?"
                : "Kamu Berhasill! Yipeeee!";

            if (moreLevels)
            {
                nextGO.SetActive(true);
                nextBtn.onClick.AddListener(() => {
                    SceneGameManager.instance.NextLevel();
                });
            }
            else
            {
                resetGO.SetActive(true);
                resetBtn.onClick.AddListener(() => {
                    SceneGameManager.instance.ResetGame(GameManager.instance.bahasa);
                });
            }
        }
        else
        {
            gameOverStateText.text = "Kamu Kalah! Permainan selesai!";
            resetGO.SetActive(true);
            resetBtn.onClick.AddListener(() => {
                LevelManager.instance.kamusUsed.Clear();
                SceneGameManager.instance.ResetGame(GameManager.instance.bahasa);
            });
        }

        // Set up submit button
        submitBtn.onClick.AddListener(() => {
            Debug.Log("[Submit] Clicked");
            submitFormGO.SetActive(false);
            submitBtn.gameObject.SetActive(false);
            submitStateText.text = "Sedang mengirim data...";
            StartCoroutine(SubmitScoreCoroutine());
        });
    }


    

    private IEnumerator RestartGame()
    {
        // if your LevelManager.Start is a coroutine you want to re-run
        yield return LevelManager.instance.Start();
    }

    private void InitializeSkor()
    {
        // clear previous entries
        foreach (Transform t in scoreListParent) Destroy(t.gameObject);

        foreach (var player in GameManager.instance.playersList)
        {
            // instantiate a prefab that contains both an InputField and a score Text
            GameObject entryGO = Instantiate(scoreEntryPrefab, scoreListParent);
            entryGO.name = $"SkorEntry_{player.playerName}";
            
            var textmeshproEntry = entryGO.GetComponentInChildren<TextMeshProUGUI>();
            var playeScore = LevelManager.instance.HUDUICanvas
                .transform.Find($"SkorPanel_{player.playerName}").transform.Find("ScoreTotal")
                .GetComponent<TextMeshProUGUI>()
                .text;
            if (textmeshproEntry != null)
            {
                textmeshproEntry.text = $"{player.playerName}: {playeScore}";
            }
            else
            {
                Debug.LogWarning("[GameOver] No TextMeshProUGUI found in scoreEntryPrefab. Please assign it.");
            }
        }
    }

    private IEnumerator SubmitScoreCoroutine()
    {
        foreach (var player in GameManager.instance.skorGameList)
        {
            yield return NetworkManager.instance.SkorPost(
                player.playerName,
                player.playerScore.ToString(),
                _ => Debug.Log("[GameOver] Success updating Skor"),
                err => Debug.LogError("[GameOver] Failed updating Skor: " + err)
            );
        }
        submitStateText.text = "Data berhasil dikirim!";
    }
}
