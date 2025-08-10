using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MultiplayerRegistration : MonoBehaviour
{
    [Header("UI References")]
    public GameObject regisPrefab; // Prefab with UIDocument and DaftarMultiplayer component
    [SerializeField] private int maxPlayer = 3;

    private UIDocument uiDocument;
    private VisualElement formsContainer;
    private Button tambahPlayerButton;
    private Button mulaiGameButton;
    private VisualElement regisCanvas;

    public static MultiplayerRegistration Instance { get; private set; }
    public List<DaftarMultiplayer> checker { get; private set; } = new List<DaftarMultiplayer>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
        DontDestroyOnLoad(gameObject);

        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        formsContainer = root.Q<VisualElement>("formsContainer");
        tambahPlayerButton = root.Q<Button>("tambahPlayerButton");
        mulaiGameButton = root.Q<Button>("mulaiGameButton");
    }

    private void Start()
    {
        tambahPlayerButton.clicked += AddNewPlayerForm;
        mulaiGameButton.clicked += StartGame;
        UpdateUiState();
    }

    private void Update()
    {
        UpdateUiState();
    }

    private bool CheckAllRegistrationStatus()
    {
        bool allCompleted = checker.Count > 0 && checker.All(f => f.selesaiDaftarBool);
        return allCompleted;
    }

    private void UpdateUiState()
    {
        tambahPlayerButton.SetEnabled(checker.Count < maxPlayer);
        mulaiGameButton.SetEnabled(CheckAllRegistrationStatus());
    }
    private void AddNewPlayerForm()
    {
        if (checker.Count >= maxPlayer) return;

        GameObject newFormGO = Instantiate(regisPrefab, transform);
        newFormGO.name = $"form_{checker.Count}"; // Set index-based name

        DaftarMultiplayer formController = newFormGO.GetComponent<DaftarMultiplayer>();
        UIDocument formUIDocument = newFormGO.GetComponent<UIDocument>();

        // Add to layout
        VisualElement formRoot = formUIDocument.rootVisualElement.Q<VisualElement>("form");
        formRoot.name = newFormGO.name; // Sync VisualElement name with GameObject
        formsContainer.Add(formRoot);
        formRoot.AddToClassList("form-item");

        // Force layout refresh
        formsContainer.MarkDirtyRepaint();
        formsContainer.schedule.Execute(() => {
            formRoot.MarkDirtyRepaint();
        });

        checker.Add(formController);
        UpdateUiState();
    }

    public void RemovePlayerForm(DaftarMultiplayer form)
    {
        if (form == null || checker == null) return;

        // Remove from list first
        checker.Remove(form);

        // Get references before destruction
        VisualElement formElement = form.root;
        GameObject formGameObject = form.gameObject;

        // Safely remove from UI
        if (formElement != null && formElement.parent == formsContainer.contentContainer)
        {
            formsContainer.contentContainer.Remove(formElement);
        }

        // Destroy GameObject if it exists
        if (formGameObject != null)
        {
            Destroy(formGameObject);
        }

        UpdateUiState();
    }

    private void StartGame()
    {
        StartCoroutine(GotoPlay());
    }

    private IEnumerator GotoPlay()
    {
        // 1) Make sure our local GameManager has the latest players
        GameManager.instance.playerDatas = PlayerDatabase.Instance.Players;

        // Wait until at least one player exists
        yield return new WaitUntil(() =>
            GameManager.instance.playerDatas != null &&
            GameManager.instance.playerDatas.Count > 0
        );

        foreach (var item in GameManager.instance.playerDatas)
        {

            Debug.Log($"{item.playerName} = here");
        }
        // 2) Tell the server “I’m in Hangman mode”
        yield return StartCoroutine(
            NetworkManager.instance.UpdateDeviceStatus(
                "Hangman",
                NetworkManager.CLIENT_NAME,
                NetworkManager.CLIENT_NAME
            )
        );

        // 3) For each ESP32 client, set them to “ready to play”
        foreach (var client in NetworkManager.SseClientUpdate.clientList)
        {
            if (client.type == "esp32")
            {
                yield return StartCoroutine(
                    NetworkManager.instance.UpdateDeviceStatus(
                        "siapBermain",
                        client.nama,
                        client.type
                    )
                );
            }
        }

        // 4) Wait until the SSE client list actually reflects “siapBermain”—
        //    for example, you could wait until a flag on your NetworkManager is set,
        //    or simply pause a moment if your SSE updates come in slightly later.
        yield return new WaitUntil(() =>
            NetworkManager.SseClientUpdate.clientList
                .Where(c => c.type == "esp32")
                .All(c => c.status == "siapBermain")
        );

        // 5) Now load the Hangman level—and yield on it directly so
        //    Unity doesn’t start the load before you finish your network calls.
        AsyncOperation loadOp = SceneManager.LoadSceneAsync("LevelHangman");
        loadOp.allowSceneActivation = true;    // you can also control activation if you want a loading screen

        while (!loadOp.isDone)
        {
            Debug.Log($"Loading progress: {loadOp.progress * 100f:F0}%");
            yield return null;
        }

        Debug.Log("Step 1️⃣: Changing Unity status to 'Hangman' — scene loaded.");
    }


}