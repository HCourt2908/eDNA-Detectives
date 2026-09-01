using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    [DisallowMultipleComponent]
    public sealed class InvestigationSeamountGraphic : MaskableGraphic
    {
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
            AddTriangle(vh, Point(rect, 0.10f, 0.02f), Point(rect, 0.90f, 0.02f), Point(rect, 0.50f, 0.96f), new Color32(42, 125, 163, 105));
            AddTriangle(vh, Point(rect, 0.28f, 0.02f), Point(rect, 0.72f, 0.02f), Point(rect, 0.50f, 0.96f), new Color32(95, 212, 214, 25));
        }

        private static Vector2 Point(Rect rect, float x, float y) => new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y);

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert; vertex.color = color;
            vertex.position = a; vh.AddVert(vertex); vertex.position = b; vh.AddVert(vertex); vertex.position = c; vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
