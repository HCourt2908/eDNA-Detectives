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
        Warning,
        Check,
        ColdFish,
        PredatorFish,
        StableFish,
        PreyFish,
        DeepFish,
        WarmFish
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
                case InvestigationGlyph.Check:
                    DrawCheck(vertexHelper);
                    break;
                case InvestigationGlyph.ColdFish:
                    DrawColdFish(vertexHelper);
                    break;
                case InvestigationGlyph.PredatorFish:
                    DrawPredatorFish(vertexHelper);
                    break;
                case InvestigationGlyph.StableFish:
                    DrawStableFish(vertexHelper);
                    break;
                case InvestigationGlyph.PreyFish:
                    DrawPreyFish(vertexHelper);
                    break;
                case InvestigationGlyph.DeepFish:
                    DrawDeepFish(vertexHelper);
                    break;
                case InvestigationGlyph.WarmFish:
                    DrawWarmFish(vertexHelper);
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

        private void DrawCheck(VertexHelper vh)
        {
            AddPolyline(vh, new[] { P(0.18f, 0.50f), P(0.40f, 0.29f), P(0.82f, 0.72f) });
        }

        private void DrawColdFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.16f, 0.50f), P(0.31f, 0.63f), P(0.62f, 0.61f),
                P(0.82f, 0.50f), P(0.62f, 0.39f), P(0.31f, 0.37f), P(0.16f, 0.50f)
            });
            AddPolyline(vh, new[] { P(0.17f, 0.50f), P(0.05f, 0.67f), P(0.06f, 0.33f), P(0.17f, 0.50f) });
            AddPolyline(vh, new[] { P(0.42f, 0.62f), P(0.50f, 0.76f), P(0.59f, 0.61f) });
            AddLine(vh, P(0.29f, 0.50f), P(0.70f, 0.50f), strokeWidth * 0.6f);
            AddCircle(vh, P(0.68f, 0.53f), 0.018f, strokeWidth * 1.3f);
        }

        private void DrawPredatorFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.13f, 0.50f), P(0.32f, 0.70f), P(0.62f, 0.67f),
                P(0.88f, 0.53f), P(0.70f, 0.44f), P(0.60f, 0.31f),
                P(0.32f, 0.33f), P(0.13f, 0.50f)
            });
            AddPolyline(vh, new[] { P(0.15f, 0.50f), P(0.03f, 0.72f), P(0.06f, 0.29f), P(0.15f, 0.50f) });
            AddPolyline(vh, new[] { P(0.38f, 0.68f), P(0.51f, 0.84f), P(0.63f, 0.66f) });
            AddCircle(vh, P(0.70f, 0.56f), 0.022f, strokeWidth * 1.4f);
            AddLine(vh, P(0.75f, 0.49f), P(0.86f, 0.53f), strokeWidth * 0.65f);
        }

        private void DrawStableFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.17f, 0.50f), P(0.33f, 0.72f), P(0.61f, 0.70f),
                P(0.80f, 0.50f), P(0.61f, 0.30f), P(0.33f, 0.28f), P(0.17f, 0.50f)
            });
            AddPolyline(vh, new[] { P(0.18f, 0.50f), P(0.06f, 0.68f), P(0.06f, 0.32f), P(0.18f, 0.50f) });
            AddLine(vh, P(0.38f, 0.31f), P(0.38f, 0.69f), strokeWidth * 0.65f);
            AddLine(vh, P(0.50f, 0.30f), P(0.50f, 0.70f), strokeWidth * 0.65f);
            AddCircle(vh, P(0.64f, 0.55f), 0.020f, strokeWidth * 1.35f);
        }

        private void DrawPreyFish(VertexHelper vh)
        {
            DrawMiniFish(vh, 0.20f, 0.66f, 0.38f, 0.76f);
            DrawMiniFish(vh, 0.43f, 0.45f, 0.72f, 0.59f);
            DrawMiniFish(vh, 0.18f, 0.25f, 0.43f, 0.38f);
        }

        private void DrawMiniFish(VertexHelper vh, float left, float bottom, float right, float top)
        {
            float midY = (bottom + top) * 0.5f;
            float tailX = left + (right - left) * 0.20f;
            AddPolyline(vh, new[]
            {
                P(tailX, midY), P(left, top), P(left, bottom), P(tailX, midY),
                P(left + (right - left) * 0.48f, top), P(right, midY),
                P(left + (right - left) * 0.48f, bottom), P(tailX, midY)
            });
        }

        private void DrawDeepFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.16f, 0.47f), P(0.30f, 0.69f), P(0.61f, 0.73f),
                P(0.83f, 0.52f), P(0.68f, 0.28f), P(0.34f, 0.27f), P(0.16f, 0.47f)
            });
            AddPolyline(vh, new[] { P(0.17f, 0.47f), P(0.05f, 0.66f), P(0.07f, 0.29f), P(0.17f, 0.47f) });
            AddPolyline(vh, new[] { P(0.49f, 0.72f), P(0.57f, 0.86f), P(0.72f, 0.86f) });
            AddCircle(vh, P(0.75f, 0.86f), 0.025f, strokeWidth);
            AddCircle(vh, P(0.66f, 0.56f), 0.024f, strokeWidth * 1.4f);
        }

        private void DrawWarmFish(VertexHelper vh)
        {
            AddPolyline(vh, new[]
            {
                P(0.16f, 0.50f), P(0.37f, 0.77f), P(0.63f, 0.67f),
                P(0.83f, 0.50f), P(0.63f, 0.33f), P(0.37f, 0.23f), P(0.16f, 0.50f)
            });
            AddPolyline(vh, new[] { P(0.17f, 0.50f), P(0.05f, 0.74f), P(0.05f, 0.26f), P(0.17f, 0.50f) });
            AddPolyline(vh, new[] { P(0.38f, 0.75f), P(0.54f, 0.91f), P(0.61f, 0.68f) });
            AddPolyline(vh, new[] { P(0.39f, 0.25f), P(0.53f, 0.09f), P(0.60f, 0.32f) });
            AddCircle(vh, P(0.66f, 0.55f), 0.020f, strokeWidth * 1.35f);
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
