using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Retracts the recovery cable from the fixed deck winch to the moving CTD.
/// The cable remains an independent authored UI object; runtime only changes
/// its length and endpoint while the recovery animation is playing.
/// </summary>
public class RecoveryCableController : MonoBehaviour
{
    [Header("References")]
    public RectTransform cableRect;
    public RectTransform recoveredRosette;
    public Image cableImage;

    [Header("Winch and reel")]
    [Tooltip("Vertical position of the fixed cable attachment on the deck.")]
    public float winchTopY = 440f;
    [Min(0f)] public float minimumLength = 18f;
    [Tooltip("Small overlap into the Rosette top edge, in local parent units.")]
    public float rosetteOverlap = 2f;
    [Range(0f, 1f)] public float fadeAtEnd = 0.92f;

    private RectTransform parentRect;
    private Color authoredColour;
    private bool captured;
    private readonly Vector3[] corners = new Vector3[4];

    public void Begin(RectTransform rosette)
    {
        if (cableRect == null)
        {
            cableRect = GetComponent<RectTransform>();
        }

        if (cableImage == null)
        {
            cableImage = GetComponent<Image>();
        }

        recoveredRosette = rosette;
        parentRect = cableRect != null ? cableRect.parent as RectTransform : null;
        if (!captured && cableRect != null)
        {
            winchTopY = cableRect.anchoredPosition.y + (1f - cableRect.pivot.y) * cableRect.rect.height;
            authoredColour = cableImage != null ? cableImage.color : Color.white;
            captured = true;
        }

        if (cableRect == null || recoveredRosette == null)
        {
            return;
        }

        cableRect.pivot = new Vector2(0.5f, 1f);
        cableRect.anchoredPosition = new Vector2(cableRect.anchoredPosition.x, winchTopY);
        SetTarget(recoveredRosette, 0f);
    }

    public void SetTarget(RectTransform rosette, float recoveryProgress)
    {
        if (cableRect == null || rosette == null || parentRect == null)
        {
            return;
        }

        recoveredRosette = rosette;
        rosette.GetWorldCorners(corners);
        float rosetteTopY = float.MinValue;
        for (int index = 0; index < corners.Length; index++)
        {
            rosetteTopY = Mathf.Max(rosetteTopY, parentRect.InverseTransformPoint(corners[index]).y);
        }

        float cableLength = Mathf.Max(minimumLength, winchTopY - rosetteTopY + rosetteOverlap);
        cableRect.pivot = new Vector2(0.5f, 1f);
        cableRect.anchoredPosition = new Vector2(cableRect.anchoredPosition.x, winchTopY);
        cableRect.sizeDelta = new Vector2(cableRect.sizeDelta.x, cableLength);

        if (cableImage != null)
        {
            float fade = fadeAtEnd >= 1f
                ? 1f
                : 1f - Mathf.InverseLerp(fadeAtEnd, 1f, Mathf.Clamp01(recoveryProgress));
            Color colour = captured ? authoredColour : cableImage.color;
            colour.a *= fade;
            cableImage.color = colour;
        }
    }
}
