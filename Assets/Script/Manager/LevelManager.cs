using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;
    public TextMeshProUGUI levelText;
    public GameObject levelPrefab, levelPrefabParent, level;


    public bool menang;
    public bool levelStarted = false;

    public int currentLevel = 0;

    [Header("World UI")]
    public GameObject idle, storm,salah1, salah2, salah3, hati1, hati2, hati3, awan;
    public GameObject gedungSate;

    [Header("UI State")]
    public GameObject slotKeyboardPrefab, slotKeyboardParent;
    public GameObject stringPlaceParent, stringPlacePrefab;
    public List<GameObject> _skorPanels = new List<GameObject>();
    // Map device (or player) name → its score TextMeshProUGUI
    public Dictionary<string, TextMeshProUGUI> scoreTextByDevice = new();
    public Button hint, menuDariGameBtn;
    public GameObject petunjukGameObjek;
    public Image petunjuk;
    public AudioClip petunjukAudio;
    public AudioSource petunjukAudioSource;
    public Button petunjukAudioBtn;
    public GameObject HUDUICanvas, skorPanelPrefab;

    public GridLayoutGroup beardyLayout;
    public UIHangmanManager hangmanManager;


    public Randomizer random;
    public CekRicek cekRicek;


    public GameObject gameOverPanel;

    public List<char> hurufTerpakai;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene change events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Check immediately in case we loaded in wrong scene
            CheckScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene(scene.name);
    }

    private void CheckScene(string sceneName)
    {
        string targetSceneName = $"LevelHangman{GameManager.instance.bahasa}";

        if (!sceneName.Equals(targetSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[LevelManager] Destroying because scene '{sceneName}' != '{targetSceneName}'");
            Destroy(gameObject);
        }
    }

    public IEnumerator Start()
    {
        level = Instantiate(levelPrefab, levelPrefabParent.GetComponent<Transform>());
        level.name = $"Level_{currentLevel + 1}";
        GameManager.instance.lastHuruf = '\0';
        GameManager.instance.currentHuruf = '\0';
        GameManager.instance.lastDevice = "";
        GameManager.instance.currentDevice = "";

        hurufTerpakai.Clear();
        yield return new WaitUntil(() => level != null); 
        hangmanManager = level.GetComponent<UIHangmanManager>();
        yield return NetworkManager.instance.ClearLogs();
        yield return NetworkManager.instance.UpdateClientStatusType("main", "esp32", Player => Debug.Log("[Randomizer]Success updating ESP32 Status"));


        

        idle = hangmanManager.world.idle;
        storm = hangmanManager.world.storm;
        salah1 = hangmanManager.world.salah1;
        salah2 = hangmanManager.world.salah2;
        salah3 = hangmanManager.world.salah3;
        hati1 = hangmanManager.world.hati1;
        hati2 = hangmanManager.world.hati2;
        hati3 = hangmanManager.world.hati3;
        awan = hangmanManager.world.awan;
        gedungSate = hangmanManager.world.gedungSate;

        gameOverPanel = hangmanManager.uiHangman.gameOverPanel;
        slotKeyboardParent = hangmanManager.uiHangman.keyboardParent;
        petunjukGameObjek = hangmanManager.uiHangman.petunjukObj;
        petunjukAudio = hangmanManager.uiHangman.petunjukAudioObj.GetComponent<AudioSource>().clip;
        petunjukAudioSource = hangmanManager.uiHangman.petunjukAudioObj.GetComponent<AudioSource>();
        petunjukAudioBtn = hangmanManager.uiHangman.petunjukAudioObj.GetComponent<Button>();
        menuDariGameBtn = hangmanManager.uiHangman.menuButtonObj.GetComponent<Button>();


        petunjuk = hangmanManager.uiHangman.petunjukImg.GetComponent<Image>();
        hint = hangmanManager.uiHangman.hintButton.GetComponent<Button>();
        HUDUICanvas = hangmanManager.uiHangman.hudParent;
        levelText = hangmanManager.uiHangman.levelText.GetComponent<TextMeshProUGUI>();
        stringPlaceParent = hangmanManager.uiHangman.stringPlaceParent;
        beardyLayout = hangmanManager.behaviourHangman.beardyLayout;



        random = hangmanManager.behaviourHangman.randomizer;
        cekRicek = hangmanManager.behaviourHangman.cekRicek;

        
        
        Debug.Log("[LevelManager] Initialized with " + GameManager.instance.playersList.Count + " players.");

        levelText.text = $"Level : {currentLevel + 1} / {maxLevel}";

        menuDariGameBtn.onClick.AddListener(() => SceneGameManager.instance.MainMenu());
         

        yield return random.Initialize();
        yield return cekRicek.Initialize();

    }

    public void CreateScorePanels()
    {
        if (GameManager.instance.playersList.Count > 0)
        {
            foreach (var player in GameManager.instance.playersList)
            {
                var panel = Instantiate(skorPanelPrefab, HUDUICanvas.transform);
                panel.name = $"SkorPanel_{player.playerName}";

                // find the two text children:
                var nameTxt = panel.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>();
                var scoreTxt = panel.transform.Find("ScoreTotal").GetComponent<TextMeshProUGUI>();

                var s = GameManager.instance.skorGameList.Find(p => p.playerName == player.playerName);

                nameTxt.text = player.playerName;
                scoreTxt.text = s.playerScore.ToString();

                // store into your existing dictionary
                slotSkor.Add(panel, scoreTxt);

                // *** ALSO store by device/player name ***
                scoreTextByDevice.Add(player.playerName, scoreTxt);
                _skorPanels.Add(panel);
                
            }
        }
    }


    
    public int jumlahBenarYangDibutuhkan = 0;
    public int benarCount = 0;
    public int maxLevel = 10;
    public int nyawa = 3;
    public int maxNyawa = 3;
    public string guessWord = "";

    public List<KamusData> kamusUsed = new List<KamusData>();
    public List<SkorData> skorList = new List<SkorData>();

    public List<GameObject> slotStringPlaceList = new List<GameObject>();
    public List<GameObject> slotKeyboardList = new List<GameObject>();
    public List<GameObject> slotKeyboardTextList = new List<GameObject>();
    public List<GameObject> slotKeyboardButtonList = new List<GameObject>();

    public Dictionary<GameObject, TextMeshProUGUI> slotSkor = new Dictionary<GameObject, TextMeshProUGUI>();
}

