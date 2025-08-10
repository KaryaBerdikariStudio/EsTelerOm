using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created  
    void Start()
    {

    }

    // Update is called once per frame  
    void Update()
    {

    }

    public static GameManager instance { get; set; }

    public string bahasa = "Campalagiang";
    public string gameMode = "";

    public char lastHuruf = '\0';
    public string lastDevice = "";

    public string currentDevice = "";
    public char currentHuruf = '\0';

    public string audioKamusKataIndonesia = "";
    public string audioKamusKataDaerah = "";


    public List<KamusData> kamusInternet = new List<KamusData>();
    public List<SkorData> skorInternetList = new List<SkorData>();
    public List<SkorData> skorGameList = new List<SkorData>();

    public List<KamusData> kamusBahasa = new List<KamusData>();
    public List<PlayerData> playersList = new List<PlayerData>();
}
