using UnityEngine;
using UnityEngine.UI;

namespace EDNA.Investigation.V2
{
    [DisallowMultipleComponent]
    public sealed class InvestigationV2ResponsiveGridLayout : LayoutGroup
    {
        [SerializeField, Min(1)] private int wideColumns = 4;
        [SerializeField, Min(1)] private int mediumColumns = 2;
        [SerializeField, Min(1)] private int narrowColumns = 1;
        [SerializeField, Min(20f)] private float cellHeight = 112f;
        [SerializeField, Min(0f)] private float spacing = 10f;
        [SerializeField, Min(200f)] private float wideBreakpoint = 900f;
        [SerializeField, Min(200f)] private float mediumBreakpoint = 520f;

        public void Configure(int wide, int medium, float height, float gap)
        {
            Configure(wide, medium, 1, height, gap);
        }

        public void Configure(int wide, int medium, int narrow, float height, float gap)
        {
            wideColumns = Mathf.Max(1, wide);
            mediumColumns = Mathf.Max(1, medium);
            narrowColumns = Mathf.Max(1, narrow);
            cellHeight = Mathf.Max(20f, height);
            spacing = Mathf.Max(0f, gap);
            SetDirty();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            SetLayoutInputForAxis(padding.horizontal, -1f, -1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            int columns = GetColumns(rectTransform.rect.width);
            int rows = Mathf.CeilToInt(rectChildren.Count / (float)columns);
            float height = padding.vertical + rows * cellHeight + Mathf.Max(0, rows - 1) * spacing;
            SetLayoutInputForAxis(height, height, height, 1);
        }

        public override void SetLayoutHorizontal() => LayoutChildren();
        public override void SetLayoutVertical() => LayoutChildren();

        private void LayoutChildren()
        {
            float width = rectTransform.rect.width;
            int columns = GetColumns(width);
            float cellWidth = (width - padding.horizontal - Mathf.Max(0, columns - 1) * spacing) / columns;
            for (int index = 0; index < rectChildren.Count; index++)
            {
                int row = index / columns;
                int column = index % columns;
                float x = padding.left + column * (cellWidth + spacing);
                float y = padding.top + row * (cellHeight + spacing);
                SetChildAlongAxis(rectChildren[index], 0, x, cellWidth);
                SetChildAlongAxis(rectChildren[index], 1, y, cellHeight);
            }
        }

        private int GetColumns(float width)
        {
            if (width >= wideBreakpoint) return wideColumns;
            if (width >= mediumBreakpoint) return mediumColumns;
            return narrowColumns;
        }
    }
}
