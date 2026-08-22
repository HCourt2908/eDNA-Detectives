using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2MarineSnowGraphic : MaskableGraphic
    {
        [SerializeField, Range(12, 80)] private int particleCount = 42;
        [SerializeField, Min(1f)] private float driftSpeed = 8f;
        private float offset;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private void Update()
        {
            if (InvestigationV2MotionSettings.ReducedMotion) return;
            offset = Mathf.Repeat(offset + Time.unscaledDeltaTime * driftSpeed, Mathf.Max(1f, rectTransform.rect.height));
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            uint seed = 0x9E3779B9u;
            for (int index = 0; index < particleCount; index++)
            {
                seed = seed * 1664525u + 1013904223u;
                float x01 = (seed & 0xFFFFu) / 65535f;
                seed = seed * 1664525u + 1013904223u;
                float y01 = ((seed >> 8) & 0xFFFFu) / 65535f;
                seed = seed * 1664525u + 1013904223u;
                float size = 1.1f + ((seed >> 16) & 0xFFu) / 255f * 2.4f;
                float y = Mathf.Repeat(y01 * rect.height + offset * (0.45f + (index % 5) * 0.1f), rect.height);
                Vector2 center = new Vector2(rect.xMin + x01 * rect.width, rect.yMin + y);
                AddQuad(vertexHelper, center, size, new Color32(200, 240, 248, (byte)(55 + (index % 4) * 22)));
            }
        }

        private static void AddQuad(VertexHelper vh, Vector2 center, float size, Color color)
        {
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center + new Vector2(-size, -size); vh.AddVert(vertex);
            vertex.position = center + new Vector2(-size, size); vh.AddVert(vertex);
            vertex.position = center + new Vector2(size, size); vh.AddVert(vertex);
            vertex.position = center + new Vector2(size, -size); vh.AddVert(vertex);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
