using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    /// <summary>A circular restart arrow in the same stroke style as the HUD icons.</summary>
    public sealed class InvestigationRestartGraphic : MaskableGraphic
    {
        protected override void Awake() { base.Awake(); raycastTarget = false; }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * .34f;
            float halfStroke = Mathf.Min(rect.width, rect.height) * .045f;
            const int segments = 32;
            for (int i = 0; i <= segments; i++)
            {
                float angle = Mathf.Lerp(135f, 430f, i / (float)segments) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * (radius - halfStroke), color, Vector2.zero);
                vh.AddVert(center + direction * (radius + halfStroke), color, Vector2.zero);
                if (i == segments) continue;
                int a = i * 2;
                vh.AddTriangle(a, a + 1, a + 2);
                vh.AddTriangle(a + 1, a + 3, a + 2);
            }
            Vector2 tip = center + new Vector2(-.7071f, .7071f) * radius;
            int first = vh.currentVertCount;
            vh.AddVert(tip + new Vector2(-halfStroke * 2f, -halfStroke * 3f), color, Vector2.zero);
            vh.AddVert(tip + new Vector2(-halfStroke * 2f, halfStroke * 3f), color, Vector2.zero);
            vh.AddVert(tip + new Vector2(halfStroke * 4f, -halfStroke * 3f), color, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
        }
    }
}
