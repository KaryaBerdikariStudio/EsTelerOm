// SkorKomboBehaviour.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SkorKomboBehaviour : MonoBehaviour
{
    public UIDocument skorUIDocument;
    public VisualElement rootSkor;
    public string namaPlayer, statusNow, statusLast;
    public int skorPlayer;
    public float komboPlayer;
    public Label namaLabel, skorLabel, komboLabel;

    private void OnEnable()
    {
        if (skorUIDocument == null)
        {
            Debug.LogError($"[SkorKomboBehaviour] No UIDocument on {name}");
            return;
        }

        // Grab the root element immediately
        rootSkor = skorUIDocument.rootVisualElement;

        // Defer querying and UI-wiring until after the UXML clone is applied:
        rootSkor.schedule.Execute(_ =>
        {
            namaLabel = rootSkor.Q<Label>("labelNama");
            skorLabel = rootSkor.Q<Label>("labelSkorValue");
            komboLabel = rootSkor.Q<Label>("labelKomboValue");

            // In case Initialize() was already called earlier:
            RefreshUI();
        });
    }

    /// <summary>
    /// Call right after instantiating to set up starting values.
    /// </summary>
    public void Initialize(string nama, int skor, float kombo)
    {
        namaPlayer = nama;
        skorPlayer = skor;
        komboPlayer = Mathf.Max(1f, kombo);
        statusLast = string.Empty;
        RefreshUI();
    }

    /// <summary>
    /// Call whenever LevelManager tells you the player's new status.
    /// </summary>
    public void UpdateStatus(string status)
    {
        statusNow = status.ToLowerInvariant();

        switch (statusNow)
        {
            case "duplicate":
                komboPlayer = 1f;
                skorPlayer += -10;
                break;

            case "benar":
                komboPlayer = (statusLast == "benar") ? (komboPlayer + 0.5f) : 1.5f;
                skorPlayer += Mathf.RoundToInt(100 * komboPlayer);
                break;

            default:
                komboPlayer = 1f;
                skorPlayer += -50;
                break;
        }

        statusLast = statusNow;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (namaLabel != null) namaLabel.text = namaPlayer;
        if (skorLabel != null) skorLabel.text = skorPlayer.ToString();
        if (komboLabel != null) komboLabel.text = "(" + komboPlayer.ToString("F1") +") X" ;
    }
}
