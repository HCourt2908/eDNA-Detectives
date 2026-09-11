using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public sealed class InvestigationSurveyLens : MonoBehaviour
    {
        private RectMask2D mask;
        private RectTransform divider;
        private Slider slider;
        public void Configure(RectMask2D currentMask, RectTransform handle, Slider control)
        {
            mask = currentMask; divider = handle; slider = control;
            slider.onValueChanged.AddListener(Apply);
            Apply(slider.value);
        }
        private void OnRectTransformDimensionsChange() { if (slider != null) Apply(slider.value); }
        private void Apply(float value)
        {
            if (mask == null) return;
            // RectMask2D applies padding in canvas space. Using the same width
            // keeps the endpoint fully clipped in scaled, fractional-width panes.
            mask.padding = new Vector4(mask.canvasRect.width * value, 0f, 0f, 0f);
            divider.anchorMin = new Vector2(value, 0f); divider.anchorMax = new Vector2(value, 1f);
            divider.offsetMin = new Vector2(-1f, 0f); divider.offsetMax = new Vector2(1f, 0f);
        }
    }
}
