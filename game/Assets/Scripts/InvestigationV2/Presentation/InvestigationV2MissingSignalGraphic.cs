using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2MissingSignalGraphic : MaskableGraphic
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
            Vector2 start = new Vector2(rect.xMin + rect.width * 0.20f, rect.yMin + rect.height * 0.24f);
            Vector2 end = new Vector2(rect.xMin + rect.width * 0.80f, rect.yMin + rect.height * 0.76f);
            Vector2 direction = (end - start).normalized;
            float total = Vector2.Distance(start, end);
            float segment = total * 0.18f;
            float gap = total * 0.10f;
            for (float offset = 0f; offset < total; offset += segment + gap)
            {
                Vector2 a = start + direction * offset;
                Vector2 b = start + direction * Mathf.Min(offset + segment, total);
                AddLine(vh, a, b, color, Mathf.Max(2f, rect.height * 0.035f));
            }
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * thickness * 0.5f;
            int startIndex = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }
    }
}
