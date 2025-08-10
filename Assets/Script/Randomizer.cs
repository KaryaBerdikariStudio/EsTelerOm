using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Randomizer : MonoBehaviour
{
    [Tooltip("Prefab for the word container (e.g. an empty GO with HorizontalLayoutGroup)")]
    public GameObject stringPlaceParent;

    [Tooltip("Prefab containing 'CharText' and 'UnderScore' TMPUGUI children")]
    public GameObject charSlotPrefab;

    public Dictionary<char, List<GameObject>> letterSlots = new Dictionary<char, List<GameObject>>();
    public string kata;

    private void Start()
    {
        
    }

    public IEnumerator Initialize()
    {
        Debug.Log("[Randomizer] Start Randomizer");
        GameManager.instance.gameMode = "Hangman";
        GameManager.instance.currentDevice = "";
        GameManager.instance.lastDevice = "";
        GameManager.instance.lastHuruf = '\0';
        GameManager.instance.currentHuruf = '\0';
        yield return new WaitUntil(() => LevelManager.instance != null && GameManager.instance != null && NetworkManager.instance.serverStarted);
        yield return NetworkManager.instance.GetAllPlayers(Player => Debug.Log("[Randomizer]Success updating Unity Status"), err => Debug.LogError("[CekRicek ]Failed updating Unity Status : " + err));
        yield return new WaitUntil(() => GameManager.instance.playersList.Count > 0);
        Debug.Log("[Randomizer ]Players List Count : " + GameManager.instance.playersList.Count);
        LevelManager.instance.CreateScorePanels();
        // Fix: Remove 'yield return' since UpdateClientStatusType returns void, not IEnumerator
        yield return NetworkManager.instance.UpdateClientStatusType("main", "esp32", Player => Debug.Log("[Randomizer]Success updating ESP32 Status"));

        LevelManager.instance.gameOverPanel.SetActive(false);
        LevelManager.instance.hangmanManager.uiAllParent.SetActive(true);
        charSlotPrefab = LevelManager.instance.stringPlacePrefab;
        stringPlaceParent = LevelManager.instance.stringPlaceParent;

        LevelManager.instance.idle.SetActive(true);
        LevelManager.instance.salah1.SetActive(false);
        LevelManager.instance.salah2.SetActive(false);
        LevelManager.instance.salah3.SetActive(false);

        LevelManager.instance.levelStarted = true;

        yield return BuatSlot();
    }

    public void KeyboardMaker()
    {
        for (int i = 0; i < 26; i++)
        {
            char letter = (char)('A' + i);

            // 1) Instantiate the keySlot
            GameObject keySlot = Instantiate(
                LevelManager.instance.slotKeyboardPrefab,
                LevelManager.instance.slotKeyboardParent.transform
            );
            keySlot.name = $"SlotKeyboard_{letter}";
            LevelManager.instance.slotKeyboardList.Add(keySlot);

            // 2) Find and size the buttonSlot to match keySlot
            var keyRT = keySlot.GetComponent<RectTransform>();

            Transform buttonT = keySlot.transform.Find("Button_");
            if (buttonT == null)
            {
                Debug.LogError($"[Randomizer] slotKeyboardPrefab missing a child named 'Button_'");
            }
            else
            {
                GameObject buttonGO = buttonT.gameObject;
                buttonGO.name = $"ButtonKeyboard_{letter}";

                var btnRT = buttonGO.GetComponent<RectTransform>();
                if (btnRT != null && keyRT != null)
                {
                    // match width and height
                    btnRT.sizeDelta = LevelManager.instance.beardyLayout.cellSize;
                }
                else
                {
                    Debug.LogWarning($"[Randomizer] Couldn't match RectTransforms on '{keySlot.name}'");
                }

                char letterUpper = char.ToUpper(letter);

                var keyButton = buttonGO.GetComponent<Button>();
                if (keyButton != null)
                    keyButton.onClick.AddListener(() =>
                        StartCoroutine(NetworkManager.instance.ESP320PressButton(
                                letterUpper,
                                "esp32_0",
                                client => Debug.Log("[Randomizer]Success updating ESP320 Scan"), 
                                err => Debug.LogError("[Randomizer]Failed updating ESP32 Scan : " + err)
                            )
                        )
                    );
                else
                    Debug.LogError($"[Randomizer] Button component missing on '{buttonGO.name}'");

                LevelManager.instance.slotKeyboardButtonList.Add(buttonGO);
            }

            // 3) Label setup (unchanged)
            Transform textCharT = keySlot.transform.Find("TextChar");
            if (textCharT != null)
            {
                var textGO = textCharT.gameObject;
                textGO.name = $"TextCharKeyboard_{letter}";
                LevelManager.instance.slotKeyboardTextList.Add(textGO);

                var keyText = textGO.GetComponent<TextMeshProUGUI>();
                if (keyText != null)
                {
                    keyText.text = letter.ToString();
                    keyText.fontSize = LevelManager.instance.beardyLayout.cellSize.y * (12f / 15f);
                }
                else Debug.LogError($"[Randomizer] Missing TMP on '{textGO.name}'");
            }
            else Debug.LogError($"[Randomizer] slotKeyboardPrefab missing a child named 'TextChar'");
        }
    }


    public IEnumerator BuatSlot()
    {
        kata = "";
        KeyboardMaker();
        yield return null;
        LevelManager.instance.petunjukGameObjek.SetActive(false);
        yield return new WaitUntil(() =>
            GameManager.instance != null &&
            GameManager.instance.kamusBahasa.Count > 0 &&
            AssetManager.instance != null &&
            AssetManager.instance.GetMaxIDValue() > 0
        );

        LevelManager.instance.jumlahBenarYangDibutuhkan = 0;
        LevelManager.instance.benarCount = 0;

        LevelManager.instance.petunjukGameObjek.SetActive(false);
        yield return new WaitUntil(() =>
            GameManager.instance != null &&
            GameManager.instance.kamusBahasa.Count > 0 &&
            AssetManager.instance != null &&
            AssetManager.instance.GetMaxIDValue() > 0
        );

        LevelManager.instance.jumlahBenarYangDibutuhkan = 0;
        LevelManager.instance.benarCount = 0;

        int kataCount = GameManager.instance.kamusBahasa.Count;
        int idx = -1;
        int attempts = 0;

        // Keep picking until we find one not in kamusUsed, or exhaust all possibilities
        do
        {
            idx = Random.Range(0, kataCount);
            attempts++;
            if (attempts > kataCount)
            {
                Debug.LogWarning("[Randomizer] All words have been used at least once — reusing.");
                break;
            }
        }
        while (LevelManager.instance.kamusUsed.Contains(GameManager.instance.kamusBahasa[idx]));

        // Record and display
        var entry = GameManager.instance.kamusBahasa[idx];
        kata = entry.bahasaDaerah;
        LevelManager.instance.kamusUsed.Add(entry);

        string bindo = entry.bahasaIndonesia;
        LevelManager.instance.petunjuk.sprite = AssetManager.instance.LoadSprite(bindo);
        LevelManager.instance.petunjukAudio = AssetManager.instance.LoadAudio(bindo, "Indonesia");
        AudioClip audioClip = LevelManager.instance.petunjukAudio;
        LevelManager.instance.petunjukAudioBtn.onClick.RemoveAllListeners();
        LevelManager.instance.petunjukAudioBtn.onClick.AddListener(() =>
            LevelManager.instance.petunjukAudioSource.PlayOneShot(audioClip)
        );
        LevelManager.instance.hint.onClick.AddListener(() =>
            LevelManager.instance.petunjukGameObjek.SetActive(true)
        );

        LevelManager.instance.guessWord = kata;
        letterSlots.Clear();

        foreach (char c in kata)
        {
            char upperChar = char.ToUpper(c);

            GameObject slot = Instantiate(charSlotPrefab, stringPlaceParent.transform);
            slot.name = $"Slot_{upperChar}";
            LevelManager.instance.slotStringPlaceList.Add(slot);

            var charText = slot.transform.Find("CharText").GetComponent<TextMeshProUGUI>();
            var underscore = slot.transform.Find("UnderScore").GetComponent<TextMeshProUGUI>();

            if (c == '\'' || c == '-')
            {
                underscore.text = "";
                charText.text = c.ToString();
                charText.gameObject.SetActive(true);
            }
            else
            {
                underscore.text = "_";
                charText.text = upperChar.ToString();
                charText.gameObject.SetActive(false);
                LevelManager.instance.jumlahBenarYangDibutuhkan++;
            }

            if (!letterSlots.ContainsKey(upperChar))
                letterSlots[upperChar] = new List<GameObject>();
            letterSlots[upperChar].Add(charText.gameObject);
        }

        yield return null; // Wait for UI to update



        Debug.Log($"[Randomizer] “{kata}” requires {LevelManager.instance.jumlahBenarYangDibutuhkan} reveals.");
    }

    
}
