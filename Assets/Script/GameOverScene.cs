using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameOverScene : MonoBehaviour
{
    public Button retryButton;
    public Button nextLevelButton;
    public Button submitButton;
    public Label menangKalah;
    public GameObject gameOverPanel;
    public UIDocument gameOverDoc;
    public VisualElement rootHangmanUI, rootGameOver, rootGameOverParent;
    public List<PlayerDatabase.PlayerData> players;


    private void OnEnable()
    {
        rootHangmanUI = LevelManager.instance.rootHangman;
        rootGameOverParent = LevelManager.instance.gameOverPopUp;

        rootGameOver = gameOverDoc.rootVisualElement;

        Debug.Log()
        rootGameOverParent.Add(rootGameOver);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        players = PlayerDatabase.Instance.Players;

        retryButton.clicked +=(() => StartCoroutine(Retry()));
        nextLevelButton.clicked+=(() =>
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
