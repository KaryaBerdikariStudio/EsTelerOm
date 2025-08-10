using System.Collections;
using UnityEngine;

public class DisappearAwayShush : MonoBehaviour
{

    public IEnumerator DisappearAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}
