using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Core game logic for Hangman: level progression, hearts, input handling, word management,
/// and broadcasting player status changes. UI (scores, game over) is handled via VisualElements.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager instance { get; private set; }

    [Header("UI Documents & Templates")]
    public UIDocument hangmanUI;
    public VisualTreeAsset gameOverContainerTemplate;

    public VisualElement rootHangman;
    public VisualElement gameOverPopUp;

    [SerializeField]
    private string _menangAtauKalah;
    public string menangAtauKalah { get => _menangAtauKalah; set => _menangAtauKalah = value; }

    [Header("Level Info")]
    public int levelIndex;
    public int maxLevel = 10;
    private int _jumlahBenarYangDibutuhkan;
    public int jumlahBenarYangDibutuhkan { get => _jumlahBenarYangDibutuhkan; set => _jumlahBenarYangDibutuhkan = value; }
    public event Action<int> OnHeartsChanged;

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

    [SerializeField]
    private char _inputChar;
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
    public event Action<char> OnInputCharChanged;

    public List<char> _charsInitialValue = new List<char>();
    public List<char> charsRemaining
    {
        get => _charsInitialValue;
        set => _charsInitialValue = value;
    }

    public string kataSekarang;
    public List<string> kataYangTersedia = new List<string>();
    public List<string> kataYangTelahDipakai = new List<string>();

    private int _prevHearts;

    private void Awake()
    {
        levelIndex = 1;
        kataYangTelahDipakai.Clear();

        if (instance != null && instance != this)
            Destroy(gameObject);
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Grab and cache root UI elements
        rootHangman = hangmanUI.rootVisualElement.Q<VisualElement>("hangmanUIRoot");
        gameOverPopUp = rootHangman.Q<VisualElement>("gameOverPopUp");

        // Inject the game-over content template
        var gameOverContent = gameOverContainerTemplate.CloneTree();
        gameOverPopUp.Clear();
        gameOverPopUp.Add(gameOverContent);
        gameOverPopUp.style.display = DisplayStyle.None;

        // Subscribe to hearts change
        OnHeartsChanged += HandleHeartsChanged;

        // Initialize previous heart count
        _prevHearts = _hearts;
    }

    private void OnDisable()
    {
        OnHeartsChanged -= HandleHeartsChanged;
    }

    private void HandleHeartsChanged(int currentHearts)
    {
        switch (currentHearts)
        {
            case 0:
                AnimationManager.instance.PlayHatiSalah(2);
                ShowGameOverPanel(true);
                break;
            case 1:
                AnimationManager.instance.PlayHatiSalah(1);
                break;
            case 2:
                AnimationManager.instance.PlayHatiSalah(0);
                break;
            case 3:
                AnimationManager.instance.PlayHatiBenar();
                break;
        }
        _prevHearts = currentHearts;
    }

    public void ShowGameOverPanel(bool show)
    {
        gameOverPopUp.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void NextLevel()
    {
        if (levelIndex < maxLevel)
            levelIndex++;
    }

    public bool IsGameOver()
    {
        return heartsRemaining <= 0 || levelIndex > maxLevel;
    }
}
