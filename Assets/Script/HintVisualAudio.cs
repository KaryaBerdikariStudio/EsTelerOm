using UnityEngine;
using UnityEngine.UIElements;

public class HintVisualAudio : MonoBehaviour
{
    public string kataBahasaDaerah, kataBahasaIndonesia;
    public UIDocument rootHangmanUIDoc, rootFooterDoc;
    public VisualElement rootHangmanUI, rootHintPanelParent, rootHintPanel;
    public GameObject footerPrefab;
    public Button hintButton;
    public VisualTreeAsset hintPanelUXML;  // Changed to VisualTreeAsset

    private void Awake()
    {
        // No need to instantiate a GameObject; UXML will be cloned into UI
    }

    void Start()
    {
        kataBahasaDaerah = LevelManager.instance.kataSekarang;
        kataBahasaIndonesia = AssetManager.instance.CariKataBahasaIndonesiaBerdasarKataBahasaDaerah(kataBahasaDaerah);

        hintButton.clickable.clicked += () =>
        {
            ShowHintPanel();
        };
    }

    private void OnEnable()
    {
        rootHangmanUIDoc = GetComponent<UIDocument>();
        rootHangmanUI = rootHangmanUIDoc.rootVisualElement.Q<VisualElement>("hangmanUIRoot");

        // Instantiate and rename immediately
        GameObject footerUI = Instantiate(footerPrefab);
        footerUI.name = footerPrefab.name; // Removes "(Clone)" suffix
        footerUI.transform.SetParent(transform, false);

        // Get the footer's root VisualElement
        rootFooterDoc = footerUI.GetComponent<UIDocument>();
        VisualElement footerRoot = rootFooterDoc.rootVisualElement.Q<VisualElement>("footerRoot");
        footerRoot.name = "footerRoot"; // Optional: Ensure clean name in UI Debugger

        // Add to container
        VisualElement roothintKeyboardParent = rootHangmanUI.Q<VisualElement>("hintKeyBoardContainer");
        roothintKeyboardParent.Clear();
        roothintKeyboardParent.Add(footerRoot);

        // Get hint button
        hintButton = footerRoot.Q<Button>("hintButton");
    }

    private void ShowHintPanel()
    {
        Sprite sprite = AssetManager.instance.DisplayKata(
                kataBahasaIndonesia,
                AssetManager.instance._wordList.Find(k => k.indonesia == kataBahasaIndonesia).isVisualAssetReady
        );

        // Clone the panel UXML into the main UI
        rootHintPanelParent = rootHangmanUI.Q<VisualElement>("hintPanelContainer");
        if (hintPanelUXML != null)
        {
            hintPanelUXML.CloneTree(rootHintPanelParent);
        }

        // Additional logic to configure the hint panel...
    }
}