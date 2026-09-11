using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout is authored in the prefab. Only indicator state changes at runtime.
public class SamplingCockpitView : MonoBehaviour
{
    public RectTransform gauge, marker, targetBand;
    public TMP_Text depthLabel;
    public Button actionButton;
    public TMP_Text actionLabel;
    public Image buttonFace;
    public Image[] bottleOutlines;
    public TMP_Text[] bottleChecks;
    public Image[] bottleStateOverlays;
    [Header("Sampling cue colours")]
    public Color normalBlue = new Color(0.02f, 0.38f, 0.68f);
    public Color alertYellow = new Color(1f, 0.78f, 0.08f);
    public Color collectedGreen = new Color(0.12f, 0.95f, 0.34f, 0.72f);
    public Color failedRed = new Color(0.95f, 0.12f, 0.16f, 0.72f);
    [Range(0.5f, 8f)] public float flashCyclesPerSecond = 4f;
    [Range(0f, 0.25f)] public float flashPulseScale = 0.08f;
    public float Depth { get; private set; }
    public bool CueActive { get; private set; }

    private Vector3 baseButtonScale = Vector3.one;

    private void Awake()
    {
        if (actionButton != null)
        {
            baseButtonScale = actionButton.transform.localScale;
        }
    }

    public void ResetIndicators()
    {
        if (bottleOutlines != null)
        {
            foreach (Image outline in bottleOutlines)
            {
                if (outline != null) outline.color = new Color(0.12f, 0.2f, 0.25f);
            }
        }

        if (bottleStateOverlays != null)
        {
            foreach (Image overlay in bottleStateOverlays)
            {
                if (overlay != null) overlay.gameObject.SetActive(false);
            }
        }

        if (bottleChecks != null)
        {
            foreach (TMP_Text check in bottleChecks)
            {
                if (check != null) check.gameObject.SetActive(false);
            }
        }

        ShowDepth(0f, 1000f);
        if (targetBand != null) targetBand.gameObject.SetActive(false);
        SetCue(false);
    }
    public void ShowDepth(float depth, float maximum)
    {
        Depth = depth;
        depthLabel.text = $"{Mathf.RoundToInt(depth)} m";
        var position = marker.anchoredPosition;
        position.y = -Mathf.Clamp01(depth / Mathf.Max(1f, maximum)) * gauge.rect.height;
        marker.anchoredPosition = position;
    }
    public void SetCue(bool active)
    {
        CueActive = active;
        if (!active)
        {
            ApplyButtonColor(normalBlue, false);
            if (actionButton != null) actionButton.transform.localScale = baseButtonScale;
        }

        if (targetBand != null)
        {
            Image bandImage = targetBand.GetComponent<Image>();
            if (bandImage != null)
            {
                bandImage.color = active
                    ? new Color(1f, 0.78f, 0.08f, 0.9f)
                    : new Color(0.1f, 0.65f, 0.9f, 0.28f);
            }
        }
    }

    public void MarkCollected(int index)
    {
        ApplyBottleState(index, collectedGreen, new Color(0.20f, 1f, 0.34f), "✓");
    }

    public void MarkFailed(int index)
    {
        ApplyBottleState(index, failedRed, new Color(1f, 0.22f, 0.22f), "MISS");
    }

    private void ApplyBottleState(int index, Color overlayColor, Color outlineColor, string label)
    {
        if (index < 0 || bottleOutlines == null || index >= bottleOutlines.Length)
        {
            return;
        }

        if (bottleOutlines[index] != null)
        {
            bottleOutlines[index].color = outlineColor;
        }

        if (bottleStateOverlays != null && index < bottleStateOverlays.Length &&
            bottleStateOverlays[index] != null)
        {
            bottleStateOverlays[index].color = overlayColor;
            bottleStateOverlays[index].gameObject.SetActive(true);
        }

        if (bottleChecks != null && index < bottleChecks.Length && bottleChecks[index] != null)
        {
            bottleChecks[index].text = label;
            bottleChecks[index].color = outlineColor;
            bottleChecks[index].gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (CueActive && actionButton != null)
        {
            float phase = Mathf.Repeat(Time.unscaledTime * flashCyclesPerSecond, 1f);
            bool yellow = phase < 0.5f;
            ApplyButtonColor(yellow ? alertYellow : normalBlue, yellow);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * flashCyclesPerSecond * Mathf.PI * 2f) * flashPulseScale;
            actionButton.transform.localScale = baseButtonScale * pulse;
        }
    }

    private void ApplyButtonColor(Color color, bool darkLabel)
    {
        if (buttonFace != null) buttonFace.color = color;
        if (actionLabel != null) actionLabel.color = darkLabel
            ? new Color(0.015f, 0.04f, 0.065f)
            : Color.white;
    }

    private void OnDisable()
    {
        SetCue(false);
    }
}
