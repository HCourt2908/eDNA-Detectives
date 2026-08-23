using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2SeamountFogGraphic : MaskableGraphic
    {
        private const int Segments = 32;

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

            AddMistEllipse(vh, rect, 0.50f, 0.34f, 0.50f, 0.28f, 0.18f);
            AddMistEllipse(vh, rect, 0.38f, 0.50f, 0.34f, 0.24f, 0.10f);
            AddMistEllipse(vh, rect, 0.66f, 0.52f, 0.30f, 0.22f, 0.08f);
        }

        private void AddMistEllipse(
            VertexHelper vh,
            Rect rect,
            float centerX,
            float centerY,
            float radiusX,
            float radiusY,
            float centerAlpha)
        {
            int centerIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            Color centerColor = color;
            centerColor.a *= centerAlpha;
            vertex.color = centerColor;
            vertex.position = new Vector2(
                rect.xMin + rect.width * centerX,
                rect.yMin + rect.height * centerY);
            vh.AddVert(vertex);

            Color edgeColor = color;
            edgeColor.a = 0f;
            vertex.color = edgeColor;
            for (int index = 0; index <= Segments; index++)
            {
                float angle = index / (float)Segments * Mathf.PI * 2f;
                vertex.position = new Vector2(
                    rect.xMin + rect.width * (centerX + Mathf.Cos(angle) * radiusX),
                    rect.yMin + rect.height * (centerY + Mathf.Sin(angle) * radiusY));
                vh.AddVert(vertex);
            }

            for (int index = 0; index < Segments; index++)
            {
                vh.AddTriangle(centerIndex, centerIndex + 1 + index, centerIndex + 2 + index);
            }
        }
    }
}
