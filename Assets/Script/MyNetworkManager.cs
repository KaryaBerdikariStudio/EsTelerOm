using UnityEngine;
using Mirror;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    private string serverUrl = "http://localhost:8000";
    private string lastCheckedUID = "";
    private float checkInterval = 0.2f; // ✅ Prevent spamming requests

    public void Awake()
    {
        StartCoroutine(StartNodeServer());
        StartCoroutine(ClearInputLog());
        InvokeRepeating(nameof(CheckInputLog), 1f, checkInterval); // ✅ Check every 1 second
    }

    // ✅ Starts Node.js Server (Optional)
    IEnumerator StartNodeServer()
    {
        Debug.Log("🚀 Starting Node.js Server...");
        try
        {
            System.Diagnostics.Process.Start("node", "server.js");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Failed to start Node.js Server: {ex.Message}");
        }
        yield return null;
    }

    // ✅ Clears Input Log at Start
    IEnumerator ClearInputLog()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/clear-log"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Input Log Cleared!");
            }
            else
            {
                Debug.LogError($"❌ Clear Log Failed: {request.error}");
            }
        }
    }

    // ✅ Checks Input Log (Runs every 1 sec)
    void CheckInputLog()
    {
        StartCoroutine(FetchInputLog());
    }

    IEnumerator FetchInputLog()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl + "/input-log"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(responseText)) // ✅ Prevent null response crash
                {
                    InputLogResponse data = JsonUtility.FromJson<InputLogResponse>(responseText);
                    if (data != null && data.input_log != null && data.input_log.Count > 0)
                    {
                        string latestUID = data.input_log[0].UID;

                        if (!string.IsNullOrEmpty(latestUID) && latestUID != lastCheckedUID)
                        {
                            lastCheckedUID = latestUID;
                            StartCoroutine(GetRFIDLetter(latestUID));
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Input Log is empty.");
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to get input log: {request.error}");
            }
        }
    }

    // ✅ Fetches RFID Letter from Server
    IEnumerator GetRFIDLetter(string uid)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverUrl}/rfid/{uid}"))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;

                if (!string.IsNullOrEmpty(responseText)) // ✅ Prevent null response crash
                {
                    RFIDResponse data = JsonUtility.FromJson<RFIDResponse>(responseText);
                    if (data != null && !string.IsNullOrEmpty(data.Huruf))
                    {
                        Debug.Log($"📡 RFID UID={uid} assigned letter={data.Huruf}");
                        SendToClients(uid, data.Huruf);
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ No letter assigned for UID={uid}");
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Failed to fetch RFID letter: {request.error}");
            }
        }
    }

    // ✅ Send Letter to All Clients
    [Server]
    void SendToClients(string uid, string huruf)
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                var receiver = conn.identity.GetComponent<RPCReceiveLetter>();
                if (receiver != null)
                {
                    receiver.RpcReceiveLetter(uid, huruf);
                }
            }
        }
    }

    // ✅ Data Structures for JSON Parsing
    [System.Serializable]
    class InputLogResponse
    {
        public List<InputEntry> input_log;
    }

    [System.Serializable]
    class InputEntry
    {
        public string UID;
        public string IP_ADD;
    }

    [System.Serializable]
    class RFIDResponse
    {
        public string UID;
        public string Huruf;
    }
}

// ✅ Client-Side RPC Handler
public class RPCReceiveLetter : NetworkBehaviour
{
    public static RPCReceiveLetter instance;
    public string UID;

    private void Awake()
    {
        instance = this; // ✅ Ensure singleton is assigned
    }

    // ✅ Clients Receive Letter
    [ClientRpc]
    public void RpcReceiveLetter(string uid, string huruf)
    {
        Debug.Log($"📡 Client Received: UID={uid}, Letter={huruf}");

        if (!string.IsNullOrEmpty(huruf)) // ✅ Prevent errors
        {
            UID = uid;
            if (GameManager.instance != null)
            {
                GameManager.instance.inputChar = huruf.ToUpper()[0];
            }
            else
            {
                Debug.LogWarning("⚠️ GameManager instance is null.");
            }
        }
    }
}
