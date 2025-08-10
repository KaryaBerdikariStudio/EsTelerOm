using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CekRicek : MonoBehaviour
{
    private const float BetweenAnimationsDelay = 2f;
    private bool _isBusy = false;
    private char _bufferedHuruf = '\0';
    private bool _hasBufferedHuruf = false;

    public string penebak;
    public GameObject skorPenebak;
    public char currentHuruf;

    private void Start()
    {
    }

    public IEnumerator Initialize()
    {
        yield return new WaitUntil(() => GameManager.instance != null && LevelManager.instance != null && NetworkManager.instance.serverStarted);
        yield return new WaitUntil(() =>
            LevelManager.instance.random != null && GameManager.instance.playersList.Count > 0
        );
        Debug.Log("[CekRicek] CekRicek started, initializing...");

        FireHeartTrigger(LevelManager.instance.hati1, "TrReset");
        FireHeartTrigger(LevelManager.instance.hati2, "TrReset");
        FireHeartTrigger(LevelManager.instance.hati3, "TrReset");

        // start polling logs (do not block the initialize coroutine forever)
        StartCoroutine(NetworkManager.instance.PollScanLogs());
        Debug.Log("[CekRicek] CekRicek started, waiting for guesses...");
    }

    private void Update()
    {
        if (GameManager.instance == null) return;

        // New input arrived?
        // New input arrived?
        if (GameManager.instance.currentHuruf != '\0' && GameManager.instance.currentHuruf != GameManager.instance.lastHuruf)
        {
            var incoming = GameManager.instance.currentHuruf;

            if (!_isBusy)
            {
                // not busy: take it and process immediately
                currentHuruf = incoming;
                CheckGuess(currentHuruf);

                GameManager.instance.lastDevice = GameManager.instance.currentDevice;
                GameManager.instance.lastHuruf = incoming;
            }
            else
            {
                // busy: buffer the first incoming letter only (simple single-slot buffer)
                if (!_hasBufferedHuruf)
                {
                    _bufferedHuruf = incoming;
                    _hasBufferedHuruf = true;
                    Debug.Log($"[CekRicek] Busy — buffered incoming letter '{_bufferedHuruf}'");
                    // mark lastHuruf to avoid repeated buffering of same letter
                    GameManager.instance.lastHuruf = incoming;
                }
                else
                {
                    Debug.Log($"[CekRicek] Busy — already buffered '{_bufferedHuruf}', ignoring '{incoming}'");
                }
            }
        }

        // keep internal copy if busy (optional)
        if (_isBusy)
        {
            currentHuruf = GameManager.instance.currentHuruf;
        }

        // keep internal copy if busy (optional)
        if (_isBusy)
        {
            currentHuruf = GameManager.instance.currentHuruf;
        }
    }

    /// <summary>
    /// Try to process any buffered letter. Called when an animation/sequence finishes.
    /// </summary>
    private void TryProcessBufferedHuruf()
    {
        if (!_hasBufferedHuruf) return;

        // grab and clear buffer before processing to avoid reentrancy problems
        char toProcess = _bufferedHuruf;
        _bufferedHuruf = '\0';
        _hasBufferedHuruf = false;

        Debug.Log($"[CekRicek] Processing buffered letter '{toProcess}'");

        // Apply to GameManager state consistently
        GameManager.instance.currentHuruf = toProcess;
        GameManager.instance.lastDevice = GameManager.instance.currentDevice;
        GameManager.instance.lastHuruf = toProcess;

        // Process it — this will start a new animation sequence if needed
        CheckGuess(toProcess);
    }

    public bool CheckGuess(char guessedChar)
    {

        if (GameManager.instance == null || LevelManager.instance == null)
        {
            Debug.LogWarning("[CekRicek] Managers not ready");
            return false;
        }

        if(LevelManager.instance.hurufTerpakai.Contains(guessedChar)) return false;
        else
            LevelManager.instance.hurufTerpakai.Add(guessedChar);

        // Get the current device/player name from your GameManager
        string device = GameManager.instance.currentDevice;
        string player = GameManager.instance.playersList
            .Find(p => p.playerDeviceName == device)?.playerName ?? device;

        // Look up the corresponding score TextMeshProUGUI
        if (!LevelManager.instance.scoreTextByDevice.TryGetValue(player, out var scoreTxt))
        {
            Debug.LogError($"[CekRicek] No score text found for device '{player}'");
            return false;
        }

        guessedChar = char.ToUpper(guessedChar);
        if (!char.IsLetter(guessedChar))
        {
            Debug.Log("[CekRicek] Ignoring invalid guess: " + guessedChar);
            return false;
        }

        // If an animation is in progress, we buffer instead of ignoring (but CheckGuess should only be called when not busy)
        if (_isBusy)
        {
            if (!_hasBufferedHuruf)
            {
                _bufferedHuruf = guessedChar;
                _hasBufferedHuruf = true;
                Debug.Log($"[CekRicek] Busy — buffered guess '{_bufferedHuruf}' inside CheckGuess");
            }
            return false;
        }

        // Reveal matching letters
        bool found = false;
        if (LevelManager.instance.random.letterSlots
            .TryGetValue(guessedChar, out List<GameObject> slots))
        {
            foreach (var go in slots)
            {
                if (go != null && !go.activeSelf)
                {
                    go.SetActive(true);
                    LevelManager.instance.benarCount++;
                    found = true;
                }
            }
        }

        // Disable the on-screen key and its label (safe null checks)
        var btn = LevelManager.instance.slotKeyboardButtonList
                     .Find(b => b != null && b.name == $"ButtonKeyboard_{guessedChar}");
        if (btn) btn.SetActive(false);

        var lbl = LevelManager.instance.slotKeyboardTextList
                     .Find(t => t != null && t.name == $"TextCharKeyboard_{guessedChar}");
        if (lbl) lbl.SetActive(false);

        // Update the score: +1000 if correct, −500 if wrong
        int currentScore = 0;
        int.TryParse(scoreTxt.text, out currentScore);
        scoreTxt.text = (found ? currentScore + 1000 : currentScore - 500).ToString();

        // Trigger the correct or wrong animation sequence
        if (found)
        {
            HandleCorrectGuess();
            Debug.Log($"[CekRicek] Correct guess: {guessedChar} by {device}, +1000");
        }
        else
        {
            HandleWrongGuess(guessedChar);
            Debug.Log($"[CekRicek] Wrong guess: {guessedChar} by {device}, -500");
        }

        return found;
    }

    private void HandleCorrectGuess()
    {
        if (LevelManager.instance.nyawa < LevelManager.instance.maxNyawa)
            StartCoroutine(PlayRightAnimationSequence());

        LevelManager.instance.nyawa = LevelManager.instance.maxNyawa;
        Debug.Log("[CekRicek] Correct guess.");

        if (LevelManager.instance.benarCount >= LevelManager.instance.jumlahBenarYangDibutuhkan)
        {
            if (LevelManager.instance.nyawa < LevelManager.instance.maxNyawa)
                StartCoroutine(PlayRightAnimationSequence());
            LevelManager.instance.menang = true;

            UpdateSkorEachPlayers();

            StartCoroutine(ShowGameOverAfter(2f));
            Debug.Log("[CekRicek] Level complete!");
        }
    }

    private void UpdateSkorEachPlayers()
    {
        if (GameManager.instance == null || LevelManager.instance == null) return;
        if (GameManager.instance.skorGameList == null) return;

        var keys = LevelManager.instance.scoreTextByDevice.Keys.ToList();

        foreach (var playerName in keys)
        {
            var scoreText = LevelManager.instance.scoreTextByDevice[playerName];
            if (scoreText == null) continue;

            if (!int.TryParse(scoreText.text, out int skor)) skor = 0;

            var s = GameManager.instance.skorGameList.Find(p => p.playerName == playerName);
            if (s != null) s.playerScore = skor;
        }

        // remove keys after enumerating
        foreach (var key in keys)
            LevelManager.instance.scoreTextByDevice.Remove(key);
    }


    private void HandleWrongGuess(char guessedChar)
    {
        LevelManager.instance.nyawa--;
        Debug.Log($"[CekRicek] Wrong guess: {guessedChar} leh {GameManager.instance.currentDevice} skor dikurangi 500");

        switch (LevelManager.instance.nyawa)
        {
            case 2:
                if (LevelManager.instance.idle != null)
                    LevelManager.instance.idle.SetActive(false);
                StartCoroutine(PlayWrongAnimationSequence(
                    LevelManager.instance.salah1,
                    LevelManager.instance.salah2,
                    LevelManager.instance.hati1));
                break;
            case 1:
                StartCoroutine(PlayWrongAnimationSequence(
                    LevelManager.instance.salah2,
                    LevelManager.instance.salah3,
                    LevelManager.instance.hati2));
                break;
            case 0:
                if (GameManager.instance.bahasa == "Sunda")
                {
                    FireHeartTrigger(LevelManager.instance.gedungSate, "TrStart");
                }
                StartCoroutine(PlayWrongAnimationSequence(
                    LevelManager.instance.salah3,
                    null,
                    LevelManager.instance.hati3));
                UpdateSkorEachPlayers();
                StartCoroutine(ShowGameOverAfter(1f));
                break;
        }
    }

    private IEnumerator PlayWrongAnimationSequence(
        GameObject firstGO,
        GameObject nextGO,
        GameObject heartGO)
    {
        _isBusy = true;
        NetworkManager.instance.UpdateClientStatusName("unity", "Busy", "unity", client => { }, err => { });

        // 1) storm on
        if (LevelManager.instance.storm != null) LevelManager.instance.storm.SetActive(true);

        // 2) animate firstGO (and wait its clip)
        yield return StartCoroutine(AnimateAndHide(firstGO));

        // 3) optional delay (one frame)
        yield return null;

        if (LevelManager.instance.storm != null) LevelManager.instance.storm.SetActive(false);

        // 4) show nextGO
        if (nextGO != null) nextGO.SetActive(true);

        // 5) heart trigger
        FireHeartTrigger(heartGO, "TrStart");

        // finish sequence
        _isBusy = false;
        NetworkManager.instance.UpdateClientStatusName("unity", "notBusy", "unity", client => { }, err => { });

        // If we buffered a letter while busy, process it now
        TryProcessBufferedHuruf();
    }

    private IEnumerator PlayRightAnimationSequence()
    {
        _isBusy = true;
        NetworkManager.instance.UpdateClientStatusName("unity", "Busy", "unity", client => { }, err => { });

        if (GameManager.instance.bahasa == "Sunda")
        {
            FireHeartTrigger(LevelManager.instance.gedungSate, "TrReset");
        }
        if (LevelManager.instance.idle != null)
            LevelManager.instance.idle.SetActive(true);
        if (LevelManager.instance.salah1 != null) LevelManager.instance.salah1.SetActive(false);
        if (LevelManager.instance.salah2 != null) LevelManager.instance.salah2.SetActive(false);
        if (LevelManager.instance.salah3 != null) LevelManager.instance.salah3.SetActive(false);

        // animate success cloud
        yield return StartCoroutine(AnimateAndHide(LevelManager.instance.awan));

        // reset hearts
        FireHeartTrigger(LevelManager.instance.hati1, "TrReset");
        FireHeartTrigger(LevelManager.instance.hati2, "TrReset");
        FireHeartTrigger(LevelManager.instance.hati3, "TrReset");

        _isBusy = false;
        NetworkManager.instance.UpdateClientStatusName("unity", "notBusy", "unity", client => { }, err => { });

        // If we buffered a letter while busy, process it now
        TryProcessBufferedHuruf();
    }

    private IEnumerator AnimateAndHide(GameObject go)
    {
        if (go == null) yield break;

        go.SetActive(true);
        var anim = go.GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogWarning($"[CekRicek] No Animator on {go.name}");
            yield break;
        }


        AudioClip audioClip = LevelManager.instance.storm.GetComponent<AudioSource>().clip;
        float audioLen = audioClip != null ? audioClip.length : 0f;
        

        yield return null;

        anim.ResetTrigger("TrStart");
        anim.SetTrigger("TrStart");




        // Wait a frame to let the animator switch state
        yield return null;

        float animLen = 0f;
        var clipInfo = anim.GetCurrentAnimatorClipInfo(0);
        if (clipInfo.Length > 0 && clipInfo[0].clip != null)
        {
            animLen = clipInfo[0].clip.length;
        }

        if (animLen > 0f && audioLen > 0f)
        {
            anim.speed = animLen / audioLen;
            Debug.Log($"[CekRicek] Adjusted animation speed to {anim.speed:F2} so anim matches audio ({animLen:F2}s → {audioLen:F2}s).");
        }

        // Wait for audio to finish (or animation if audio missing)
        yield return new WaitForSeconds(audioLen > 0f ? audioLen : animLen);

        go.SetActive(false);
        Debug.Log($"[CekRicek] {go.name} hidden after animation");
    }

    private IEnumerator ShowGameOverAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisableAllKeys();
        if (LevelManager.instance != null) LevelManager.instance.levelStarted = false;
        if (LevelManager.instance != null && LevelManager.instance.gameOverPanel != null)
        {
            LevelManager.instance.gameOverPanel.SetActive(true);
            LevelManager.instance.hangmanManager.uiAllParent.SetActive(false);
        }
    }

    private void FireHeartTrigger(GameObject heartGO, string triggerName)
    {
        if (heartGO == null) return;
        var anim = heartGO.GetComponent<Animator>();
        if (anim == null) return;
        if (!heartGO.activeInHierarchy) heartGO.SetActive(true);
        anim.ResetTrigger(triggerName);
        anim.SetTrigger(triggerName);
    }

    private void DisableAllKeys()
    {
        if (LevelManager.instance == null) return;

        LevelManager.instance.random?.letterSlots?.Clear();
        foreach (var key in LevelManager.instance.slotKeyboardList) Destroy(key);
        LevelManager.instance.slotKeyboardList.Clear();
        LevelManager.instance.slotKeyboardButtonList.Clear();
        LevelManager.instance.slotKeyboardTextList.Clear();
        foreach (var slot in LevelManager.instance.slotStringPlaceList) Destroy(slot);
        LevelManager.instance.slotStringPlaceList.Clear();
    }
}
