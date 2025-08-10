using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class KeyboardAvailable : MonoBehaviour
{
    [Header("Assign in inspector")]
    public GameObject keyPrefab;       // Prefab with UIDocument containing your key template
    public UIDocument footerUIDocument;

    private VisualElement _keyboardContainer;
    private Dictionary<char, List<VisualElement>> _keyboardSlots = new Dictionary<char, List<VisualElement>>();

    private void OnEnable()
    {
        // Kick off initialization
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        // 1) Wait until LevelManager has initialized charsRemaining
        yield return new WaitUntil(() =>
            LevelManager.instance != null &&
            LevelManager.instance.charsRemaining != null &&
            LevelManager.instance.charsRemaining.Count > 0
        );

        // 2) Grab the keyboard container from your footer UXML
        VisualElement root = footerUIDocument.rootVisualElement;
        _keyboardContainer = root.Q<VisualElement>("keyboardContainer");
        if (_keyboardContainer == null)
        {
            Debug.LogError("Could not find 'keyboardContainer' in footer UIDocument!");
            yield break;
        }

        // 3) Build the initial keyboard
        BuildKeyboard(LevelManager.instance.charsRemaining);
    }

    private void BuildKeyboard(List<char> charsAvailable)
    {
        // Clear any old keys
        _keyboardContainer.Clear();
        _keyboardSlots.Clear();

        // We still parent the prefab GameObjects under this MonoBehaviour for cleanup
        var parentGO = new GameObject("KeyParent");
        parentGO.transform.SetParent(transform, false);

        foreach (char c in charsAvailable)
        {
            // Instantiate your prefab (must contain a UIDocument with your key UXML)
            GameObject keyGO = Instantiate(keyPrefab, parentGO.transform);
            keyGO.name = $"Key_{c}";

            UIDocument keyDoc = keyGO.GetComponent<UIDocument>();
            VisualElement keyRoot = keyDoc.rootVisualElement.Q<VisualElement>("keySlotRoot");
            if (keyRoot == null)
            {
                Debug.LogError("Prefab UIDocument missing 'keySlotRoot' element!");
                continue;
            }

            // Set the character label
            Label label = keyRoot.Q<Label>("keySlotValue");
            if (label != null)
                label.text = c.ToString().ToUpper();

            // Add into the toolkit container
            _keyboardContainer.Add(keyRoot);
            // 🔹 Simpan referensi di dictionary untuk akses nanti  
            if (!_keyboardSlots.ContainsKey(c))
            {
                _keyboardSlots[c] = new List<VisualElement>();
            }

            _keyboardSlots[c].Add(label);

        }
    }

    /// <summary>
    /// Visually hides (and forgets) the key for that character.
    /// </summary>
    public void HapusKeyboard(char inputChar)
    {
        if (_keyboardSlots.ContainsKey(inputChar))
        {

            foreach (VisualElement charText in _keyboardSlots[inputChar])
            {
                Debug.Log($"Letter '{inputChar}' is correct!");;
                charText.style.display = DisplayStyle.None; // Show the character
            }
        }
    }
}
