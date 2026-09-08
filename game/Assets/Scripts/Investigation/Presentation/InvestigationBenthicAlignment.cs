using UnityEngine;

namespace EDNA.Investigation
{
    public sealed class InvestigationBenthicAlignment : MonoBehaviour
    {
        private RectTransform plot;
        private RectTransform mountain;
        private RectTransform[] markers;
        private Vector2[] positions;
        public void Configure(RectTransform area, RectTransform artwork, RectTransform[] targets)
        {
            plot = area; mountain = artwork; markers = targets;
            positions = new Vector2[targets.Length];
            for (int i = 0; i < targets.Length; i++) positions[i] = targets[i].anchorMin;
        }
        private void OnEnable() => Canvas.willRenderCanvases += Align;
        private void OnDisable() => Canvas.willRenderCanvases -= Align;
        private void Align()
        {
            if (plot == null || mountain == null || markers == null || plot.rect.width <= 0f) return;
            float width = Mathf.Min(1f, mountain.rect.width / plot.rect.width);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] == null) continue;
                Vector2 position = new Vector2(.5f + (positions[i].x - .5f) * width, positions[i].y);
                if (markers[i].anchorMin == position) continue;
                markers[i].anchorMin = markers[i].anchorMax = position;
            }
        }
    }
}
