using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One preloaded organism in the 3.1 depth world. Its authored x position,
/// size, depth and lane offset remain editable in the Inspector; runtime code
/// only applies the current depth scroll and horizontal swimming.
/// </summary>
public class SamplingBiologyFish : MonoBehaviour
{
    [Header("Depth encounter")]
    [Range(0f, 1010f)] public float depthMeters = 500f;
    [Tooltip("Small lane adjustment that keeps organisms at different heights while sharing a depth.")]
    public float laneOffset;

    [Header("Horizontal swim")]
    public float horizontalSpeed = 28f;
    public float swimPhase;
    [Range(0f, 120f)] public float swimWobble = 12f;
    [Range(0f, 10f)] public float swimWobbleSpeed = 1.4f;
    public float horizontalWrap = 1040f;

    [Header("Visual response")]
    [Range(0f, 1f)] public float opacity = 0.82f;
    [Min(1f)] public float depthPixelsPerMeter = 1.8f;
    [Min(0f)] public float fadeDepthMeters = 210f;

    private RectTransform rectTransform;
    private Image image;
    private float authoredX;
    private float initialX;
    private float wobblePhase;
    private float swimTime;
    private bool initialized;

    public void Configure(Sprite sprite, float depth, float speed, float scale, float phase)
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        depthMeters = depth;
        horizontalSpeed = speed;
        wobblePhase = phase;
        rectTransform.sizeDelta = new Vector2(scale, scale);
        authoredX = rectTransform.anchoredPosition.x;
        initialX = authoredX;
        initialized = true;
    }

    public void Begin(float currentDepth, float maximumDepth)
    {
        if (!initialized)
        {
            image = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            authoredX = rectTransform.anchoredPosition.x;
            initialX = authoredX;
            wobblePhase = swimPhase;
            initialized = true;
        }

        depthPixelsPerMeter = Mathf.Max(1f, depthPixelsPerMeter);
        horizontalWrap = Mathf.Max(horizontalWrap, 100f);
        initialX = Mathf.Clamp(initialX, -horizontalWrap, horizontalWrap);
        authoredX = initialX;
        swimTime = 0f;
        rectTransform.anchoredPosition = new Vector2(authoredX, 0f);
        ApplyDepth(currentDepth, maximumDepth, 0f);
    }

    public void ApplyDepth(float currentDepth, float maximumDepth, float deltaTime)
    {
        if (!initialized || rectTransform == null || image == null)
        {
            return;
        }

        authoredX += horizontalSpeed * deltaTime;
        if (authoredX > horizontalWrap)
        {
            authoredX = -horizontalWrap;
        }
        else if (authoredX < -horizontalWrap)
        {
            authoredX = horizontalWrap;
        }

        swimTime += Mathf.Max(0f, deltaTime);
        float wobble = Mathf.Sin(swimTime * swimWobbleSpeed + wobblePhase) * swimWobble;
        float relativeDepth = (currentDepth - depthMeters) * depthPixelsPerMeter;
        rectTransform.anchoredPosition = new Vector2(authoredX, relativeDepth + laneOffset + wobble);

        float distance = Mathf.Abs(currentDepth - depthMeters);
        float visibility = fadeDepthMeters <= 0f
            ? 1f
            : 1f - Mathf.Clamp01(distance / fadeDepthMeters);
        Color colour = image.color;
        colour.a = opacity * visibility;
        image.color = colour;
    }
}
