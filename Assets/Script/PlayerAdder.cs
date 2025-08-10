using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAdder : MonoBehaviour
{
    public static PlayerAdder instance;

    [Header("Prefabs / UI")]
    public GameObject playerPrefab;      // must contain PlayerForm component
    public Transform spawnPoint;         // parent for instantiated forms
    public Button play;                  // Play button (hidden until all ready)

    // map deviceName -> PlayerForm instance
    public Dictionary<string, PlayerForm> playerObjects = new Dictionary<string, PlayerForm>();

    // snapshot to detect changes quickly
    private HashSet<string> knownPlayerDeviceNames = new HashSet<string>();

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
            return;
        }

        if (play != null)
        {
            play.gameObject.SetActive(false);
            // ensure single listener; remove previous to be safe
            play.onClick.RemoveAllListeners();
            play.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnEnable()
    {
        // initial sync
        SyncWithGameManager();
        UpdatePlayButtonState();
    }

    private void OnDisable()
    {
        // keep things tidy
        if (play != null)
            play.onClick.RemoveListener(OnPlayClicked);
    }

    private void Update()
    {
        // lightweight guard
        if (GameManager.instance == null || GameManager.instance.playersList == null) return;

        bool changed = GameManager.instance.playersList.Count != knownPlayerDeviceNames.Count ||
                       GameManager.instance.playersList.Any(p => !knownPlayerDeviceNames.Contains(p.playerDeviceName));

        if (changed)
        {
            SyncWithGameManager();
        }

        // compute ready state every frame (cheap)
        UpdatePlayButtonState();
    }

    private void OnPlayClicked()
    {
        // optional: require at least 1 player
        if (playerObjects.Count == 0) return;
        // load the main game level
        SceneGameManager.instance.LoadLevel("Main");
    }

    private void UpdatePlayButtonState()
    {
        if (play == null) return;

        // All player forms must exist and be ready
        bool allReady = playerObjects.Count > 0 &&
                        playerObjects.Values.All(pf => pf != null && pf.isReady);

        play.gameObject.SetActive(allReady);
    }

    // inside PlayerAdder class, reuse existing fields

    private void SyncWithGameManager()
    {
        if (GameManager.instance == null || GameManager.instance.playersList == null) return;

        // build device name list from GameManager
        var deviceNames = new List<string>();
        foreach (var p in GameManager.instance.playersList)
            deviceNames.Add(p.playerDeviceName);

        // Remove forms no longer present
        var toRemove = new List<string>();
        foreach (var key in playerObjects.Keys)
        {
            if (!deviceNames.Contains(key))
                toRemove.Add(key);
        }
        foreach (var k in toRemove)
        {
            var pf = playerObjects[k];
            if (pf != null) Destroy(pf.gameObject);
            playerObjects.Remove(k);
            knownPlayerDeviceNames.Remove(k);
        }

        // Add missing forms
        foreach (var player in GameManager.instance.playersList)
        {
            if (!playerObjects.ContainsKey(player.playerDeviceName))
            {
                GameObject go = Instantiate(playerPrefab, spawnPoint);
                go.name = player.playerDeviceName;
                go.transform.localScale = Vector3.one;
                var pf = go.GetComponent<PlayerForm>();
                if (pf == null)
                {
                    Debug.LogError("[PlayerAdder] playerPrefab missing PlayerForm component");
                    Destroy(go);
                    continue;
                }

                pf.playerTextDeviceNameTitle.text = player.playerDeviceName;
                pf.chosenDevice = player.playerDeviceName;

                playerObjects[player.playerDeviceName] = pf;
                knownPlayerDeviceNames.Add(player.playerDeviceName);
            }
        }

        UpdatePlayButtonState();
    }

    private void HandleFormChanged(PlayerForm changedForm)
    {
        // If a form selected a device and marked ready, remove that device from other forms' option lists.
        // If a form cancelled, repopulating full lists (SyncWithGameManager) will restore it.

        // We will recalc globally to keep things simple and stable:
        SyncWithGameManager();
    }

}
