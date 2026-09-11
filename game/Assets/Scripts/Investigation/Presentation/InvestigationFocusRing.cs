using System.Collections;
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
        public bool KeepVisibleOnKeyboardFocus { get; set; }

        public void Configure(float radius, Color color)
        {
            if (ring != null) return;
            ring = new GameObject(
                "Focus Ring",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(InvestigationBorderGraphic));
            ring.transform.SetParent(transform, false);
            ring.transform.SetAsFirstSibling();
            RectTransform rect = ring.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-2f, -2f);
            rect.offsetMax = new Vector2(2f, 2f);
            InvestigationBorderGraphic stroke = ring.GetComponent<InvestigationBorderGraphic>();
            stroke.Configure(radius + 2f, 2f);
            stroke.color = color;
            ring.SetActive(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SelectedByPointer = eventData is PointerEventData;
            if (ring != null) ring.SetActive(!SelectedByPointer);
            if (!SelectedByPointer && KeepVisibleOnKeyboardFocus) StartCoroutine(RevealFocusedControl());
        }

        private IEnumerator RevealFocusedControl()
        {
            yield return null;
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != gameObject) yield break;
            foreach (ScrollRect scroll in GetComponentsInParent<ScrollRect>())
            {
                if (scroll.content == null || scroll.viewport == null || !transform.IsChildOf(scroll.content)) continue;
                float hiddenHeight = scroll.content.rect.height - scroll.viewport.rect.height;
                if (!scroll.vertical || hiddenHeight <= 0f) continue;
                Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
                float shift = bounds.max.y > scroll.viewport.rect.yMax - 6f ? bounds.max.y - scroll.viewport.rect.yMax + 6f
                    : bounds.min.y < scroll.viewport.rect.yMin + 6f ? bounds.min.y - scroll.viewport.rect.yMin - 6f : 0f;
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + shift / hiddenHeight);
            }
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
            StopAllCoroutines();
            SelectedByPointer = false;
            if (ring != null) ring.SetActive(false);
        }
    }
}
