// PlayerDatabase.cs
using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerDatabase : MonoBehaviour
{
    public static PlayerDatabase Instance { get; private set; }
    [System.Serializable]
    public class PlayerData
    {
        public string playerName;
        public string deviceId;
        public string rgbColor;
        public int score;
        public int combo;

        private string _status;
        public event System.Action<PlayerData, string> OnStatusChanged; // Changed event signature

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChanged?.Invoke(this, _status); // Pass both player and status
                }
            }
        }
    }

    [SerializeField] private List<PlayerData> _players = new List<PlayerData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Public accessors
    public List<PlayerData> Players{ get => _players; }
    public int PlayerCount => _players.Count;

    public void AddPlayer(PlayerData newPlayer)
    {
        if (!_players.Exists(p => p.deviceId == newPlayer.deviceId))
        {
            _players.Add(newPlayer);
            SaveDatabase();
        }
    }

    public void RemovePlayer(string deviceId)
    {
        _players.RemoveAll(p => p.deviceId == deviceId);
        SaveDatabase();
    }

    public void UpdateScore(string deviceId, int scoreDelta)
    {
        var player = _players.Find(p => p.deviceId == deviceId);
        if (player != null)
        {
            player.score += scoreDelta * player.combo;
            SaveDatabase();
        }
    }

    public void UpdateStatus(string deviceId, string status)
    {
        var player = _players.Find(p => p.deviceId == deviceId);
        if (player != null)
        {
            player.Status = status;
            SaveDatabase();
        }
    }

    public void UpdateCombo(string deviceId, int combo)
    {
        var player = _players.Find(p => p.deviceId == deviceId);
        if (player != null)
        {
            player.combo = combo;
            SaveDatabase();
        }
    }

    private void SaveDatabase()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }
}