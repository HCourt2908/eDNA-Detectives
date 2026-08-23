using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    /// <summary>
    /// Darkens the outer edges of the water so the eye settles on the comparison
    /// in the middle. Static: built once, never rebuilt.
    ///
    /// It sits behind the UI on purpose. Over the interface it would dim the
    /// Notebook paper and the header text; behind it, it only shapes the water.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationV2VignetteGraphic : MaskableGraphic
    {
        private const int SegmentsPerSide = 12;
        private const int PerimeterSegments = SegmentsPerSide * 4;
        private const float ClearRadius = 0.62f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            Vector2 center = rect.center;
            Color edge = color * InvestigationV2Theme.Vignette;
            Color clear = edge;
            clear.a = 0f;

            UIVertex vertex = UIVertex.simpleVert;
            int start = vh.currentVertCount;

            // Walk each side explicitly so the outer ring includes all four
            // corners. A radial polygon misses them unless a sample happens to
            // land on the exact corner angle for the current aspect ratio.
            for (int segment = 0; segment <= PerimeterSegments; segment++)
            {
                Vector2 outer = PerimeterPoint(rect, segment);
                Vector2 inner = center + (outer - center) * ClearRadius;

                vertex.color = clear;
                vertex.position = inner; vh.AddVert(vertex);
                vertex.color = edge;
                vertex.position = outer; vh.AddVert(vertex);
            }

            for (int segment = 0; segment < PerimeterSegments; segment++)
            {
                int a = start + segment * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }

        private static Vector2 PerimeterPoint(Rect rect, int segment)
        {
            int wrapped = segment % PerimeterSegments;
            int side = wrapped / SegmentsPerSide;
            float t = (wrapped % SegmentsPerSide) / (float)SegmentsPerSide;

            switch (side)
            {
                case 0: return new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, t), rect.yMin);
                case 1: return new Vector2(rect.xMax, Mathf.Lerp(rect.yMin, rect.yMax, t));
                case 2: return new Vector2(Mathf.Lerp(rect.xMax, rect.xMin, t), rect.yMax);
                default: return new Vector2(rect.xMin, Mathf.Lerp(rect.yMax, rect.yMin, t));
            }
        }
    }
}
