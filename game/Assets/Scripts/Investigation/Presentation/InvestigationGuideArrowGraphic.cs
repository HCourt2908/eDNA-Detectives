using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation
{
    /// <summary>A pointer-transparent, gently pulsing arrow for the current action.</summary>
    public sealed class InvestigationGuideArrowGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            AddVertex(vh, r, .36f, .46f);
            AddVertex(vh, r, .64f, .46f);
            AddVertex(vh, r, .64f, .90f);
            AddVertex(vh, r, .36f, .90f);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
            AddVertex(vh, r, .10f, .50f);
            AddVertex(vh, r, .50f, .10f);
            AddVertex(vh, r, .90f, .50f);
            vh.AddTriangle(4, 5, 6);
        }

        private void AddVertex(VertexHelper vh, Rect r, float x, float y)
        {
            vh.AddVert(new Vector3(r.xMin + r.width * x, r.yMin + r.height * y, 0f), color, Vector2.zero);
        }
    }
}
