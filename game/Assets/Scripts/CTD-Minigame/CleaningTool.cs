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
    public RectTransform contactPoint;
    public GameObject waterFlow;

    private bool dragging;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

    }

    private void Update()
    {
        if (dragging && minigame != null)
        {
            var camera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, contactPoint != null ? contactPoint.position : rectTransform.position);
            bool rinsing = toolType == CleaningToolType.SterileWater && minigame.IsRinsing;
            if (waterFlow != null) waterFlow.SetActive(rinsing);
            minigame.ApplyTool(toolType, screenPoint, Time.unscaledDeltaTime);
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
        if (waterFlow != null) waterFlow.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (minigame != null && !minigame.IsRinsing)
        {
            minigame.SelectTool(toolType);
        }
    }
}
