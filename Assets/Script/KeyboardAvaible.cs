using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardAvaible : MonoBehaviour
{
    [SerializeField] private List<char> charsAvaible;
    public GameObject gridLayout;
    public GameObject keyPrefab; // Prefab tombol keyboard

    private Dictionary<char, GameObject> letterSlots = new Dictionary<char, GameObject>();

    void Start()
    {
        // 🔹 Ambil huruf yang tersedia dari GameManager
        charsAvaible = GameManager.instance.charsRemaining;

        // 🔹 Jalankan BuatKeyboard sebagai coroutine, menunggu hingga BuatStringPlace selesai
        BuatKeyboard(charsAvaible, gridLayout);
    }

    /// <summary>
    /// Membuat tombol keyboard berdasarkan huruf yang tersedia.
    /// </summary>
    void BuatKeyboard(List<char> charsAvaible, GameObject grid)
    {
        if (grid == null || keyPrefab == null)
        {
            Debug.LogError("Grid atau Key Prefab belum diassign di Inspector!");
            return;
        }


        // 🔹 Hapus semua tombol lama sebelum membuat yang baru
        foreach (Transform child in grid.transform)
        {
            Destroy(child.gameObject);
        }

        letterSlots.Clear();

        foreach (char c in charsAvaible)
        {
            // 🔹 Buat tombol dari prefab
            GameObject key = Instantiate(keyPrefab, grid.transform);
            key.name = $"Slot_{c}";

            // 🔹 Atur teks pada tombol
            TextMeshProUGUI keyText = key.GetComponent<TextMeshProUGUI>();
            if (keyText != null)
            {
                keyText.text = c.ToString().ToUpper();
            }

            // 🔹 Simpan referensi tombol ke dalam dictionary untuk penghapusan nanti
            letterSlots[c] = key;
        }
    }

    /// <summary>
    /// Menghapus tombol keyboard berdasarkan huruf yang ditekan.
    /// </summary>
    public void HapusKeyboard(char inputChar)
    {
        if (letterSlots.ContainsKey(inputChar))
        {
            letterSlots[inputChar].GetComponent<TextMeshProUGUI>().text = ""; // 🔹 Hapus tombol dari UI
            letterSlots.Remove(inputChar); // 🔹 Hapus dari dictionary
        }
    }
}
