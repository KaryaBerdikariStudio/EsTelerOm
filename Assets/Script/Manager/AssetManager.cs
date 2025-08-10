using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

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

    /// <summary>
    /// Highest ID in the loaded word list (or 0 if empty).
    /// </summary>
    public int GetMaxIDValue() => _wordList.Count > 0 ? _wordList.Max(w => w.id) : 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        CopySpritesToResources(); // Optional: copy sprites from StreamingAssets to Resources/Sprite
    }

    private void Start()
    {
        // Now that all Awake()s have run, GameManager.instance should be valid
        if (GameManager.instance != null && !string.IsNullOrEmpty(GameManager.instance.bahasa))
        {
            StartCoroutine(LoadWordListFromCSV(GameManager.instance.bahasa));
            CopyAudiosToResources();
        }
        else
        {
            Debug.LogError("[AssetManager] Cannot load CSV: GameManager.instance or its bahasa is null");
        }
    }

    /// <summary>
    /// Loads ListKata{bahasa}.csv from StreamingAssets and fills _wordList.
    /// Also registers each entry into GameManager.instance.kamusBahasa.
    /// </summary>
    /// <summary>
    /// Loads ListKata{bahasa}.csv from StreamingAssets and fills _wordList.
    /// Also registers each entry into GameManager.instance.kamusBahasa.
    /// </summary>
    public IEnumerator LoadWordListFromCSV(string bahasaDaerahApa)
    {
        string fileName = $"ListKata{bahasaDaerahApa}.csv";
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        string csvText;

        if (Application.platform == RuntimePlatform.Android)
        {
            using var request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AssetManager] CSV not found on Android: {request.error}");
                yield break;
            }
            csvText = request.downloadHandler.text;
        }
        else
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[AssetManager] CSV not found at path: {path}");
                yield break;
            }
            csvText = File.ReadAllText(path);
        }

        // Clear old
        _wordList.Clear();
        GameManager.instance.kamusBahasa.Clear();

        // Split lines, skip header
        var lines = csvText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1);

        // Split on commas or semicolons not inside quotes
        var pattern = @"[;,](?=(?:[^""]*""[^""]*"")*[^""]*$)";

        foreach (var rawLine in lines)
        {
            var fields = Regex.Split(rawLine, pattern);

            if (fields.Length < 7)
            {
                Debug.LogWarning($"[AssetManager] Skipping invalid CSV line: {rawLine}");
                continue;
            }

            // Trim whitespace & quotes
            for (int i = 0; i < fields.Length; i++)
                fields[i] = fields[i].Trim().Trim('"');

            if (!int.TryParse(fields[0], out int id))
            {
                Debug.LogWarning($"[AssetManager] Bad ID '{fields[0]}' in line: {rawLine}");
                continue;
            }

            var word = new WordData
            {
                id = id,
                hurufAlphabet = fields[1].Length > 0 ? fields[1][0] : '\0',
                bahasaDaerah = fields[2],
                indonesia = fields[3],
                isVisualAssetReady = fields[4].Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                isAudioBahasaDaerahAssetReady = fields[5].Equals("TRUE", StringComparison.OrdinalIgnoreCase),
                isAudioIndonesiaAssetReady = fields[6].Equals("TRUE", StringComparison.OrdinalIgnoreCase)
            };

            _wordList.Add(word);
            GameManager.instance.kamusBahasa.Add(new KamusData(
                bahasaDaerahApa,
                word.bahasaDaerah,
                word.indonesia
            ));
        }

        Debug.Log($"[AssetManager] Parsed {_wordList.Count} words from CSV.");
    }

    // Lookup helpers
    public string CariKataBahasaDaerahBerdasarID(int id) =>
        _wordList.FirstOrDefault(w => w.id == id)?.bahasaDaerah ?? string.Empty;

    public string CariKataBahasaIndonesiaBerdasarID(int id) =>
        _wordList.FirstOrDefault(w => w.id == id)?.indonesia ?? string.Empty;

    public char CariHurufBerdasarID(int id) =>
        _wordList.FirstOrDefault(w => w.id == id)?.hurufAlphabet ?? '\0';

    // Resources‐based asset loaders (optional or in addition)
    public Sprite LoadSprite(string kata)
    {
        // try exact match
        var path = $"Sprite/Sprite/{kata}";
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning($"[AssetManager] Sprite '{path}' not found, loading default.");
            sprite = Resources.Load<Sprite>("Sprite/Default");
            if (sprite == null)
                Debug.LogError("[AssetManager] Default sprite not found at Resources/Sprite/Default.png");
        }
        return sprite;
    }

    /// <summary>
    /// Coroutine that loads an AudioClip from StreamingAssets/Audio/{bahasa}/{kata}.mp3,
    /// falling back to StreamingAssets/Audio/{bahasa}/Default.mp3 if missing.
    /// </summary>
    public AudioClip LoadAudio(string kata, string bahasa)
    {
        // try exact match
        var path = $"Audio/{bahasa}/{kata}";
        var audio = Resources.Load<AudioClip>(path);
        if (audio == null)
        {
            Debug.LogWarning($"[AssetManager] Sprite '{path}' not found, loading default.");
            audio = Resources.Load<AudioClip>($"Audio/{bahasa}/Default");
            if (audio == null)
                Debug.LogError("[AssetManager] Default sprite not found at Resources/Sprite/Default.png");
        }
        return audio;
    }

    
    public void CopySpritesToResources()
    {
        var srcDir = Path.Combine(Application.streamingAssetsPath, "Sprite");
        var dstDir = Path.Combine(Application.dataPath, "Resources/Sprite/Sprite");
        if (!Directory.Exists(srcDir))
        {
            Debug.LogError($"Source folder not found: {srcDir}");
            return;
        }

        // ensure target exists
        Directory.CreateDirectory(dstDir);

        // copy every file
        foreach (var file in Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(srcDir.Length + 1);
            var destPath = Path.Combine(dstDir, relative);
            var destDir = Path.GetDirectoryName(destPath);
            Directory.CreateDirectory(destDir);
            File.Copy(file, destPath, overwrite: true);
            Debug.Log($"Copied → Resources/Sprite/{relative}");
        }

        // force Unity to reimport those assets
        //AssetDatabase.Refresh();
        Debug.Log("✅ StreamingAssets/Sprite → Resources/Sprite/Sprite sync complete");
    }

    public void CopyAudiosToResources()
    {
        StartCoroutine(new WaitUntil(() => GameManager.instance != null));
        var srcDaerahDir = Path.Combine(Application.streamingAssetsPath, $"Audio/{GameManager.instance.bahasa}");
        var dstDaerahDir = Path.Combine(Application.dataPath, $"Resources/Audio/{GameManager.instance.bahasa}");

        var srcIndoDir = Path.Combine(Application.streamingAssetsPath, "Audio/Indonesia");
        var dstIndoDir = Path.Combine(Application.dataPath, "Resources/Audio/Indonesia");
        
        if (!Directory.Exists(srcDaerahDir))
        {
            Debug.LogError($"Source folder not found: {srcDaerahDir}");
            return;
        }

        if (!Directory.Exists(srcIndoDir))
        {
            Debug.LogError($"Source folder not found: {srcIndoDir}");
            return;
        }

        // ensure target exists
        Directory.CreateDirectory(dstDaerahDir);
        Directory.CreateDirectory(dstIndoDir);
        // copy every file
        
        foreach (var file in Directory.GetFiles(srcDaerahDir, "*.*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(srcDaerahDir.Length + 1);
            var destPath = Path.Combine(dstDaerahDir, relative);
            var destDir = Path.GetDirectoryName(destPath);
            Directory.CreateDirectory(destDir);
            File.Copy(file, destPath, overwrite: true);
            Debug.Log($"Copied → Resources/Audio/{GameManager.instance.bahasa}/{relative}");
        }

        foreach (var file in Directory.GetFiles(srcIndoDir, "*.*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(srcIndoDir.Length + 1);
            var destPath = Path.Combine(dstIndoDir, relative);
            var destDir = Path.GetDirectoryName(destPath);
            Directory.CreateDirectory(destDir);
            File.Copy(file, destPath, overwrite: true);
            Debug.Log($"Copied → Resources/Audio/Indonesia/{relative}");
        }

        // force Unity to reimport those assets
        //AssetDatabase.Refresh();
        Debug.Log("✅ StreamingAssets/Audio → Resources/Audio sync complete");
    }

    /// <summary>
    /// Saves a byte[] (e.g. downloaded audio or generated image) into StreamingAssets.
    /// Automatically creates the subfolder if missing.
    /// </summary>
    /// <param name="subfolder">"Audio/{bahasa}" or "Sprite"</param>
    /// <param name="fileName">filename including extension, e.g. "kata.mp3"</param>
    /// <param name="data">raw bytes to write</param>
    public static void WriteToStreamingAssets(string subfolder, string fileName, byte[] data)
    {
        var targetDir = Path.Combine(Application.streamingAssetsPath, subfolder);
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var fullPath = Path.Combine(targetDir, fileName);
        File.WriteAllBytes(fullPath, data);
        Debug.Log($"✅ Wrote {fileName} to StreamingAssets/{subfolder}");
    }
}
