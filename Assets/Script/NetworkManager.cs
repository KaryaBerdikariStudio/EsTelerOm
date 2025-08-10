using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.VisualScripting;
using UnityEditor.Hardware;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance { get; private set; }

    // your LAN IP
    public string LocalIP { get; private set; }

    public bool ok;

    // API endpoints
    private const string ApiBase = "http://localhost:8000/api";
    private const string SseUrl = ApiBase + "/sse/clients";

    public string _api { get => ApiBase; }

    // metadata
    private const string CLIENT_TYPE = "unity";
    public const string CLIENT_NAME = "unity";

    // for launching server
    private Process _serverProcess;

    // SSE client
    private EventSource _sse;

    // event for SSE updates
    public event Action<SseClientUpdate> OnClientUpdate;

    public string jsonSSE;
    private void Awake()
    {
        // --- singleton guard ---
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LaunchNodeInNewTerminal();
    }

    private IEnumerator Start()
    {
        // 1) Get local IP
        LocalIP = NetworkUtils.GetLocalIPv4();
        Debug.Log($"✅ Local IP: {LocalIP}");


        // 2) Clear old registrations
        yield return StartCoroutine(DeleteAllClientele());

        // 3) Register ourselves
        yield return StartCoroutine(RegisterClientCoroutine());
        Debug.Log(ok ? "✅ Unity registered" : "❌ Unity registration failed");

        // 4) Open SSE stream
        SubscribeSse();
    }

    // Add this field to expose devices in inspector
    [Header("DEBUG - Device List")]
    [SerializeField] private List<DeviceData> _editorDeviceList = new();

    private void Update()
    {
        // Sync with SSE client list
        _editorDeviceList = SseClientUpdate.clientList
            .OrderBy(d => d.type)
            .ThenBy(d => d.nama)
            .ToList();
    }


    private IEnumerator DeleteAllClientele()
    {
        using var req = UnityWebRequest.Delete($"{ApiBase}/deleteClientele");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log("✅ All clientele cleared.");
        else
            Debug.LogError($"❌ delete-clientele failed: {req.error}");

        using var req2 = UnityWebRequest.Delete($"{ApiBase}/deletePlayer");
        yield return req2.SendWebRequest();
        if (req2.result == UnityWebRequest.Result.Success)
            Debug.Log("✅ All players cleared.");
        else
            Debug.LogError($"❌ delete-player failed: {req2.error}");
    }

    public IEnumerator DeleteClientele(string deviceName)
    {
        string url = $"{ApiBase}/deleteClientele/{UnityWebRequest.EscapeURL(deviceName)}";
        using (var req = UnityWebRequest.Delete(url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Deleted {deviceName} from clientele");
            }
            else
            {
                Debug.LogError($"Failed to delete {deviceName}: {req.error}");
            }
        }
    }


    /// <summary>
    /// Posts { nama, status } to update-status.
    /// </summary>
    public IEnumerator UpdateDeviceStatus(string status, string deviceName, string deviceType)
    {
        var payload = new DeviceData(deviceName, status, deviceType);
        string json = JsonUtility.ToJson(payload);
        Debug.Log($"→ update-status payload: {json}");

        using var req = new UnityWebRequest($"{ApiBase}/clientele/updateStatus", "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"✅ {deviceName} status updated to '{status}'.");
        else
            Debug.LogError($"❌ update-status failed: {req.error}");
    }

    private void SubscribeSse()
    {
        _sse = new EventSourceBuilder()
            .WithUrl(SseUrl)
            .OnOpen(() => Debug.Log("✅ SSE connected"))
            .OnError(err =>
            {
                Debug.LogError("❌ SSE error: " + err);
                StartCoroutine(ReconnectSse());
            })
            // In SubscribeSse() method:
            .OnMessage(raw =>
            {
                Debug.Log($"📡 Raw SSE received: {raw}");
                NetworkManager.instance.jsonSSE = raw;

                try
                {
                    SseClientUpdate.ProcessJsonSSE(raw);
                    var upd = JsonUtility.FromJson<SseClientUpdate>(raw);
                    OnClientUpdate?.Invoke(upd);
                }
                catch (Exception e)
                {
                    Debug.LogError($"🔥 Error processing SSE: {e.Message}");
                }
            })
            .Build();

        _sse.Connect();
    }

    private IEnumerator ReconnectSse()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("🔄 Reconnecting SSE…");
        SubscribeSse();
    }

    private IEnumerator RegisterClientCoroutine()
    {
        var payload = new DeviceData(LocalIP, "unity", "unity", "Ready");
        var json = JsonUtility.ToJson(payload);
        using var req = new UnityWebRequest($"{ApiBase}/addClientele", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        Debug.Log(json);
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Unity registered");
            ok = true;
        }
        else
            Debug.LogError($"❌ Unity registration failed: {req.error}");
    }

    private void LaunchNodeInNewTerminal()
    {
        string script = Path.Combine(Application.dataPath, "WebServerDB", "server.js");
        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c start \"Node Server\" cmd /k node \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };
        try
        {
            Process.Start(psi);
            Debug.Log("✅ Launched Node.js server in new terminal");
        }
        catch (Exception ex)
        {
            Debug.LogError("❌ Failed to launch Node.js: " + ex);
        }
    }

    private void OnApplicationQuit()
    {
        _sse?.Close();
        StartCoroutine(DeleteAllClientele());
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill();
            Debug.Log("🛑 Node.js server stopped.");
        }
    }



    [Serializable]
    public class DeviceData
    {
        public string ip;
        public string type;
        public string nama;
        public string status;
        public DeviceData(string ip, string type, string nama, string status)
        {
            this.ip = ip;
            this.type = type;
            this.nama = nama;
            this.status = status;
        }

        public DeviceData(string nama, string status, string type)
        {
            this.nama = nama;
            this.status = status;
            this.type = type;
        }
    }

    [Serializable]
    public class SseClientUpdate
    {
        public string @event;
        public string ip;
        public string type;
        public string nama;
        public string huruf;
        public string status;
        public string uid;

        public static List<DeviceData> clientList = new List<DeviceData>();

        public void HandleStatusUpdate()
        {
            var device = clientList.FirstOrDefault(d =>
                d.nama.Equals(nama, StringComparison.OrdinalIgnoreCase)
            );

            if (device != null)
            {
                if (!string.IsNullOrEmpty(type))
                    device.type = type.ToLower();

                device.status = status;
                Debug.Log($"🔄 Updated {device.nama} ({device.type})");
            }
            else
            {
                var newType = string.IsNullOrEmpty(type) ? "unknown" : type.ToLower();
                var newDevice = new DeviceData(
                    ip ?? "",
                    newType,
                    nama ?? "",
                    status ?? "unknown"
                );
                clientList.Add(newDevice);
            }
        }

        public void HandleClientAdded(string namaDevice)
        {
            // Find existing client or null
            var client = clientList.Find(d =>
                d.nama.Equals(nama)
            );

            if (client != null)
            {
                Debug.Log($"📥 Existed client: {client.nama} ({client.type})");
                if (client.status.Equals("siapBermain"))
                {
                    Debug.Log($"📥 Existed client is ready to play: {client.nama} ({client.type})");
                    NetworkManager.instance.StartCoroutine(NetworkManager.instance.UpdateDeviceStatus("siapBermain", namaDevice, "esp32"));
                }
            }
            else
            {
                // Remove any existing entries with same name
                clientList.RemoveAll(d =>
                    d.nama.Equals(nama, StringComparison.OrdinalIgnoreCase)
                );

                // Create new device with default values
                var newDevice = new DeviceData(
                    ip,
                    type?.ToLower(),
                    nama,
                    status ?? "terdaftar"
                );

                clientList.Add(newDevice);
                Debug.Log($"📥 Added client: {newDevice.nama} ({newDevice.type})");
            }
        }

        public void HandleCheckInput(string huruf, string deviceName, string status)
        {
            try
            {
                LevelManager.instance.inputChar = huruf[0];
                PlayerDatabase.Instance.UpdateStatus(deviceName, status);

            }
            catch (Exception e)
            {
                Debug.LogError($"🔥 Error processing check-input: {e.Message}");
            }
        }


        public void HandleUIDRegistration(string uid)
        {

        }

        [Serializable]
        public class BenarSalah
        {
            public bool benar;
            public string namaDevice;

            public override string ToString() => $"{namaDevice}: {benar}";
        }

        public static void ProcessJsonSSE(string jsonSSE)
        {
            try
            {
                Debug.Log($"📥 Raw SSE received: {jsonSSE}");
                var update = JsonUtility.FromJson<SseClientUpdate>(jsonSSE);

                if (update == null)
                {
                    Debug.LogError("🚫 Failed to parse SSE update");
                    return;
                }

                Debug.Log($"🔍 Parsed event: {update.@event}");

                // Normalize event name
                var normalizedEvent = update.@event
                    .Replace("-", "")
                    .ToLower();

                switch (normalizedEvent)
                {
                    case "clientadded":
                        update.HandleClientAdded(update.nama);
                        break;
                    case "statusupdated":
                        update.HandleStatusUpdate();
                        break;
                    case "inputlogged":
                        update.HandleCheckInput(update.huruf, update.nama, update.status);
                        break;
                    case "wantregistercard":
                        update.HandleCheckInput(update.huruf, update.nama, update.status);
                        break;
                    default:
                        Debug.LogWarning($"⚠️ Unhandled event type: {update.@event}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"💀 Critical SSE processing error: {ex.Message}");
            }

            Debug.Log($"🏁 Finished processing event\n");
        }
    }

    // — Simple SSE client builder —
    public class EventSourceBuilder
    {
        private string _url;
        private Action _onOpen;
        private Action<string> _onMessage;
        private Action<string> _onError;

        public EventSourceBuilder WithUrl(string url) { _url = url; return this; }
        public EventSourceBuilder OnOpen(Action cb) { _onOpen = cb; return this; }
        public EventSourceBuilder OnError(Action<string> cb) { _onError = cb; return this; }
        public EventSourceBuilder OnMessage(Action<string> cb) { _onMessage = cb; return this; }
        public EventSource Build() => new EventSource(_url, _onOpen, _onMessage, _onError);
    }

    public class EventSource
    {
        private readonly string _url;
        private readonly Action _onOpen;
        private readonly Action<string> _onMessage;
        private readonly Action<string> _onError;
        private System.Threading.CancellationTokenSource _cts;

        public EventSource(string url, Action onOpen,
                           Action<string> onMsg, Action<string> onErr)
        {
            _url = url;
            _onOpen = onOpen;
            _onMessage = onMsg;
            _onError = onErr;
        }

        public async void Connect()
        {
            _cts = new System.Threading.CancellationTokenSource();
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var req = new System.Net.Http.HttpRequestMessage(
                    System.Net.Http.HttpMethod.Get, _url);
                req.Headers.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream")
                );
                using var resp = await client.SendAsync(
                    req,
                    System.Net.Http.HttpCompletionOption.ResponseHeadersRead,
                    _cts.Token
                );
                _onOpen?.Invoke();

                using var stream = await resp.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                while (!_cts.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line != null && line.StartsWith("data: "))
                    {
                        _onMessage?.Invoke(line.Substring(6));
                        NetworkManager.instance.jsonSSE = line;
                    }
                }
            }
            catch (Exception e)
            {
                _onError?.Invoke(e.Message);
            }
        }

        public void Close() => _cts?.Cancel();
    }

    // — Utility to get LAN IPv4 —
    public static class NetworkUtils
    {
        public static string GetLocalIPv4()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                       .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork
                                         && !IPAddress.IsLoopback(a))
                       ?.ToString();
        }
    }
}
