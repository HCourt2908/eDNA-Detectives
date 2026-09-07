using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Interactable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISubmitHandler, ISelectHandler, IDeselectHandler
{
    private Outline outline;
    public bool isInteractable;

    [SerializeField] private UnityEvent onInteract;

    private Coroutine glowCoroutine;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        isInteractable = true;

        Color color = outline.effectColor;
        color.a = 0f;
        outline.effectColor = color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isInteractable) SetGlow(100f/255f);
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

    public void OnSubmit(BaseEventData eventData)
    {
        if (!isInteractable) return;

        SetGlow(0f);
        onInteract.Invoke();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (isInteractable)
        {
            SetGlow(100f / 255f);
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        SetGlow(0f);
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
        isInteractable = false;
    }

    public void enableInteraction()
    {
        isInteractable = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isInteractable) return;
        SetGlow(0f);
        onInteract.Invoke();
    }


}
