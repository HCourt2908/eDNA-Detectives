using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    /// <summary>
    /// Drifting particulate. Configure several instances at different sizes and
    /// speeds to build depth: the parallax between layers is what reads as
    /// "suspended in water" rather than "noise on a flat colour".
    ///
    /// At least one layer belongs in front of the scene. With particles only ever
    /// behind it, a rendered subject reads as a sticker on glass.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationMarineSnowGraphic : MaskableGraphic
    {
        private const int BlobSides = 8;
        private const float RebuildInterval = 1f / 30f;

        [SerializeField, Range(4, 80)] private int particleCount = 34;
        [SerializeField] private float minSize = 1f;
        [SerializeField] private float maxSize = 2.2f;
        [SerializeField, Min(1f)] private float driftSpeed = 8f;
        [SerializeField, Range(0f, 1f)] private float minAlpha = 0.10f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.17f;
        [SerializeField] private float swayAmplitude = 10f;
        [SerializeField] private uint seed = 0x9E3779B9u;

        private float offset;
        private float sinceRebuild;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        /// <param name="sizeRange">x = smallest particle, y = largest.</param>
        /// <param name="alphaRange">x = faintest particle, y = strongest.</param>
        public void Configure(int count, Vector2 sizeRange, float speed, Vector2 alphaRange, float sway, uint randomSeed)
        {
            particleCount = Mathf.Clamp(count, 4, 80);
            minSize = Mathf.Max(0.5f, sizeRange.x);
            maxSize = Mathf.Max(minSize, sizeRange.y);
            driftSpeed = Mathf.Max(0.1f, speed);
            minAlpha = Mathf.Clamp01(alphaRange.x);
            maxAlpha = Mathf.Clamp01(Mathf.Max(minAlpha, alphaRange.y));
            swayAmplitude = Mathf.Max(0f, sway);
            seed = randomSeed == 0u ? 0x9E3779B9u : randomSeed;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (InvestigationMotionSettings.ReducedMotion) return;
            offset = Mathf.Repeat(offset + Time.unscaledDeltaTime * driftSpeed, Mathf.Max(1f, rectTransform.rect.height));
            sinceRebuild += Time.unscaledDeltaTime;
            if (sinceRebuild < RebuildInterval) return;
            sinceRebuild = 0f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            uint state = seed;
            for (int index = 0; index < particleCount; index++)
            {
                state = state * 1664525u + 1013904223u;
                float x01 = (state & 0xFFFFu) / 65535f;
                state = state * 1664525u + 1013904223u;
                float y01 = ((state >> 8) & 0xFFFFu) / 65535f;
                state = state * 1664525u + 1013904223u;
                float size = Mathf.Lerp(minSize, maxSize, ((state >> 16) & 0xFFu) / 255f);
                state = state * 1664525u + 1013904223u;
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, ((state >> 12) & 0xFFu) / 255f);

                float y = Mathf.Repeat(y01 * rect.height + offset * (0.45f + (index % 5) * 0.1f), rect.height);
                float sway = Mathf.Sin(offset * 0.05f + index * 1.31f) * swayAmplitude;
                Vector2 center = new Vector2(rect.xMin + x01 * rect.width + sway, rect.yMin + y);
                AddBlob(vertexHelper, center, size, alpha);
            }
        }

        /// <summary>
        /// A soft blob rather than a hard quad: bright at the centre, fully
        /// transparent at the rim, so the particle reads as out-of-focus matter
        /// instead of a lit pixel.
        /// </summary>
        private void AddBlob(VertexHelper vh, Vector2 center, float size, float alpha)
        {
            Color core = color;
            core.a *= alpha;
            Color rim = core;
            rim.a = 0f;

            int centerIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = core;
            vertex.position = center;
            vh.AddVert(vertex);

            vertex.color = rim;
            for (int side = 0; side < BlobSides; side++)
            {
                float angle = side / (float)BlobSides * Mathf.PI * 2f;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
                vh.AddVert(vertex);
            }

            for (int side = 0; side < BlobSides; side++)
            {
                int current = centerIndex + 1 + side;
                int next = centerIndex + 1 + (side + 1) % BlobSides;
                vh.AddTriangle(centerIndex, current, next);
            }
        }
    }
}
