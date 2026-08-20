using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
    }

    public IEnumerator FadeToBlack(float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);

            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);

            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
