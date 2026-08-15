using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    /// <summary>
    /// Static low-contrast bathymetric and sonar detail for the investigation shell.
    /// It adds depth without textures, animation, raycasts, or asset-loading cost.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationBackdropGraphic : MaskableGraphic
    {
        [SerializeField, Min(0.5f)] private float lineWidth = 1f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();

            for (int index = 1; index < 9; index++)
            {
                float y = Mathf.Lerp(rect.yMin, rect.yMax, index / 9f);
                AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), lineWidth * 0.55f);
            }

            Vector2 contourCentre = new Vector2(rect.xMax - rect.width * 0.18f, rect.yMax - rect.height * 0.22f);
            for (int ring = 0; ring < 5; ring++)
            {
                AddEllipse(vh, contourCentre, rect.width * (0.08f + ring * 0.045f), rect.height * (0.07f + ring * 0.035f), 42);
            }

            Vector2 sonarCentre = new Vector2(rect.xMin + rect.width * 0.12f, rect.yMin + rect.height * 0.16f);
            for (int ring = 1; ring <= 3; ring++)
            {
                AddArc(vh, sonarCentre, rect.height * ring * 0.08f, 0.05f, 0.42f, 24);
            }
            AddLine(vh, sonarCentre, sonarCentre + new Vector2(rect.height * 0.22f, rect.height * 0.11f), lineWidth);
        }

        private void AddEllipse(VertexHelper vh, Vector2 centre, float radiusX, float radiusY, int segments)
        {
            Vector2 previous = centre + new Vector2(radiusX, 0f);
            for (int index = 1; index <= segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                Vector2 next = centre + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                AddLine(vh, previous, next, lineWidth);
                previous = next;
            }
        }

        private void AddArc(VertexHelper vh, Vector2 centre, float radius, float start, float end, int segments)
        {
            float firstAngle = Mathf.Lerp(start, end, 0f) * Mathf.PI * 2f;
            Vector2 previous = centre + new Vector2(Mathf.Cos(firstAngle), Mathf.Sin(firstAngle)) * radius;
            for (int index = 1; index <= segments; index++)
            {
                float angle = Mathf.Lerp(start, end, index / (float)segments) * Mathf.PI * 2f;
                Vector2 next = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddLine(vh, previous, next, lineWidth);
                previous = next;
            }
        }

        private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float width)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width * 0.5f;
            int offset = vh.currentVertCount;
            AddVertex(vh, start - normal);
            AddVertex(vh, start + normal);
            AddVertex(vh, end + normal);
            AddVertex(vh, end - normal);
            vh.AddTriangle(offset, offset + 1, offset + 2);
            vh.AddTriangle(offset, offset + 2, offset + 3);
        }

        private void AddVertex(VertexHelper vh, Vector2 position)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vh.AddVert(vertex);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            lineWidth = Mathf.Max(0.5f, lineWidth);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
