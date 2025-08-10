using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBehaviour : MonoBehaviour
{
    public bool buttonClicked = false;
    public GameObject playerAdder;
    public List<Button> buttons;
    public List<string> nama;
    public Dictionary<string, Button> buttonDictionary = new Dictionary<string, Button>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.instance.skorGameList.Clear();
        GameManager.instance.gameMode = $"Menu{GameManager.instance.bahasa}";

        StartCoroutine(
            NetworkManager.instance.UpdateClientStatusType("siap", "esp32", Player => Debug.Log("[MenuBehaviour]Success updating ESP32 Status"))
        );

        for (int i = 0; i < nama.Count && i < buttons.Count; i++)
        {
            // capture both the name and the button in locals
            string levelName = nama[i];
            Button btn = buttons[i];

            if (levelName == "Main")
            {
                btn.onClick.AddListener(() =>
                {
                    playerAdder.SetActive(true);
                });

                return;
            }

            // populate your dictionary if you still need it
            buttonDictionary[levelName] = btn;

            // add a listener that uses the captured levelName
            btn.onClick.AddListener(() =>
            {
                buttonClicked = true;
                SceneGameManager.instance.LoadLevel(levelName);

            });
        }
    }

    private Coroutine _pollCoroutine;

    void OnEnable()
    {
        // start polling once, after server is up
        StartCoroutine(WaitThenStartPolling());
    }

    void OnDisable()
    {
        if (_pollCoroutine != null)
            StopCoroutine(_pollCoroutine);
    }

    private IEnumerator WaitThenStartPolling()
    {
        // wait until serverStarted is true
        while (!NetworkManager.instance.serverStarted)
            yield return null;

        // then poll once per second
        _pollCoroutine = StartCoroutine(PollPlayers());
    }

    private IEnumerator PollPlayers()
    {
        while (!buttonClicked)
        {
            yield return NetworkManager.instance.GetAllPlayers(
                players => Debug.Log("[MenuBehaviour] Got ESP32 list: " + players.Length),
                err => Debug.LogError("[MenuBehaviour] GetAllPlayers failed: " + err)
            );
            yield return new WaitForSeconds(0.2f);
        }
    }
}
