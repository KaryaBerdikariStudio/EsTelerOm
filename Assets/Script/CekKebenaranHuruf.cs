using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CekKebenaranHuruf : MonoBehaviour
{
    public RandomizerKata randomizerKata;
    public string namaLevel;
    public TMP_InputField inputField;
    public char inputChar;
    public Button button;

    public int maxNyawa = 3;
    private int jumlahNyawa;

    private void Start()
    {
        inputField.characterLimit = 1;

        namaLevel = GameManager.instance.namaLevel;
        jumlahNyawa = GameManager.instance.heartsRemaining;

        button.onClick.AddListener(OnSubmit);

        // 📌 Listen to inputChar updates from GameManager
        GameManager.instance.OnInputCharChanged += HandleNewInputChar;
    }

    private void OnDestroy()
    {
        // 📌 Unsubscribe when destroyed to prevent memory leaks
        if (GameManager.instance != null)
            GameManager.instance.OnInputCharChanged -= HandleNewInputChar;
    }

    // 📌 Triggered when inputChar updates in GameManager
    private void HandleNewInputChar(char newChar)
    {
        inputChar = newChar;
        StartCoroutine(CekHuruf(inputChar));
        inputField.text = ""; // Clear input field
        UIManager.instance.keyboardScript.HapusKeyboard(inputChar);
        Debug.Log($"📌 Auto-Checking New Input: {inputChar}");
    }

    private void OnSubmit()
    {
        if (!string.IsNullOrEmpty(inputField.text))
        {
            inputChar = char.ToUpper(inputField.text[0]); // Convert to uppercase
            GameManager.instance.inputChar = inputChar; // ✅ This triggers HandleNewInputChar()
            inputField.text = ""; // Clear input field
        }
    }

    public IEnumerator CekHuruf(char inputLetter)
    {
        List<char> removedChar = new List<char> { inputLetter };

        while (randomizerKata.letterSlots == null || randomizerKata.letterSlots.Count == 0)
            yield return null;

        if (GameManager.instance.charsRemaining.Contains(inputLetter))
        {
            if (randomizerKata.letterSlots.ContainsKey(inputLetter))
            {
                Debug.Log($"Letter '{inputLetter}' is correct!");

                foreach (GameObject charText in randomizerKata.letterSlots[inputLetter])
                {
                    charText.SetActive(true);
                    GameManager.instance.jumlahBenarYangDibutuhkan--;
                    GameManager.instance.heartsRemaining = maxNyawa;
                    jumlahNyawa = GameManager.instance.heartsRemaining;
                }
            }
            else
            {
                jumlahNyawa--;
                GameManager.instance.heartsRemaining = jumlahNyawa;
                Debug.Log($"Letter '{inputLetter}' is incorrect!");
            }

            if (GameManager.instance.jumlahBenarYangDibutuhkan <= 0)
            {
                inputField.enabled = false;
                button.enabled = false;
                GameManager.instance.menangAtauKalah = "Game Menang Horeee";
                UIManager.instance.popUpGameOver.SetActive(true);
            }
            else if (namaLevel == "Hangman" && jumlahNyawa == 0)
            {
                inputField.enabled = false;
                button.enabled = false;
                GameManager.instance.menangAtauKalah = "Game Kalah Huuu";
                UIManager.instance.popUpGameOver.SetActive(true);
            }

            GameManager.instance.charsRemaining = removedChar;
        }
        else
        {
            Debug.Log("Huruf Telah Dipakai");
        }
    }
}
