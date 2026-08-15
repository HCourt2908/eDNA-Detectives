using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    public enum InvestigationGlyph
    {
        None,
        CaseFile,
        Compare,
        Hypothesis,
        Sample,
        Conclusion,
        Dna,
        Fish,
        Warning
    }

    /// <summary>
    /// Small code-native line icons used by the investigation UI. They avoid
    /// platform-dependent emoji and keep one stroke language at every scale.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationGlyphGraphic : MaskableGraphic
    {
        [SerializeField] private InvestigationGlyph glyph;
        [SerializeField, Min(1f)] private float strokeWidth = 2f;

        public InvestigationGlyph Glyph => glyph;

        public void SetGlyph(InvestigationGlyph value)
        {
            if (glyph == value) return;
            glyph = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (glyph == InvestigationGlyph.None) return;

            switch (glyph)
            {
                case InvestigationGlyph.CaseFile:
                    DrawCaseFile(vertexHelper);
                    break;
                case InvestigationGlyph.Compare:
                    DrawCompare(vertexHelper);
                    break;
                case InvestigationGlyph.Hypothesis:
                    DrawHypothesis(vertexHelper);
                    break;
                case InvestigationGlyph.Sample:
                    DrawSample(vertexHelper);
                    break;
                case InvestigationGlyph.Conclusion:
                    DrawConclusion(vertexHelper);
                    break;
                case InvestigationGlyph.Dna:
                    DrawDna(vertexHelper);
                    break;
                case InvestigationGlyph.Fish:
                    DrawFish(vertexHelper);
                    break;
                case InvestigationGlyph.Warning:
                    DrawWarning(vertexHelper);
                    break;
            }
        }

        private void DrawCaseFile(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.14f, 0.25f), P(0.14f, 0.73f), P(0.40f, 0.73f),
                P(0.49f, 0.63f), P(0.86f, 0.63f), P(0.86f, 0.25f), P(0.14f, 0.25f)
            });
            AddLine(vh, P(0.20f, 0.53f), P(0.80f, 0.53f));
        }

        private void DrawCompare(VertexHelper vh)
        {
            AddLine(vh, P(0.22f, 0.25f), P(0.22f, 0.75f));
            AddLine(vh, P(0.78f, 0.25f), P(0.78f, 0.75f));
            AddLine(vh, P(0.30f, 0.64f), P(0.66f, 0.64f));
            AddLine(vh, P(0.60f, 0.71f), P(0.68f, 0.64f));
            AddLine(vh, P(0.60f, 0.57f), P(0.68f, 0.64f));
            AddLine(vh, P(0.70f, 0.36f), P(0.34f, 0.36f));
            AddLine(vh, P(0.40f, 0.43f), P(0.32f, 0.36f));
            AddLine(vh, P(0.40f, 0.29f), P(0.32f, 0.36f));
        }

        private void DrawHypothesis(VertexHelper vh)
        {
            AddCircle(vh, P(0.50f, 0.72f), 0.07f);
            AddCircle(vh, P(0.25f, 0.29f), 0.07f);
            AddCircle(vh, P(0.75f, 0.29f), 0.07f);
            AddLine(vh, P(0.50f, 0.64f), P(0.50f, 0.49f));
            AddLine(vh, P(0.50f, 0.49f), P(0.25f, 0.37f));
            AddLine(vh, P(0.50f, 0.49f), P(0.75f, 0.37f));
        }

        private void DrawSample(VertexHelper vh)
        {
            AddLine(vh, P(0.34f, 0.78f), P(0.66f, 0.78f));
            AddLine(vh, P(0.40f, 0.78f), P(0.40f, 0.63f));
            AddLine(vh, P(0.60f, 0.78f), P(0.60f, 0.63f));
            AddPolyline(vh, new[]
            {
                P(0.40f, 0.63f), P(0.31f, 0.28f), P(0.36f, 0.21f),
                P(0.64f, 0.21f), P(0.69f, 0.28f), P(0.60f, 0.63f)
            });
            AddLine(vh, P(0.34f, 0.36f), P(0.66f, 0.36f));
        }

        private void DrawConclusion(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.20f, 0.20f), P(0.20f, 0.80f), P(0.80f, 0.80f),
                P(0.80f, 0.20f), P(0.20f, 0.20f)
            });
            AddPolyline(vh, new[] { P(0.33f, 0.49f), P(0.45f, 0.36f), P(0.69f, 0.64f) });
        }

        private void DrawDna(VertexHelper vh)
        {
            List<Vector2> left = new List<Vector2>();
            List<Vector2> right = new List<Vector2>();
            const int steps = 14;
            for (int index = 0; index <= steps; index++)
            {
                float t = index / (float)steps;
                float wave = Mathf.Sin(t * Mathf.PI * 2f) * 0.16f;
                left.Add(P(0.50f + wave, 0.16f + t * 0.68f));
                right.Add(P(0.50f - wave, 0.16f + t * 0.68f));
                if (index % 3 == 0) AddLine(vh, left[index], right[index], strokeWidth * 0.7f);
            }
            AddPolyline(vh, left);
            AddPolyline(vh, right);
        }

        private void DrawFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.18f, 0.50f), P(0.34f, 0.67f), P(0.60f, 0.69f),
                P(0.78f, 0.50f), P(0.60f, 0.31f), P(0.34f, 0.33f), P(0.18f, 0.50f)
            });
            AddPolyline(vh, new[] { P(0.18f, 0.50f), P(0.06f, 0.68f), P(0.06f, 0.32f), P(0.18f, 0.50f) });
            AddCircle(vh, P(0.62f, 0.54f), 0.025f, strokeWidth * 1.5f);
        }

        private void DrawWarning(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.50f, 0.84f), P(0.14f, 0.20f), P(0.86f, 0.20f), P(0.50f, 0.84f)
            });
            AddLine(vh, P(0.50f, 0.59f), P(0.50f, 0.38f), strokeWidth * 1.2f);
            AddCircle(vh, P(0.50f, 0.29f), 0.018f, strokeWidth * 1.6f);
        }

        private Vector2 P(float x, float y)
        {
            Rect rect = GetPixelAdjustedRect();
            float inset = Mathf.Max(strokeWidth * 1.5f, 2f);
            return new Vector2(
                Mathf.Lerp(rect.xMin + inset, rect.xMax - inset, x),
                Mathf.Lerp(rect.yMin + inset, rect.yMax - inset, y));
        }

        private void AddPolyline(VertexHelper vh, IReadOnlyList<Vector2> points)
        {
            for (int index = 1; index < points.Count; index++)
            {
                AddLine(vh, points[index - 1], points[index]);
            }
        }

        private void AddCircle(VertexHelper vh, Vector2 center, float normalizedRadius, float width = -1f)
        {
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * normalizedRadius;
            const int segments = 16;
            Vector2 previous = center + Vector2.right * radius;
            for (int index = 1; index <= segments; index++)
            {
                float angle = index / (float)segments * Mathf.PI * 2f;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                AddLine(vh, previous, next, width < 0f ? strokeWidth : width);
                previous = next;
            }
        }

        private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float width = -1f)
        {
            float resolvedWidth = width < 0f ? strokeWidth : width;
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f) return;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * resolvedWidth * 0.5f;
            int startIndex = vh.currentVertCount;
            AddVertex(vh, start - normal);
            AddVertex(vh, start + normal);
            AddVertex(vh, end + normal);
            AddVertex(vh, end - normal);
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
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
            strokeWidth = Mathf.Max(1f, strokeWidth);
            raycastTarget = false;
            SetVerticesDirty();
        }
#endif
    }
}
