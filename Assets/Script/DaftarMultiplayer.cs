// DaftarMultiplayer.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;


public class DaftarMultiplayer : MonoBehaviour
{
    [Header("UI & Icon Pickers")]
    public Button hubungkanPemain;
    public Button resetButton;
    public Button selesaiDaftarButton;
    public Button hapusPendaftaran;
    public Button ikonBebek;
    public Button ikonKapal;
    public Button ikonKucing;
    public Label playerTitle;
    public Sprite buttonMerah;
    public Sprite buttonAbu;
    public Sprite buttonPutih;
    public List<ColourPicker> colourPicker;
    public VisualElement root;
    public VisualElement warningCanvas;
    public Label warningText;
    public Button warningClose;
    public TextField nameInputField;
    public VisualElement form;
    public VisualElement formCanvas;


    public GameObject self;


    [Header("Player Info")]
    public PlayerDatabase.PlayerData playerData;
    public List<RegistrationRequest> registrationRequests;
    public string namaPlayer;
    public string namaDevice;
    public string rgb;
    public bool selesaiDaftarBool;
    public bool menghubungkanPerangkat;

    private void Awake()
    {
    }

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        form = root.Q<VisualElement>("form");

        // Button initialization
        hubungkanPemain = root.Q<Button>("buttonFormHubungkan");
        selesaiDaftarButton = root.Q<Button>("buttonFormSelesai");
        resetButton = root.Q<Button>("buttonFormReset");
        hapusPendaftaran = root.Q<Button>("buttonHapus");

        // Icon buttons
        ikonBebek = root.Q<Button>("buttonPlayerIkonBebek");
        ikonKapal = root.Q<Button>("buttonPlayerIkonKapal");
        ikonKucing = root.Q<Button>("buttonPlayerIkonKucing");

        // Text elements
        warningText = root.Q<Label>("warningText");
        playerTitle = root.Q<Label>("player");
        nameInputField = root.Q<TextField>("inputField");

