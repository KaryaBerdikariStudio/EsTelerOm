using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RandomizerKata : MonoBehaviour
{
    public GameObject stringPlace;
    public int jumlahKata, idKata, jumlahBenarYangDibutuhkan;
    public string namaLevel;
    public string kataBahasaDaerah;


    public List<char> charsInitialValue = new List<char>
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
                'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
                'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
                'Y', 'Z'
            };

    public GameObject charSlotPrefab; // 🔹 Prefab untuk setiap kotak huruf
    public Dictionary<char, List<GameObject>> letterSlots = new Dictionary<char, List<GameObject>>();

    private void Awake()
    {
        if (AssetManager.instance == null)
        {
            Debug.LogError("AssetManager instance itu NULL! Instansiasi dulu di Scene baru pakai");
            return;
        }

        GameManager.instance.namaLevel = namaLevel;
        

    }

    private void Start()
    {
        

        Debug.Log("Randomizer Aktif");
        jumlahKata = AssetManager.instance.GetMaxIDValue();
        idKata = UnityEngine.Random.Range(0, jumlahKata);

        kataBahasaDaerah = AssetManager.instance.CariKataBahasaDaerahBerdasarID(idKata).ToUpper();



        BuatStringPlace(kataBahasaDaerah, stringPlace);
    }

    public void BuatStringPlace(string kata, GameObject stringPlace)
    {
        GameManager.instance.charsInitialValue = charsInitialValue;

        if (stringPlace == null || charSlotPrefab == null)
        {
            Debug.LogError("StringPlace atau Prefab belum diassign di Inspector!");
            return;
        }

        // 🔹 Hapus semua slot lama sebelum membuat yang baru
        foreach (Transform child in stringPlace.transform)
        {
            Destroy(child.gameObject);
        }

        int wordLength = kata.Length;
        RectTransform parentRect = stringPlace.GetComponent<RectTransform>();
        float parentWidth = parentRect.rect.width;

        // 🔹 Hitung ukuran huruf dan spasi
        float maxSlotWidth = parentWidth / wordLength * 0.8f;
        float adjustedFontSize = Mathf.Clamp(maxSlotWidth * 0.5f, 30f, 100f);
        float underscoreFontSize = adjustedFontSize * 0.8f;
        //float underscoreOffset = 5f;

        letterSlots.Clear();

        for (int i = 0; i < wordLength; i++)
        {
            char c = kata[i];

            // 🔹 Buat CharSlot dari Prefab
            GameObject charSlot = Instantiate(charSlotPrefab, stringPlace.transform);
            charSlot.name = $"Slot_{c}";

            RectTransform slotRect = charSlot.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(maxSlotWidth, maxSlotWidth);

            // 🔹 Ambil referensi komponen dalam Prefab
            TextMeshProUGUI charText = charSlot.transform.Find("CharText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI underscoreText = charSlot.transform.Find("UnderScore").GetComponent<TextMeshProUGUI>();

            if (charText != null)
            {
                charText.text = c.ToString().ToUpper();
                charText.fontSize = adjustedFontSize;
                charText.color = Color.black;
                charText.alignment = TextAlignmentOptions.Center;
            }

            if (underscoreText != null)
            {
                underscoreText.fontSize = underscoreFontSize;
                underscoreText.color = Color.black;
                underscoreText.alignment = TextAlignmentOptions.Center;
            }

            // 🔹 Penyesuaian tampilan untuk karakter khusus
            switch (c)
            {
                case '\'':
                case '-':
                    if (underscoreText != null) underscoreText.text = ""; // Tidak ada garis bawah
                    if (charText != null) charText.gameObject.SetActive(true); // Tampilkan huruf
                    break;

                default:
                    if (underscoreText != null) underscoreText.text = "_"; // Garis bawah untuk huruf biasa
                    if (charText != null) charText.gameObject.SetActive(false); // Sembunyikan huruf
                    GameManager.instance.jumlahBenarYangDibutuhkan++;
                    break;
            }

            Debug.Log(GameManager.instance.jumlahBenarYangDibutuhkan);

            // 🔹 Simpan referensi di dictionary untuk akses nanti
            if (!letterSlots.ContainsKey(c))
            {
                letterSlots[c] = new List<GameObject>();
            }
            letterSlots[c].Add(charText.gameObject);
            
            
        }

        
    }

}
