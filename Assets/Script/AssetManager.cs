using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO; // For file reading
using UnityEngine.Networking;
using System.Collections; // For Android file loading

[System.Serializable]
public class WordData
{
    public int id;
    public char hurufAlphabet;
    public string bahasaDaerah;
    public string indonesia;
    public bool isVisualAssetReady;
    public bool isAudioBahasaDaerahAssetReady;
    public bool isAudioIndonesiaAssetReady;
}

public class AssetManager : MonoBehaviour
{
    public static AssetManager instance { get; private set; }


    


    [SerializeField]
    private List<WordData> _wordList = new List<WordData>();

    [SerializeField]
    public int GetMaxIDValue() => _wordList.Count > 0 ? _wordList.Max(word => word.id) : 0; // Jumlah ID

    // Muncul pas aplikasi dipanggil
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        

        // Load method buat ListKata.csv disini juga dia ngitung jumlah si ID di ListKata.csv yang ready asetnya
        StartCoroutine(LoadWordListFromCSV("Campalagiang")) ; // Jangan lupa ganti ini kalau gamenya beda bahasa
    }

   public string CariKataBahasaDaerahBerdasarID(int id)
    {
        WordData word = _wordList.FirstOrDefault(w => w.id == id);
        return word != null ? word.bahasaDaerah : string.Empty;
    }

    public string CariKataBahasaIndonesiaBerdasarID(int id)
    {
        WordData word = _wordList.FirstOrDefault(w => w.id == id);
        return word != null ? word.indonesia : string.Empty;
    }

    public char CariHurufBerdasarID(int id)
    {
        WordData word = _wordList.FirstOrDefault(w => w.id == id);
        return word != null ? word.hurufAlphabet : '\0'; // Return null character
    }

    public IEnumerator LoadWordListFromCSV(string bahasaDaerahApa)
    {
        string path = Path.Combine(Application.streamingAssetsPath, $"ListKata{bahasaDaerahApa}.csv");

        string csvText = "";

        if (Application.platform == RuntimePlatform.Android)
        {
            UnityWebRequest request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest(); // Async file loading

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("CSV file not found on Android: " + request.error);
                yield break; // Exit coroutine
            }

            csvText = request.downloadHandler.text;
        }
        else
        {
            if (!File.Exists(path))
            {
                Debug.LogError("CSV file not found at path: " + path);
                yield break;
            }

            csvText = File.ReadAllText(path);
        }

        Debug.Log("Berhasil load file list Kata");

        // Clear previous data
        _wordList.Clear();

        // Split into lines (assuming each line represents a word entry)
        string[] lines = csvText.Split('\n');

        if (lines.Length <= 1)
        {
            Debug.LogError("CSV file contains no data!");
            yield break;
        }

        // Loop through each line (skip header)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue; // Skip empty lines

            // Split line into fields (assuming comma `,` is the separator)
            string[] fields = line.Split(';');

            if (fields.Length < 7) // Ensure correct column count
            {
                Debug.LogError($"Invalid CSV format on line {i + 1}: {line}");
                continue;
            }

            WordData word = new WordData
            {
                id = int.Parse(fields[0]),
                hurufAlphabet = fields[1][0], // Take first character
                bahasaDaerah = fields[2],
                indonesia = fields[3],
                isVisualAssetReady = fields[4].ToLower() == "true",
                isAudioBahasaDaerahAssetReady = fields[5].ToLower() == "true",
                isAudioIndonesiaAssetReady = fields[6].ToLower() == "true"
            };
            _wordList.Add(word);
        }
    }


    // Alamat AsetSprite : Root/StreamingAssets/Sprite/'kataBahasaDaerah'.png
    // Alamat AsetAudio : Root/StreamingAssets/AudioBahasaApa/'kataYangDiPassdariParam'.mp3/.wav


    public Sprite DisplayKata(string kataBahasaIndonesia, bool isAssetReady)
    {
        // Bikin Algoritma biar ngetampilin sprite dari param kataBahasaDaerah di displayGameObject yang telah dsediakan
        // Jangan lupa cek ketersediaan aset

        string path = isAssetReady ? Path.Combine(Application.streamingAssetsPath + $"Sprites/Default.png") : Path.Combine(Application.streamingAssetsPath + $"Sprites/{kataBahasaIndonesia}.png");

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogError("Failed to load sprite at path: " + path);
        }

        return sprite;
    }

    public AudioClip AudioKata(string kataBahasaDaerah, bool isAssetReady, string bahasaApa)
    {
        // Bikin Algoritma biar memanggil audio_kataBahasaDaerah.mp3 dari param string yang telah disediakan
        string path = isAssetReady ? Path.Combine(Application.streamingAssetsPath + $"Audio/{bahasaApa}/Default.mp3") : Path.Combine(Application.streamingAssetsPath + $"Audio/{bahasaApa}/{kataBahasaDaerah}.mp3");


        AudioClip clip = Resources.Load<AudioClip>(path);

        if (clip == null)
        {
            Debug.LogError("Failed to load audio file at path: " + path);
        }

        return clip;
    }

}
