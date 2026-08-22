using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    public enum InvestigationV2Glyph
    {
        Shark,
        Tuna,
        Krill,
        SeaStar,
        Mussel,
        Warming,
        Plastic,
        LongLine,
        BottomTrawling,
        Check,
        Cross,
        Question,
        ArrowRight,
        FishingLine,
        Seafloor
    }

    [DisallowMultipleComponent]
    public sealed class InvestigationV2GlyphGraphic : MaskableGraphic
    {
        [SerializeField] private InvestigationV2Glyph glyph;
        [SerializeField] private bool ghost;

        public void SetGlyph(InvestigationV2Glyph value)
        {
            glyph = value;
            SetVerticesDirty();
        }

        public void SetGhost(bool value)
        {
            ghost = value;
            SetVerticesDirty();
        }

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color drawColor = color;
            if (ghost) drawColor.a *= 0.38f;
            switch (glyph)
            {
                case InvestigationV2Glyph.Shark: DrawFish(vh, drawColor, 1.08f, true); break;
                case InvestigationV2Glyph.Tuna: DrawFish(vh, drawColor, 1f, false); break;
                case InvestigationV2Glyph.Krill: DrawKrill(vh, drawColor); break;
                case InvestigationV2Glyph.SeaStar: DrawSeaStar(vh, drawColor); break;
                case InvestigationV2Glyph.Mussel: DrawMussel(vh, drawColor); break;
                case InvestigationV2Glyph.Warming: DrawWarming(vh, drawColor); break;
                case InvestigationV2Glyph.Plastic: DrawPlastic(vh, drawColor); break;
                case InvestigationV2Glyph.LongLine: DrawLongLine(vh, drawColor); break;
                case InvestigationV2Glyph.BottomTrawling: DrawTrawl(vh, drawColor); break;
                case InvestigationV2Glyph.Check: DrawCheck(vh, drawColor); break;
                case InvestigationV2Glyph.Cross: DrawCross(vh, drawColor); break;
                case InvestigationV2Glyph.Question: DrawQuestion(vh, drawColor); break;
                case InvestigationV2Glyph.ArrowRight: DrawArrow(vh, drawColor); break;
                case InvestigationV2Glyph.FishingLine: DrawLongLine(vh, drawColor); break;
                case InvestigationV2Glyph.Seafloor: DrawSeafloor(vh, drawColor); break;
            }
            if (ghost) DrawDashedFrame(vh, drawColor);
        }

        private void DrawFish(VertexHelper vh, Color c, float scale, bool shark)
        {
            Vector2 center = P(0.52f, 0.5f);
            AddEllipse(vh, center, 0.31f * scale, 0.19f, c, 20);
            AddTriangle(vh, P(0.22f, 0.5f), P(0.04f, 0.72f), P(0.05f, 0.28f), c);
            if (shark)
            {
                AddTriangle(vh, P(0.46f, 0.68f), P(0.56f, 0.91f), P(0.64f, 0.67f), c);
                AddTriangle(vh, P(0.54f, 0.34f), P(0.65f, 0.16f), P(0.68f, 0.37f), c);
            }
            else
            {
                AddTriangle(vh, P(0.49f, 0.68f), P(0.57f, 0.82f), P(0.65f, 0.67f), c);
            }
            AddCircle(vh, P(0.72f, 0.55f), 0.026f, InvestigationV2Theme.Background, 10);
        }

        private void DrawKrill(VertexHelper vh, Color c)
        {
            AddEllipse(vh, P(0.5f, 0.52f), 0.32f, 0.14f, c, 18);
            AddTriangle(vh, P(0.19f, 0.52f), P(0.06f, 0.66f), P(0.08f, 0.39f), c);
            for (int index = 0; index < 4; index++)
            {
                float x = 0.34f + index * 0.11f;
                AddLine(vh, P(x, 0.41f), P(x - 0.04f, 0.22f), c, 0.022f);
            }
            AddCircle(vh, P(0.73f, 0.56f), 0.022f, InvestigationV2Theme.Background, 9);
        }

        private void DrawSeaStar(VertexHelper vh, Color c)
        {
            List<Vector2> points = new List<Vector2>();
            for (int index = 0; index < 10; index++)
            {
                float angle = Mathf.PI * 0.5f + index * Mathf.PI / 5f;
                float radius = index % 2 == 0 ? 0.38f : 0.16f;
                points.Add(P(0.5f + Mathf.Cos(angle) * radius, 0.5f + Mathf.Sin(angle) * radius));
            }
            AddPolygon(vh, points, c);
        }

        private void DrawMussel(VertexHelper vh, Color c)
        {
            List<Vector2> points = new List<Vector2>
            {
                P(0.50f, 0.88f), P(0.73f, 0.70f), P(0.78f, 0.40f), P(0.62f, 0.14f),
                P(0.38f, 0.14f), P(0.22f, 0.40f), P(0.27f, 0.70f)
            };
            AddPolygon(vh, points, c);
            AddLine(vh, P(0.5f, 0.82f), P(0.5f, 0.19f), InvestigationV2Theme.Background, 0.018f);
        }

        private void DrawWarming(VertexHelper vh, Color c)
        {
            AddLine(vh, P(0.45f, 0.77f), P(0.45f, 0.34f), c, 0.055f);
            AddLine(vh, P(0.55f, 0.77f), P(0.55f, 0.34f), c, 0.055f);
            AddCircle(vh, P(0.5f, 0.25f), 0.15f, c, 18);
            AddLine(vh, P(0.5f, 0.63f), P(0.5f, 0.31f), InvestigationV2Theme.Background, 0.025f);
            AddLine(vh, P(0.70f, 0.70f), P(0.86f, 0.79f), c, 0.025f);
            AddLine(vh, P(0.73f, 0.50f), P(0.91f, 0.50f), c, 0.025f);
            AddLine(vh, P(0.70f, 0.31f), P(0.86f, 0.22f), c, 0.025f);
        }

        private void DrawPlastic(VertexHelper vh, Color c)
        {
            AddQuad(vh, P(0.34f, 0.18f), P(0.66f, 0.18f), P(0.66f, 0.68f), P(0.34f, 0.68f), c);
            AddQuad(vh, P(0.41f, 0.68f), P(0.59f, 0.68f), P(0.59f, 0.86f), P(0.41f, 0.86f), c);
            AddLine(vh, P(0.35f, 0.48f), P(0.65f, 0.48f), InvestigationV2Theme.Background, 0.025f);
        }

        private void DrawLongLine(VertexHelper vh, Color c)
        {
            AddLine(vh, P(0.18f, 0.77f), P(0.78f, 0.77f), c, 0.035f);
            AddLine(vh, P(0.72f, 0.77f), P(0.72f, 0.45f), c, 0.035f);
            AddArc(vh, P(0.58f, 0.42f), 0.18f, -1.55f, 1.7f, c, 0.04f, 13);
        }

        private void DrawTrawl(VertexHelper vh, Color c)
        {
            AddQuad(vh, P(0.20f, 0.76f), P(0.80f, 0.76f), P(0.68f, 0.22f), P(0.32f, 0.22f), c);
            for (int index = 0; index < 4; index++)
            {
                float t = index / 3f;
                AddLine(vh, Vector2.Lerp(P(0.22f, 0.72f), P(0.34f, 0.26f), t), Vector2.Lerp(P(0.78f, 0.72f), P(0.66f, 0.26f), 1f - t), InvestigationV2Theme.Background, 0.017f);
            }
            AddLine(vh, P(0.12f, 0.14f), P(0.88f, 0.14f), c, 0.04f);
        }

        private void DrawCheck(VertexHelper vh, Color c)
        {
            AddLine(vh, P(0.18f, 0.48f), P(0.40f, 0.27f), c, 0.07f);
            AddLine(vh, P(0.40f, 0.27f), P(0.83f, 0.75f), c, 0.07f);
        }

        private void DrawCross(VertexHelper vh, Color c)
        {
            AddLine(vh, P(0.23f, 0.23f), P(0.77f, 0.77f), c, 0.07f);
            AddLine(vh, P(0.77f, 0.23f), P(0.23f, 0.77f), c, 0.07f);
        }

        private void DrawQuestion(VertexHelper vh, Color c)
        {
            AddArc(vh, P(0.5f, 0.64f), 0.23f, -0.2f, 3.5f, c, 0.06f, 15);
            AddLine(vh, P(0.51f, 0.44f), P(0.51f, 0.32f), c, 0.06f);
            AddCircle(vh, P(0.51f, 0.18f), 0.04f, c, 10);
        }

        private void DrawArrow(VertexHelper vh, Color c)
        {
            AddLine(vh, P(0.15f, 0.5f), P(0.82f, 0.5f), c, 0.05f);
            AddTriangle(vh, P(0.82f, 0.5f), P(0.64f, 0.68f), P(0.64f, 0.32f), c);
        }

        private void DrawSeafloor(VertexHelper vh, Color c)
        {
            AddPolyline(vh, new[] { P(0.08f, 0.32f), P(0.25f, 0.38f), P(0.42f, 0.30f), P(0.58f, 0.36f), P(0.75f, 0.29f), P(0.92f, 0.34f) }, c, 0.045f);
            AddLine(vh, P(0.15f, 0.65f), P(0.85f, 0.65f), c, 0.025f);
        }

        private void DrawDashedFrame(VertexHelper vh, Color c)
        {
            for (int index = 0; index < 6; index++)
            {
                float start = 0.12f + index * 0.13f;
                AddLine(vh, P(start, 0.08f), P(start + 0.07f, 0.08f), c, 0.018f);
                AddLine(vh, P(start, 0.92f), P(start + 0.07f, 0.92f), c, 0.018f);
            }
        }

        private Vector2 P(float x, float y)
        {
            Rect rect = rectTransform.rect;
            return new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y);
        }

        private static void AddEllipse(VertexHelper vh, Vector2 center, float radiusX01, float radiusY01, Color c, int segments)
        {
            Rect bounds = vh == null ? Rect.zero : Rect.zero;
            float rx = radiusX01 * 100f;
            float ry = radiusY01 * 100f;
            List<Vector2> points = new List<Vector2>(segments);
            for (int index = 0; index < segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                points.Add(center + new Vector2(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry));
            }
            AddPolygon(vh, points, c);
        }

        private static void AddCircle(VertexHelper vh, Vector2 center, float radius01, Color c, int segments)
        {
            float radius = radius01 * 100f;
            List<Vector2> points = new List<Vector2>(segments);
            for (int index = 0; index < segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            AddPolygon(vh, points, c);
        }

        private static void AddArc(VertexHelper vh, Vector2 center, float radius01, float from, float to, Color c, float width01, int segments)
        {
            float radius = radius01 * 100f;
            for (int index = 0; index < segments; index++)
            {
                float a = Mathf.Lerp(from, to, index / (float)segments);
                float b = Mathf.Lerp(from, to, (index + 1) / (float)segments);
                AddLine(vh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, c, width01);
            }
        }

        private static void AddPolyline(VertexHelper vh, IReadOnlyList<Vector2> points, Color c, float width01)
        {
            for (int index = 0; index < points.Count - 1; index++) AddLine(vh, points[index], points[index + 1], c, width01);
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Color c, float width01)
        {
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * Mathf.Max(1.2f, width01 * 100f * 0.5f);
            AddQuad(vh, a - normal, a + normal, b + normal, b - normal, c);
        }

        private static void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert; vertex.color = color;
            vertex.position = a; vh.AddVert(vertex); vertex.position = b; vh.AddVert(vertex);
            vertex.position = c; vh.AddVert(vertex); vertex.position = d; vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddTriangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert; vertex.color = color;
            vertex.position = a; vh.AddVert(vertex); vertex.position = b; vh.AddVert(vertex); vertex.position = c; vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2);
        }

        private static void AddPolygon(VertexHelper vh, IReadOnlyList<Vector2> points, Color color)
        {
            if (points == null || points.Count < 3) return;
            Vector2 center = Vector2.zero;
            for (int index = 0; index < points.Count; index++) center += points[index];
            center /= points.Count;
            int centerIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert; vertex.color = color; vertex.position = center; vh.AddVert(vertex);
            for (int index = 0; index < points.Count; index++) { vertex.position = points[index]; vh.AddVert(vertex); }
            for (int index = 0; index < points.Count; index++) vh.AddTriangle(centerIndex, centerIndex + 1 + index, centerIndex + 1 + ((index + 1) % points.Count));
        }
    }
}
