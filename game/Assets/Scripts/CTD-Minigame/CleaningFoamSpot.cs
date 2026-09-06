using UnityEngine;
using UnityEngine.UI;

public class CleaningFoamSpot : MonoBehaviour
{
    public Image[] bubbles;
    private RectTransform rect;
    private Vector2[] basePositions;
    private float seed;
    private float coverage;
    public RectTransform Rect => rect != null ? rect : rect = GetComponent<RectTransform>();

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        seed = Mathf.Abs(rect.anchoredPosition.x * .017f + rect.anchoredPosition.y * .031f + transform.GetSiblingIndex() * 1.37f);
        basePositions = new Vector2[bubbles == null ? 0 : bubbles.Length];
        for (int i = 0; i < basePositions.Length; i++)
        {
            basePositions[i] = bubbles[i].rectTransform.anchoredPosition;
        }
    }

    private void Update()
    {
        if (coverage <= .01f || bubbles == null) return;
        float time = Time.unscaledTime;
        float spotPulse = 1f + Mathf.Sin(time * 3.2f + seed) * .035f;
        transform.localScale = Vector3.one * Mathf.Lerp(.55f, 1f, coverage) * spotPulse;
        for (int i = 0; i < bubbles.Length; i++)
        {
            Image bubble = bubbles[i];
            float phase = seed + i * 1.83f;
            float wobble = 1f + Mathf.Sin(time * (2.1f + i * .22f) + phase) * .1f;
            Vector2 drift = new Vector2(
                Mathf.Sin(time * (1.3f + i * .11f) + phase) * 3.5f,
                Mathf.Cos(time * (1.6f + i * .09f) + phase) * 3.5f);
            bubble.rectTransform.anchoredPosition = basePositions[i] + drift;
            bubble.rectTransform.localScale = Vector3.one * wobble;
            Color colour = bubble.color;
            colour.a = Mathf.Lerp(0f, .88f, coverage) * (.9f + Mathf.Sin(time * 2.6f + phase) * .1f);
            bubble.color = colour;
        }
    }

    public void SetCoverage(float coverage)
    {
        this.coverage = Mathf.Clamp01(coverage);
        gameObject.SetActive(coverage > .01f);
        float pulse = coverage > .01f ? 1f + Mathf.Sin(Time.unscaledTime * 4f + Rect.anchoredPosition.x) * .04f : 1f;
        transform.localScale = Vector3.one * Mathf.Lerp(.55f, 1f, coverage) * pulse;
        foreach (Image bubble in bubbles)
        {
            Color c = bubble.color;
            c.a = Mathf.Lerp(0f, .88f, this.coverage);
            bubble.color = c;
        }
    }
}
