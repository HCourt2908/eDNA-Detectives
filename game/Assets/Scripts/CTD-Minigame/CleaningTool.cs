using UnityEngine;
using UnityEngine.EventSystems;

public enum CleaningToolType
{
    DecontaminationSolution,
    SterileWater
}

public class CleaningTool : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public CleaningToolType toolType;
    public CleaningMinigame minigame;
    public RectTransform rectTransform;
    public Canvas rootCanvas;

    private Vector2 startingPosition;
    private bool dragging;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        startingPosition = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (dragging && minigame != null)
        {
            minigame.ApplyTool(toolType, rectTransform.position, Time.unscaledDeltaTime);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = rootCanvas == null ? 1f : rootCanvas.scaleFactor;
        rectTransform.anchoredPosition += eventData.delta / Mathf.Max(0.01f, scaleFactor);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        rectTransform.anchoredPosition = startingPosition;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (minigame != null)
        {
            minigame.SelectTool(toolType);
        }
    }
}
