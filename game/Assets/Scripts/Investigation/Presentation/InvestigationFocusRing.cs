using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class InvestigationFocusRing : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerDownHandler
    {
        private GameObject ring;

        public bool SelectedByPointer { get; private set; }

        public void Configure(float radius, Color color)
        {
            if (ring != null) return;
            ring = new GameObject(
                "Focus Ring",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(InvestigationRoundedCorners),
                typeof(Outline));
            ring.transform.SetParent(transform, false);
            ring.transform.SetAsFirstSibling();
            RectTransform rect = ring.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-3f, -3f);
            rect.offsetMax = new Vector2(3f, 3f);
            Image image = ring.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            ring.GetComponent<InvestigationRoundedCorners>().Configure(radius + 3f);
            Outline outline = ring.GetComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            ring.SetActive(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SelectedByPointer = eventData is PointerEventData;
            if (ring != null) ring.SetActive(!SelectedByPointer);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            SelectedByPointer = true;
            if (ring != null) ring.SetActive(false);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SelectedByPointer = false;
            if (ring != null) ring.SetActive(false);
        }

        private void OnDisable()
        {
            SelectedByPointer = false;
            if (ring != null) ring.SetActive(false);
        }
    }
}
