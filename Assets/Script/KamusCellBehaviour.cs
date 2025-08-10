using UnityEngine;
using UnityEngine.UI;

public class KamusCellBehaviour : MonoBehaviour
{
    public Button button;
    public string kataIndonesia, kataDaerah;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button.onClick.AddListener(() =>
            SetKata()
        );
    }

    // Update is called once per frame
    void SetKata()
    {
        GameManager.instance.audioKamusKataIndonesia = kataIndonesia;
        GameManager.instance.audioKamusKataDaerah = kataDaerah;
    }
}
