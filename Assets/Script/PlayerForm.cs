using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerForm : MonoBehaviour
{
    [Header("UI (assign in prefab)")]
    public TextMeshProUGUI playerTextDeviceNameTitle; // shows device name (e.g. "esp32_1")
    public TMP_InputField playerNameInput;
    public Button ready;
    public Button cancel;

    // public state
    public string chosenDevice { get; set; } = string.Empty;
    public string playerName { get; private set; } = string.Empty;
    public bool isReady = false;

    // originals for restore on Cancel
    private string _originalSelectedDevice;
    private string _originalPlayerName;

    // event to notify manager (PlayerAdder) about changes
    public event Action<PlayerForm> OnFormChanged;

    private void Awake()
    {
        // ensure no duplicate listeners
        if (ready != null)
        {
            ready.onClick.RemoveAllListeners();
            ready.onClick.AddListener(OnReadyClicked);
        }

        if (cancel != null)
        {
            cancel.onClick.RemoveAllListeners();
            cancel.onClick.AddListener(OnCancelClicked);
        }
    }

    private void OnDestroy()
    {
        if (ready != null) ready.onClick.RemoveListener(OnReadyClicked);
        if (cancel != null) cancel.onClick.RemoveListener(OnCancelClicked);
    }

    /// <summary>
    /// Initialize this form. Call from PlayerAdder after instantiating the prefab.
    /// </summary>
    /// <param name="deviceName">Device name to display (e.g. "esp32_1")</param>
    /// <param name="initialPlayerName">Optional initial player name</param>
    public void InitializeOptions(string deviceName, string initialPlayerName = "")
    {
        _originalSelectedDevice = deviceName ?? string.Empty;
        _originalPlayerName = initialPlayerName ?? string.Empty;

        chosenDevice = _originalSelectedDevice;
        playerName = _originalPlayerName;

        if (playerTextDeviceNameTitle != null)
            playerTextDeviceNameTitle.text = chosenDevice;

        if (playerNameInput != null)
            playerNameInput.text = playerName;

        // reset buttons / interactable
        isReady = false;
        if (ready != null) ready.gameObject.SetActive(true);
        if (playerNameInput != null) playerNameInput.interactable = true;

        // notify manager that this form was (re-)initialized
        OnFormChanged?.Invoke(this);
    }

    private void OnReadyClicked()
    {
        // safety checks
        if (string.IsNullOrEmpty(chosenDevice))
        {
            Debug.LogWarning("[PlayerForm] Ready clicked but no chosenDevice set.");
            return;
        }

        playerName = playerNameInput != null ? playerNameInput.text?.Trim() ?? string.Empty : string.Empty;
        isReady = true;

        // update GameManager entry (if present)
        if (GameManager.instance != null && !string.IsNullOrEmpty(chosenDevice))
        {
            var p = GameManager.instance.playersList.Find(x => x.playerDeviceName == chosenDevice);
            var s = GameManager.instance.skorGameList.Find(s => s.playerName == p.playerName);

            if (s == null) 
            {
                GameManager.instance.skorGameList.Add(
                    new SkorData(p.playerName, 0)    
                );
            }
            else if (p != null && !string.IsNullOrWhiteSpace(playerName))
            {
                p.playerName = playerName;
                s.playerName = playerName;
            }
        }

        // optionally lock UI so user can't change without cancelling
        if (playerNameInput != null) playerNameInput.interactable = false;
        if (ready != null) ready.gameObject.SetActive(false);

        OnFormChanged?.Invoke(this);
    }

    private void OnCancelClicked()
    {
        // restore GameManager name if this form had applied a name
        if (!string.IsNullOrEmpty(chosenDevice) && GameManager.instance != null)
        {
            var p = GameManager.instance.playersList.Find(x => x.playerDeviceName == chosenDevice);
            if (p != null)
            {
                p.playerName = _originalPlayerName;
            }
        }

        playerName = _originalPlayerName;
        isReady = false;

        if (playerTextDeviceNameTitle != null)
            playerTextDeviceNameTitle.text = chosenDevice;

        if (playerNameInput != null)
        {
            playerNameInput.text = _originalPlayerName;
            playerNameInput.interactable = true;
        }

        if (ready != null) ready.gameObject.SetActive(true);

        OnFormChanged?.Invoke(this);
    }

    /// <summary>
    /// If PlayerAdder wants to change the device assigned to this form,
    /// call this method to set the displayed device (keeps original snapshot).
    /// </summary>
    public void SetDeviceName(string deviceName)
    {
        _originalSelectedDevice = deviceName ?? string.Empty;
        chosenDevice = _originalSelectedDevice;

        if (playerTextDeviceNameTitle != null)
            playerTextDeviceNameTitle.text = chosenDevice;

        OnFormChanged?.Invoke(this);
    }
}
