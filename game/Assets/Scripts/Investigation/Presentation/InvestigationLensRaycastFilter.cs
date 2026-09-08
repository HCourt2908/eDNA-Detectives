using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed class InvestigationLensRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
    {
        private Slider slider;
        private bool historical;
        public void Configure(Slider value, bool isHistorical = false) { slider = value; historical = isHistorical; }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (slider == null) return true;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPoint, eventCamera, out Vector2 point)) return false;
            float x = Mathf.InverseLerp(rect.rect.xMin, rect.rect.xMax, point.x);
            return historical ? x < slider.value : x >= slider.value;
        }
    }
}
