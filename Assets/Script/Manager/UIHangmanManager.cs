using System.Security.Cryptography.X509Certificates;
using Beardy;
using UnityEngine;

public class UIHangmanManager : MonoBehaviour
{

    [Header("UI Elements")]
    public UIHangmanElements uiHangman;
    public GameObject uiAllParent;

    [Header("World Objects")]
    public WorldObjects world;

    [Header("Behaviour")]
    public BehaviourHangman behaviourHangman;

}

[System.Serializable]
public class UIHangmanElements
{
    public GameObject gameOverPanel;
    public GameObject stringPlaceParent;
    public GameObject keyboardParent;
    public GameObject hudParent;
    public GameObject levelText;
    public GameObject hintButton;
    public GameObject petunjukImg;
    public GameObject petunjukObj;
    public GameObject petunjukAudioObj;
    public GameObject menuButtonObj;
}

[System.Serializable]
public class WorldObjects
{
    public GameObject awan;
    public GameObject idle;
    public GameObject storm;
    public GameObject salah1;
    public GameObject salah2;
    public GameObject salah3;
    public GameObject hati1;
    public GameObject hati2;
    public GameObject hati3;
    public GameObject gedungSate;
}


[System.Serializable]
public class BehaviourHangman
{
    public Randomizer randomizer;
    public CekRicek cekRicek;
    public GridLayoutGroup beardyLayout;
}