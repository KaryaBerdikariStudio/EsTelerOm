using System;
using System.Collections.Generic;
using UnityEngine;



public enum Gamemode
{
    Hangman,
    Kamus
}


public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }



    [SerializeField] private Gamemode _gamemode;
    public Gamemode gamemode { get => _gamemode; set => _gamemode = value; }

    [SerializeField]
    public List<PlayerDatabase.PlayerData> playerDatas = new List<PlayerDatabase.PlayerData>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
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

