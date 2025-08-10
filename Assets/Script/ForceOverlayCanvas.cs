using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class ForceOverlayCanvas : MonoBehaviour
{
    public GameObject go;
    void Awake()
    {
        Canvas cv = go.GetComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100; // ensure topmost
    }
}
