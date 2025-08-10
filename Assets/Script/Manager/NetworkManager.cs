using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
    [Tooltip("Base URL of your API, e.g. http://localhost:8000/api")]
    public string serverAddress = "http://localhost:8000/api";
    public bool serverStarted = false;
    public bool serverLaunching = false;
    public float pollInterval = 0.2f;

    public List<ClienteleData> clienteleList = new List<ClienteleData>();

    private void Awake()
    {
        serverAddress = "http://127.0.0.1:8000/api";

        if (instance != null && instance != this)
        {
            Debug.LogWarning("[NetworkManager] Instance already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Ensure this is a root GameObject before DontDestroyOnLoad
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
        Debug.Log("[NetworkManager] Instance created and ready.");

        // optional initial flag
        serverLaunching= false;
        serverStarted = false;

        // start server only if not started
        if (!serverStarted && !serverLaunching)
            StartServer();
    }




    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ip = host.AddressList
            .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork &&
                                 !IPAddress.IsLoopback(a));
        if (ip == null)
            throw new InvalidOperationException("No network adapters with an IPv4 address in the system!");
        Debug.Log($"Local IP Address: {ip}");
        return ip.ToString();
    }

    public void StartServer()
    {
        if (serverStarted || serverLaunching) return;
        serverLaunching = true;
        StartCoroutine(StartServerProcess());
    }

    private IEnumerator WaitForServer()
    {
        var url = $"{serverAddress}/test";
        Debug.Log($"[NetworkManager] Waiting for server at {url}…");
        while (true)
        {
            using var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success && www.responseCode == 200)
            {
                Debug.Log("[NetworkManager] Server is ready!");
                serverStarted = true;
                break;
            }
            else
            {
                Debug.LogWarning(
                  $"[NetworkManager] Waiting for server… " +
                  $"result={www.result}, code={www.responseCode}, err={www.error}"
                );
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator StartServerProcess()
    {
        string serverPath = Path.Combine(Application.streamingAssetsPath, "WebServerDB", "server.exe");

        if (!File.Exists(serverPath))
        {
            Debug.LogError("Server executable not found at: " + serverPath);
            yield break;
        }

        // 1) Launch the server
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            UseShellExecute = true,
            CreateNoWindow = false,
            WorkingDirectory = Path.GetDirectoryName(serverPath)
        };

        Process proc;
        try
        {
            proc = Process.Start(startInfo);
            Debug.Log("Server started with PID: " + proc.Id);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start server: " + e.Message);
            yield break;
        }

        // 2) Wait for server to spin up
        yield return WaitForServer();

        // 3) Initialize API endpoints
        yield return ClearClientele();
        yield return ClearLogs();
        yield return AddUnityClientele("Unity", data =>
        {
            Debug.Log($"Unity Client added: {data.nama} ({data.status})");
        }, err => Debug.LogError($"AddUnityClientele failed: {err}"));

        yield return AddESP0Clientele("esp32_0", data =>
        {
            Debug.Log($"esp32_0 Client added: {data.nama} ({data.status})");
        }, err => Debug.LogError($"AddESP0Clientele failed: {err}"));

        yield return UpdateClientStatusName(
            "Unity",
            "notBusy",
            "unity",
            data => Debug.Log($"Updated Unity Client status: {data.nama} → {data.status}"),
            err => Debug.LogError($"UpdateClientStatusName failed: {err}")
        );
    }

    //---------------------------------------------
    // x) Delete Specific Clientele
    //---------------------------------------------
    public IEnumerator DeleteSpesificClientele(string nama)
    {
        string url = $"{serverAddress}/clientele/delete/{nama}";
        Debug.Log($"[NetworkManager] → DeleteSpesificClientele URL: {url}");
        using var www = new UnityWebRequest(url, "DELETE");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NetworkManager] DeleteSpesificClientele ERROR: {www.error}");
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<ClienteleData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            Debug.Log($"[NetworkManager] Deleted Clientele: {nama}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetworkManager] Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 0) Clear Clientele
    //---------------------------------------------
    public IEnumerator ClearClientele()
    {
        string url = $"{serverAddress}/clientele/clear";
        Debug.Log($"[NetworkManager] → ClearClientele URL: {url}");
        using var www = new UnityWebRequest(url, "DELETE");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NetworkManager] ClearClientele ERROR: {www.error}");
            yield break;
        }
    }

    //---------------------------------------------
    // 1) Add Unity Clientele
    //---------------------------------------------
    public IEnumerator AddUnityClientele(string nama, Action<ClienteleData> onSuccess, Action<string> onError = null)
    {
        var body = new { type = "unity", nama };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/addClientele";

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<ClienteleData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 2) Update Client Status by Name
    //---------------------------------------------
    public IEnumerator UpdateClientStatusName(string nama, string newStatus, string type, Action<ClienteleData> onSuccess, Action<string> onError = null)
    {
        var body = new { nama, type, status = newStatus };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/updateClientStatus";

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<ClienteleData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 3) Get Client Status by Name
    //---------------------------------------------
    public IEnumerator GetClientStatus(string nama, Action<string> onSuccess, Action<string> onError = null)
    {
        string url = $"{serverAddress}/clientele/get/{UnityWebRequest.EscapeURL(nama)}";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<ClienteleData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten.status);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 4) Fetch Newest Log Entry
    //---------------------------------------------
    public IEnumerator GetNewestLog(Action<LogEntry> onSuccess, Action<string> onError = null)
    {
        string url = $"{serverAddress}/getLog/FetchOne";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<LogEntry>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 5) Upsert a Kamus entry
    //---------------------------------------------
    public IEnumerator UpsertKamus(string tipe, string bahasaIndonesia, string bahasaDaerah, Action<KamusData> onSuccess, Action<string> onError = null)
    {
        var body = new { tipe, bahasaIndonesia, bahasaDaerah };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/postKata";

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<KamusData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 6) Get all Kamus entries by tipe
    //---------------------------------------------
    public IEnumerator GetKamusByType(string tipe, Action<KamusData[]> onSuccess, Action<string> onError = null)
    {
        string url = $"{serverAddress}/getAllKata/{UnityWebRequest.EscapeURL(tipe)}";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseArray<KamusData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.kontens);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 7) Poll for the latest RFID scan logs
    //---------------------------------------------
    public IEnumerator PollScanLogs()
    {
        string url = $"{serverAddress}/getLog/Scan";
        while (true)
        {
            using var www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string raw = www.downloadHandler?.text;
                if (string.IsNullOrEmpty(raw))
                {
                    Debug.LogWarning("[NetworkManager] PollScanLogs: empty response body.");
                }
                else
                {
                    try
                    {
                        var resp = JsonConvert.DeserializeObject<ApiResponseSingle<LogEntry>>(raw);
                        if (resp == null)
                        {
                            Debug.LogWarning("[NetworkManager] PollScanLogs: deserialized resp is null. Raw: " + raw);
                        }
                        else if (resp.konten == null)
                        {
                            Debug.LogWarning("[NetworkManager] PollScanLogs: resp.konten is null. Raw: " + raw);
                        }
                        else if (string.IsNullOrEmpty(resp.konten.message))
                        {
                            Debug.LogWarning("[NetworkManager] PollScanLogs: konten.message empty. Raw: " + raw);
                        }
                        else
                        {
                            var payload = JObject.Parse(resp.konten.message).ToObject<RFIDData>();
                            if (payload != null)
                            {
                                if (GameManager.instance != null)
                                {
                                    GameManager.instance.currentDevice = payload.nama;
                                    if (!string.IsNullOrEmpty(payload.letter))
                                        GameManager.instance.currentHuruf = payload.letter[0];
                                }
                                Debug.Log($"[NetworkManager] RFID scan → UID={payload.uid}, letter={payload.letter}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[NetworkManager] PollScanLogs ERROR: {e.Message}\nRaw: {raw}");
                    }
                }
            }
            else
            {
                Debug.LogError($"[NetworkManager] PollScanLogs ERROR: {www.error} (code {www.responseCode})");
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }


    //---------------------------------------------
    // 8) Get all players
    //---------------------------------------------
    public IEnumerator GetAllPlayers(Action<ClienteleData[]> onSuccess, Action<string> onError = null)
    {
        string url = $"{serverAddress}/clientele/get/type/esp32";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseArray<ClienteleData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.kontens);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 9) Add ESP32_0 Clientele
    //---------------------------------------------
    public IEnumerator AddESP0Clientele(string nama, Action<ClienteleData> onSuccess, Action<string> onError = null)
    {
        var body = new { type = "esp32", nama };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/addClientele";

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseSingle<ClienteleData>>(www.downloadHandler.text);
            //ParseApiResponse(resp);
            onSuccess?.Invoke(resp.konten);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 10) ESP32 Press Button
    //---------------------------------------------
    public IEnumerator ESP320PressButton(char buttonPressedChar, string nama, Action<RFIDData> onSuccess, Action<string> onError = null)
    {
        var body = new { espId = nama, uid = buttonPressedChar.ToString() };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/scanRFID";

        Debug.Log($"[NetworkManager] → ESP320PressButton URL: {url}, Body: {json}");

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }
    }

    //---------------------------------------------
    // 11) UpdateClientStatusType
    //---------------------------------------------
    public IEnumerator UpdateClientStatusType(string newStatus, string type, Action<ClienteleData> onSuccess)
    {
        foreach (var item in clienteleList)
        {
            Debug.Log($"[NetworkManager] Checking item: {item.nama} with type {item.type}");
            if (item.type != type) continue;

            yield return UpdateClientStatusName(
                item.nama,
                newStatus,
                type,
                data => onSuccess?.Invoke(data),
                error => Debug.LogError($"UpdateClientStatusType failed for {item.nama}: {error}")
            );
        }

        yield return null; // Wait for all requests to complete
    }

    //---------------------------------------------
    // 12) Clear Logs
    //---------------------------------------------
    public IEnumerator ClearLogs()
    {
        string url = $"{serverAddress}/deleteLogs";
        Debug.Log($"[NetworkManager] → ClearLogs URL: {url}");
        using var www = new UnityWebRequest(url, "DELETE");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NetworkManager] ClearLogs ERROR: {www.error}");
            yield break;
        }
        Debug.Log("[NetworkManager] ✅ ClearLogs succeeded");
    }

    //---------------------------------------------
    // 13) Get all skor entries
    //---------------------------------------------
    public IEnumerator GetAllskor(Action<SkorData[]> onSuccess, Action<string> onError = null)
    {
        string url = $"{serverAddress}/skorGet";

        using var www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }

        try
        {
            var resp = JsonConvert.DeserializeObject<ApiResponseArray<SkorData>>(www.downloadHandler.text);
            ParseApiResponse(resp);
            onSuccess?.Invoke(resp.kontens);
        }
        catch (Exception e)
        {
            onError?.Invoke($"Deserialization error: {e.Message}");
        }
    }

    //---------------------------------------------
    // 14) AddSubmitSkor
    //---------------------------------------------
    public IEnumerator SkorPost(string nama, string skor, Action<ClienteleData> onSuccess, Action<string> onError = null)
    {
        var body = new { nama = nama, skor = skor };
        string json = JsonConvert.SerializeObject(body);
        string url = $"{serverAddress}/skorPost";

        using var www = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(www.error);
            yield break;
        }
    }

    //---------------------------------------------
    // Response Parsing System
    //---------------------------------------------
    private void ParseApiResponse<T>(ApiResponseSingle<T> response)
    {
        string table = response.tbl.ToLower();
        string eventName = response.events.ToLower();

        switch (table)
        {
            case "clientele":
                HandleClienteleResponse(response.konten as ClienteleData, eventName);
                break;
            case "rfid":
                HandleRFIDResponse(response.konten as RFIDData, eventName);
                break;
            case "kamus":
                HandleKamusResponse(response.konten as KamusData, eventName);
                break;
            case "log":
                HandleLogResponse(response.konten as LogEntry, eventName);
                break;
        }
    }

    private void ParseApiResponse<T>(ApiResponseArray<T> response)
    {
        string table = response.tbl.ToLower();
        string eventName = response.events.ToLower();

        switch (table)
        {
            case "clientele":
                foreach (var item in response.kontens)
                    HandleClienteleResponse(item as ClienteleData, eventName);
                break;
            case "skor":
                foreach (var item in response.kontens)
                    HandleSkorResponse(item as SkorData, eventName);
                break;
            case "kamus":
                foreach (var item in response.kontens)
                    HandleKamusResponse(item as KamusData, eventName);
                break;
        }
    }

    private void HandleClienteleResponse(ClienteleData clientele, string eventName)
    {
        if (clientele == null) return;

        if (eventName.Contains("add"))
        {
            clienteleList.Add(clientele);
            Debug.Log($"Added clientele {clientele.nama}");
        }
        else if (eventName.Contains("update"))
        {
            var existing = clienteleList.Find(c => c.nama == clientele.nama);
            if (existing != null)
            {
                existing.status = clientele.status;
                Debug.Log($"Updated clientele {clientele.nama}");
            }
        }
        else if (eventName.Contains("delete"))
        {
            clienteleList.RemoveAll(c => c.nama == clientele.nama);
            GameManager.instance.playersList.RemoveAll(p => p.playerDeviceName == clientele.nama);
            Debug.Log($"Removed clientele {clientele.nama}");
        }
        else if (eventName.Contains("getstatus/esp32"))
        {
            var clienteleData = clienteleList.Find(c => c.nama == clientele.nama);
            var playerData = GameManager.instance.playersList.Find(p => p.playerDeviceName == clientele.nama);

            if (playerData == null)
            {
                string playerName = $"Player {clientele.nama[clientele.nama.Length - 1]}";
                clienteleList.Add(clientele);
                GameManager.instance.playersList.Add(new PlayerData(playerName, clientele.nama));
            }
        }
    }

    private void HandleRFIDResponse(RFIDData rfid, string eventName)
    {
        if (rfid == null) return;

        Debug.Log($"RFID scan [HRR]: UID = {rfid.uid} → {rfid.letter}");

        if (eventName.Contains("scan") && GameManager.instance != null &&
            GameManager.instance.gameMode == "Hangman")
        {
            GameManager.instance.currentDevice = rfid.nama;
            GameManager.instance.currentHuruf = rfid.letter[0];
        }
    }

    private void HandleKamusResponse(KamusData kamus, string eventName)
    {
        if (kamus == null) return;

        if (eventName.Contains("fetchallbytype") && GameManager.instance != null)
        {
            GameManager.instance.kamusBahasa.Add(kamus);
            Debug.Log($"Added kamus entry: {kamus.tipe} - {kamus.bahasaDaerah} → {kamus.bahasaIndonesia}");
        }
    }

    private void HandleLogResponse(LogEntry log, string eventName)
    {
        if (log == null) return;
        Debug.Log($"Log entry: {log.events} - {log.message} at {log.timestamp}");

        if (eventName.Contains("fetchone") && GameManager.instance != null &&
            GameManager.instance.gameMode == "Hangman")
        {
            try
            {
                var scanData = JsonConvert.DeserializeObject<RFIDData>(log.message);
                Debug.Log($"Parsed RFIDData: UID={scanData.uid}, Letter={scanData.letter}, Nama={scanData.nama}");
                GameManager.instance.currentDevice = scanData.nama;
                GameManager.instance.currentHuruf = scanData.letter[0];
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Failed parsing RFIDData: {e.Message}");
            }
        }
    }

    private void HandleSkorResponse(SkorData skor, string eventName)
    {
        if (skor == null) return;
        Debug.Log($"Score: {skor.playerName} = {skor.playerScore}");
    }

    private void OnDestroy()
    {
        if(instance == this)
        {
            StopAllCoroutines();
            instance = null; 
        
        }
    }

}

// Data classes
[Serializable]
public class LogEntry
{
    public string tbl;
    [JsonProperty("event")]
    public string events;
    public string message;
    public string timestamp;
}

[Serializable]
public class ApiResponseSingle<T>
{
    public string tbl;
    public string events;
    public T konten;
}

[Serializable]
public class ApiResponseArray<T>
{
    public string tbl;
    public string events;
    public T[] kontens;
}
