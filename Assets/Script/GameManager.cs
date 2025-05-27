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
    private List<PlayerDatabase.PlayerData> _players;
    public List<PlayerDatabase.PlayerData> playerDatas { set => _players = value; get => _players; }


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

       if(_players == null)
            _players = new List<PlayerDatabase.PlayerData>();
        DontDestroyOnLoad(gameObject);

        Debug.Log($" → Keeping GameManager (ID {GetInstanceID()}) – players after init: {_players.Count}");
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

