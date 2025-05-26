using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance { get; private set; }
    public GameOverScene gameOverScene;
    public VisualElement rootHangman, rootGameOverParent;
    public UIDocument hangmanUI;

    [SerializeField] private string _menangAtauKalah;
    public string menangAtauKalah { get => _menangAtauKalah; set => _menangAtauKalah = value; }

    [Header("Level Info")]
    public int levelIndex;
    public int maxLevel = 10;
    private int _jumlahBenarYangDibutuhkan;
    public int jumlahBenarYangDibutuhkan { get => _jumlahBenarYangDibutuhkan; set => _jumlahBenarYangDibutuhkan = value; }
    public event System.Action<int> OnHeartsChanged;

    [SerializeField]
    private int _hearts = 3;
    public int heartsRemaining
    {
        get => _hearts;
        set
        {
            _hearts = Mathf.Clamp(value, 0, 3);
            OnHeartsChanged?.Invoke(_hearts);
        }
    }

    [SerializeField] private char _inputChar;
    public event Action<char> OnInputCharChanged;

    public char inputChar
    {
        get => _inputChar;
        set
        {
            if (_inputChar != value)
            {
                _inputChar = value;
                OnInputCharChanged?.Invoke(_inputChar);
            }
        }
    }

    public List<char> _charsInitialValue = new List<char>();
    public List<char> charsRemaining
    {
        get => _charsInitialValue;
        set => _charsInitialValue = value;
    }

    public string kataSekarang;
    public List<string> kataYangTersedia = new List<string>();
    public List<string> kataYangTelahDipakai = new List<string>();
    public List<PlayerDatabase.PlayerData> playersInformation = PlayerDatabase.Instance.Players;

    private void Awake()
    {
        levelIndex = 1;
        kataYangTelahDipakai.Clear();

        if (instance != null && instance != this) Destroy(gameObject);
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
    }

    void Start()
    {
        InitializePlayerScores();

        if (gameOverScene != null)
            gameOverScene.ShowGameOverPanel(false);
    }

    void InitializePlayerScores()
    {
        
    }

}