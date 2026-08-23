using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    /// <summary>
    /// Vertical depth gradient for the deep sea backdrop.
    ///
    /// Drawn once as vertex-coloured bands: no texture, no shader, no per-frame
    /// rebuild. Reduced motion never removes it, which is the point - the scene
    /// has to read as underwater without relying on animation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InvestigationV2WaterColumnGraphic : MaskableGraphic
    {
        // Normalised height up the column: 0 is the sea floor, 1 the surface.
        private static readonly float[] StopHeights = { 0f, 0.34f, 0.68f, 1f };

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        private static Color32 StopColor(int index)
        {
            switch (index)
            {
                case 0: return InvestigationV2Theme.WaterFloor;
                case 1: return InvestigationV2Theme.WaterLower;
                case 2: return InvestigationV2Theme.WaterUpper;
                default: return InvestigationV2Theme.WaterTop;
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            for (int band = 0; band < StopHeights.Length - 1; band++)
            {
                float lowY = rect.yMin + rect.height * StopHeights[band];
                float highY = rect.yMin + rect.height * StopHeights[band + 1];
                AddBand(vh, rect.xMin, rect.xMax, lowY, highY, StopColor(band), StopColor(band + 1));
            }
        }

        private static void AddBand(VertexHelper vh, float xMin, float xMax, float lowY, float highY, Color lowColor, Color highColor)
        {
            int start = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;

            vertex.color = lowColor;
            vertex.position = new Vector2(xMin, lowY); vh.AddVert(vertex);
            vertex.position = new Vector2(xMax, lowY); vh.AddVert(vertex);
            vertex.color = highColor;
            vertex.position = new Vector2(xMax, highY); vh.AddVert(vertex);
            vertex.position = new Vector2(xMin, highY); vh.AddVert(vertex);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
