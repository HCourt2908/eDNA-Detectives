using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    /// <summary>
    /// Shafts of light angling down from above the frame, plus a caustic band at
    /// the very top to give them a source.
    ///
    /// The mesh is tiny (five quads) but it lives on the shared UI canvas, so a
    /// rebuild re-batches that canvas. It is therefore throttled rather than run
    /// every frame, and stops entirely under reduced motion.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationV2GodRayGraphic : MaskableGraphic
    {
        private const int RayCount = 5;
        private const float RebuildInterval = 1f / 20f;
        private const int CausticSegments = 24;

        private float elapsed;
        private float sinceRebuild;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            if (InvestigationV2MotionSettings.ReducedMotion) return;
            elapsed += Time.unscaledDeltaTime;
            sinceRebuild += Time.unscaledDeltaTime;
            if (sinceRebuild < RebuildInterval) return;
            sinceRebuild = 0f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            for (int index = 0; index < RayCount; index++) AddRay(vh, rect, index);
            AddCaustic(vh, rect);
        }

        private void AddRay(VertexHelper vh, Rect rect, int index)
        {
            float sway = Mathf.Sin(elapsed * 0.16f + index * 1.7f) * rect.width * 0.05f;
            float topLeft = rect.xMin - rect.width * 0.12f + index * rect.width * 0.27f + sway;
            float topRight = topLeft + rect.width * 0.10f;
            float length = rect.height * (index % 2 == 0 ? 0.62f : 0.74f);
            float bottomY = rect.yMax - length;

            Color head = color * InvestigationV2Theme.GodRay;
            Color tail = head;
            tail.a = 0f;

            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = head;
            vertex.position = new Vector2(topLeft, rect.yMax); vh.AddVert(vertex);
            vertex.position = new Vector2(topRight, rect.yMax); vh.AddVert(vertex);
            vertex.color = tail;
            vertex.position = new Vector2(topRight + rect.width * 0.20f, bottomY); vh.AddVert(vertex);
            vertex.position = new Vector2(topLeft - rect.width * 0.06f, bottomY); vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private void AddCaustic(VertexHelper vh, Rect rect)
        {
            Color head = color * InvestigationV2Theme.Caustic;
            Color tail = head;
            tail.a = 0f;

            float baseY = rect.yMax - rect.height * 0.10f;
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;

            for (int segment = 0; segment <= CausticSegments; segment++)
            {
                float t = segment / (float)CausticSegments;
                float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
                float wave = Mathf.Sin(t * 9.2f + elapsed * 0.7f) * rect.height * 0.022f;

                vertex.color = head;
                vertex.position = new Vector2(x, rect.yMax); vh.AddVert(vertex);
                vertex.color = tail;
                vertex.position = new Vector2(x, baseY + wave); vh.AddVert(vertex);
            }

            for (int segment = 0; segment < CausticSegments; segment++)
            {
                int a = start + segment * 2;
                vh.AddTriangle(a, a + 2, a + 3);
                vh.AddTriangle(a, a + 3, a + 1);
            }
        }
    }
}
