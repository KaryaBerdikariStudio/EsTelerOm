using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using static NetworkManager;

public class RandomizerKata : MonoBehaviour
{

    public VisualElement rootHangmanUI, rootStringPlace, stringPlaceParentHierarchy;
    public List<VisualElement> stringPlaceList = new List<VisualElement>(); // 🔹 List untuk menyimpan referensi tempat string
    public GameObject stringPlacePrefab; // 🔹 Prefab untuk tempat string
    public int jumlahKata, idKata, jumlahBenarYangDibutuhkan;
    public string namaLevel;
    public string kataBahasaDaerah;

    List<string> listKata = new List<string>();


    public List<char> charsInitialValue = new List<char>
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
                'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
                'Y', 'Z'
            };

    public Dictionary<char, List<VisualElement>> letterSlots = new Dictionary<char, List<VisualElement>>();

    private void Awake()
    {
        if (AssetManager.instance == null)
        {
            Debug.LogError("AssetManager instance itu NULL! Instansiasi dulu di Scene baru pakai");
            return;
        }
    }

    private void OnEnable()
    {
        rootHangmanUI = GetComponent<UIDocument>().rootVisualElement;
        rootStringPlace = stringPlacePrefab.GetComponent<UIDocument>().rootVisualElement;

        stringPlaceParentHierarchy = rootHangmanUI.Q<VisualElement>("stringPlaceContainer");
    }

    private void Start()
    {
        
        listKata = AssetManager.instance.listKata;
        listKata.RemoveAll(k => LevelManager.instance.kataYangTelahDipakai.Contains(k));

        jumlahKata = listKata.Count;

        idKata = UnityEngine.Random.Range(0, jumlahKata);
        kataBahasaDaerah = listKata[idKata];
        
        LevelManager.instance.kataSekarang = kataBahasaDaerah;

        LevelManager.instance.charsRemaining = charsInitialValue;
        Debug.Log("Randomizer Aktif"); // 🔹 Panggil fungsi Randomizer
        StartCoroutine(Randomizer(kataBahasaDaerah.ToUpper())); // 🔹 Panggil fungsi Randomizer
        StartCoroutine(BuatStringPlace(kataBahasaDaerah.ToUpper()));
    }


    [Serializable]
    public class KataPayload
    {
        public string kata;
    }

    public IEnumerator Randomizer(string kata)
    {
        var payload = new KataPayload { kata = kata };
        string json = JsonUtility.ToJson(payload);
        Debug.Log($"→ Sending payload: {json}");

        using var req = new UnityWebRequest($"{NetworkManager.instance._api}/setKata", "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"✅ Kata berhasil diubah ke '{kata}'");
        else
            Debug.LogError($"❌ Gagal mengubah kata: {req.error}");
    }



    public IEnumerator BuatStringPlace(string kata)
    {
        LevelManager.instance.charsRemaining = charsInitialValue;

        int wordLength = kata.Length;
        
        letterSlots.Clear();

        for (int i = 0; i < wordLength; i++)
        {
            char c = kata[i];

            GameObject newCharSlot= Instantiate(stringPlacePrefab, transform);
            newCharSlot.name = $"Slot_{c}"; // Set index-based name

            UIDocument charSlotUIDocument = newCharSlot.GetComponent<UIDocument>();

            // Add to layout
            VisualElement charSlotRoot = charSlotUIDocument.rootVisualElement.Q<VisualElement>("stringPlaceRoot");
            charSlotRoot.name = newCharSlot.name;
            
            // Sync VisualElement name with GameObject
            stringPlaceParentHierarchy.Add(charSlotRoot);

            // 🔹 Penyesuaian tampilan untuk karakter khusus  
            Label underscoreText = charSlotRoot.Q<Label>("stringUnderScoreSlotValue");
            Label charText = charSlotRoot.Q<Label>("stringSlotValue");

            switch (c)
            {
                case '\'':
                case '-':
                    charText.text = c.ToString().ToUpper(); // Kosongkan huruf untuk karakter khusus
                    if (underscoreText != null) underscoreText.text = ""; // Tidak ada garis bawah  
                    if (charText != null) charText.style.display = DisplayStyle.Flex; // Tampilkan huruf  
                    break;

                default:
                    if (underscoreText != null) underscoreText.text = "_"; // Garis bawah untuk huruf biasa  
                    if (charText != null) {
                        charText.text = c.ToString().ToUpper(); // Set huruf ke huruf besar
                        charText.style.display = DisplayStyle.None; 
                    }// Sembunyikan huruf  
                    LevelManager.instance.jumlahBenarYangDibutuhkan++;
                    break;
            }

            Debug.Log(LevelManager.instance.jumlahBenarYangDibutuhkan);

            // 🔹 Simpan referensi di dictionary untuk akses nanti  
            if (!letterSlots.ContainsKey(c))
            {
                letterSlots[c] = new List<VisualElement>();
            }

            letterSlots[c].Add(charText);
             // Tunggu satu frame sebelum melanjutkan
        }

        LevelManager.instance.kataYangTelahDipakai.Add(kata);
        yield return null;
    }

}
