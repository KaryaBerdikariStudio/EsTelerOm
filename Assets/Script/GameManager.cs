using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public struct Score
{
    string playerName;
    int playerIndex;
    int score;
    DateTime timestamnp;
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    [SerializeField]
    private string _menangAtauKalah;
    public string menangAtauKalah
    {
        get => _menangAtauKalah;
        set => _menangAtauKalah = value;
    }

    [SerializeField]
    public List<Score> playersScores = new List<Score>();

    [SerializeField]
    private int _jumlahBenarYangDibutuhkan;
    public int jumlahBenarYangDibutuhkan
    {
        get => _jumlahBenarYangDibutuhkan;
        set => _jumlahBenarYangDibutuhkan = value;
    }

    public event System.Action<int> OnHeartsChanged;
    [SerializeField]
    private int _hearts = 3;
    public int heartsRemaining
    {
        get => _hearts;
        set
        {
            _hearts = Mathf.Clamp(value, 0, 3); // Prevent negative hearts
            OnHeartsChanged?.Invoke(_hearts); // 🔥 Notify UIManager
        }
    }


    private string _namaLevel;
    public string namaLevel
    {
        get => _namaLevel;
        set => _namaLevel = value;
    }

    [SerializeField]
    private char _inputChar;
    public event Action<char> OnInputCharChanged; // 📌 Event for input changes

    public char inputChar
    {
        get => _inputChar;
        set
        {
            if (_inputChar != value) // ✅ Only trigger if value changes
            {
                _inputChar = value;
                OnInputCharChanged?.Invoke(_inputChar); // 📌 Notify listeners
            }
        }
    }



    [SerializeField]
    private List<char> _charsAvaible = new List<char>();

    public List<char> charsRemaining
    {
        get => _charsAvaible;
        set
        {
            _charsAvaible = RemoveOneChar(value, _charsAvaible);
        }
    }

    public List<char> charsInitialValue
    {
        get => _charsAvaible;
        set => _charsAvaible = value;
    }
    private static List<char> RemoveOneChar(List<char> charYouWantRemoved, List<char> charsList)
    {
        //buat algoritma ini gaes, diferensiasi antara char A, dan B

        List<char> newCharList = new List<char>(charsList);

        foreach (char c in charYouWantRemoved)
        {
            newCharList.Remove(c); // Remove only the first occurrence of each character
        }

        return newCharList;
    }

    


    private void Awake()
    {
        InitializeSingleton();

        

       

    }



    private void InitializeSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }


}
