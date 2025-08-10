using UnityEngine;
using UnityEngine.UI;

public class IconRGBPlayerPicker : MonoBehaviour
{
    public RawImage rawImage;
    public Texture2D texture2D;
    public Button button;
    public Color rgbPicker;
    private string rgb;
    public int index;


    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // now rawImage.texture is already your Texture2D
        button.targetGraphic = rawImage;
        rgb = GetRGBString();
        Debug.Log("Icon ke -:" + index);
        Debug.Log("Warna : " + rgb);
    }



    // Update is called once per frame
    void Update()
    {

    }

    public string GetRGBString()
    {
        int r = Mathf.RoundToInt(rgbPicker.r * 255f);
        int g = Mathf.RoundToInt(rgbPicker.g * 255f);
        int b = Mathf.RoundToInt(rgbPicker.b * 255f);
        return $"{r},{g},{b}";
    }



}
