using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScene : MonoBehaviour
{
    public Button retryButton;
    public Button nextLevelButton;
    public Button submitButton;
    public TextMeshProUGUI menangKalah;
    public GameObject gameOverPanel;

    public List<PlayerDatabase.PlayerData> players;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        players = PlayerDatabase.Instance.Players;

        retryButton.onClick.AddListener(() => StartCoroutine(Retry()));
        nextLevelButton.onClick.AddListener(() =>
        {
            LevelManager.instance.levelIndex++;
            StartCoroutine(Retry());
        });
        menangKalah.text = LevelManager.instance.menangAtauKalah;

       
    }

    private IEnumerator Retry()
    {
        
        AsyncOperation retryLevel = SceneManager.LoadSceneAsync("LevelHangman");

        while (!retryLevel.isDone)
        {
            Debug.Log(retryLevel.progress / .9f);

            yield return null;
        }

        yield return retryLevel;
    }

    public bool ShowGameOverPanel(bool show)
    {
        gameOverPanel.SetActive(show);
        return gameOverPanel.activeSelf;
    }


}