        // Visual elements
        warningCanvas = root.Q<VisualElement>("warningCanvas");
        warningClose = root.Q<Button>("warningClose");
        formCanvas = root.Q<VisualElement>("formCanvas");
    }

    private void InitializeAllUI()
    {
        // Set initial states
        hubungkanPemain.style.display = DisplayStyle.Flex;
        resetButton.style.display = DisplayStyle.None;
        selesaiDaftarButton.style.display = DisplayStyle.None;
        warningCanvas.style.display = DisplayStyle.None;

        // Initialize icon pickers with correct RGB values
        colourPicker = new List<ColourPicker>
        {
            new ColourPicker(ikonBebek, 0, "251,242,54"),    // Yellow (RGB: 251,242,54)
            new ColourPicker(ikonKapal, 1, "99,155,255"),    // Light Blue (RGB: 99,155,255)
            new ColourPicker(ikonKucing, 2, "246,237,237")   // Off-White (RGB: 246,237,237)
        };
    }


    private void Start()
    {
        InitializeAllUI();


        selesaiDaftarBool = false;
        registrationRequests = new List<RegistrationRequest>(); // ⭐ Initialize here



        menghubungkanPerangkat = false;

        RefreshHubungkanButtons();

        hubungkanPemain.clicked += () =>
            StartCoroutine(StartRegistrationCoroutine());
        resetButton.clicked += () =>
            StartCoroutine(ResetRegistrationCoroutine());
        selesaiDaftarButton.clicked += () =>
            StartCoroutine(SendRegistrationCoroutine(playerData));
        hapusPendaftaran.clicked += () =>
            StartCoroutine(HapusPendaftaranCoroutine());

        warningClose.clicked += () =>
            CloseWarning();

        foreach (var icon in colourPicker)
        {
            icon.button.clicked += () => OnIconClicked(icon.index);
        }

    }

    private void CloseWarning()
    {
        warningCanvas.style.display = DisplayStyle.None;
        warningText.text = string.Empty;
    }

    private IEnumerator ShowWarning(string message)
    {
        warningText.text = message;
        warningCanvas.style.display = DisplayStyle.Flex;
        yield return null ;
    }

    private void OnIconClicked(int index)
    {
        ColourPicker selectedPicker = null;

        foreach (var picker in colourPicker)
        {
            if (picker.index == index)
            {
                // Darken selected by 10% using RGB only
                picker.button.style.unityBackgroundImageTintColor = new Color(0.9f, 0.9f, 0.9f);
                picker.button.SetEnabled(false);
                selectedPicker = picker;
            }
            else
            {
                // Keep others visible but non-interactable
                picker.button.SetEnabled(true);
                // Maintain original color with full opacity
                picker.button.style.unityBackgroundImageTintColor = Color.white;
            }
        }

        if (selectedPicker != null)
        {
            rgb = selectedPicker.rgbColorString;
            Debug.Log($"🎨 Selected icon {index} - {rgb}");
        }
        else
        {
            Debug.LogError($"❌ No icon found with index {index}");
        }
    }


    /// <summary>
    /// Disables hubungkanPemain on any form if there is at least one other form currently connecting.
    /// </summary>
    private void RefreshHubungkanButtons()
    {
        bool someoneConnecting = MultiplayerRegistration.Instance.checker
            .Any(f => f != this && f.menghubungkanPerangkat);

        // if someone else is connecting, we must disable our own button
        hubungkanPemain.SetEnabled(!someoneConnecting && !selesaiDaftarBool);
    }


    private IEnumerator StartRegistrationCoroutine()
    {
        menghubungkanPerangkat = true;
        RefreshHubungkanButtons();

        // Validation
        if (string.IsNullOrEmpty(rgb))
        {
            yield return ShowWarning("Pilih ikon terlebih dahulu!");
            goto Cleanup;
        }
        if (string.IsNullOrEmpty(nameInputField.text))
        {
            yield return ShowWarning("Isi nama pemain terlebih dahulu!");
            goto Cleanup;
        }

        namaPlayer = nameInputField.text;
        hubungkanPemain.SetEnabled(false);

        // Disable other forms
        foreach (var form in MultiplayerRegistration.Instance.checker)
        {
            if (form != this && !form.selesaiDaftarBool)
            {
                form.hubungkanPemain.SetEnabled(false);
            }
        }

        // Step 1: Unity → “open”
        Debug.Log("Step 1️⃣: Changing Unity status to 'open'");
        yield return StartCoroutine(
            NetworkManager.instance.UpdateDeviceStatus("open",
                                                       NetworkManager.CLIENT_NAME,
                                                       NetworkManager.CLIENT_NAME)
        );

        // Step 2: wait for an ESP32 to confirm
        Debug.Log("Step 2️⃣: Waiting for ESP32 confirmation...");
        yield return new WaitUntil(() =>
            NetworkManager.SseClientUpdate.clientList.Any(d =>
                d.type.Equals("esp32", StringComparison.OrdinalIgnoreCase) &&
                d.status == "terkonfirmasi"
            )
        );
        var confirmedEsp = NetworkManager.SseClientUpdate.clientList
            .First(d => d.type.Equals("esp32", StringComparison.OrdinalIgnoreCase)
                     && d.status == "terkonfirmasi");
        namaDevice = confirmedEsp.nama;
        Debug.Log($"Step 2✅: Confirmed device: {namaDevice}");

        // Step 3: mark all other ESP32s “terdaftar”
        Debug.Log("Step 3️⃣: Updating other ESP32 devices to 'terdaftar'");
        foreach (var device in NetworkManager.SseClientUpdate.clientList.ToArray())
        {
            if (device.type == "esp32"
             && !device.nama.Equals(namaDevice, StringComparison.OrdinalIgnoreCase)
             && device.status != "terdaftar")
            {
                yield return StartCoroutine(
                    NetworkManager.instance.UpdateDeviceStatus("terdaftar", device.nama, "esp32")
                );
                yield return new WaitUntil(() =>
                    NetworkManager.SseClientUpdate.clientList
                        .Any(d => d.nama == device.nama && d.status == "terdaftar")
                );
            }
        }

        // Step 4: Unity → “ready”
        Debug.Log("Step 4️⃣: Setting Unity status to 'ready'");
        yield return StartCoroutine(
            NetworkManager.instance.UpdateDeviceStatus("ready",
                                                       NetworkManager.CLIENT_NAME,
                                                       NetworkManager.CLIENT_NAME)
        );

        // Step 5: Update UI
        hubungkanPemain.style.display = DisplayStyle.None;
        resetButton.style.display = DisplayStyle.Flex;
        selesaiDaftarButton.style.display = DisplayStyle.Flex;

        registrationRequests.Add(new RegistrationRequest(namaPlayer, namaDevice, rgb));

    Cleanup:
        menghubungkanPerangkat = false;
        RefreshHubungkanButtons();
        yield break;

    }

    private IEnumerator ResetRegistrationCoroutine()
    {
        menghubungkanPerangkat = false;
        Debug.Log("Starting reset process...");

        // Step 1: Get all ESP devices EXCEPT UNITY
        var espDevices = NetworkManager.SseClientUpdate.clientList
            .Where(d => d.type == "esp32" && d.nama != NetworkManager.CLIENT_NAME).ToList();

        Debug.Log($"Found {espDevices.Count} ESP devices to reset");

        // Step 2: Update all ESP devices to 'terdaftar'
        foreach (var device in espDevices)
        {

            if (device.status != "siapBermain")
            {
                Debug.Log($"Resetting {device.nama}");
                yield return StartCoroutine(
                    NetworkManager.instance.UpdateDeviceStatus("terdaftar", device.nama, "esp32")
                );

                // Wait for confirmation
                yield return new WaitUntil(() =>
                    NetworkManager.SseClientUpdate.clientList
                        .Any(d => d.nama == device.nama && d.status == "terdaftar")
                );

                var playerToRemove = registrationRequests
    .FirstOrDefault(p => p.namaDevice == device.nama);

                if (playerToRemove != null)
                {
                    registrationRequests.Remove(playerToRemove);
                    Debug.Log($"🧹 Removed player: {playerToRemove.namaPlayer}");
                }
            }

        }


        // Step 3: Wait for final SSE update
        yield return new WaitForSeconds(0.5f);  // Small buffer

        // Step 4: Update UI
        // Update UI
        resetButton.style.display = DisplayStyle.None;
        hubungkanPemain.style.display = DisplayStyle.Flex;
        selesaiDaftarButton.style.display = DisplayStyle.None;

        // Re-enable siblings
        foreach (var form in MultiplayerRegistration.Instance.checker)
        {
            if (form != this && !form.selesaiDaftarBool)
            {
                form.hubungkanPemain.SetEnabled(true);
            }
        }

        Debug.Log("Reset process completed! 🔄");
    }

    private IEnumerator SendRegistrationCoroutine(PlayerDatabase.PlayerData playerData)
    {
        menghubungkanPerangkat = false;
        selesaiDaftarBool = true;
        Debug.Log($"📝 Sending player data: {namaPlayer}, {namaDevice}, {rgb}");
        var payload = new RegistrationRequest(namaPlayer, namaDevice, rgb);
        string json = JsonUtility.ToJson(payload);

        var req = new UnityWebRequest(
            $"{NetworkManager.instance._api}/daftarPlayer",
            UnityWebRequest.kHttpVerbPOST
        );
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Player registered");

            // 1a. Add to player informations
            var newPlayer   = new PlayerDatabase.PlayerData
            {
                playerName = namaPlayer,
                deviceId = namaDevice,
                rgbColor = rgb,
                score = 0,
                combo = 0,
                Status = ""
            };

            playerData = newPlayer;
            Debug.Log($"📥 Added to player informations: {newPlayer}");

            // 1b. Update device status to "siap-bermain"
            yield return StartCoroutine(
                NetworkManager.instance.UpdateDeviceStatus("siapBermain", namaDevice, "esp32")
            );

            // Wait for status confirmation
            yield return new WaitUntil(() =>
                NetworkManager.SseClientUpdate.clientList.Any(d =>
                    d.nama == namaDevice &&
                    d.status == "siapBermain"
                )
            );
            Debug.Log($"🔄 Updated {namaDevice} status to siapBermain");

            selesaiDaftarButton.style.display = DisplayStyle.None;
            playerTitle.text = namaPlayer;
            selesaiDaftarBool = true;

            // Toggle visibility using UI Toolkit
            formCanvas.style.display = DisplayStyle.None;

            // Re-enable siblings who still need to register
            foreach (var form in MultiplayerRegistration.Instance.checker)
            {
                if (form != this && !form.selesaiDaftarBool)
                {
                    form.hubungkanPemain.SetEnabled(true);
                }
            }


            PlayerDatabase.Instance.AddPlayer(playerData);

            Debug.Log("🎉 Registration process fully completed!");
        }
        else
        {
            Debug.LogError("❌ register-player failed: " + req.error);
        }
    }

    private IEnumerator HapusPendaftaranCoroutine()
    {

        // Add null checks
        if (MultiplayerRegistration.Instance == null || registrationRequests == null)
        {
            Debug.LogError("Critical references are null!");
            yield break;
        }
        

        // Remove from registration requests
        var requestToRemove = registrationRequests.FirstOrDefault(r => r.namaDevice == namaDevice);
        if (requestToRemove != null)
        {
            registrationRequests.Remove(requestToRemove);
            Debug.Log($"✅ Removed from registration requests: {requestToRemove.namaPlayer}");
        }

        // Remove from player information
        if (PlayerDatabase.Instance.Players != null)
        {
            var playerToRemove = PlayerDatabase.Instance.Players.FirstOrDefault(p => p.deviceId == namaDevice);
            if (playerToRemove != null)
            {
                PlayerDatabase.Instance.RemovePlayer(namaDevice);
                Debug.Log($"✅ Removed from player information: {playerToRemove.playerName}");
            }


            // Reset device status
            if (!string.IsNullOrEmpty(namaDevice))
            {
                yield return StartCoroutine(
                    NetworkManager.instance.UpdateDeviceStatus("terdaftar", namaDevice, "esp32")
                );

                yield return new WaitUntil(() =>
                    NetworkManager.SseClientUpdate.clientList.Any(d =>
                        d.nama == namaDevice &&
                        d.status == "terdaftar"
                    )
                );
            }


            // Remove the VisualElement safely
            if (root != null && root.panel != null) // Ensure it's in the UI hierarchy
            {
                root.RemoveFromHierarchy();
                form.RemoveFromHierarchy();
                Destroy(this);
                Debug.Log($"🗑️ Removing registration for {namaPlayer} ({namaDevice})");

            }


            // Remove from registration list
            MultiplayerRegistration.Instance.RemovePlayerForm(this);

        }


        Debug.Log("🧹 Registration fully removed");
    }

    [Serializable]
    public class RegistrationRequest
    {
        public string namaPlayer;
        public string namaDevice;
        public string RGB;
        public RegistrationRequest(string namaPlayer, string namaDevice, string rgb)
        {
            this.namaPlayer = namaPlayer;
            this.namaDevice = namaDevice;
            this.RGB = rgb;
        }
    }

    [Serializable]
    public class ColourPicker
    {
        public Button button;
        public int index;
        public String rgbColorString;

        public ColourPicker(Button button, int index, String rgbColorString)
        {
            this.button = button;
            this.index = index;
            this.rgbColorString = rgbColorString;
        }
    }

    // local copy from SSE
    private class SseClientUpdate
    {
        public string @event, ip, type, nama, status;
    }


}
