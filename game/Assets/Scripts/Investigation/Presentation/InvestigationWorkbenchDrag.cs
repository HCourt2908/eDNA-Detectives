using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed class InvestigationWorkbenchDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public string Kind { get; private set; }
        public string Value { get; private set; }
        private RectTransform ghost;
        private RectTransform canvas;
        private string title;
        public void Configure(string kind, string value, string label) { Kind = kind; Value = value; title = label; }
        public void OnBeginDrag(PointerEventData eventData)
        {
            Canvas owner = GetComponentInParent<Canvas>();
            if (owner == null) return;
            canvas = owner.rootCanvas.GetComponent<RectTransform>();
            ghost = new GameObject("Workbench Drag Preview", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
            ghost.SetParent(canvas, false);
            ghost.sizeDelta = new Vector2(180f, 56f);
            ghost.GetComponent<Image>().color = InvestigationTheme.SurfaceRaised;
            CanvasGroup group = ghost.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false; group.alpha = .9f;
            Text text = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(ghost, false); text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 4f); text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            text.font = InvestigationTheme.BodyFont; text.fontSize = 14; text.color = InvestigationTheme.TextPrimary; text.alignment = TextAnchor.MiddleCenter; text.text = title; text.raycastTarget = false;
            OnDrag(eventData);
        }
        public void OnDrag(PointerEventData eventData)
        {
            if (ghost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, eventData.position, eventData.pressEventCamera, out Vector2 point)) ghost.anchoredPosition = point;
        }
        public void OnEndDrag(PointerEventData eventData) => RemoveGhost();
        private void OnDisable() => RemoveGhost();
        private void RemoveGhost() { if (ghost != null) { ghost.gameObject.SetActive(false); Destroy(ghost.gameObject); } ghost = null; }
    }
}
