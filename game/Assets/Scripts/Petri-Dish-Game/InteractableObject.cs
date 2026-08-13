using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Interactable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Outline outline;
    public bool interactable;

    [SerializeField] private UnityEvent onInteract;

    private Coroutine glowCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        interactable = true;

        Color color = outline.effectColor;
        color.a = 0f;
        outline.effectColor = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (interactable) SetGlow(100f/255f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetGlow(0f);
    }

    public void SetGlow(float alpha)
    {
        if (glowCoroutine != null) StopCoroutine(glowCoroutine);

        glowCoroutine = StartCoroutine(FadeGlow(alpha));
    }

    private IEnumerator FadeGlow(float alpha)
    {
        Color color = outline.effectColor;
        float startAlpha = color.a;

        float time = 0f;
        float duration = 0.25f;

        while (time < duration)
        {
            time += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, alpha, time / duration);
            outline.effectColor = color;

            yield return null;
        }

        color.a = alpha;
        outline.effectColor = color;
    }

    public void disableInteraction()
    {
        interactable = false;
    }

    public void enableInteraction()
    {
        interactable = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        SetGlow(0f);
        onInteract.Invoke();
    }


}
