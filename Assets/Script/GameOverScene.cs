using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverScene : MonoBehaviour
{
    public Button retryButton;
    public TextMeshProUGUI menangKalah;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        UIManager.instance.inputField.SetActive(false);
        UIManager.instance.stringPlace.SetActive(false);
        

        retryButton.onClick.AddListener(() => StartCoroutine(Retry()));
        menangKalah.text = GameManager.instance.menangAtauKalah;
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
}
