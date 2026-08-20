using System.Collections;
using UnityEngine;

public class TreeConnection : MonoBehaviour
{


    public IEnumerator FadeIn()
    {
        float duration = 0.5f;
        float time = 0f;

        CanvasGroup canvasGroup = transform.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = time / duration;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}
