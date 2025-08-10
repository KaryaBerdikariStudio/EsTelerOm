using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class CekKebenaranHuruf : MonoBehaviour
{
    public RandomizerKata randomizerKata;
    public string namaLevel;
    public char inputChar;
    public KeyboardAvailable keyboardAvailable;
    public GameOverScene gameOverScene;

    public int maxNyawa = 3;
    private int jumlahNyawa;

    private void Start()
    {

        jumlahNyawa = LevelManager.instance.heartsRemaining;

        // 📌 Listen to inputChar updates from GameManager
        LevelManager.instance.OnInputCharChanged += HandleNewInputChar;
    }

    private void OnDestroy()
    {
        // 📌 Unsubscribe when destroyed to prevent memory leaks
        if (LevelManager.instance != null)
            LevelManager.instance.OnInputCharChanged -= HandleNewInputChar;
    }

    // 📌 Triggered when inputChar updates in GameManager
    private void HandleNewInputChar(char newChar)
    {
        inputChar = newChar;
        StartCoroutine(CekHuruf());
        keyboardAvailable.HapusKeyboard(inputChar);
        Debug.Log($"📌 Auto-Checking New Input: {inputChar}");
    }


    public IEnumerator CekHuruf()
    {
        char inputLetter = LevelManager.instance.inputChar;


        char removedChar = inputChar;

        while (randomizerKata.letterSlots == null || randomizerKata.letterSlots.Count == 0)
            yield return null;

        if (LevelManager.instance.charsRemaining.Contains(inputLetter))
        {
            if (randomizerKata.letterSlots.ContainsKey(inputLetter))
            {
                Debug.Log($"Letter '{inputLetter}' is correct!");

                foreach (VisualElement charText in randomizerKata.letterSlots[inputLetter])
                {
                    charText.style.display = DisplayStyle.Flex; // Show the character
                    LevelManager.instance.jumlahBenarYangDibutuhkan--;
                    LevelManager.instance.heartsRemaining = maxNyawa;
                    jumlahNyawa = LevelManager.instance.heartsRemaining;
                    
                }
            }
            else
            {
                jumlahNyawa--;
                LevelManager.instance.heartsRemaining = jumlahNyawa;
                Debug.Log($"Letter '{inputLetter}' is incorrect!");
                
            }

            if (LevelManager.instance.jumlahBenarYangDibutuhkan <= 0)
            {
                
                if (LevelManager.instance.levelIndex >= LevelManager.instance.maxLevel)
                {
                    LevelManager.instance.menangAtauKalah = "Game Menang Horeee";
                    gameOverScene.ShowGameOverPanel(true);
                }
                else
                {
                    LevelManager.instance.menangAtauKalah = "Lanjut ke Level Selanjutnya";
                    gameOverScene.ShowGameOverPanel(true);
                }

            }
            else if (namaLevel == "Hangman" && jumlahNyawa == 0)
            {

                LevelManager.instance.levelIndex = 1;
                LevelManager.instance.menangAtauKalah = "Game Kalah Huuu";
                gameOverScene.ShowGameOverPanel(true);
            }

            LevelManager.instance.charsRemaining.Remove(removedChar);
        }
        else
        {
            Debug.Log("Huruf Telah Dipakai");
        }
    }
}
