// ScoreSpawner.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Spawns SkorKomboBehaviour prefabs into the 'tempatSkor' container
/// and listens for player status changes to update their scores.
/// </summary>
public class ScoreSpawner : MonoBehaviour
{
    [Header("UI Document & Container")]
    [Tooltip("Your Hangman UI (the UXML with a VisualElement named 'tempatSkor')")]
    public UIDocument hangmanUIDocument;

    [Header("Score Prefab")]
    [Tooltip("Prefab containing a SkorKomboBehaviour + its UIDocument")]
    public GameObject skorPrefab, skorPrefabParent;

    private VisualElement tempatSkorContainer;
    private List<SkorKomboBehaviour> spawnedBehaviours = new List<SkorKomboBehaviour>();
    private Dictionary<string, SkorKomboBehaviour> lookupByName = new Dictionary<string, SkorKomboBehaviour>();

    private void OnEnable()
    {
        // Grab the container element
        tempatSkorContainer = hangmanUIDocument.rootVisualElement.Q<VisualElement>("tempatSkor");
        if (tempatSkorContainer == null)
        {
            Debug.LogError("[ScoreSpawner] Cannot find 'tempatSkor' in hangmanUIDocument.");
            return;
        }

        Debug.Log("Skor Spawner is Active");

        // Wait for players to be ready, then subscribe & spawn
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        // Wait until GameManager has at least one player
        yield return new WaitUntil(() =>
            GameManager.instance != null &&
            GameManager.instance.playerDatas != null &&
            GameManager.instance.playerDatas.Count > 0
        );

        // Subscribe to each player's status change
        foreach (var pd in GameManager.instance.playerDatas)
            pd.OnStatusChanged += OnPlayerStatusChanged;

        // Spawn the UI slots
        SpawnScores(GameManager.instance.playerDatas);
        Debug.Log($"[ScoreSpawner] Spawned {spawnedBehaviours.Count} score UI elements.");
    }

    private void OnDisable()
    {
        if (GameManager.instance?.playerDatas == null) return;
        foreach (var pd in GameManager.instance.playerDatas)
            pd.OnStatusChanged -= OnPlayerStatusChanged;
    }

    public void SpawnScores(List<PlayerDatabase.PlayerData> players)
    {
        // Clear out any previous UI
        tempatSkorContainer.Clear();
        spawnedBehaviours.Clear();
        lookupByName.Clear();

        Debug.Log($"[ScoreSpawner] About to spawn {players.Count} players into '{tempatSkorContainer.name}'.");
        Debug.Log($"[ScoreSpawner] Initial child count: {tempatSkorContainer.childCount}");

        for (int i = 0; i < players.Count; i++)
        {
            var data = players[i];
            Debug.Log($"[ScoreSpawner] Spawning slot for {data.playerName} (index {i})");

            // Instantiate under this spawner directly
            GameObject go = Instantiate(skorPrefab, transform);
            go.name = $"SkorPlayer_{data.playerName}";

            var behaviour = go.GetComponent<SkorKomboBehaviour>();
            if (behaviour == null)
            {
                Debug.LogError($"[ScoreSpawner] Prefab missing SkorKomboBehaviour!");
                Destroy(go);
                continue;
            }

            behaviour.Initialize(data.playerName, 0, 1f);
            spawnedBehaviours.Add(behaviour);
            lookupByName[data.playerName] = behaviour;

            // Grab the UI root immediately
            var uiDoc = go.GetComponent<UIDocument>();
            var rootVE = uiDoc.rootVisualElement;
            rootVE.name = go.name;

            // Defer the Add until UXML has been cloned
            rootVE.schedule.Execute(_ =>
            {
                tempatSkorContainer.Add(rootVE);
                Debug.Log($"[ScoreSpawner] Added '{rootVE.name}'. New childCount = {tempatSkorContainer.childCount}");
            });
        }
    }


    private void OnPlayerStatusChanged(PlayerDatabase.PlayerData player, string status)
    {
        if (lookupByName.TryGetValue(player.playerName, out var behaviour))
            behaviour.UpdateStatus(status);
    }
}
