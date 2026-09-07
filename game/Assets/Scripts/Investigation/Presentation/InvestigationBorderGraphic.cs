using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    // An actual hollow stroke, so keyboard focus never paints over a control.
    public sealed class InvestigationBorderGraphic : MaskableGraphic
    {
        private float radius = 10f;
        private float thickness = 2f;

        public void Configure(float cornerRadius, float strokeWidth)
        {
            radius = cornerRadius;
            thickness = strokeWidth;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect outer = rectTransform.rect;
            if (outer.width <= 0f || outer.height <= 0f) return;
            float stroke = Mathf.Min(thickness, Mathf.Min(outer.width, outer.height) * 0.5f);
            Rect inner = new Rect(outer.xMin + stroke, outer.yMin + stroke, outer.width - stroke * 2f, outer.height - stroke * 2f);
            float outerRadius = Mathf.Clamp(radius, stroke, Mathf.Min(outer.width, outer.height) * 0.5f);
            float innerRadius = Mathf.Max(0f, outerRadius - stroke);
            const int steps = 8;
            for (int corner = 0; corner < 4; corner++)
            {
                for (int step = 0; step <= steps; step++)
                {
                    float angle = (corner * 90f + step * 90f / steps) * Mathf.Deg2Rad;
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    vh.AddVert(CornerCenter(outer, outerRadius, corner) + direction * outerRadius, color, Vector2.zero);
                    vh.AddVert(CornerCenter(inner, innerRadius, corner) + direction * innerRadius, color, Vector2.zero);
                }
            }
            int pairs = 4 * (steps + 1);
            for (int index = 0; index < pairs; index++)
            {
                int next = (index + 1) % pairs;
                vh.AddTriangle(index * 2, next * 2, index * 2 + 1);
                vh.AddTriangle(next * 2, next * 2 + 1, index * 2 + 1);
            }
        }

        private static Vector2 CornerCenter(Rect rect, float r, int corner)
        {
            switch (corner)
            {
                case 0: return new Vector2(rect.xMax - r, rect.yMax - r);
                case 1: return new Vector2(rect.xMin + r, rect.yMax - r);
                case 2: return new Vector2(rect.xMin + r, rect.yMin + r);
                default: return new Vector2(rect.xMax - r, rect.yMin + r);
            }
        }
    }
}
