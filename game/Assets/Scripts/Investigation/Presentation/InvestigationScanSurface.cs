using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed class InvestigationScanSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private Slider slider;
        public void Configure(Slider control) => slider = control;
        public void OnPointerDown(PointerEventData data) => Scan(data);
        public void OnDrag(PointerEventData data) => Scan(data);
        private void Scan(PointerEventData data)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (slider != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out Vector2 point))
                slider.value = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, point.x);
        }
    }
}
